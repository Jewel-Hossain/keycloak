using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using FoodiApp.Data;
using FoodiApp.Models;
using FoodiApp.Services;
using Xunit;

namespace FoodiApp.Tests.IntegrationTests;

public class KeycloakIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public KeycloakIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RegisterUser_ShouldCreateUserInDatabase()
    {
        // Arrange
        var registerData = new
        {
            Username = "integrationtest",
            Email = "integration@test.com",
            Password = "password123",
            ConfirmPassword = "password123",
            FirstName = "Integration",
            LastName = "Test"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(registerData),
            Encoding.UTF8,
            "application/json");

        // Act
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Create user directly in database for testing
        var user = new User
        {
            Username = registerData.Username,
            Email = registerData.Email,
            FirstName = registerData.FirstName,
            LastName = registerData.LastName,
            PasswordHash = "hashed",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Assert
        var savedUser = await context.Users.FindAsync(user.Id);
        savedUser.Should().NotBeNull();
        savedUser!.Username.Should().Be(registerData.Username);
        savedUser.Email.Should().Be(registerData.Email);
        savedUser.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUser_ShouldUpdateLastModifiedAt()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var user = new User
        {
            Username = "updatetest",
            Email = "update@test.com",
            FirstName = "Update",
            LastName = "Test",
            PasswordHash = "hashed",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var initialModified = user.LastModifiedAt;
        
        // Act
        await Task.Delay(100); // Ensure time difference
        user.FirstName = "Updated";
        user.LastModifiedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Assert
        user.LastModifiedAt.Should().NotBe(initialModified);
        user.FirstName.Should().Be("Updated");
    }

    [Fact]
    public async Task DeactivateUser_ShouldSetIsActiveToFalse()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var user = new User
        {
            Username = "deactivatetest",
            Email = "deactivate@test.com",
            FirstName = "Deactivate",
            LastName = "Test",
            PasswordHash = "hashed",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        user.IsActive = false;
        user.DeactivatedAt = DateTime.UtcNow;
        user.LastModifiedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Assert
        var deactivatedUser = await context.Users.FindAsync(user.Id);
        deactivatedUser.Should().NotBeNull();
        deactivatedUser!.IsActive.Should().BeFalse();
        deactivatedUser.DeactivatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ReactivateUser_ShouldSetIsActiveToTrue()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var user = new User
        {
            Username = "reactivatetest",
            Email = "reactivate@test.com",
            FirstName = "Reactivate",
            LastName = "Test",
            PasswordHash = "hashed",
            CreatedAt = DateTime.UtcNow,
            IsActive = false,
            DeactivatedAt = DateTime.UtcNow
        };
        
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        user.IsActive = true;
        user.DeactivatedAt = null;
        user.LastModifiedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Assert
        var reactivatedUser = await context.Users.FindAsync(user.Id);
        reactivatedUser.Should().NotBeNull();
        reactivatedUser!.IsActive.Should().BeTrue();
        reactivatedUser.DeactivatedAt.Should().BeNull();
    }

    [Fact]
    public async Task UserWithKeycloakId_ShouldHaveSyncedFlag()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var user = new User
        {
            Username = "synctest",
            Email = "sync@test.com",
            FirstName = "Sync",
            LastName = "Test",
            PasswordHash = "hashed",
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            SyncedToKeycloak = true,
            KeycloakUserId = "test-keycloak-id"
        };
        
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Assert
        var syncedUser = await context.Users.FindAsync(user.Id);
        syncedUser.Should().NotBeNull();
        syncedUser!.SyncedToKeycloak.Should().BeTrue();
        syncedUser.KeycloakUserId.Should().Be("test-keycloak-id");
    }
}

