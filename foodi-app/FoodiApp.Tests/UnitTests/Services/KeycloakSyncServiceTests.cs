using System.Net;
using FoodiApp.Models;
using FoodiApp.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace FoodiApp.Tests.UnitTests.Services;

public class KeycloakSyncServiceTests
{
    private readonly Mock<ILogger<KeycloakSyncService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;

    public KeycloakSyncServiceTests()
    {
        _loggerMock = new Mock<ILogger<KeycloakSyncService>>();
        _configurationMock = new Mock<IConfiguration>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);

        // Setup configuration
        _configurationMock.Setup(c => c["Keycloak:BaseUrl"]).Returns("http://keycloak:8080");
        _configurationMock.Setup(c => c["Keycloak:Realm"]).Returns("master");
        _configurationMock.Setup(c => c["Keycloak:AdminUsername"]).Returns("admin");
        _configurationMock.Setup(c => c["Keycloak:AdminPassword"]).Returns("admin123");
    }

    [Fact]
    public async Task SyncUserToKeycloakAsync_WithValidUser_ReturnsUserId()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hashedpassword"
        };

        var tokenResponse = "{\"access_token\":\"test-token\",\"token_type\":\"Bearer\"}";
        var userId = "abc-123-def-456";

        _httpMessageHandlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            // First call - get token
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(tokenResponse)
            })
            // Second call - create user
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Headers = { Location = new Uri($"http://keycloak:8080/admin/realms/master/users/{userId}") }
            });

        var service = new KeycloakSyncService(_httpClient, _configurationMock.Object, _loggerMock.Object);

        // Act
        var result = await service.SyncUserToKeycloakAsync(user, "password123");

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public async Task SyncUserToKeycloakAsync_WithInvalidCredentials_ReturnsNull()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized
            });

        var service = new KeycloakSyncService(_httpClient, _configurationMock.Object, _loggerMock.Object);

        // Act
        var result = await service.SyncUserToKeycloakAsync(user, "password123");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SyncUserToKeycloakAsync_WhenKeycloakIsDown_ReturnsNull()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var service = new KeycloakSyncService(_httpClient, _configurationMock.Object, _loggerMock.Object);

        // Act
        var result = await service.SyncUserToKeycloakAsync(user, "password123");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SyncUserToKeycloakAsync_WithDuplicateUser_ReturnsNull()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };

        var tokenResponse = "{\"access_token\":\"test-token\",\"token_type\":\"Bearer\"}";

        _httpMessageHandlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(tokenResponse)
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Conflict,
                Content = new StringContent("{\"errorMessage\":\"User exists\"}")
            });

        var service = new KeycloakSyncService(_httpClient, _configurationMock.Object, _loggerMock.Object);

        // Act
        var result = await service.SyncUserToKeycloakAsync(user, "password123");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateUserInKeycloakAsync_WithValidUser_ReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            KeycloakUserId = "abc-123-def-456",
            IsActive = true
        };

        var tokenResponse = "{\"access_token\":\"test-token\",\"token_type\":\"Bearer\"}";

        _httpMessageHandlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(tokenResponse)
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent
            });

        var service = new KeycloakSyncService(_httpClient, _configurationMock.Object, _loggerMock.Object);

        // Act
        var result = await service.UpdateUserInKeycloakAsync(user);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUserInKeycloakAsync_WithNoKeycloakId_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            KeycloakUserId = null
        };

        var service = new KeycloakSyncService(_httpClient, _configurationMock.Object, _loggerMock.Object);

        // Act
        var result = await service.UpdateUserInKeycloakAsync(user);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SetUserActiveStatusInKeycloakAsync_DeactivateUser_ReturnsTrue()
    {
        // Arrange
        var keycloakUserId = "abc-123-def-456";
        var tokenResponse = "{\"access_token\":\"test-token\",\"token_type\":\"Bearer\"}";

        _httpMessageHandlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(tokenResponse)
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent
            });

        var service = new KeycloakSyncService(_httpClient, _configurationMock.Object, _loggerMock.Object);

        // Act
        var result = await service.SetUserActiveStatusInKeycloakAsync(keycloakUserId, false);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SetUserActiveStatusInKeycloakAsync_ActivateUser_ReturnsTrue()
    {
        // Arrange
        var keycloakUserId = "abc-123-def-456";
        var tokenResponse = "{\"access_token\":\"test-token\",\"token_type\":\"Bearer\"}";

        _httpMessageHandlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(tokenResponse)
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent
            });

        var service = new KeycloakSyncService(_httpClient, _configurationMock.Object, _loggerMock.Object);

        // Act
        var result = await service.SetUserActiveStatusInKeycloakAsync(keycloakUserId, true);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUserPasswordInKeycloakAsync_WithValidPassword_ReturnsTrue()
    {
        // Arrange
        var keycloakUserId = "abc-123-def-456";
        var newPassword = "newPassword123";
        var tokenResponse = "{\"access_token\":\"test-token\",\"token_type\":\"Bearer\"}";

        _httpMessageHandlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(tokenResponse)
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent
            });

        var service = new KeycloakSyncService(_httpClient, _configurationMock.Object, _loggerMock.Object);

        // Act
        var result = await service.UpdateUserPasswordInKeycloakAsync(keycloakUserId, newPassword);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUserInKeycloakAsync_WhenKeycloakFails_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            KeycloakUserId = "abc-123-def-456"
        };

        var tokenResponse = "{\"access_token\":\"test-token\",\"token_type\":\"Bearer\"}";

        _httpMessageHandlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(tokenResponse)
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\":\"Invalid request\"}")
            });

        var service = new KeycloakSyncService(_httpClient, _configurationMock.Object, _loggerMock.Object);

        // Act
        var result = await service.UpdateUserInKeycloakAsync(user);

        // Assert
        result.Should().BeFalse();
    }
}

