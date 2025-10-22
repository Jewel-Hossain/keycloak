using Microsoft.EntityFrameworkCore;
using FoodiApp.Models;

namespace FoodiApp.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<FoodItem> FoodItems { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed some initial food items
        modelBuilder.Entity<FoodItem>().HasData(
            new FoodItem { Id = 1, Name = "Margherita Pizza", Description = "Classic tomato and mozzarella", Price = 12.99m, ImageUrl = "/images/pizza.jpg" },
            new FoodItem { Id = 2, Name = "Cheeseburger", Description = "Juicy beef patty with cheese", Price = 9.99m, ImageUrl = "/images/burger.jpg" },
            new FoodItem { Id = 3, Name = "Caesar Salad", Description = "Fresh romaine with parmesan", Price = 7.99m, ImageUrl = "/images/salad.jpg" },
            new FoodItem { Id = 4, Name = "Spaghetti Carbonara", Description = "Creamy pasta with bacon", Price = 13.99m, ImageUrl = "/images/pasta.jpg" },
            new FoodItem { Id = 5, Name = "Chicken Wings", Description = "Crispy wings with hot sauce", Price = 8.99m, ImageUrl = "/images/wings.jpg" },
            new FoodItem { Id = 6, Name = "Fish Tacos", Description = "Fresh fish with salsa", Price = 11.99m, ImageUrl = "/images/tacos.jpg" }
        );
    }
}

