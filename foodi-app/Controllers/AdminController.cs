using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodiApp.Data;
using FoodiApp.Models;
using FoodiApp.Services;

namespace FoodiApp.Controllers;

[Authorize(Policy = "AdminOrAbove")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly KeycloakSyncService _keycloakSync;
    private readonly RoleService _roleService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ApplicationDbContext context,
        KeycloakSyncService keycloakSync,
        RoleService roleService,
        ILogger<AdminController> logger)
    {
        _context = context;
        _keycloakSync = keycloakSync;
        _roleService = roleService;
        _logger = logger;
    }

    /// <summary>
    /// Admin dashboard - shows system overview
    /// </summary>
    public async Task<IActionResult> Dashboard()
    {
        var totalUsers = await _context.Users.CountAsync();
        var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
        var syncedUsers = await _context.Users.CountAsync(u => u.SyncedToKeycloak);

        ViewData["TotalUsers"] = totalUsers;
        ViewData["ActiveUsers"] = activeUsers;
        ViewData["SyncedUsers"] = syncedUsers;
        ViewData["InactiveUsers"] = totalUsers - activeUsers;

        return View();
    }

    /// <summary>
    /// User management - list all users
    /// </summary>
    public async Task<IActionResult> Users()
    {
        var users = await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        return View(users);
    }

    /// <summary>
    /// View user details
    /// </summary>
    public async Task<IActionResult> UserDetails(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction(nameof(Users));
        }

        // Fetch available roles for display
        var (foodiRoles, keycloakRoles) = await _roleService.GetAllRolesAsync();
        ViewBag.FoodiRoles = foodiRoles;
        ViewBag.AvailableKeycloakRoles = keycloakRoles;

        return View(user);
    }

    /// <summary>
    /// Toggle user active status (Head only)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "HeadOnly")]
    public async Task<IActionResult> ToggleUserStatus(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction(nameof(Users));
        }

        user.IsActive = !user.IsActive;
        user.DeactivatedAt = user.IsActive ? null : DateTime.UtcNow;
        user.LastModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Sync to Keycloak
        if (!string.IsNullOrEmpty(user.KeycloakUserId))
        {
            try
            {
                await _keycloakSync.SetUserActiveStatusInKeycloakAsync(user.KeycloakUserId, user.IsActive);
                _logger.LogInformation($"User {user.Username} status toggled to {(user.IsActive ? "active" : "inactive")}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing user status to Keycloak");
            }
        }

        TempData["Success"] = $"User {user.Username} is now {(user.IsActive ? "active" : "inactive")}.";
        return RedirectToAction(nameof(Users));
    }

    /// <summary>
    /// Update user role (Head only)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "HeadOnly")]
    public async Task<IActionResult> UpdateUserRole(int id, string newRole)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction(nameof(Users));
        }

        // Validate role
        var role = RoleExtensions.ParseRole(newRole);
        if (!role.HasValue)
        {
            TempData["Error"] = "Invalid role specified.";
            return RedirectToAction(nameof(UserDetails), new { id });
        }

        user.Roles = role.Value.ToString();
        user.LastModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Sync to Keycloak
        if (!string.IsNullOrEmpty(user.KeycloakUserId))
        {
            try
            {
                await _keycloakSync.SyncUserRolesToKeycloakAsync(user.KeycloakUserId, user.GetRoles());
                _logger.LogInformation($"User {user.Username} role updated to {role.Value}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing user role to Keycloak");
            }
        }

        TempData["Success"] = $"User {user.Username} role updated to {role.Value.GetDisplayName()}.";
        return RedirectToAction(nameof(UserDetails), new { id });
    }

    /// <summary>
    /// Update user's Keycloak roles (Head only)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "HeadOnly")]
    public async Task<IActionResult> UpdateUserKeycloakRoles(int id, List<string>? selectedKeycloakRoles)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction(nameof(Users));
        }

        // Update user's Keycloak roles
        user.SetKeycloakRoles(selectedKeycloakRoles ?? new List<string>());
        user.LastModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Sync to Keycloak
        if (!string.IsNullOrEmpty(user.KeycloakUserId))
        {
            try
            {
                var keycloakRoles = user.GetKeycloakRoles();
                await _keycloakSync.SyncUserKeycloakRolesAsync(user.KeycloakUserId, keycloakRoles);
                _logger.LogInformation($"User {user.Username} Keycloak roles updated to: {string.Join(", ", keycloakRoles)}");
                TempData["Success"] = $"Keycloak roles updated successfully for {user.Username}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing Keycloak roles to Keycloak");
                TempData["Warning"] = "Keycloak roles updated in database, but sync to Keycloak failed.";
            }
        }
        else
        {
            TempData["Success"] = $"Keycloak roles updated for {user.Username} (not yet synced to Keycloak).";
        }

        return RedirectToAction(nameof(UserDetails), new { id });
    }

    /// <summary>
    /// Resync user to Keycloak
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResyncUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction(nameof(Users));
        }

        try
        {
            if (string.IsNullOrEmpty(user.KeycloakUserId))
            {
                TempData["Warning"] = "User has no Keycloak ID. Cannot sync.";
                return RedirectToAction(nameof(UserDetails), new { id });
            }

            // Update user in Keycloak
            var success = await _keycloakSync.UpdateUserInKeycloakAsync(user);
            
            if (success)
            {
                // Also sync roles
                await _keycloakSync.SyncUserRolesToKeycloakAsync(user.KeycloakUserId, user.GetRoles());
                
                TempData["Success"] = $"User {user.Username} successfully resynced to Keycloak.";
                _logger.LogInformation($"User {user.Username} manually resynced to Keycloak");
            }
            else
            {
                TempData["Error"] = "Failed to resync user to Keycloak.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resyncing user to Keycloak");
            TempData["Error"] = "An error occurred while resyncing user to Keycloak.";
        }

        return RedirectToAction(nameof(UserDetails), new { id });
    }
}


