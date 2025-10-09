using System;
using System.Threading.Tasks;
using DnsClient.Protocol;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using UsersService.Configurations;
using UsersService.Controllers;
using UsersService.Models;
using UsersService.Models.DTOs;
using UsersService.Services;
using Xunit;

namespace UsersService.Tests
{
    public class UsersController_Register_Tests
    {
        private static UsersController BuildController(out Mock<IUsersService> usersSvcMock, out Mock<IAuthService> authSvcMock)
        {
            usersSvcMock = new Mock<IUsersService>(MockBehavior.Strict);
            authSvcMock = new Mock<IAuthService>(MockBehavior.Loose);

            var jwt = Options.Create(new JWTSettings
            {
                SecretKey = "914eac5e360a1532aaa30b41997adbcb1ddaa49d50cf6ff69ff01a9fe1621f53",
                Issuer = "CICD_Proj.Auth",
                Audience = "CICD_Proj.Users",
                ExpiryMinutes = 60
            });

            return new UsersController(usersSvcMock.Object, authSvcMock.Object, jwt);
        }

        [Fact]
        public async Task Register_Should_Return_201With_User()
        {
            // Arrange
            var controller = BuildController(out var usersSvcMock, out _);

            var request = new RegisterRequest
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test.user@example.com",
                Password = "Password123!",
                Role = "User",
                BirthDate = new DateTime(1990, 1, 1)
            };

            var createdUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                HashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = Role.User
            };

            usersSvcMock.Setup(s => s.CreateUserAsync(It.IsAny<RegisterRequest>()))
                        .ReturnsAsync(createdUser);

            // Act
            var result = await controller.Register(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            createdResult.StatusCode.Should().Be(201);
            createdResult.ActionName.Should().Be(nameof(UsersController.Register));
            createdResult.Value.Should().BeEquivalentTo(createdUser, opt => opt.ExcludingMissingMembers());

            usersSvcMock.Verify(s => s.CreateUserAsync(It.Is<RegisterRequest>(r => r.Email == request.Email)), Times.Once);
        }

        [Fact]
        public async Task Register_Should_Return_409_When_Email_Already_Exists()
        {
            // Arrange
            var userService = new Mock<IUsersService>();
            var authService = new Mock<IAuthService>();
            var jwtOptions = Options.Create(new JWTSettings
            {
                SecretKey = "914eac5e360a1532aaa30b41997adbcb1ddaa49d50cf6ff69ff01a9fe1621f53",
                Issuer = "CICD_Proj.Auth",
                Audience = "CICD_Proj.Users",
                ExpiryMinutes = 60
            });
            var controller = new UsersController(userService.Object, authService.Object, jwtOptions);

            var request = new RegisterRequest
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test.user@example.com",
                Password = "Password123!",
                Role = "User",
                BirthDate = new DateTime(1990, 1, 1)
            };

            userService.Setup(s => s.CreateUserAsync(It.IsAny<RegisterRequest>()))
                       .ThrowsAsync(new InvalidOperationException("Email already exists."));

            // Act
            var result = await controller.Register(request);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            conflictResult.StatusCode.Should().Be(409);
            conflictResult.Value.Should().NotBeNull();
            conflictResult.Value.Should().BeEquivalentTo(new { Error = "Email already exists." }, opt => opt.ExcludingMissingMembers());

            userService.Verify(s =>
                s.CreateUserAsync(It.Is<RegisterRequest>(r => r.Email == request.Email)),
                Times.Once);
        }

        [Fact]
        public async Task Register_Should_Return_400_When_Service_Returns_Null()
        {
            // Arrange
            var controller = BuildController(out var usersSvcMock, out _);

            var request = new RegisterRequest
            {
                FirstName = "Test",
                LastName = "User",
                Email = "invalid-email-format", // Invalid email format
                Password = "Password123!",
                Role = "User",
                BirthDate = new DateTime(1990, 1, 1)
            };

            usersSvcMock.Setup(s => s.CreateUserAsync(It.IsAny<RegisterRequest>()))
                        .ReturnsAsync((User?)null); // Simulate service returning null

            // Act
            var result = await controller.Register(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            badRequestResult.StatusCode.Should().Be(400);
            usersSvcMock.Verify(s => s.CreateUserAsync(It.Is<RegisterRequest>(r => r.Email == request.Email)), Times.Once);
        }

        [Fact]
        public async Task Register_Should_Return_400BadRequest_On_ArgumentNullException()
        {
            // Arrange
            var controller = BuildController(out var usersSvcMock, out _);

            var request = new RegisterRequest
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test.user@example.com",
                Password = "Password123!",
                Role = "User",
                BirthDate = new DateTime(1990, 1, 1)
            };

            usersSvcMock.Setup(s => s.CreateUserAsync(It.IsAny<RegisterRequest>()))
                        .ThrowsAsync(new ArgumentNullException("request", "Request cannot be null"));

            // Act
            var result = await controller.Register(request!);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            badRequestResult.StatusCode.Should().Be(400);
            badRequestResult.Value.Should().NotBeNull();

            usersSvcMock.Verify(s => s.CreateUserAsync(It.Is<RegisterRequest>(r => r.Email == request.Email)), Times.Once);
        }

        [Fact]
        public async Task Register_Should_Return_409Conflict_On_Unexpected_Exception()
        {
            // Arrange
            var controller = BuildController(out var usersSvcMock, out _);
            var request = new RegisterRequest
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test.user@example.com",
                Password = "Password123!",
                Role = "User",
                BirthDate = new DateTime(1990, 1, 1)
            };

            usersSvcMock.Setup(s => s.CreateUserAsync(It.IsAny<RegisterRequest>()))
                        .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await controller.Register(request);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            conflictResult.StatusCode.Should().Be(500);
            conflictResult.Value.Should().NotBeNull();
            usersSvcMock.Verify(s => s.CreateUserAsync(It.Is<RegisterRequest>(r => r.Email == request.Email)), Times.Once);
        }

        [Fact]
        public async Task Register_Should_Return_409Conflict_When_Email_Already_Exists()
        {
            // Arrange
            var controller = BuildController(out var usersSvcMock, out _);
            var request = new RegisterRequest
            {
                FirstName = "Test",
                LastName = "User",
                Email = "existing@example.com",
                Password = "Password123!",
                Role = "User",
                BirthDate = new DateTime(1990, 1, 1)
            };

            usersSvcMock.Setup(s => s.CreateUserAsync(It.IsAny<RegisterRequest>()))
                        .ThrowsAsync(new DuplicateEmailException(request.Email));

            // Act
            var result = await controller.Register(request);

            // Assert
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            conflict.StatusCode.Should().Be(409);
            conflict.Value.Should().NotBeNull();
            usersSvcMock.Verify(s => s.CreateUserAsync(It.Is<RegisterRequest>(r => r.Email == request.Email)), Times.Once);
        }
    }
}