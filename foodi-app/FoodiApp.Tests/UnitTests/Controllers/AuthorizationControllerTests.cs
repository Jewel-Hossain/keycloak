using System.Security.Claims;
using FluentAssertions;
using FoodiApp.Controllers;
using FoodiApp.Data;
using FoodiApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenIddict.Abstractions;
using Xunit;

namespace FoodiApp.Tests.UnitTests.Controllers;

public class AuthorizationControllerTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IOpenIddictApplicationManager> _applicationManagerMock;
    private readonly Mock<IOpenIddictAuthorizationManager> _authorizationManagerMock;
    private readonly Mock<IOpenIddictScopeManager> _scopeManagerMock;
    private readonly AuthorizationController _controller;

    public AuthorizationControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _applicationManagerMock = new Mock<IOpenIddictApplicationManager>();
        _authorizationManagerMock = new Mock<IOpenIddictAuthorizationManager>();
        _scopeManagerMock = new Mock<IOpenIddictScopeManager>();

        _controller = new AuthorizationController(
            _context,
            _applicationManagerMock.Object,
            _authorizationManagerMock.Object,
            _scopeManagerMock.Object);
    }

    [Fact]
    public async Task Userinfo_WithValidUser_ReturnsUserClaims()
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
            new Claim(OpenIddict.Abstractions.OpenIddictConstants.Claims.Subject, "1")
        };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = await _controller.Userinfo();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var claims_dict = okResult.Value.Should().BeAssignableTo<Dictionary<string, object>>().Subject;
        claims_dict[OpenIddict.Abstractions.OpenIddictConstants.Claims.Email].Should().Be("test@example.com");
        claims_dict[OpenIddict.Abstractions.OpenIddictConstants.Claims.Name].Should().Be("testuser");
    }

    [Fact]
    public async Task Userinfo_WithInvalidUserId_ReturnsBadRequest()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(OpenIddict.Abstractions.OpenIddictConstants.Claims.Subject, "999")
        };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = await _controller.Userinfo();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Userinfo_WithoutSubjectClaim_ReturnsBadRequest()
    {
        // Arrange
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = await _controller.Userinfo();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}

