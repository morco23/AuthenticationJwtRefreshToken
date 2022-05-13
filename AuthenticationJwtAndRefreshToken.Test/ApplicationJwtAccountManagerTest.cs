using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MorCohen.Data;
using MorCohen.Interfaces;
using MorCohen.Models;
using MorCohen.Models.AuthenticateModels;
using MorCohen.Services;

namespace MorCohen.Test
{
    //  Test for ApplicationJwtAccountManagerTest
    //  Packages used:
    //  Moq - For creating predefined object for tests
    //  TestTools.UnitTesting

    [TestClass]
    public class ApplicationJwtAccountManagerTest
    {
        private readonly ApplicationJwtAccountManager _accountManager;
        private readonly List<ApplicationUser> _users = new List<ApplicationUser>();
        private readonly List<RefreshToken> _refreshTokens = new List<RefreshToken>();
        private readonly Dictionary<string, string> _userToRole = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _userToPassword = new Dictionary<string, string>();
        private const string TEST_EMAIL = "Test@test.com";
        private const string TEST_PASSWORD = "123456";

        public ApplicationJwtAccountManagerTest()
        {
            IUsersRepository userRepo = GetUserRepoMock(_users);
            IRefreshTokensRepository refreshTokensRepository = GetRefreshTokensRepoMock(_users, _refreshTokens);
            ApplicationRepositories repos = new ApplicationRepositories(userRepo, refreshTokensRepository);
            Mock<ILogger<ApplicationJwtAccountManager>> loggerMock = new Mock<ILogger<ApplicationJwtAccountManager>>();
            UserManager<ApplicationUser> userManagerMock = GetUserManagerMock(_users, _userToRole, _userToPassword);

            _accountManager = new ApplicationJwtAccountManager(new AppSettings() { Secret = "SECRET123456789SECRET123456789" },
                                                                userManagerMock,
                                                                loggerMock.Object,
                                                                repos);
        }

        private static IUsersRepository GetUserRepoMock(List<ApplicationUser> users)
        {
            Mock<IUsersRepository> userRepoMock = new Mock<IUsersRepository>();
            userRepoMock.Setup(repo => repo.GetById(It.IsAny<string>(), It.IsAny<bool>()))
                        .Returns((string userId, bool includeRefreshTokens) =>
                        {
                            return users.Where(user => user.Id == userId).FirstOrDefault();
                        });
            userRepoMock.Setup(repo => repo.GetByEmail(It.IsAny<string>(), It.IsAny<bool>()))
                        .Returns((string emailAddress, bool includeRefreshTokens) =>
                        {
                            return users.Where(user => user.Email == emailAddress).FirstOrDefault();
                        });
            userRepoMock.Setup(repo => repo.Count())
                        .Returns(() => users.Count);

            return userRepoMock.Object;
        }

