using FoodiApp.Controllers;
using FoodiApp.Data;
using FoodiApp.Models;
using FoodiApp.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FoodiApp.Tests.UnitTests.Controllers;

public class AccountControllerTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<KeycloakSyncService> _keycloakSyncMock;
    private readonly Mock<ILogger<AccountController>> _loggerMock;
    private readonly AccountController _controller;

    public AccountControllerTests()
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
        
        // Setup HttpContext with all required MVC services
        var httpContext = new DefaultHttpContext();
        var serviceCollection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        serviceCollection.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie();
        serviceCollection.AddLogging();
        serviceCollection.AddRouting();
        serviceCollection.AddSingleton<Microsoft.AspNetCore.Mvc.Routing.IUrlHelperFactory, 
            Microsoft.AspNetCore.Mvc.Routing.UrlHelperFactory>();
        serviceCollection.AddSingleton<Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor, 
            Microsoft.AspNetCore.Mvc.Infrastructure.ActionContextAccessor>();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        httpContext.RequestServices = serviceProvider;
        
        // Setup ActionContext for URL generation
        var actionContext = new Microsoft.AspNetCore.Mvc.ActionContext(
            httpContext,
            new Microsoft.AspNetCore.Routing.RouteData(),
            new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor());
        
        _controller.ControllerContext = new ControllerContext(actionContext);
        
        // Setup TempData with a mock
        _controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            httpContext,
            Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
    }

    [Fact]
    public void Register_Get_ReturnsView()
    {
        // Act
        var result = _controller.Register();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Register_WithValidModel_CreatesUserAndRedirects()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "password123",
            ConfirmPassword = "password123",
            FirstName = "New",
            LastName = "User"
        };

        _keycloakSyncMock
            .Setup(s => s.SyncUserToKeycloakAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync("keycloak-user-id");

        // Act
        var result = await _controller.Register(model);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult!.ActionName.Should().Be("Login");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == "newuser");
        user.Should().NotBeNull();
        user!.Email.Should().Be("newuser@example.com");
        user.SyncedToKeycloak.Should().BeTrue();
        user.KeycloakUserId.Should().Be("keycloak-user-id");
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsViewWithError()
    {
        // Arrange
        _context.Users.Add(new User
        {
            Username = "existing",
            Email = "existing@example.com",
            PasswordHash = "hash",
            FirstName = "Existing",
            LastName = "User"
        });
        await _context.SaveChangesAsync();

        var model = new RegisterViewModel
        {
            Username = "newuser",
            Email = "existing@example.com",
            Password = "password123",
            ConfirmPassword = "password123",
            FirstName = "New",
            LastName = "User"
        };

        // Act
        var result = await _controller.Register(model);

        // Assert
        result.Should().BeOfType<ViewResult>();
        _controller.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Register_WhenKeycloakSyncFails_StillCreatesLocalUser()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "password123",
            ConfirmPassword = "password123",
            FirstName = "New",
            LastName = "User"
        };

        _keycloakSyncMock
            .Setup(s => s.SyncUserToKeycloakAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _controller.Register(model);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == "newuser");
        user.Should().NotBeNull();
        user!.SyncedToKeycloak.Should().BeFalse();
        user.KeycloakUserId.Should().BeNull();
    }

    [Fact]
    public void Login_Get_ReturnsView()
    {
        // Act
        var result = _controller.Login();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Login_WithValidCredentials_RedirectsToHome()
    {
        // Arrange
        var password = "password123";
        var passwordHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(password)));

        _context.Users.Add(new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = passwordHash,
            FirstName = "Test",
            LastName = "User"
        });
        await _context.SaveChangesAsync();

        var model = new LoginViewModel
        {
            UsernameOrEmail = "testuser",
            Password = password
        };

        // Act
        var result = await _controller.Login(model);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult!.ControllerName.Should().Be("Home");
        redirectResult.ActionName.Should().Be("Index");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsViewWithError()
    {
        // Arrange
        _context.Users.Add(new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "wronghash",
            FirstName = "Test",
            LastName = "User"
        });
        await _context.SaveChangesAsync();

        var model = new LoginViewModel
        {
            UsernameOrEmail = "testuser",
            Password = "wrongpassword"
        };

        // Act
        var result = await _controller.Login(model);

        // Assert
        result.Should().BeOfType<ViewResult>();
        _controller.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsViewWithError()
    {
        // Arrange
        var model = new LoginViewModel
        {
            UsernameOrEmail = "nonexistent",
            Password = "password123"
        };

        // Act
        var result = await _controller.Login(model);

        // Assert
        result.Should().BeOfType<ViewResult>();
        _controller.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Login_WithEmail_AuthenticatesSuccessfully()
    {
        // Arrange
        var password = "password123";
        var passwordHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(password)));

        _context.Users.Add(new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = passwordHash,
            FirstName = "Test",
            LastName = "User"
        });
        await _context.SaveChangesAsync();

        var model = new LoginViewModel
        {
            UsernameOrEmail = "test@example.com",
            Password = password
        };

        // Act
        var result = await _controller.Login(model);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
    }
}

