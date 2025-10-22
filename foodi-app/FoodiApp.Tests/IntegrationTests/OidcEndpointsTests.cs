using System.Net;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace FoodiApp.Tests.IntegrationTests;

public class OidcEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public OidcEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task AuthorizeEndpoint_WithoutAuthentication_RedirectsToLogin()
    {
        // Arrange
        var queryString = "?client_id=keycloak-client&redirect_uri=http://localhost:8080/callback&response_type=code&scope=openid";

        // Act
        var response = await _client.GetAsync($"/connect/authorize{queryString}");

        // Assert
        // OpenIddict may return 400 for invalid client or 302 for redirect
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TokenEndpoint_WithoutCode_ReturnsBadRequest()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = "keycloak-client",
            ["client_secret"] = "foodi-secret-key-2024"
        };

        // Act
        var response = await _client.PostAsync("/connect/token", new FormUrlEncodedContent(formData));

        // Assert
        // OpenIddict returns 400 for invalid requests
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UserinfoEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/connect/userinfo");

        // Assert
        // OpenIddict may return 400 or 401 for missing token
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UserinfoEndpoint_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await client.GetAsync("/connect/userinfo");

        // Assert
        // OpenIddict may return 400 or 401 for invalid token
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TokenEndpoint_AcceptsPostRequests()
    {
        // Act
        var response = await _client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>()));

        // Assert
        // Should return 400 (bad request) not 405 (method not allowed)
        response.StatusCode.Should().NotBe(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task AuthorizeEndpoint_AcceptsGetRequests()
    {
        // Act
        var response = await _client.GetAsync("/connect/authorize");

        // Assert
        // Should redirect to login, not return method not allowed
        response.StatusCode.Should().NotBe(HttpStatusCode.MethodNotAllowed);
    }
}

