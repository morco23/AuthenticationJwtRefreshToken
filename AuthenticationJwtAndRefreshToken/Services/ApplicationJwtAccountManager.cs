using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using MorCohen.Data;
using MorCohen.Helpers;
using MorCohen.Interfaces;
using MorCohen.Models;
using MorCohen.Models.AuthenticateModels;
using static MorCohen.Models.AuthenticateModels.IdentityTypes;

namespace MorCohen.Services
{
    public class ApplicationJwtAccountManager : IApplicationAccountManager
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger _logger;
        public readonly string _secret;
        public readonly byte[] _secretBytes;
        private readonly ApplicationRepositories _applicationRepositories;

        public ApplicationJwtAccountManager(AppSettings appSettings,
                                                UserManager<ApplicationUser> userManager,
                                                ILogger<ApplicationJwtAccountManager> logger,
                                                ApplicationRepositories applicationRepositories)
        {
            _userManager = userManager;
            _logger = logger;
            _secretBytes = Encoding.ASCII.GetBytes(appSettings.Secret);
            _applicationRepositories = applicationRepositories;
        }

        private async Task<string> GenerateToken(ApplicationUser user)
        {
            string role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? nameof(Role.User);

            var tokenHandler = new JwtSecurityTokenHandler(); // Designed for creating and validating JWT.
            var tokenDescriptor = new SecurityTokenDescriptor // Contains the information which used to build the JWT.
            {
                Claims = new Dictionary<string, object>()
                {
                    [ClaimTypes.NameIdentifier] = user.Id,
                    [ClaimTypes.Email] = user.Email,
                    [ClaimTypes.Role] = role
                },
                Expires = DateTime.Now.AddMinutes(30),

                // Represents the cryptographic key and security algorithms that are used to generate a digital signature.
                // Next, when the token will be validated we don't dont have to mention which algorithems was used thanks to the tokens header which contains this information.
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(_secretBytes), SecurityAlgorithms.HmacSha256Signature),
            };

            var tokenCreated = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(tokenCreated);
        }

        private static RefreshToken CreateRefreshToken(AdditionalLoginInfo additionalLoginInfo, string userId)
        {
            return new RefreshToken()
            {
                Token = Guid.NewGuid().ToString("N"),
                Expires = DateTime.Now.AddDays(14),
                Created = DateTime.Now,
                CreatedByIp = additionalLoginInfo.IpAddress,
                ApplicationUserId = userId
            };
        }

        /// <summary>
        /// In case the user sends the refresh token and it was not expired but revoked by another user, it's considered a security issue.
        /// The refresh token is exclusive and should be always active or expired.
        /// </summary>
        private void HandleRefreshTokenWasNotExpiredButRevoked(AdditionalLoginInfo additionalLoginInfo, string userId)
        {
            _applicationRepositories.RefreshTokensRepository.GetByApplicationUserId(userId, false, false)
                                                            .ForEach(token =>
                                                            {
                                                                token.Revoked = DateTime.Now;
                                                                token.RevokedByIp = additionalLoginInfo.IpAddress;
                                                            });
        }

        public static TokenValidationParameters GetTokenValidationParams(string secret, byte[] secretBytes = null)
        {
            secretBytes ??= Encoding.ASCII.GetBytes(secret);
            return new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(secretBytes),
                ValidateIssuer = false,
                ValidateAudience = false,
                // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                ClockSkew = TimeSpan.Zero,
            };
        }

        public async Task CreateAccountAsync(CreateUserRequest request)
        {
            _logger.LogInformation("Started.");

            var newAccount = new ApplicationUser()
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            IdentityResult identityResult = await _userManager.CreateAsync(newAccount, request.Password);
            if (!identityResult.Succeeded)
            {
                throw new AppException(string.Join(", ", identityResult.Errors.Select(error => error.Description)));
            }

            _logger.LogInformation("User created.");

            bool isFirstAccount = _applicationRepositories.UsersRepository.Count() == 1;
            string roleName = isFirstAccount ? nameof(Role.Admin) : nameof(Role.User);

            await _userManager.AddToRoleAsync(newAccount, roleName);

            _logger.LogInformation($"Assigned {roleName} to username {newAccount.UserName}.");
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request, AdditionalLoginInfo additionalLoginInfo)
        {
            _logger.LogInformation("Started.");

            ApplicationUser user = _applicationRepositories.UsersRepository.GetByEmail(request.Email, true);

            if (user == null)
            {
                _logger.LogInformation($"The user with the email {request.Email} has not been found.");
                return new LoginResponse(false);
            }

            if (!await _userManager.CheckPasswordAsync(user, request.Password))
            {
                _logger.LogInformation($"The user with the email {request.Email} has entered wrong password.");
                return new LoginResponse(false);
            }

            string jwtToken = await GenerateToken(user);

            RefreshToken refreshToken = CreateRefreshToken(additionalLoginInfo, user.Id);
            _applicationRepositories.RefreshTokensRepository.Add(refreshToken);

            return new LoginResponse(true, jwtToken, refreshToken.Token);
        }

        public async Task<LoginResponse> RefreshAsync(string userId, AdditionalLoginInfo additionalLoginInfo)
        {
            _logger.LogInformation("Started.");

            RefreshToken curRefreshToken = _applicationRepositories.RefreshTokensRepository.GetByTokenValue(additionalLoginInfo.RefreshToken, true);
            ApplicationUser user = curRefreshToken?.ApplicationUser;

            if (curRefreshToken == null)
            {
                _logger.LogInformation("Refresh token was not exist.");
                return new LoginResponse(false);
            }

            if (user == null || user.Id != userId)
            {
                _logger.LogInformation("Refresh token is not belong to the user.");
                return new LoginResponse(false);
            }

            if (curRefreshToken.IsExpired)
            {
                _logger.LogInformation("Refresh token was expired.");
                return new LoginResponse(false);
            }

            if (curRefreshToken.Revoked != null)
            {
                _logger.LogWarning("Refresh token was already revoked.");
                HandleRefreshTokenWasNotExpiredButRevoked(additionalLoginInfo, user.Id);
                return new LoginResponse(false);
            }

            curRefreshToken.Revoked = DateTime.Now;
            curRefreshToken.RevokedByIp = additionalLoginInfo.IpAddress;

            string jwtToken = await GenerateToken(user);

            RefreshToken newRefreshToken = CreateRefreshToken(additionalLoginInfo, user.Id);
            _applicationRepositories.RefreshTokensRepository.Add(newRefreshToken);

            return new LoginResponse(true, jwtToken, newRefreshToken.Token);
        }
    }
}
