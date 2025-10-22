using System.ComponentModel.DataAnnotations;

namespace FoodiApp.Models;

public class User
{
    public int Id { get; set; }
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastModifiedAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime? DeactivatedAt { get; set; }
    
    public bool SyncedToKeycloak { get; set; } = false;
    
    public string? KeycloakUserId { get; set; }
    
    /// <summary>
    /// Comma-separated list of user roles (e.g., "Agent", "Agent,Lead")
    /// </summary>
    public string Roles { get; set; } = "Agent";
    
    /// <summary>
    /// Check if user has a specific role
    /// </summary>
    public bool HasRole(Role role)
    {
        var userRoles = GetRoles();
        return userRoles.Contains(role);
    }
    
    /// <summary>
    /// Get list of user roles
    /// </summary>
    public List<Role> GetRoles()
    {
        if (string.IsNullOrWhiteSpace(Roles))
        {
            return new List<Role> { Role.Agent };
        }
        
        var roles = new List<Role>();
        var roleStrings = Roles.Split(',', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var roleString in roleStrings)
        {
            var role = RoleExtensions.ParseRole(roleString.Trim());
            if (role.HasValue)
            {
                roles.Add(role.Value);
            }
        }
        
        return roles.Any() ? roles : new List<Role> { Role.Agent };
    }
    
    /// <summary>
    /// Add a role to the user
    /// </summary>
    public void AddRole(Role role)
    {
        var currentRoles = GetRoles();
        if (!currentRoles.Contains(role))
        {
            currentRoles.Add(role);
            Roles = string.Join(",", currentRoles.Select(r => r.ToString()));
        }
    }
    
    /// <summary>
    /// Set user roles (replaces existing roles)
    /// </summary>
    public void SetRoles(List<Role> roles)
    {
        if (roles == null || !roles.Any())
        {
            Roles = "Agent";
        }
        else
        {
            Roles = string.Join(",", roles.Select(r => r.ToString()));
        }
    }
    
    /// <summary>
    /// Get role names as strings
    /// </summary>
    public List<string> GetRoleNames()
    {
        return GetRoles().Select(r => r.ToString()).ToList();
    }
}

