using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FoodiApp.Data;
using FoodiApp.Models;
using FoodiApp.Services;

namespace FoodiApp.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly KeycloakSyncService _keycloakSync;
    private readonly RoleService _roleService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        ApplicationDbContext context,
        KeycloakSyncService keycloakSync,
        RoleService roleService,
        ILogger<AccountController> logger)
    {
        _context = context;
        _keycloakSync = keycloakSync;
        _roleService = roleService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        // Fetch Keycloak roles to display in the form
        var (foodiRoles, keycloakRoles) = await _roleService.GetAllRolesAsync();
        ViewBag.FoodiRoles = foodiRoles;
        ViewBag.KeycloakRoles = keycloakRoles;
        
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // Re-populate roles for display
            var (foodiRoles, keycloakRoles) = await _roleService.GetAllRolesAsync();
            ViewBag.FoodiRoles = foodiRoles;
            ViewBag.KeycloakRoles = keycloakRoles;
            return View(model);
        }

        // Check if user already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == model.Email || u.Username == model.Username);

        if (existingUser != null)
        {
            ModelState.AddModelError("", "User with this email or username already exists.");
            var (foodiRoles, keycloakRoles) = await _roleService.GetAllRolesAsync();
            ViewBag.FoodiRoles = foodiRoles;
            ViewBag.KeycloakRoles = keycloakRoles;
            return View(model);
        }

        // Get selected roles
        var selectedFoodiRole = model.GetSelectedRole();
        var selectedKeycloakRoles = model.GetSelectedKeycloakRoles();
        
        // Create new user
        var user = new User
        {
            Email = model.Email,
            Username = model.Username,
            FirstName = model.FirstName,
            LastName = model.LastName,
            PasswordHash = HashPassword(model.Password),
            CreatedAt = DateTime.UtcNow,
            Roles = selectedFoodiRole.ToString() // Set the Foodi internal role
        };

        // Set Keycloak roles
        user.SetKeycloakRoles(selectedKeycloakRoles);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Sync to Keycloak
        try
        {
            var keycloakUserId = await _keycloakSync.SyncUserToKeycloakAsync(user, model.Password);
            if (!string.IsNullOrEmpty(keycloakUserId))
            {
                user.SyncedToKeycloak = true;
                user.KeycloakUserId = keycloakUserId;
                await _context.SaveChangesAsync();
                
                // Sync Keycloak roles to Keycloak server
                if (selectedKeycloakRoles.Any())
                {
                    try
                    {
                        await _keycloakSync.SyncUserKeycloakRolesAsync(keycloakUserId, selectedKeycloakRoles);
                        _logger.LogInformation($"User {user.Username} Keycloak roles synced: {string.Join(", ", selectedKeycloakRoles)}");
                    }
                    catch (Exception roleEx)
                    {
                        _logger.LogError(roleEx, "Error syncing Keycloak roles to Keycloak");
                    }
                }
                
                TempData["Success"] = $"Account created successfully as {selectedFoodiRole.GetDisplayName()} and synced with Keycloak!";
                _logger.LogInformation($"User {user.Username} registered with Foodi role: {selectedFoodiRole}, Keycloak roles: {string.Join(", ", selectedKeycloakRoles)}");
            }
            else
            {
                TempData["Warning"] = "Account created but could not sync with Keycloak. You can still use Foodi.";
                _logger.LogWarning($"User {user.Username} registered but Keycloak sync failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Keycloak sync");
            TempData["Warning"] = "Account created but could not sync with Keycloak. You can still use Foodi.";
        }

        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == model.UsernameOrEmail || u.Username == model.UsernameOrEmail);

        if (user == null || !VerifyPassword(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError("", "Invalid login attempt.");
            return View(model);
        }

        // Check if user is active
        if (!user.IsActive)
        {
            ModelState.AddModelError("", "Your account has been deactivated. Please contact support to reactivate.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.GivenName, user.FirstName),
            new Claim(ClaimTypes.Surname, user.LastName),
            new Claim("sub", user.Id.ToString())
        };
        
        // Add role claims
        var userRoles = user.GetRoles();
        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        _logger.LogInformation($"User {user.Username} logged in");

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Profile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return RedirectToAction(nameof(Login));
        }

        var user = await _context.Users.FindAsync(int.Parse(userIdClaim));
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var model = new UpdateProfileViewModel
        {
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName
        };
        
        // Pass role information to view
        ViewData["UserRoles"] = user.GetRoles();
        ViewData["UserRolesDisplay"] = string.Join(", ", user.GetRoles().Select(r => r.GetDisplayName()));
        ViewData["UserKeycloakRoles"] = user.GetKeycloakRoles();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> UpdateProfile(UpdateProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Profile", model);
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return RedirectToAction(nameof(Login));
        }

        var user = await _context.Users.FindAsync(int.Parse(userIdClaim));
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        // Update user profile
        user.Email = model.Email;
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.LastModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Sync to Keycloak
        if (user.SyncedToKeycloak && !string.IsNullOrEmpty(user.KeycloakUserId))
        {
            try
            {
                var syncResult = await _keycloakSync.UpdateUserInKeycloakAsync(user);
                if (syncResult)
                {
                    TempData["Success"] = "Profile updated successfully and synced with Keycloak!";
                    _logger.LogInformation($"User {user.Username} profile updated and synced to Keycloak");
                }
                else
                {
                    TempData["Warning"] = "Profile updated but could not sync with Keycloak.";
                    _logger.LogWarning($"User {user.Username} profile updated but Keycloak sync failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Keycloak sync for profile update");
                TempData["Warning"] = "Profile updated but could not sync with Keycloak.";
            }
        }
        else
        {
            TempData["Success"] = "Profile updated successfully!";
        }

        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return RedirectToAction(nameof(Login));
        }

        var user = await _context.Users.FindAsync(int.Parse(userIdClaim));
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        // Verify current password
        if (!VerifyPassword(model.CurrentPassword, user.PasswordHash))
        {
            ModelState.AddModelError("", "Current password is incorrect.");
            return View(model);
        }

        // Update password
        user.PasswordHash = HashPassword(model.NewPassword);
        user.LastModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Sync to Keycloak
        if (user.SyncedToKeycloak && !string.IsNullOrEmpty(user.KeycloakUserId))
        {
            try
            {
                var syncResult = await _keycloakSync.UpdateUserPasswordInKeycloakAsync(user.KeycloakUserId, model.NewPassword);
                if (syncResult)
                {
                    TempData["Success"] = "Password changed successfully and synced with Keycloak!";
                    _logger.LogInformation($"User {user.Username} password changed and synced to Keycloak");
                }
                else
                {
                    TempData["Warning"] = "Password changed but could not sync with Keycloak.";
                    _logger.LogWarning($"User {user.Username} password changed but Keycloak sync failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Keycloak sync for password change");
                TempData["Warning"] = "Password changed but could not sync with Keycloak.";
            }
        }
        else
        {
            TempData["Success"] = "Password changed successfully!";
        }

        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateAccount()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return RedirectToAction(nameof(Login));
        }

        var user = await _context.Users.FindAsync(int.Parse(userIdClaim));
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        // Deactivate user
        user.IsActive = false;
        user.DeactivatedAt = DateTime.UtcNow;
        user.LastModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Sync to Keycloak
        if (user.SyncedToKeycloak && !string.IsNullOrEmpty(user.KeycloakUserId))
        {
            try
            {
                var syncResult = await _keycloakSync.SetUserActiveStatusInKeycloakAsync(user.KeycloakUserId, false);
                if (syncResult)
                {
                    _logger.LogInformation($"User {user.Username} deactivated and synced to Keycloak");
                }
                else
                {
                    _logger.LogWarning($"User {user.Username} deactivated but Keycloak sync failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Keycloak sync for account deactivation");
            }
        }

        // Sign out the user
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        TempData["Success"] = "Your account has been deactivated.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReactivateAccount(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        
        if (user == null || !VerifyPassword(password, user.PasswordHash))
        {
            ModelState.AddModelError("", "Invalid credentials.");
            return View("Login");
        }

        if (user.IsActive)
        {
            ModelState.AddModelError("", "Account is already active.");
            return RedirectToAction(nameof(Login));
        }

        // Reactivate user
        user.IsActive = true;
        user.DeactivatedAt = null;
        user.LastModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Sync to Keycloak
        if (user.SyncedToKeycloak && !string.IsNullOrEmpty(user.KeycloakUserId))
        {
            try
            {
                var syncResult = await _keycloakSync.SetUserActiveStatusInKeycloakAsync(user.KeycloakUserId, true);
                if (syncResult)
                {
                    _logger.LogInformation($"User {user.Username} reactivated and synced to Keycloak");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Keycloak sync for account reactivation");
            }
        }

        TempData["Success"] = "Your account has been reactivated!";
        return RedirectToAction(nameof(Login));
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        var hashedPassword = HashPassword(password);
        return hashedPassword == hash;
    }
}

