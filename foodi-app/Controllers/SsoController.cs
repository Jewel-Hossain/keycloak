using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodiApp.Controllers;

[Authorize]
public class SsoController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SsoController> _logger;

    public SsoController(IConfiguration configuration, ILogger<SsoController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Redirects authenticated Foodi user to Keycloak for automatic SSO login
    /// </summary>
    public IActionResult GoToKeycloak()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return RedirectToAction("Login", "Account");
        }

        var username = User.Identity?.Name ?? "Unknown";
        _logger.LogInformation($"User {username} initiating SSO to Keycloak");

        // Redirect to Keycloak's account console with hint to use foodi provider
        // This initiates a proper authentication flow through the foodi identity provider
        var keycloakBaseUrl = _configuration["Keycloak:BaseUrl"]?.Replace("keycloak", "localhost") 
                              ?? "http://localhost:8080";
        var realm = _configuration["Keycloak:Realm"] ?? "master";
        
        // Use the account console which will trigger authentication via foodi provider
        var keycloakUrl = $"{keycloakBaseUrl}/realms/{realm}/protocol/openid-connect/auth?" +
                         $"client_id=account&" +
                         $"redirect_uri={Uri.EscapeDataString($"{keycloakBaseUrl}/realms/{realm}/account")}&" +
                         $"response_type=code&" +
                         $"scope=openid&" +
                         $"kc_idp_hint=foodi";
        
        _logger.LogInformation($"Redirecting to Keycloak with IdP hint: {keycloakUrl}");

        return Redirect(keycloakUrl);
    }
}

