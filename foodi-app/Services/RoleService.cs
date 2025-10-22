using FoodiApp.Models;

namespace FoodiApp.Services;

/// <summary>
/// Service to manage both Foodi internal roles and Keycloak roles
/// </summary>
public class RoleService
{
    private readonly KeycloakSyncService _keycloakSync;
    private readonly ILogger<RoleService> _logger;

    public RoleService(KeycloakSyncService keycloakSync, ILogger<RoleService> logger)
    {
        _keycloakSync = keycloakSync;
        _logger = logger;
    }

    /// <summary>
    /// Get all Foodi internal roles
    /// These are hardcoded and control application permissions
    /// </summary>
    public Task<List<RoleInfo>> GetFoodiRolesAsync()
    {
        var roles = new List<RoleInfo>
        {
            new RoleInfo 
            { 
                Name = "Agent", 
                Value = "Agent", 
                Description = "Standard user with basic features"
            },
            new RoleInfo 
            { 
                Name = "Lead", 
                Value = "Lead", 
                Description = "Team management and advanced features"
            },
            new RoleInfo 
            { 
                Name = "Admin", 
                Value = "Admin", 
                Description = "Administrative features and reporting"
            },
            new RoleInfo 
            { 
                Name = "Head", 
                Value = "Head", 
                Description = "Full system access and user management"
            }
        };

        return Task.FromResult(roles);
    }

    /// <summary>
    /// Get all Keycloak roles from Keycloak server
    /// These are dynamic and synced to Keycloak
    /// </summary>
    public async Task<List<RoleInfo>> GetKeycloakRolesAsync()
    {
        try
        {
            var roleNames = await _keycloakSync.GetAllKeycloakRolesAsync();
            
            return roleNames.Select(name => new RoleInfo
            {
                Name = name,
                Value = name,
                Description = $"Keycloak role: {name}"
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Keycloak roles");
            return new List<RoleInfo>();
        }
    }

    /// <summary>
    /// Get both Foodi and Keycloak roles for display
    /// </summary>
    public async Task<(List<RoleInfo> FoodiRoles, List<RoleInfo> KeycloakRoles)> GetAllRolesAsync()
    {
        var foodiRoles = await GetFoodiRolesAsync();
        var keycloakRoles = await GetKeycloakRolesAsync();
        
        return (foodiRoles, keycloakRoles);
    }

    /// <summary>
    /// Clear Keycloak roles cache
    /// </summary>
    public void ClearKeycloakRolesCache()
    {
        _keycloakSync.ClearRolesCache();
    }
}

/// <summary>
/// DTO for role information
/// </summary>
public class RoleInfo
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

