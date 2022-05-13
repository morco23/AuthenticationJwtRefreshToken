using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MorCohen.Controllers;
using MorCohen.Interfaces;
using System;
using System.Threading.Tasks;
using MorCohen.Models.AuthenticateModels;

namespace MorCohen.Controllers
{
    public class AccountController: ApiBaseController
    {
        private readonly IApplicationAccountManager _appAccountsManager;

        private static void SetRefreshTokenCookieToResponse(HttpResponse response, string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7),
                SameSite = SameSiteMode.None,
                Secure = true
            };
            response.Cookies.Append("refreshToken", token, cookieOptions);
        }

        private AdditionalLoginInfo GetAdditionalLoginInfo()
        {
            string ipAddress = Request.Headers.ContainsKey("X-Forwarded-For") ?    Request.Headers["X-Forwarded-For"] : 
                                                                            HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
            AdditionalLoginInfo additionalLoginInfo = new()
            {
                IpAddress = ipAddress
            };
            Request.Cookies.TryGetValue("refreshToken", out string refreshToken);
            additionalLoginInfo.RefreshToken = refreshToken;
            return additionalLoginInfo;
        }

        public AccountController(IApplicationAccountManager appAccountsManager, IHttpContextAccessor httpContextAccessor) : base (httpContextAccessor)
        {
            _appAccountsManager = appAccountsManager;
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] CreateUserRequest request)
        {
            await _appAccountsManager.CreateAccountAsync(request);
            return Ok("Account has been created.");
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            AdditionalLoginInfo additionalLoginInfo = GetAdditionalLoginInfo();

            LoginResponse loginResponse = await _appAccountsManager.LoginAsync(request, additionalLoginInfo);

            if (!loginResponse.Success)
            {
                return Unauthorized();
            }

            SetRefreshTokenCookieToResponse(Response, loginResponse.RefreshToken);

            return Ok(loginResponse.Token);
        }

        [HttpGet]
        public async Task<IActionResult> Refresh()
        {
            AdditionalLoginInfo additionalLoginInfo = GetAdditionalLoginInfo();

            LoginResponse loginResponse = await _appAccountsManager.RefreshAsync(UserId, additionalLoginInfo);

            if (!loginResponse.Success)
            {
                return Unauthorized();
            }

            SetRefreshTokenCookieToResponse(Response, loginResponse.RefreshToken);

            return Ok(loginResponse.Token);
        }

        [Authorize]
        [HttpGet]
        public IActionResult GetUsersResponse()
        {
            return Ok($"GetAllUsersResponse OK. UserId: {UserId}");
        }

        [Authorize(Roles = nameof(IdentityTypes.Role.Admin))]
        [HttpGet]
        public IActionResult GetAdminResponse()
        {
            return Ok($"GetAdminResponse OK. UserId: {UserId}");
        }
    }
}
