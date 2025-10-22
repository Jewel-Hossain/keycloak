using System.ComponentModel.DataAnnotations;

namespace FoodiApp.Models;

public class Order
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    public User? User { get; set; }
    
    [Required]
    public string Items { get; set; } = string.Empty; // JSON string of ordered items
    
    public decimal TotalAmount { get; set; }
    
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Delivered
}

