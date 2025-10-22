using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FoodiApp.Models;

namespace FoodiApp.Services;

public class KeycloakSyncService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeycloakSyncService> _logger;

    public KeycloakSyncService(HttpClient httpClient, IConfiguration configuration, ILogger<KeycloakSyncService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
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

            // Ensure all roles exist in Keycloak
            foreach (var role in roles)
            {
                await EnsureRoleExistsAsync(baseUrl!, realm!, token, role);
            }

            // Get current user's role mappings
            var currentRolesResponse = await _httpClient.GetAsync(
                $"{baseUrl}/admin/realms/{realm}/users/{keycloakUserId}/role-mappings/realm");

            if (!currentRolesResponse.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to get current roles for user: {currentRolesResponse.StatusCode}");
                return false;
            }

            var currentRolesContent = await currentRolesResponse.Content.ReadAsStringAsync();
            var currentRoles = JsonSerializer.Deserialize<List<JsonElement>>(currentRolesContent);

            // Remove old Foodi role assignments (head, admin, lead, agent)
            var foodiRoleNames = new[] { "head", "admin", "lead", "agent" };
            var rolesToRemove = new List<object>();
            
            if (currentRoles != null)
            {
                foreach (var roleElement in currentRoles)
                {
                    var roleName = roleElement.GetProperty("name").GetString();
                    if (roleName != null && foodiRoleNames.Contains(roleName.ToLower()))
                    {
                        rolesToRemove.Add(new
                        {
                            id = roleElement.GetProperty("id").GetString(),
                            name = roleName
                        });
                    }
                }
            }

            if (rolesToRemove.Any())
            {
                var removeContent = new StringContent(
                    JsonSerializer.Serialize(rolesToRemove),
                    Encoding.UTF8,
                    "application/json");

                var removeResponse = await _httpClient.SendAsync(new HttpRequestMessage
                {
                    Method = HttpMethod.Delete,
                    RequestUri = new Uri($"{baseUrl}/admin/realms/{realm}/users/{keycloakUserId}/role-mappings/realm"),
                    Content = removeContent
                });

                if (!removeResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to remove old roles: {removeResponse.StatusCode}");
                }
            }

            // Assign new roles
            foreach (var role in roles)
            {
                var roleInfo = await GetRoleByNameAsync(baseUrl!, realm!, token, role);
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
                        $"{baseUrl}/admin/realms/{realm}/users/{keycloakUserId}/role-mappings/realm",
                        assignContent);

                    if (assignResponse.IsSuccessStatusCode)
                    {
                        _logger.LogInformation($"Successfully assigned role {role} to user in Keycloak");
                    }
                    else
                    {
                        var error = await assignResponse.Content.ReadAsStringAsync();
                        _logger.LogError($"Failed to assign role {role}: {assignResponse.StatusCode} - {error}");
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing user roles to Keycloak");
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
        try
        {
            var roleName = role.ToString().ToLower();
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
            _logger.LogError(ex, $"Error getting role {role} from Keycloak");
            return null;
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

            var response = await _httpClient.PostAsync(
                $"{baseUrl}/realms/{realm}/protocol/openid-connect/token",
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
}