        private static IRefreshTokensRepository GetRefreshTokensRepoMock(  List<ApplicationUser> users,
                                                        List<RefreshToken> refreshTokens)
        {
            Mock<IRefreshTokensRepository> refreshTokensMock = new Mock<IRefreshTokensRepository>();
            refreshTokensMock.Setup(repo => repo.GetByTokenValue(It.IsAny<string>(), It.IsAny<bool>()))
                                .Returns((string tokenValue, bool includeApplicationUsers) =>
                                {
                                    return refreshTokens.Where(token => token.Token == tokenValue).FirstOrDefault();
                                });
            refreshTokensMock.Setup(repo => repo.GetByApplicationUserId(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                                .Returns((string userId, bool isExpired, bool isRevoked) =>
                                {
                                    var queryable = refreshTokens.Where(token => token.ApplicationUserId == userId);
                                    queryable = isExpired ? queryable.Where(token => token.Expires <= DateTime.Now) : queryable.Where(token => token.Expires > DateTime.Now);
                                    queryable = isRevoked ? queryable.Where(token => token.Revoked != null) : queryable.Where(token => token.Revoked == null);
                                    return queryable.ToList();
                                });
            refreshTokensMock.Setup(repo => repo.Add(It.IsAny<RefreshToken>()))
                                .Callback((RefreshToken token) =>
                                {
                                    token.ApplicationUser = users.FirstOrDefault(user => user.Id == token.ApplicationUserId);
                                    token.ApplicationUser.RefreshTokens ??= new List<RefreshToken>();
                                    token.ApplicationUser.RefreshTokens.Add(token);
                                    refreshTokens.Add(token);
                                });
            return refreshTokensMock.Object;
        }

        private static UserManager<ApplicationUser> GetUserManagerMock(List<ApplicationUser> users,
                                                                Dictionary<string, string> userToRole,
                                                                Dictionary<string, string> userToPassword)
        {
            Mock<UserManager<ApplicationUser>> userManagerMock = new Mock<UserManager<ApplicationUser>>(new Mock<IUserStore<ApplicationUser>>().Object, null, null, null, null, null, null, null, null);
            userManagerMock.Setup(mock => mock.GetRolesAsync(It.IsAny<ApplicationUser>()))
                            .ReturnsAsync((ApplicationUser userToGetHisRole) =>
                            {
                                if (userToRole.TryGetValue(userToGetHisRole.Id, out string roleName))
                                {
                                    return new List<string>() { roleName };
                                }
                                return null;
                            });
            userManagerMock.Setup(mock => mock.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                            .ReturnsAsync((ApplicationUser newUser, string password) =>
                            {
                                newUser.Id = Guid.NewGuid().ToString();
                                users.Add(newUser);
                                userToPassword.Add(newUser.Id, password);
                                return IdentityResult.Success;
                            });
            userManagerMock.Setup(mock => mock.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                            .Callback((ApplicationUser newUser, string role) =>
                            {
                                userToRole.TryAdd(newUser.Id, role);
                            });
            userManagerMock.Setup(mock => mock.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                            .ReturnsAsync((ApplicationUser newUser, string password) =>
                            {
                                return userToPassword[newUser.Id] == password;
                            });
            return userManagerMock.Object;
        }

        private async Task InsertTestAccount(string userName = null)
        {
            await _accountManager.CreateAccountAsync(new CreateUserRequest()
            {
                FirstName = "Test",
                LastName = "Test",
                Email = userName ?? TEST_EMAIL,
                Password = TEST_PASSWORD,
                ConfirmPassword = "123456"
            });
        }

        [TestMethod]
        public async Task CreateAccountAsync_AddFirstUser_UserAddedAndWithAdminRole()
        {
            // Arrange
            await InsertTestAccount();

            // Assert
            Assert.AreEqual(1, _users.Count);
            Assert.AreEqual("Admin", _userToRole[_users.First().Id]);
        }

        [TestMethod]
        public async Task CreateAccountAsync_AddAndThenLogin_LoginSuccess()
        {
            // Arrange
            await InsertTestAccount();

            // Act
            var res = await _accountManager.LoginAsync(new LoginRequest() { Email = TEST_EMAIL, Password = TEST_PASSWORD }, new AdditionalLoginInfo());

            // Assert
            Assert.IsTrue(res.Success);
            Assert.IsNotNull(res.RefreshToken);
            Assert.IsNotNull(res.Token);
        }

        [TestMethod]
        public async Task LoginAsync_UsingIncorrectPassword_LoginFaild()
        {
            // Act
            var res = await _accountManager.LoginAsync(new LoginRequest() { Email = TEST_EMAIL, Password = TEST_PASSWORD }, new AdditionalLoginInfo());

            // Assert
            Assert.IsFalse(res.Success);
        }

        [TestMethod]
        public async Task CreateAccountAsync_RegisterTwoUsers_SecondOneGetRoleUser()
        {
            // Arrange
            await InsertTestAccount();
            const string userEmail = "second@test.com";
            await InsertTestAccount(userEmail);

            var newUserId = _users.Where(user => user.Email == userEmail).First().Id;

            // Assert
            Assert.AreEqual(nameof(IdentityTypes.Role.User), _userToRole[newUserId]);
        }

        [TestMethod]
        public async Task LoginAsync_LoginWithIncorrectPassword_LoginFailed()
        {
            // Arrange
            await InsertTestAccount();

            // Act
            var res = await _accountManager.LoginAsync(new LoginRequest() { Email = TEST_EMAIL, Password = "wrongpass" }, new AdditionalLoginInfo());

            // Assert
            Assert.IsFalse(res.Success);
        }

        [TestMethod]
        public async Task RefreshAsync_ActiveTokenUsed_Success()
        {
            // Arrange
            await InsertTestAccount();

            // Act
            var loginRes = await _accountManager.LoginAsync(new LoginRequest() { Email = TEST_EMAIL, Password = "wrongpass" }, new AdditionalLoginInfo());
            var refreshTokenRespose = await _accountManager.RefreshAsync(_users[0].Id, new AdditionalLoginInfo() { RefreshToken = loginRes.RefreshToken });
    
            // Assert
            Assert.IsFalse(refreshTokenRespose.Success);
        }

        [TestMethod]
        public async Task RefreshAsync_SecureSameRefreshTokenWasUsed_ReturnsFailedAndAllRefreshTokensAreRevoked()
        {
            // Arrange
            await InsertTestAccount();

            // Act
            var loginRes = await _accountManager.LoginAsync(new LoginRequest() { Email = TEST_EMAIL, Password = TEST_PASSWORD }, new AdditionalLoginInfo());
            var refreshTokenRespose = await _accountManager.RefreshAsync(_users[0].Id, new AdditionalLoginInfo() { RefreshToken = loginRes.RefreshToken });
            int activeRefreshTokens = _refreshTokens.Where(token => token.IsActive).Count();
            var refreshTokenRespose2 = await _accountManager.RefreshAsync(_users[0].Id, new AdditionalLoginInfo() { RefreshToken = loginRes.RefreshToken });
            int activeRefreshTokensAfterUsingSameToken = _refreshTokens.Where(token => token.IsActive).Count();

            // Assert
            Assert.IsTrue(refreshTokenRespose.Success);
            Assert.AreEqual(1, activeRefreshTokens);
            Assert.IsFalse(refreshTokenRespose2.Success);
            Assert.AreEqual(0, activeRefreshTokensAfterUsingSameToken);
        }
    }
}
