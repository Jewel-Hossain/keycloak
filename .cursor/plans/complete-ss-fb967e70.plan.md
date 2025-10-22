<!-- fb967e70-bbaf-4146-b2f9-b4db7bcf15f8 4a7d9a25-30b0-421b-9ead-4d54d7e3275f -->
# Role-Based Menu Permissions Implementation

## Overview

Add role-based access control (RBAC) to Foodi with automatic Keycloak role synchronization. Users select roles during registration, with "agent" as the default. Roles control both UI menu visibility and API access permissions.

## Roles

- **head**: Full system access, user management
- **admin**: Administrative features, reporting
- **lead**: Team management, advanced features
- **agent**: Basic user features (default)

## Implementation Steps

### 1. Database Schema Updates

**File**: `foodi-app/Models/Role.cs` (NEW)

- Create Role enum with: Head, Admin, Lead, Agent
- Add display names and descriptions

**File**: `foodi-app/Models/User.cs`

- Add `Roles` property (string, comma-separated for simplicity)
- Add helper methods: `HasRole(Role role)`, `GetRoles()`, `AddRole(Role role)`
- Default value: "Agent"

**File**: `foodi-app/Data/ApplicationDbContext.cs`

- Update OnModelCreating to seed default roles
- Add indexes for role-based queries

### 2. Registration Flow Enhancement

**File**: `foodi-app/Models/RegisterViewModel.cs`

- Add `SelectedRole` property (optional, defaults to Agent)
- Add validation attributes

**File**: `foodi-app/Controllers/AccountController.cs`

- Update `Register` POST action:
  - Accept selected role from form
  - Assign default "Agent" role if none selected
  - Set `user.Roles` during user creation
  - Pass roles to Keycloak sync

**File**: `foodi-app/Views/Account/Register.cshtml`

- Add role selection dropdown:
  ```html
  <div class="form-group">
    <label>Account Type</label>
    <select asp-for="SelectedRole" class="form-control">
      <option value="Agent">Agent (Default)</option>
      <option value="Lead">Lead</option>
      <option value="Admin">Administrator</option>
      <option value="Head">Head</option>
    </select>
  </div>
  ```


### 3. Keycloak Role Synchronization

**File**: `foodi-app/Services/KeycloakSyncService.cs`

- Add `SyncUserRolesToKeycloakAsync(string userId, List<Role> roles)` method:
  - Get Keycloak realm roles
  - Create roles if they don't exist (head, admin, lead, agent)
  - Assign roles to user via Admin API
  - Remove old role assignments
- Update `CreateUserInKeycloakAsync`:
  - Call role sync after user creation
  - Set default "agent" role in Keycloak

**Keycloak Setup**:

- Manually create realm roles in foodi realm (or auto-create via API):
  - head, admin, lead, agent
- Configure role mappers in "foodi" identity provider

### 4. Authorization in Foodi

**File**: `foodi-app/Authorization/RoleRequirement.cs` (NEW)

- Create custom authorization requirement
- Implement `IAuthorizationRequirement` interface

**File**: `foodi-app/Authorization/RoleAuthorizationHandler.cs` (NEW)

- Implement `AuthorizationHandler<RoleRequirement>`
- Check user roles from claims

**File**: `foodi-app/Program.cs`

- Register authorization policies:
  ```csharp
  builder.Services.AddAuthorization(options =>
  {
      options.AddPolicy("HeadOnly", policy => policy.RequireRole("Head"));
      options.AddPolicy("AdminOrAbove", policy => policy.RequireRole("Head", "Admin"));
      options.AddPolicy("LeadOrAbove", policy => policy.RequireRole("Head", "Admin", "Lead"));
  });
  ```


### 5. Menu Visibility Control

**File**: `foodi-app/Views/Shared/_Layout.cshtml`

- Update navigation menu with role-based visibility:
  ```html
  @if (User.IsInRole("Head") || User.IsInRole("Admin"))
  {
      <li><a href="/Admin/Dashboard">Admin Panel</a></li>
  }
  @if (User.IsInRole("Head") || User.IsInRole("Admin") || User.IsInRole("Lead"))
  {
      <li><a href="/Reports/Index">Reports</a></li>
  }
  ```


