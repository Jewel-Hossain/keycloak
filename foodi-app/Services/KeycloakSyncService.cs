using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FoodiApp.Models;
using Microsoft.Extensions.Caching.Memory;

namespace FoodiApp.Services;

public class KeycloakSyncService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeycloakSyncService> _logger;
    private readonly IMemoryCache _cache;
    private const string ROLES_CACHE_KEY = "keycloak_roles";
    private static readonly TimeSpan RolesCacheDuration = TimeSpan.FromMinutes(5);

    public KeycloakSyncService(HttpClient httpClient, IConfiguration configuration, ILogger<KeycloakSyncService> logger, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _cache = cache;
    }

    public virtual async Task<string?> SyncUserToKeycloakAsync(User user, string plainPassword)
    {
        try
        {
            var baseUrl = _configuration["Keycloak:BaseUrl"];
            var realm = _configuration["Keycloak:Realm"];
            var adminUsername = _configuration["Keycloak:AdminUsername"];
            var adminPassword = _configuration["Keycloak:AdminPassword"];

            // Get admin access token
            var token = await GetAdminTokenAsync(baseUrl!, realm!, adminUsername!, adminPassword!);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Failed to get Keycloak admin token");
                return null;
            }

            // Create user in Keycloak
            var keycloakUser = new
            {
                username = user.Username,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                enabled = true,
                emailVerified = true,
                credentials = new[]
                {
                    new
                    {
                        type = "password",
                        value = plainPassword,
                        temporary = false
                    }
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(keycloakUser),
                Encoding.UTF8,
                "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            var response = await _httpClient.PostAsync(
                $"{baseUrl}/admin/realms/{realm}/users",
                content);

            if (response.IsSuccessStatusCode)
            {
                // Get the created user ID from Location header
                var locationHeader = response.Headers.Location?.ToString();
                var userId = locationHeader?.Split('/').Last();
                _logger.LogInformation($"Successfully synced user {user.Username} to Keycloak with ID: {userId}");
                return userId;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to sync user to Keycloak: {response.StatusCode} - {error}");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing user to Keycloak");
            return null;
        }
    }

    public virtual async Task<bool> UpdateUserInKeycloakAsync(User user)
    {
        try
        {
            if (string.IsNullOrEmpty(user.KeycloakUserId))
            {
                _logger.LogWarning($"User {user.Username} has no Keycloak user ID");
                return false;
            }

            var baseUrl = _configuration["Keycloak:BaseUrl"];
            var realm = _configuration["Keycloak:Realm"];
            var adminUsername = _configuration["Keycloak:AdminUsername"];
            var adminPassword = _configuration["Keycloak:AdminPassword"];

            // Get admin access token
            var token = await GetAdminTokenAsync(baseUrl!, realm!, adminUsername!, adminPassword!);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Failed to get Keycloak admin token for user update");
                return false;
            }

            // Update user in Keycloak
            var keycloakUser = new
            {
                username = user.Username,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                enabled = user.IsActive,
                emailVerified = true
            };

            var content = new StringContent(
                JsonSerializer.Serialize(keycloakUser),
                Encoding.UTF8,
                "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            var response = await _httpClient.PutAsync(
                $"{baseUrl}/admin/realms/{realm}/users/{user.KeycloakUserId}",
                content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Successfully updated user {user.Username} in Keycloak");
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to update user in Keycloak: {response.StatusCode} - {error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user in Keycloak");
            return false;
        }
    }

    public virtual async Task<bool> SetUserActiveStatusInKeycloakAsync(string keycloakUserId, bool isActive)
    {
        try
        {
            var baseUrl = _configuration["Keycloak:BaseUrl"];
            var realm = _configuration["Keycloak:Realm"];
            var adminUsername = _configuration["Keycloak:AdminUsername"];
            var adminPassword = _configuration["Keycloak:AdminPassword"];

            // Get admin access token
            var token = await GetAdminTokenAsync(baseUrl!, realm!, adminUsername!, adminPassword!);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Failed to get Keycloak admin token for status update");
                return false;
            }

            // Update user enabled status
            var updateData = new
            {
                enabled = isActive
            };

            var content = new StringContent(
                JsonSerializer.Serialize(updateData),
                Encoding.UTF8,
                "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            var response = await _httpClient.PutAsync(
                $"{baseUrl}/admin/realms/{realm}/users/{keycloakUserId}",
                content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Successfully set user status to {(isActive ? "active" : "inactive")} in Keycloak");
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to update user status in Keycloak: {response.StatusCode} - {error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user status in Keycloak");
            return false;
        }
    }

    public virtual async Task<bool> UpdateUserPasswordInKeycloakAsync(string keycloakUserId, string newPassword)
    {
        try
        {
            var baseUrl = _configuration["Keycloak:BaseUrl"];
            var realm = _configuration["Keycloak:Realm"];
            var adminUsername = _configuration["Keycloak:AdminUsername"];
            var adminPassword = _configuration["Keycloak:AdminPassword"];

            // Get admin access token
            var token = await GetAdminTokenAsync(baseUrl!, realm!, adminUsername!, adminPassword!);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Failed to get Keycloak admin token for password update");
                return false;
            }

            // Update password
            var passwordData = new
            {
                type = "password",
                value = newPassword,
                temporary = false
            };

            var content = new StringContent(
                JsonSerializer.Serialize(passwordData),
                Encoding.UTF8,
                "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            var response = await _httpClient.PutAsync(
                $"{baseUrl}/admin/realms/{realm}/users/{keycloakUserId}/reset-password",
                content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Successfully updated user password in Keycloak");
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to update password in Keycloak: {response.StatusCode} - {error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating password in Keycloak");
            return false;
        }
    }

    public virtual async Task<bool> SyncUserRolesToKeycloakAsync(string keycloakUserId, List<Role> roles)
    {
        // Convert Role enum to string list and call the new method
        var roleNames = roles.Select(r => r.ToString().ToLower()).ToList();
        return await SyncUserKeycloakRolesAsync(keycloakUserId, roleNames);
    }

    /// <summary>
    /// Sync user's Keycloak roles to Keycloak server
    /// This replaces the user's Keycloak roles with the provided list
    /// </summary>
    public virtual async Task<bool> SyncUserKeycloakRolesAsync(string keycloakUserId, List<string> roleNames)
    {
        try
        {
            if (string.IsNullOrEmpty(keycloakUserId))
            {
                _logger.LogWarning("Cannot sync roles: Keycloak user ID is null or empty");
                return false;
            }

            var baseUrl = _configuration["Keycloak:BaseUrl"];
            var realm = _configuration["Keycloak:Realm"];
            var adminUsername = _configuration["Keycloak:AdminUsername"];
            var adminPassword = _configuration["Keycloak:AdminPassword"];

            // Get admin access token
            var token = await GetAdminTokenAsync(baseUrl!, realm!, adminUsername!, adminPassword!);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Failed to get Keycloak admin token for role sync");
                return false;
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Get the foodi-app client UUID
            var clientResponse = await _httpClient.GetAsync($"{baseUrl}/admin/realms/{realm}/clients?clientId=foodi-app");
            if (!clientResponse.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to get foodi-app client: {clientResponse.StatusCode}");
                return false;
            }

            var clientContent = await clientResponse.Content.ReadAsStringAsync();
            var clients = JsonSerializer.Deserialize<List<JsonElement>>(clientContent);
            
            if (clients == null || !clients.Any())
            {
                _logger.LogError("foodi-app client not found");
                return false;
            }

            var clientUuid = clients[0].GetProperty("id").GetString();
            if (string.IsNullOrEmpty(clientUuid))
            {
                _logger.LogError("Could not get foodi-app client UUID");
                return false;
            }

            // Get current user's client role mappings
            var currentRolesResponse = await _httpClient.GetAsync(
                $"{baseUrl}/admin/realms/{realm}/users/{keycloakUserId}/role-mappings/clients/{clientUuid}");

            if (!currentRolesResponse.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to get current client roles for user: {currentRolesResponse.StatusCode}");
                return false;
            }

            var currentRolesContent = await currentRolesResponse.Content.ReadAsStringAsync();
            var currentRoles = JsonSerializer.Deserialize<List<JsonElement>>(currentRolesContent);

            // Get all available client roles to identify which ones to manage
            var allKeycloakRoles = await GetAllKeycloakRolesAsync();
            var managedRoleNames = allKeycloakRoles.Select(r => r.ToLower()).ToList();
            
            var rolesToRemove = new List<object>();
            
            if (currentRoles != null)
            {
                foreach (var roleElement in currentRoles)
                {
                    var roleName = roleElement.GetProperty("name").GetString();
                    if (roleName != null && managedRoleNames.Contains(roleName.ToLower()))
                    {
                        // Only remove roles that we manage
                        rolesToRemove.Add(new
                        {
                            id = roleElement.GetProperty("id").GetString(),
                            name = roleName
                        });
                    }
                }
            }

            // Remove old role assignments
            if (rolesToRemove.Any())
            {
                var removeContent = new StringContent(
                    JsonSerializer.Serialize(rolesToRemove),
                    Encoding.UTF8,
                    "application/json");

                var removeResponse = await _httpClient.SendAsync(new HttpRequestMessage
                {
                    Method = HttpMethod.Delete,
                    RequestUri = new Uri($"{baseUrl}/admin/realms/{realm}/users/{keycloakUserId}/role-mappings/clients/{clientUuid}"),
                    Content = removeContent
                });

                if (!removeResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to remove old client roles: {removeResponse.StatusCode}");
                }
            }

            // Assign new roles
            foreach (var roleName in roleNames)
            {
                var roleInfo = await GetClientRoleByNameAsync(baseUrl!, realm!, token, clientUuid, roleName);
                if (roleInfo != null)
                {
                    var assignRoleData = new[]
                    {
                        new
                        {
                            id = roleInfo.Value.GetProperty("id").GetString(),
                            name = roleInfo.Value.GetProperty("name").GetString()
                        }
                    };

                    var assignContent = new StringContent(
                        JsonSerializer.Serialize(assignRoleData),
                        Encoding.UTF8,
                        "application/json");

                    var assignResponse = await _httpClient.PostAsync(
                        $"{baseUrl}/admin/realms/{realm}/users/{keycloakUserId}/role-mappings/clients/{clientUuid}",
                        assignContent);

                    if (assignResponse.IsSuccessStatusCode)
                    {
                        _logger.LogInformation($"Successfully assigned client role {roleName} to user in Keycloak");
                    }
                    else
                    {
                        var error = await assignResponse.Content.ReadAsStringAsync();
                        _logger.LogError($"Failed to assign client role {roleName}: {assignResponse.StatusCode} - {error}");
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing user client roles to Keycloak");
            return false;
        }
    }

    private async Task<bool> EnsureRoleExistsAsync(string baseUrl, string realm, string token, Role role)
    {
        try
        {
            var roleName = role.ToString().ToLower();
            
            // Check if role exists
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _httpClient.GetAsync($"{baseUrl}/admin/realms/{realm}/roles/{roleName}");
            
            if (response.IsSuccessStatusCode)
            {
                return true; // Role already exists
            }

            // Create role if it doesn't exist
            var roleData = new
            {
                name = roleName,
                description = role.GetDescription()
            };

            var content = new StringContent(
                JsonSerializer.Serialize(roleData),
                Encoding.UTF8,
                "application/json");

            var createResponse = await _httpClient.PostAsync(
                $"{baseUrl}/admin/realms/{realm}/roles",
                content);

            if (createResponse.IsSuccessStatusCode || createResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.LogInformation($"Ensured role {roleName} exists in Keycloak");
                return true;
            }
            else
            {
                var error = await createResponse.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to create role {roleName}: {createResponse.StatusCode} - {error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error ensuring role {role} exists in Keycloak");
            return false;
        }
    }

    private async Task<JsonElement?> GetRoleByNameAsync(string baseUrl, string realm, string token, Role role)
    {
        var roleName = role.ToString().ToLower();
        return await GetRoleByNameStringAsync(baseUrl, realm, token, roleName);
    }

    private async Task<JsonElement?> GetRoleByNameStringAsync(string baseUrl, string realm, string token, string roleName)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            var response = await _httpClient.GetAsync($"{baseUrl}/admin/realms/{realm}/roles/{roleName}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<JsonElement>(content);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting role {roleName} from Keycloak");
            return null;
        }
    }

    private async Task<JsonElement?> GetClientRoleByNameAsync(string baseUrl, string realm, string token, string clientUuid, string roleName)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            var response = await _httpClient.GetAsync($"{baseUrl}/admin/realms/{realm}/clients/{clientUuid}/roles/{roleName}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<JsonElement>(content);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting client role {roleName} from Keycloak");
            return null;
        }
    }

    private async Task<bool> EnsureRoleExistsByNameAsync(string baseUrl, string realm, string token, string roleName)
    {
        try
        {
            // Check if role exists
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _httpClient.GetAsync($"{baseUrl}/admin/realms/{realm}/roles/{roleName}");
            
            if (response.IsSuccessStatusCode)
            {
                return true; // Role already exists
            }

            // Create role if it doesn't exist
            var roleData = new
            {
                name = roleName,
                description = $"Role: {roleName}"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(roleData),
                Encoding.UTF8,
                "application/json");

            var createResponse = await _httpClient.PostAsync(
                $"{baseUrl}/admin/realms/{realm}/roles",
                content);

            if (createResponse.IsSuccessStatusCode || createResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.LogInformation($"Ensured role {roleName} exists in Keycloak");
                return true;
            }
            else
            {
                var error = await createResponse.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to create role {roleName}: {createResponse.StatusCode} - {error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error ensuring role {roleName} exists in Keycloak");
            return false;
        }
    }

    private async Task<string?> GetAdminTokenAsync(string baseUrl, string realm, string username, string password)
    {
        try
        {
            var tokenRequest = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "client_id", "admin-cli" },
                { "username", username },
                { "password", password }
            };

            // Admin token must be fetched from master realm
            var response = await _httpClient.PostAsync(
                $"{baseUrl}/realms/master/protocol/openid-connect/token",
                new FormUrlEncodedContent(tokenRequest));

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(content);
                return tokenResponse.GetProperty("access_token").GetString();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Keycloak admin token");
            return null;
        }
    }

    /// <summary>
    /// Get all available client roles from Keycloak foodi-app client
    /// Cached for 5 minutes to reduce API calls
    /// </summary>
    public virtual async Task<List<string>> GetAllKeycloakRolesAsync()
    {
        try
        {
            // Check cache first
            if (_cache.TryGetValue(ROLES_CACHE_KEY, out List<string>? cachedRoles) && cachedRoles != null)
            {
                _logger.LogInformation("Returning cached Keycloak client roles");
                return cachedRoles;
            }

            var baseUrl = _configuration["Keycloak:BaseUrl"];
            var realm = _configuration["Keycloak:Realm"];
            var adminUsername = _configuration["Keycloak:AdminUsername"];
            var adminPassword = _configuration["Keycloak:AdminPassword"];

            // Get admin access token
            var token = await GetAdminTokenAsync(baseUrl!, realm!, adminUsername!, adminPassword!);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Failed to get Keycloak admin token for fetching roles");
                return new List<string>();
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // First, get the foodi-app client UUID
            var clientResponse = await _httpClient.GetAsync($"{baseUrl}/admin/realms/{realm}/clients?clientId=foodi-app");
            if (!clientResponse.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to get foodi-app client: {clientResponse.StatusCode}");
                return new List<string>();
            }

            var clientContent = await clientResponse.Content.ReadAsStringAsync();
            var clients = JsonSerializer.Deserialize<List<JsonElement>>(clientContent);
            
            if (clients == null || !clients.Any())
            {
                _logger.LogError("foodi-app client not found");
                return new List<string>();
            }

            var clientUuid = clients[0].GetProperty("id").GetString();
            if (string.IsNullOrEmpty(clientUuid))
            {
                _logger.LogError("Could not get foodi-app client UUID");
                return new List<string>();
            }

            // Fetch all client roles
            var response = await _httpClient.GetAsync($"{baseUrl}/admin/realms/{realm}/clients/{clientUuid}/roles");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var roles = JsonSerializer.Deserialize<List<JsonElement>>(content);

                var roleNames = new List<string>();
                if (roles != null)
                {
                    foreach (var roleElement in roles)
                    {
                        if (roleElement.TryGetProperty("name", out var nameProperty))
                        {
                            var roleName = nameProperty.GetString();
                            if (!string.IsNullOrWhiteSpace(roleName))
                            {
                                roleNames.Add(roleName);
                            }
                        }
                    }
                }

                // Cache the results
                _cache.Set(ROLES_CACHE_KEY, roleNames, RolesCacheDuration);
                _logger.LogInformation($"Successfully fetched {roleNames.Count} Keycloak client roles");
                
                return roleNames;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to fetch Keycloak client roles: {response.StatusCode} - {error}");
                return new List<string>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Keycloak client roles");
            return new List<string>();
        }
    }

    /// <summary>
    /// Clear the roles cache (useful after creating/deleting roles in Keycloak)
    /// </summary>
    public void ClearRolesCache()
    {
        _cache.Remove(ROLES_CACHE_KEY);
        _logger.LogInformation("Keycloak roles cache cleared");
    }
}

