using System.ComponentModel.DataAnnotations;

namespace FoodiApp.Models;

public class UpdateProfileViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    public string LastName { get; set; } = string.Empty;
}

