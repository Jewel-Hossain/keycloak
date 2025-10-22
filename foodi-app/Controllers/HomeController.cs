using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodiApp.Data;
using FoodiApp.Models;

namespace FoodiApp.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var foodItems = await _context.FoodItems.Where(f => f.IsAvailable).ToListAsync();
        return View(foodItems);
    }

    [Authorize]
    public IActionResult Menu()
    {
        var foodItems = _context.FoodItems.Where(f => f.IsAvailable).ToList();
        return View(foodItems);
    }

    [Authorize]
    public IActionResult MyOrders()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var userId = int.Parse(userIdClaim.Value);
        var orders = _context.Orders
            .Include(o => o.User)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToList();

        return View(orders);
    }

    [Authorize(Policy = "LeadOrAbove")]
    public async Task<IActionResult> Reports()
    {
        var totalUsers = await _context.Users.CountAsync();
        var totalOrders = await _context.Orders.CountAsync();
        var totalFoodItems = await _context.FoodItems.CountAsync();
        var activeUsers = await _context.Users.CountAsync(u => u.IsActive);

        ViewData["TotalUsers"] = totalUsers;
        ViewData["TotalOrders"] = totalOrders;
        ViewData["TotalFoodItems"] = totalFoodItems;
        ViewData["ActiveUsers"] = activeUsers;

        // Get recent orders
        var recentOrders = await _context.Orders
            .Include(o => o.User)
            .OrderByDescending(o => o.OrderDate)
            .Take(10)
            .ToListAsync();

        return View(recentOrders);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Error()
    {
        return View();
    }
}

