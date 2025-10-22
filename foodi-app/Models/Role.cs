using System.ComponentModel.DataAnnotations;

namespace FoodiApp.Models;

/// <summary>
/// Enum representing user roles in the Foodi application
/// </summary>
public enum Role
{
    /// <summary>
    /// Agent - Basic user with standard features (default role)
    /// </summary>
    [Display(Name = "Agent", Description = "Standard user with basic features")]
    Agent = 0,

    /// <summary>
    /// Lead - Team management and advanced features
    /// </summary>
    [Display(Name = "Lead", Description = "Team management and advanced features")]
    Lead = 1,

    /// <summary>
    /// Admin - Administrative features and reporting
    /// </summary>
    [Display(Name = "Administrator", Description = "Administrative features and reporting")]
    Admin = 2,

    /// <summary>
    /// Head - Full system access and user management
    /// </summary>
    [Display(Name = "Head", Description = "Full system access and user management")]
    Head = 3
}

/// <summary>
/// Extension methods for Role enum
/// </summary>
public static class RoleExtensions
{
    /// <summary>
    /// Get display name for a role
    /// </summary>
    public static string GetDisplayName(this Role role)
    {
        var displayAttribute = role.GetType()
            .GetField(role.ToString())?
            .GetCustomAttributes(typeof(DisplayAttribute), false)
            .FirstOrDefault() as DisplayAttribute;

        return displayAttribute?.Name ?? role.ToString();
    }

    /// <summary>
    /// Get description for a role
    /// </summary>
    public static string GetDescription(this Role role)
    {
        var displayAttribute = role.GetType()
            .GetField(role.ToString())?
            .GetCustomAttributes(typeof(DisplayAttribute), false)
            .FirstOrDefault() as DisplayAttribute;

        return displayAttribute?.Description ?? string.Empty;
    }

    /// <summary>
    /// Parse role from string
    /// </summary>
    public static Role? ParseRole(string roleString)
    {
        if (Enum.TryParse<Role>(roleString, true, out var role))
        {
            return role;
        }
        return null;
    }
}


