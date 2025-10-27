using Moq;
using Microsoft.Extensions.Options;
using UsersService.Controllers;
using UsersService.Services;
using UsersService.Configurations;
using UsersService.Models.DTOs;
using UsersService.Models;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;


namespace UsersService.Tests
{
    public class UsersController_Login_Tests
    {
        private UsersController BuildController(
            out Mock<IUsersService> usersSvcMock,
            out Mock<IAuthService> authSvcMock,
            JWTSettings? jwtSettingsOverride = null)
        {
            usersSvcMock = new Mock<IUsersService>(MockBehavior.Strict);
            authSvcMock = new Mock<IAuthService>(MockBehavior.Strict);

            var jwtSettings = jwtSettingsOverride ?? new JWTSettings
            {
                SecretKey = "914eac5e360a1532aaa30b41997adbcb1ddaa49d50cf6ff69ff01a9fe1621f53",
                Issuer = "CICD_Proj.Auth",
                Audience = "CICD_Proj.Users",
                ExpiryMinutes = 60
            };
            var opts = new Mock<IOptions<JWTSettings>>();
            opts.Setup(o => o.Value).Returns(jwtSettings);

            return new UsersController(usersSvcMock.Object, authSvcMock.Object, opts.Object);
        }

        [Fact]
        public async Task Login_Should_Return_401Unauthorized_When_Credentials_Invalid()
        {
            // Arrange
            var controller = BuildController(out var usersSvcMock, out var _);
            var req = new LoginRequest { Email = "nobody@ex.com", Password = "bad" };

            usersSvcMock
                .Setup(s => s.LoginUserAsync(req))
                .ReturnsAsync((User?)null);

            // Act
            var result = await controller.Login(req);

            // Assert
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            unauthorized.StatusCode.Should().Be(401);
            unauthorized.Value.Should().NotBeNull();
            usersSvcMock.Verify(s => s.LoginUserAsync(It.Is<LoginRequest>(r => r.Email == req.Email)), Times.Once);
        }
    }
}