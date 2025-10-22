# Keycloak User Creation Role Guide

This guide explains how to create a role in Keycloak that only has permissions to create users, without granting other administrative privileges.

## Understanding Keycloak Roles and Permissions

In Keycloak, there are two main types of roles:
1. **Realm Roles** - Apply to users within a specific realm
2. **Client Roles** - Apply to users for specific clients

For user creation permissions, we'll work with **Realm Roles** and configure fine-grained permissions.

## Method 1: Using Keycloak Admin Console (UI)

### Step 1: Access Keycloak Admin Console

1. Start your Keycloak instance:
   ```bash
   docker-compose up -d
   ```

2. Open your browser and go to: http://localhost:8080
3. Log in with:
   - Username: `admin`
   - Password: `admin123`

### Step 2: Create a New Realm Role

1. **Navigate to Roles**:
   - Click on "Realm roles" in the left sidebar
   - Click "Create role" button

2. **Configure the Role**:
   - **Role name**: `user-creator` (or any name you prefer)
   - **Description**: `Role with permission to create users only`
   - **Composite roles**: Leave unchecked (we'll configure permissions separately)
   - Click "Save"

### Step 3: Configure Fine-Grained Permissions

1. **Enable Fine-Grained Permissions**:
   - Go to "Realm settings" → "Security defenses"
   - Enable "Fine-grained admin permissions" if not already enabled

2. **Navigate to Permissions**:
   - Go to "Roles" → Select your `user-creator` role
   - Click on "Permissions" tab

3. **Configure User Management Permissions**:
   - Click "Add permission"
   - Select "User management" permissions
   - Enable only these specific permissions:
     - `user:create` - Create new users
     - `user:view` - View user details (needed to verify creation)
   - **Do NOT enable**:
     - `user:update` - Update existing users
     - `user:delete` - Delete users
     - `user:manage` - Full user management
     - `user:impersonation` - Impersonate users

### Step 4: Assign the Role to Users

1. **Find the User**:
   - Go to "Users" in the left sidebar
   - Find the user you want to assign this role to

2. **Assign the Role**:
   - Click on the user
   - Go to "Role mapping" tab
   - Click "Assign role"
   - Select "user-creator" role
   - Click "Assign"

## Method 2: Using Keycloak Admin CLI

### Step 1: Install Keycloak Admin CLI

```bash
# Download the Keycloak Admin CLI
wget https://github.com/keycloak/keycloak/releases/download/23.0.0/keycloak-23.0.0.zip
unzip keycloak-23.0.0.zip
cd keycloak-23.0.0/bin
```

### Step 2: Configure CLI Connection

```bash
# Configure the CLI to connect to your Keycloak instance
./kcadm.sh config credentials --server http://localhost:8080 --realm master --user admin --password admin123
```

### Step 3: Create the Role

```bash
# Create the user-creator role
./kcadm.sh create roles -r master -s name=user-creator -s description="Role with permission to create users only"
```

### Step 4: Configure Permissions (Advanced)

For fine-grained permissions via CLI, you'll need to use the REST API directly:

```bash
# Get the role ID
ROLE_ID=$(./kcadm.sh get roles/user-creator -r master --format csv --noquotes --fields id)

# Configure user creation permissions
curl -X POST "http://localhost:8080/admin/realms/master/roles/user-creator/permissions" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "user:create",
    "description": "Permission to create users",
    "scopes": ["user:create"]
  }'
```

## Method 3: Using REST API Directly

### Step 1: Get Admin Access Token

```bash
# Get access token
ACCESS_TOKEN=$(curl -X POST "http://localhost:8080/realms/master/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "username=admin" \
  -d "password=admin123" \
  -d "grant_type=password" \
  -d "client_id=admin-cli" | jq -r '.access_token')
```

### Step 2: Create the Role

```bash
# Create user-creator role
curl -X POST "http://localhost:8080/admin/realms/master/roles" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "user-creator",
    "description": "Role with permission to create users only"
  }'
```

### Step 3: Assign Role to User

```bash
# Get user ID (replace 'username' with actual username)
USER_ID=$(curl -X GET "http://localhost:8080/admin/realms/master/users?username=username" \
  -H "Authorization: Bearer $ACCESS_TOKEN" | jq -r '.[0].id')

# Assign role to user
curl -X POST "http://localhost:8080/admin/realms/master/users/$USER_ID/role-mappings/realm" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '[{
    "id": "ROLE_ID",
    "name": "user-creator"
  }]'
```

## Testing the Role

### Step 1: Test User Creation

1. **Log in as the user with the `user-creator` role**
2. **Try to create a new user**:
   - Go to "Users" → "Add user"
   - Fill in the required fields
   - Click "Save"

3. **Verify permissions**:
   - ✅ Should be able to create users
   - ❌ Should NOT be able to delete users
   - ❌ Should NOT be able to modify existing users
   - ❌ Should NOT be able to access other admin functions

### Step 2: Test Permission Restrictions

Try to access restricted areas:
- **Realm settings**: Should be denied
- **Clients management**: Should be denied
- **User deletion**: Should be denied
- **Role management**: Should be denied

## Additional Security Considerations

### 1. Principle of Least Privilege

The `user-creator` role follows the principle of least privilege by only granting:
- `user:create` - Create new users
- `user:view` - View user details (minimal for verification)

### 2. Audit Logging

Enable audit logging to track user creation activities:
1. Go to "Realm settings" → "Events"
2. Enable "Admin events"
3. Enable "User events"
4. Set appropriate retention period

### 3. Role-Based Access Control (RBAC)

Consider creating additional specialized roles:
- `user-viewer` - Only view users
- `user-manager` - Full user management
- `user-deleter` - Only delete users

### 4. Regular Permission Reviews

Periodically review and audit:
- Who has the `user-creator` role
- What users have been created
- Whether permissions are still appropriate

## Troubleshooting

### Common Issues

1. **"Insufficient privileges" error**:
   - Ensure fine-grained permissions are enabled
   - Check that the role has the correct permissions assigned
   - Verify the user has the role assigned

2. **Cannot see "Users" menu**:
   - The role needs `user:view` permission
   - Check realm settings for menu visibility

3. **Role not appearing in assignments**:
   - Ensure the role was created in the correct realm
   - Check for typos in role name

### Debugging Steps

1. **Check user's effective roles**:
   - Go to "Users" → Select user → "Role mapping" tab
   - Verify the role is assigned

2. **Check role permissions**:
   - Go to "Roles" → Select role → "Permissions" tab
   - Verify correct permissions are enabled

3. **Check realm settings**:
   - Go to "Realm settings" → "Security defenses"
   - Ensure fine-grained permissions are enabled

## Production Considerations

### 1. Environment Variables

For production, use environment variables instead of hardcoded credentials:

```bash
export KEYCLOAK_ADMIN_USER=your-admin-user
export KEYCLOAK_ADMIN_PASSWORD=your-secure-password
export KEYCLOAK_URL=https://your-keycloak-domain.com
```

### 2. SSL/TLS

Always use HTTPS in production:
- Update `KC_HOSTNAME_STRICT_HTTPS=true`
- Configure proper SSL certificates
- Use secure SMTP settings

### 3. Database Security

- Use strong database passwords
- Enable database encryption
- Regular database backups
- Network security for database access

### 4. Monitoring and Alerting

Set up monitoring for:
- Failed login attempts
- Unusual user creation patterns
- Permission escalation attempts
- System resource usage

## Summary

This guide provides three methods to create a role with only user creation permissions:

1. **UI Method**: Easiest for beginners, visual interface
2. **CLI Method**: Good for automation and scripting
3. **API Method**: Most flexible for custom integrations

The key is to enable fine-grained permissions and only grant the specific `user:create` and `user:view` permissions while avoiding broader administrative privileges.

Remember to test thoroughly and follow security best practices in production environments.

