# 📍 Where is the Foodi Realm?

## ✅ Location of Foodi Realm in Keycloak

---

## Quick Answer

The **Foodi realm** is located in the Keycloak admin console:

**URL**: `http://localhost:8080/admin/master/console/#/foodi`

---

## How to Access the Foodi Realm

### Method 1: Via Realm Dropdown

1. **Go to Keycloak Admin Console**:
   ```
   http://localhost:8080/admin
   ```

2. **Login with admin credentials**:
   - Username: `admin`
   - Password: `admin`

3. **Look at the top-left navigation panel**
   - You'll see a button showing the current realm (e.g., "master")
   - Click on this button to open the **realm dropdown**

4. **Select "foodi"** from the dropdown
   - ✅ **foodi** ← This is your Foodi realm
   - master ← Admin/master realm

5. **You're now in the Foodi realm!**
   - URL will change to: `http://localhost:8080/admin/master/console/#/foodi`

---

### Method 2: Direct URL

Simply navigate to:
```
http://localhost:8080/admin/master/console/#/foodi
```

This takes you directly to the foodi realm (after login).

---

## What's in the Foodi Realm?

### Left Navigation Menu:

#### Manage Section:
- **Clients** - OAuth/OIDC clients
- **Client scopes** - Scope definitions
- **Realm roles** - Roles: head, admin, lead, agent
- **Users** - Users synced from Foodi app
- **Groups** - User groups
- **Sessions** - Active user sessions
- **Events** - Login/logout events

#### Configure Section:
- **Realm settings** - General realm configuration
- **Authentication** - Authentication flows
- **Identity providers** - **foodi** identity provider (Login with Foodi)
- **User federation** - LDAP/Active Directory integration

---

## Current Configuration (Created in this session)

### 1. Foodi Realm ✅
- **Name**: `foodi`
- **Status**: Enabled
- **Created**: October 22, 2025

### 2. Foodi Identity Provider ✅
- **Name**: `foodi`
- **Type**: OpenID Connect v1.0
- **Status**: Enabled
- **Configuration**:
  - Alias: `foodi`
  - Authorization URL: `http://localhost:5000/connect/authorize`
  - Token URL: `http://localhost:5000/connect/token`
  - Client ID: `keycloak-client`
  - Client Secret: `foodi-secret-key-2024`
  - Redirect URI: `http://localhost:8080/realms/foodi/broker/foodi/endpoint`

### 3. Realm Roles (Auto-created by Foodi app)
When users register, these roles are auto-created:
- `head` - Full system access
- `admin` - Administrative features
- `lead` - Team management
- `agent` - Basic features (default)

---

## Visual Guide

### Realm Dropdown Location:
```
┌──────────────────────────────────────────────────────┐
│  Keycloak Admin Console                              │
├──────────────────────────────────────────────────────┤
│  [≡]  [Keycloak Logo]      [master ▼]    [Help] [admin] │
│         ↑                       ↑                      │
│     Menu Button           REALM DROPDOWN               │
│                          (Click here!)                 │
└──────────────────────────────────────────────────────┘
```

### Navigation to Identity Provider:
```
Foodi Realm
  └── Configure
       └── Identity providers
            └── foodi (OpenID Connect)
                 └── Settings (Authorization URL, Token URL, etc.)
```

---

## URLs for Different Realms

### Master Realm (Admin/Management):
- Admin Console: `http://localhost:8080/admin/master/console/#/master`
- Account: `http://localhost:8080/realms/master/account`

### Foodi Realm (Application):
- Admin Console: `http://localhost:8080/admin/master/console/#/foodi`
- Account: `http://localhost:8080/realms/foodi/account`
- Login: `http://localhost:8080/realms/foodi/protocol/openid-connect/auth`

---

## How to Verify Realm is Working

### 1. Check Realm Exists
```bash
# Via API
curl http://localhost:8080/realms/foodi/.well-known/openid-configuration
```

Expected: JSON response with foodi realm configuration

### 2. Check Identity Provider
1. Go to: `http://localhost:8080/realms/foodi/account`
2. Click "Sign in"
3. ✅ Should see **"foodi"** button under "Or sign in with"

### 3. Check Users
1. In Keycloak Admin Console, select "foodi" realm
2. Click "Users" in left navigation
3. ✅ Should see users synced from Foodi app

### 4. Check Realm Roles
1. In Keycloak Admin Console, select "foodi" realm
2. Click "Realm roles" in left navigation
3. ✅ Should see: head, admin, lead, agent (auto-created)

---

## Troubleshooting

### Can't Find Foodi Realm?

**Check 1**: Verify realm exists
```bash
curl http://localhost:8080/realms/foodi
```

**Check 2**: Refresh Keycloak admin page
- Press F5 or Ctrl+R

**Check 3**: Verify logged into Keycloak
- Make sure you're logged in as admin user
- Login at: http://localhost:8080/admin

### Foodi Realm Shows as "Not Found"

**Solution**: The realm was just created. It should exist now!

1. Logout of Keycloak admin
2. Login again
3. Click realm dropdown
4. Select "foodi"

---

## Summary

### Where is the Foodi Realm?

**Location**: Top-left realm dropdown in Keycloak admin console

**Access**:
1. Login to Keycloak admin: http://localhost:8080/admin
2. Click realm dropdown (top-left, shows current realm)
3. Select "**foodi**"
4. ✅ You're now managing the Foodi realm!

**Direct URL**:
```
http://localhost:8080/admin/master/console/#/foodi
```

---

## What We Just Created

1. ✅ **foodi** realm
2. ✅ **foodi** identity provider (OpenID Connect)
3. ✅ Configuration for "Login with Foodi" flow

---

## Next Steps

1. **Test "Login with Foodi"**:
   - Go to: http://localhost:8080/realms/foodi/account
   - Click "Sign in"
   - Click "foodi" button
   - Login with Foodi credentials

2. **Register users with roles**:
   - Register in Foodi app with different roles
   - Verify roles appear in Keycloak

3. **Explore Foodi realm**:
   - Check Users (should show synced users)
   - Check Realm roles (head, admin, lead, agent)
   - Configure additional settings as needed

---

**Foodi Realm Status**: ✅ **Created and Ready**  
**Identity Provider**: ✅ **Configured**  
**Location**: Realm dropdown → foodi


