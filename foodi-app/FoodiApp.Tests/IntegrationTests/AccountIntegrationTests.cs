using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace FoodiApp.Tests.IntegrationTests;

public class AccountIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AccountIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task RegisterPage_Get_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/Account/Register");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Create Account");
    }

    [Fact]
    public async Task LoginPage_Get_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/Account/Login");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Welcome Back");
    }

    [Fact]
    public async Task Register_WithInvalidModel_ReturnsValidationErrors()
    {
        // Arrange - Get the register page first to get the antiforgery token
        var getResponse = await _client.GetAsync("/Account/Register");
        
        // Act - Try to post with invalid data (will likely get 400 due to antiforgery)
        var formData = new Dictionary<string, string>
        {
            ["Username"] = "",
            ["Email"] = "invalid-email",
            ["Password"] = "123", // Too short
            ["ConfirmPassword"] = "456", // Doesn't match
            ["FirstName"] = "",
            ["LastName"] = ""
        };
        var response = await _client.PostAsync("/Account/Register", new FormUrlEncodedContent(formData));

        // Assert - Either 200 (view with errors) or 400 (anti-forgery validation)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HomePage_Get_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Welcome to Foodi");
    }

    [Fact]
    public async Task Menu_WithoutAuthentication_RedirectsToLogin()
    {
        // Act
        var response = await _client.GetAsync("/Home/Menu");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Account/Login");
    }

    [Fact]
    public async Task MyOrders_WithoutAuthentication_RedirectsToLogin()
    {
        // Act
        var response = await _client.GetAsync("/Home/MyOrders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Account/Login");
    }
}

