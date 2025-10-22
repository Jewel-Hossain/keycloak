using System.ComponentModel.DataAnnotations;

namespace FoodiApp.Models;

public class RegisterViewModel
{
    [Required]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;
    
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;
    
    [Display(Name = "Foodi Role")]
    public string? SelectedRole { get; set; }
    
    [Display(Name = "Keycloak Roles")]
    public List<string>? SelectedKeycloakRoles { get; set; }
    
    /// <summary>
    /// Get the selected role as Role enum, defaulting to Agent
    /// </summary>
    public Role GetSelectedRole()
    {
        if (string.IsNullOrWhiteSpace(SelectedRole))
        {
            return Role.Agent;
        }
        
        return RoleExtensions.ParseRole(SelectedRole) ?? Role.Agent;
    }
    
    /// <summary>
    /// Get the selected Keycloak roles, or empty list if none selected
    /// </summary>
    public List<string> GetSelectedKeycloakRoles()
    {
        return SelectedKeycloakRoles ?? new List<string>();
    }
}