**File**: `foodi-app/Controllers/AdminController.cs` (NEW)

- Create admin-only controller
- Add `[Authorize(Policy = "AdminOrAbove")]` attribute
- Implement user management features

### 6. Profile Management Updates

**File**: `foodi-app/Views/Account/Profile.cshtml`

- Display current user role(s)
- Show role badge/indicator

**File**: `foodi-app/Controllers/AccountController.cs`

- Update `Profile` action to pass roles to view
- Add role change functionality (admin only)

### 7. Role-Based Feature Access

**File**: `foodi-app/Controllers/HomeController.cs`

- Add role checks for menu access:
  ```csharp
  [Authorize(Policy = "LeadOrAbove")]
  public IActionResult Menu() { ... }
  ```


**File**: `foodi-app/Controllers/OrderController.cs` (if exists)

- Add appropriate role checks for order management

### 8. Testing

**File**: `foodi-app/FoodiApp.Tests/UnitTests/Authorization/RoleAuthorizationTests.cs` (NEW)

- Test role requirement validation
- Test authorization handler logic
- Test role assignment during registration

**File**: `foodi-app/FoodiApp.Tests/UnitTests/Services/KeycloakSyncServiceRoleTests.cs` (NEW)

- Test role sync to Keycloak
- Test role creation in Keycloak
- Test role assignment/removal

**File**: `foodi-app/FoodiApp.Tests/IntegrationTests/RoleBasedAccessTests.cs` (NEW)

- Test registration with role selection
- Test menu visibility based on roles
- Test API endpoint access control
- Test Keycloak role synchronization

**File**: `foodi-app/FoodiApp.Tests/IntegrationTests/KeycloakRoleSyncTests.cs` (NEW)

- Test end-to-end role sync
- Verify roles exist in Keycloak after registration
- Test role updates propagate to Keycloak

## Key Files to Modify

1. `foodi-app/Models/User.cs` - Add Roles property
2. `foodi-app/Models/RegisterViewModel.cs` - Add SelectedRole
3. `foodi-app/Controllers/AccountController.cs` - Registration with role selection
4. `foodi-app/Services/KeycloakSyncService.cs` - Role sync methods
5. `foodi-app/Views/Account/Register.cshtml` - Role dropdown
6. `foodi-app/Views/Shared/_Layout.cshtml` - Role-based menu
7. `foodi-app/Program.cs` - Authorization policies
8. `foodi-app/Views/Account/Profile.cshtml` - Display roles

## Keycloak Configuration

Manual steps (or automated via Admin API):

1. Create realm roles in "foodi" realm: head, admin, lead, agent
2. Add role mapper to "foodi" identity provider
3. Configure role claim mapping

## Database Migration

Since using SQLite with Entity Framework, the schema will auto-update on startup. Add migration for production:

- Add `Roles` column to Users table (default: "Agent")

## Testing Strategy

1. Unit tests: Role assignment logic, authorization handlers
2. Integration tests: Full registration flow with roles, Keycloak sync
3. UI tests: Menu visibility, role-based navigation
4. Manual verification: Check Keycloak admin console for role assignments

### To-dos

- [ ] Add 'Go to Keycloak' button in _Layout.cshtml navbar
- [ ] Add IsActive, LastModifiedAt, DeactivatedAt fields to User model
- [ ] Add UpdateUserInKeycloakAsync and SetUserActiveStatusInKeycloakAsync methods to KeycloakSyncService
- [ ] Add user profile update and deactivation endpoints with real-time sync
- [ ] Update all configuration files to use 'foodi' realm instead of 'master'
- [ ] Expose PostgreSQL port 5432 in docker-compose.yml
- [ ] Add unit tests for new sync operations (update, activate, deactivate)
- [ ] Create KeycloakIntegrationTests.cs with full API flow tests
- [ ] Create SsoFlowTests.cs with UI interaction tests
- [ ] Create REALM_SETUP_GUIDE.md with step-by-step realm creation instructions