using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using FoodiApp.Data;
using FoodiApp.Services;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Configure SQLite database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.UseOpenIddict();
});

// Configure OpenIddict (to make Foodi act as an Identity Provider)
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("/connect/authorize")
            .SetTokenEndpointUris("/connect/token")
            .SetUserinfoEndpointUris("/connect/userinfo")
            .SetLogoutEndpointUris("/connect/logout");

        options.AllowAuthorizationCodeFlow()
            .AllowRefreshTokenFlow()
            .AllowClientCredentialsFlow();

        options.RegisterScopes(Scopes.Email, Scopes.Profile, Scopes.OpenId, Scopes.OfflineAccess);

        options.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();

        // Set issuer to work with Docker network
        options.SetIssuer(new Uri("http://localhost:5000/"));

        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableTokenEndpointPassthrough()
            .EnableUserinfoEndpointPassthrough()
            .EnableLogoutEndpointPassthrough()
            .DisableTransportSecurityRequirement(); // Allow HTTP for development
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

// Configure Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
    });

// Configure role-based authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("HeadOnly", policy => policy.RequireRole("Head"));
    options.AddPolicy("AdminOrAbove", policy => policy.RequireRole("Head", "Admin"));
    options.AddPolicy("LeadOrAbove", policy => policy.RequireRole("Head", "Admin", "Lead"));
    options.AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());
});

// Add memory cache for role caching
builder.Services.AddMemoryCache();

// Register Keycloak Sync Service
builder.Services.AddHttpClient<KeycloakSyncService>();
builder.Services.AddScoped<KeycloakSyncService>();

// Register Role Service
builder.Services.AddScoped<RoleService>();

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Initialize database and seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
    
    context.Database.EnsureCreated();
    
    // Seed OpenIddict client for Keycloak
    if (await manager.FindByClientIdAsync("keycloak-client") == null)
    {
        await manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = "keycloak-client",
            ClientSecret = "foodi-secret-key-2024",
            ConsentType = ConsentTypes.Implicit,
            DisplayName = "Keycloak SSO Client",
            RedirectUris =
            {
                new Uri("http://localhost:8080/realms/foodi/broker/foodi/endpoint"),
                new Uri("http://localhost:8080/realms/foodi/broker/foodi/endpoint/"),
                new Uri("http://localhost:8080/realms/master/broker/foodi/endpoint"),
                new Uri("http://localhost:8080/realms/master/broker/foodi/endpoint/")
            },
            PostLogoutRedirectUris =
            {
                new Uri("http://localhost:8080/")
            },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.Logout,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                Permissions.Scopes.Roles
            }
        });
    }
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Make the implicit Program class public so test projects can access it
public partial class Program { }

