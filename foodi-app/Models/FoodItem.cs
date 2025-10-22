using System.ComponentModel.DataAnnotations;

namespace FoodiApp.Models;

public class FoodItem
{
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Range(0.01, 999.99)]
    public decimal Price { get; set; }
    
    public string ImageUrl { get; set; } = string.Empty;
    
    public bool IsAvailable { get; set; } = true;
}

