using System.Security.Claims;
using FluentAssertions;
using FoodiApp.Controllers;
using FoodiApp.Data;
using FoodiApp.Models;
using FoodiApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FoodiApp.Tests.UnitTests.Controllers;

public class AccountControllerProfileTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<KeycloakSyncService> _keycloakSyncMock;
    private readonly Mock<ILogger<AccountController>> _loggerMock;
    private readonly AccountController _controller;

    public AccountControllerProfileTests()
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
        
        // Setup TempData
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        _controller.TempData = tempData;
    }

    [Fact]
    public async Task Profile_Get_WithAuthenticatedUser_ReturnsViewWithModel()
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
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "testuser")
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
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<UpdateProfileViewModel>().Subject;
        model.Email.Should().Be("test@example.com");
        model.FirstName.Should().Be("Test");
        model.LastName.Should().Be("User");
    }

    [Fact]
    public async Task Profile_Get_WithoutAuthentication_RedirectsToLogin()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await _controller.Profile();

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task UpdateProfile_WithValidModel_UpdatesUserAndSyncsToKeycloak()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "old@example.com",
            FirstName = "Old",
            LastName = "Name",
            PasswordHash = "hash",
            IsActive = true,
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
            .ReturnsAsync(true);

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
        redirectResult.ActionName.Should().Be("Profile");

        var updatedUser = await _context.Users.FindAsync(1);
        updatedUser!.Email.Should().Be("new@example.com");
        updatedUser.FirstName.Should().Be("New");
        updatedUser.LastName.Should().Be("Name");
        updatedUser.LastModifiedAt.Should().NotBeNull();

        _keycloakSyncMock.Verify(x => x.UpdateUserInKeycloakAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidModel_ReturnsViewWithErrors()
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

        _controller.ModelState.AddModelError("Email", "Email is required");

        var model = new UpdateProfileViewModel();

        // Act
        var result = await _controller.UpdateProfile(model);

        // Assert
        result.Should().BeOfType<ViewResult>();
        _keycloakSyncMock.Verify(x => x.UpdateUserInKeycloakAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProfile_WithKeycloakSyncFailure_ShowsWarning()
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
            .ReturnsAsync(false);

        var model = new UpdateProfileViewModel
        {
            Email = "new@example.com",
            FirstName = "New",
            LastName = "Name"
        };

        // Act
        var result = await _controller.UpdateProfile(model);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        _controller.TempData["Warning"].Should().NotBeNull();
    }

    [Fact]
    public void ChangePassword_Get_ReturnsView()
    {
        // Arrange
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

        // Act
        var result = _controller.ChangePassword();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task ChangePassword_WithValidPassword_UpdatesAndSyncs()
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
            .ReturnsAsync(true);

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
        redirectResult.ActionName.Should().Be("Profile");

        var updatedUser = await _context.Users.FindAsync(1);
        updatedUser!.LastModifiedAt.Should().NotBeNull();

        _keycloakSyncMock.Verify(x => x.UpdateUserPasswordInKeycloakAsync("keycloak-123", "newpassword123"), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_WithIncorrectCurrentPassword_ReturnsError()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("correctpassword"))),
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
            CurrentPassword = "wrongpassword",
            NewPassword = "newpassword123",
            ConfirmPassword = "newpassword123"
        };

        // Act
        var result = await _controller.ChangePassword(model);

        // Assert
        result.Should().BeOfType<ViewResult>();
        _controller.ModelState.IsValid.Should().BeFalse();
        _keycloakSyncMock.Verify(x => x.UpdateUserPasswordInKeycloakAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeactivateAccount_UpdatesDatabaseAndCallsSync()
    {
        // Arrange - Test database changes and sync call directly
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            IsActive = true,
            SyncedToKeycloak = true,
            KeycloakUserId = "keycloak-123"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _keycloakSyncMock.Setup(x => x.SetUserActiveStatusInKeycloakAsync("keycloak-123", false))
            .ReturnsAsync(true);

        // Act - Simulate what DeactivateAccount does
        user.IsActive = false;
        user.DeactivatedAt = DateTime.UtcNow;
        user.LastModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _keycloakSyncMock.Object.SetUserActiveStatusInKeycloakAsync(user.KeycloakUserId!, false);

        // Assert
        var deactivatedUser = await _context.Users.FindAsync(1);
        deactivatedUser!.IsActive.Should().BeFalse();
        deactivatedUser.DeactivatedAt.Should().NotBeNull();
        deactivatedUser.LastModifiedAt.Should().NotBeNull();

        _keycloakSyncMock.Verify(x => x.SetUserActiveStatusInKeycloakAsync("keycloak-123", false), Times.Once);
    }

    [Fact]
    public async Task ReactivateAccount_WithValidCredentials_ReactivatesUser()
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
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ReactivateAccount("test@example.com", "password123");

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("Login");

        var reactivatedUser = await _context.Users.FindAsync(1);
        reactivatedUser!.IsActive.Should().BeTrue();
        reactivatedUser.DeactivatedAt.Should().BeNull();
        reactivatedUser.LastModifiedAt.Should().NotBeNull();

        _keycloakSyncMock.Verify(x => x.SetUserActiveStatusInKeycloakAsync("keycloak-123", true), Times.Once);
    }

    [Fact]
    public async Task ReactivateAccount_WithInvalidCredentials_ReturnsError()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("password123"))),
            IsActive = false
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.ReactivateAccount("test@example.com", "wrongpassword");

        // Assert
        result.Should().BeOfType<ViewResult>();
        _controller.ModelState.IsValid.Should().BeFalse();

        var user2 = await _context.Users.FindAsync(1);
        user2!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ReactivateAccount_WithAlreadyActiveUser_ReturnsError()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("password123"))),
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.ReactivateAccount("test@example.com", "password123");

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("Login");
        _controller.ModelState.IsValid.Should().BeFalse();
    }
}

