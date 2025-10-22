using FoodiApp.Controllers;
using FoodiApp.Data;
using FoodiApp.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace FoodiApp.Tests.UnitTests.Controllers;

public class HomeControllerTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<HomeController>> _loggerMock;
    private readonly HomeController _controller;

    public HomeControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // Seed test data
        _context.FoodItems.AddRange(
            new FoodItem { Id = 1, Name = "Pizza", Description = "Delicious pizza", Price = 12.99m, IsAvailable = true },
            new FoodItem { Id = 2, Name = "Burger", Description = "Juicy burger", Price = 9.99m, IsAvailable = true },
            new FoodItem { Id = 3, Name = "Salad", Description = "Fresh salad", Price = 7.99m, IsAvailable = false }
        );
        _context.SaveChanges();

        _loggerMock = new Mock<ILogger<HomeController>>();
        _controller = new HomeController(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task Index_ReturnsViewWithAvailableFoodItems()
    {
        // Act
        var result = await _controller.Index();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result as ViewResult;
        var model = viewResult!.Model as IEnumerable<FoodItem>;
        model.Should().NotBeNull();
        model!.Count().Should().Be(2); // Only available items
        model!.All(f => f.IsAvailable).Should().BeTrue();
    }

    [Fact]
    public void Menu_ReturnsViewWithAvailableFoodItems()
    {
        // Act
        var result = _controller.Menu();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result as ViewResult;
        var model = viewResult!.Model as IEnumerable<FoodItem>;
        model.Should().NotBeNull();
        model!.Count().Should().Be(2);
    }

    [Fact]
    public void MyOrders_WithAuthenticatedUser_ReturnsViewWithOrders()
    {
        // Arrange
        var userId = 1;
        _context.Users.Add(new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            FirstName = "Test",
            LastName = "User"
        });
        _context.Orders.AddRange(
            new Order { Id = 1, UserId = userId, Items = "[]", TotalAmount = 25.00m, Status = "Pending" },
            new Order { Id = 2, UserId = userId, Items = "[]", TotalAmount = 15.00m, Status = "Delivered" }
        );
        _context.SaveChanges();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = _controller.MyOrders();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result as ViewResult;
        var model = viewResult!.Model as IEnumerable<Order>;
        model.Should().NotBeNull();
        model!.Count().Should().Be(2);
    }

    [Fact]
    public void MyOrders_WithoutAuthenticatedUser_RedirectsToLogin()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = _controller.MyOrders();

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult!.ActionName.Should().Be("Login");
        redirectResult.ControllerName.Should().Be("Account");
    }

    [Fact]
    public void Privacy_ReturnsView()
    {
        // Act
        var result = _controller.Privacy();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public void Error_ReturnsView()
    {
        // Act
        var result = _controller.Error();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }
}

