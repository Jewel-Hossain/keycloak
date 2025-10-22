using System.Security.Claims;
using FluentAssertions;
using FoodiApp.Controllers;
using FoodiApp.Data;
using FoodiApp.Models;
using FoodiApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FoodiApp.Tests.UnitTests.Controllers;

public class AccountControllerEdgeCasesTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<KeycloakSyncService> _keycloakSyncMock;
    private readonly Mock<ILogger<AccountController>> _loggerMock;
    private readonly AccountController _controller;

    public AccountControllerEdgeCasesTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _keycloakSyncMock = new Mock<KeycloakSyncService>(
            Mock.Of<HttpClient>(),
            Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>(),
            Mock.Of<ILogger<KeycloakSyncService>>());

        _loggerMock = new Mock<ILogger<AccountController>>();

        _controller = new AccountController(_context, _keycloakSyncMock.Object, _loggerMock.Object);
        
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        _controller.TempData = tempData;
    }

    [Fact]
    public async Task UpdateProfile_WithoutSyncedKeycloakUser_ShowsSuccessWithoutSync()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash",
            SyncedToKeycloak = false,
            KeycloakUserId = null
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var model = new UpdateProfileViewModel
        {
            Email = "new@example.com",
            FirstName = "New",
            LastName = "Name"
        };

        // Act
        var result = await _controller.UpdateProfile(model);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        _controller.TempData["Success"].Should().Be("Profile updated successfully!");
        _keycloakSyncMock.Verify(x => x.UpdateUserInKeycloakAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task ChangePassword_WithoutSyncedKeycloakUser_ShowsSuccessWithoutSync()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("oldpassword"))),
            SyncedToKeycloak = false,
            KeycloakUserId = null,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var model = new ChangePasswordViewModel
        {
            CurrentPassword = "oldpassword",
            NewPassword = "newpassword123",
            ConfirmPassword = "newpassword123"
        };

        // Act
        var result = await _controller.ChangePassword(model);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        _controller.TempData["Success"].Should().Be("Password changed successfully!");
        _keycloakSyncMock.Verify(x => x.UpdateUserPasswordInKeycloakAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ChangePassword_WithInvalidModel_ReturnsViewWithErrors()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        _controller.ModelState.AddModelError("NewPassword", "Password is required");

        var model = new ChangePasswordViewModel();

        // Act
        var result = await _controller.ChangePassword(model);

        // Assert
        result.Should().BeOfType<ViewResult>();
        _keycloakSyncMock.Verify(x => x.UpdateUserPasswordInKeycloakAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProfile_WhenKeycloakSyncThrowsException_ShowsWarning()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash",
            SyncedToKeycloak = true,
            KeycloakUserId = "keycloak-123"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        _keycloakSyncMock.Setup(x => x.UpdateUserInKeycloakAsync(It.IsAny<User>()))
            .ThrowsAsync(new HttpRequestException("Keycloak is down"));

        var model = new UpdateProfileViewModel
        {
            Email = "new@example.com",
            FirstName = "New",
            LastName = "Name"
        };

        // Act
        var result = await _controller.UpdateProfile(model);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        _controller.TempData["Warning"].Should().NotBeNull();
    }

    [Fact]
    public async Task ChangePassword_WhenKeycloakSyncThrowsException_ShowsWarning()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("oldpassword"))),
            SyncedToKeycloak = true,
            KeycloakUserId = "keycloak-123",
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        _keycloakSyncMock.Setup(x => x.UpdateUserPasswordInKeycloakAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Keycloak is down"));

        var model = new ChangePasswordViewModel
        {
            CurrentPassword = "oldpassword",
            NewPassword = "newpassword123",
            ConfirmPassword = "newpassword123"
        };

        // Act
        var result = await _controller.ChangePassword(model);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        _controller.TempData["Warning"].Should().NotBeNull();
    }

    [Fact]
    public async Task Profile_WithNonExistentUser_RedirectsToLogin()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "999")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = await _controller.Profile();

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task ChangePassword_WithNonExistentUser_RedirectsToLogin()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "999")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var model = new ChangePasswordViewModel
        {
            CurrentPassword = "oldpassword",
            NewPassword = "newpassword123",
            ConfirmPassword = "newpassword123"
        };

        // Act
        var result = await _controller.ChangePassword(model);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task UpdateProfile_WithNonExistentUser_RedirectsToLogin()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "999")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var model = new UpdateProfileViewModel
        {
            Email = "new@example.com",
            FirstName = "New",
            LastName = "Name"
        };

        // Act
        var result = await _controller.UpdateProfile(model);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task ReactivateAccount_WithNonExistentEmail_ReturnsError()
    {
        // Act
        var result = await _controller.ReactivateAccount("nonexistent@example.com", "password123");

        // Assert
        result.Should().BeOfType<ViewResult>();
        _controller.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePassword_WhenSyncFails_StillUpdatesLocalPassword()
    {
        // Arrange
        var oldPasswordHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("oldpassword")));
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = oldPasswordHash,
            SyncedToKeycloak = true,
            KeycloakUserId = "keycloak-123",
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        _keycloakSyncMock.Setup(x => x.UpdateUserPasswordInKeycloakAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var model = new ChangePasswordViewModel
        {
            CurrentPassword = "oldpassword",
            NewPassword = "newpassword123",
            ConfirmPassword = "newpassword123"
        };

        // Act
        var result = await _controller.ChangePassword(model);

        // Assert
        var updatedUser = await _context.Users.FindAsync(1);
        updatedUser!.PasswordHash.Should().NotBe(oldPasswordHash); // Password should be changed
        updatedUser.LastModifiedAt.Should().NotBeNull();
        _controller.TempData["Warning"].Should().NotBeNull();
    }

    [Fact]
    public async Task Register_WithKeycloakException_StillCreatesLocalUser()
    {
        // Arrange
        _keycloakSyncMock.Setup(x => x.SyncUserToKeycloakAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Keycloak connection failed"));

        var model = new RegisterViewModel
        {
            Username = "newuser",
            Email = "new@example.com",
            Password = "password123",
            ConfirmPassword = "password123",
            FirstName = "New",
            LastName = "User"
        };

        // Act
        var result = await _controller.Register(model);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        var createdUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "newuser");
        createdUser.Should().NotBeNull();
        createdUser!.SyncedToKeycloak.Should().BeFalse();
        _controller.TempData["Warning"].Should().NotBeNull();
    }

    [Fact]
    public async Task ReactivateAccount_WhenSyncFails_StillReactivatesLocally()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("password123"))),
            IsActive = false,
            DeactivatedAt = DateTime.UtcNow,
            SyncedToKeycloak = true,
            KeycloakUserId = "keycloak-123"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _keycloakSyncMock.Setup(x => x.SetUserActiveStatusInKeycloakAsync("keycloak-123", true))
            .ThrowsAsync(new Exception("Keycloak error"));

        // Act
        var result = await _controller.ReactivateAccount("test@example.com", "password123");

        // Assert
        var reactivatedUser = await _context.Users.FindAsync(1);
        reactivatedUser!.IsActive.Should().BeTrue();
        reactivatedUser.DeactivatedAt.Should().BeNull();
    }
}

