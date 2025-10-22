using FoodiApp.Data;
using FoodiApp.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FoodiApp.Tests.IntegrationTests;

public class DatabaseIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DatabaseIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Database_CanSaveAndRetrieveUser()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            FirstName = "Test",
            LastName = "User",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Assert
        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "testuser");
        savedUser.Should().NotBeNull();
        savedUser!.Email.Should().Be("test@example.com");
        savedUser.FirstName.Should().Be("Test");
    }

    [Fact]
    public async Task Database_HasSeededFoodItems()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Act
        var foodItems = await context.FoodItems.ToListAsync();

        // Assert
        foodItems.Should().NotBeEmpty();
        foodItems.Should().Contain(f => f.Name.Contains("Pizza"));
    }

    [Fact]
    public async Task Database_CanCreateOrder()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = new User
        {
            Username = "orderuser",
            Email = "order@example.com",
            PasswordHash = "hash",
            FirstName = "Order",
            LastName = "User"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var order = new Order
        {
            UserId = user.Id,
            Items = "[{\"id\":1,\"quantity\":2}]",
            TotalAmount = 25.98m,
            Status = "Pending"
        };

        // Act
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        // Assert
        var savedOrder = await context.Orders
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.UserId == user.Id);
        
        savedOrder.Should().NotBeNull();
        savedOrder!.TotalAmount.Should().Be(25.98m);
        savedOrder.User.Should().NotBeNull();
        savedOrder.User!.Username.Should().Be("orderuser");
    }

    [Fact]
    public async Task Database_EnforcesUniqueEmail()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        context.Users.Add(new User
        {
            Username = "user1",
            Email = "duplicate@example.com",
            PasswordHash = "hash1",
            FirstName = "User",
            LastName = "One"
        });
        await context.SaveChangesAsync();

        // Act
        var duplicateUser = new User
        {
            Username = "user2",
            Email = "duplicate@example.com",
            PasswordHash = "hash2",
            FirstName = "User",
            LastName = "Two"
        };

        // Assert
        var existingUser = await context.Users
            .FirstOrDefaultAsync(u => u.Email == "duplicate@example.com");
        existingUser.Should().NotBeNull();
        existingUser!.Username.Should().Be("user1");
    }
}

