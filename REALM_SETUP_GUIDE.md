# Keycloak Foodi Realm Setup Guide

## Overview

This guide walks you through creating and configuring a dedicated "foodi" realm in Keycloak for the Foodi application. The foodi realm will be used for all Foodi user management, while the master realm remains for Keycloak administration.

---

## Prerequisites

- Docker and Docker Compose installed
- Keycloak and PostgreSQL running (via `docker-compose up`)
- Access to Keycloak Admin Console at http://localhost:8080

---

## Step 1: Access Keycloak Admin Console

1. Open your browser and navigate to: **http://localhost:8080**
2. Click **"Administration Console"**
3. Login with credentials:
   - **Username**: `admin`
   - **Password**: `admin123`

---

## Step 2: Create the Foodi Realm

1. In the top-left corner, hover over **"Master"** dropdown
2. Click **"Create Realm"**
3. Fill in the realm details:
   - **Realm name**: `foodi`
   - **Enabled**: ON (toggle should be blue)
4. Click **"Create"**

You should now see "Foodi" in the realm selector.

---

## Step 3: Configure Realm Settings

### General Settings

1. Navigate to **Realm Settings** (left sidebar)
2. Configure the **General** tab:
   - **User-managed access**: OFF
   - **Endpoints**: Note the OpenID Endpoint Configuration URL for reference

### Login Settings

1. Click the **Login** tab
2. Configure these settings:
   - **User registration**: ON (if you want users to register via Keycloak)
   - **Forgot password**: ON
   - **Remember me**: ON
   - **Email as username**: OFF (we use separate username and email)
   - **Login with email**: ON

### Email Settings (Optional)

1. Click the **Email** tab
2. Configure SMTP settings:
   - **Host**: `mailhog` (for development)
   - **Port**: `1025`
   - **From**: `noreply@foodi.local`
   - **Enable SSL**: OFF
   - **Enable StartTLS**: OFF

### Tokens

1. Click the **Tokens** tab
2. Configure token lifetimes:
   - **Access Token Lifespan**: 5 minutes (default)
   - **SSO Session Idle**: 30 minutes
   - **SSO Session Max**: 10 hours
   - **Refresh Token Max Reuse**: 0

3. Click **"Save"**

---

## Step 4: Configure Identity Provider (Login with Foodi)

This allows Keycloak users to authenticate via the Foodi app.

1. Navigate to **Identity Providers** (left sidebar)
2. Click **"Add provider"** dropdown
3. Select **"OpenID Connect v1.0"**

### Provider Configuration

Fill in these details:

| Field | Value |
|-------|-------|
| **Alias** | `foodi` |
| **Display name** | `Login with Foodi` |
| **Enabled** | ON |
| **Store Tokens** | ON |
| **Trust Email** | ON |
| **Authorization URL** | `http://localhost:5000/connect/authorize` |
| **Token URL** | `http://foodi-app:5000/connect/token` |
| **Logout URL** | `http://localhost:5000/connect/logout` |
| **User Info URL** | `http://foodi-app:5000/connect/userinfo` |
| **Client Authentication** | Client secret sent as post |
| **Client ID** | `keycloak-client` |
| **Client Secret** | `foodi-secret-key-2024` |
| **Default Scopes** | `openid profile email` |
| **Validate Signatures** | OFF (development only) |
| **Use JWKS URL** | OFF |

**Important Notes:**
- Use `http://localhost:5000` for browser-accessible URLs (Authorization, Logout)
- Use `http://foodi-app:5000` for server-to-server communication (Token, UserInfo)
- In production, enable signature validation and use HTTPS

4. Click **"Add"**

### Configure Mappers

After creating the identity provider, configure attribute mappers:

1. Click on the **"Mappers"** tab
2. Click **"Add mapper"**

**Email Mapper:**
- Name: `email`
- Sync Mode Override: `inherit`
- Mapper Type: `Attribute Importer`
- Claim: `email`
- User Attribute Name: `email`

**First Name Mapper:**
- Name: `firstName`
- Mapper Type: `Attribute Importer`
- Claim: `given_name`
- User Attribute Name: `firstName`

**Last Name Mapper:**
- Name: `lastName`
- Mapper Type: `Attribute Importer`
- Claim: `family_name`
- User Attribute Name: `lastName`

**Username Mapper:**
- Name: `username`
- Mapper Type: `Username Template Importer`
- Template: `${CLAIM.preferred_username}`
- Target: `BROKER_USERNAME`

---

## Step 5: Verify Configuration

### Test the Identity Provider

1. Open a new incognito/private browser window
2. Navigate to: **http://localhost:8080/realms/foodi/account**
3. You should see a **"Login with Foodi"** button
4. Click it and verify you're redirected to Foodi's login page

---

## Step 6: Configure Foodi Application

The Foodi application is already configured to use the foodi realm (via appsettings.json):

```json
{
  "Keycloak": {
    "BaseUrl": "http://keycloak:8080",
    "Realm": "foodi",
    "AdminUsername": "admin",
    "AdminPassword": "admin123"
  }
}
```

**Note**: The admin credentials are for the master realm, which is used to manage users in the foodi realm via the Admin REST API.

---

## Step 7: Test User Synchronization

### Create a User in Foodi

1. Navigate to: **http://localhost:5000**
2. Click **"Sign Up"**
3. Register a new user:
   - Username: `testuser`
   - Email: `test@foodi.local`
   - Password: `password123`
   - First Name: `Test`
   - Last Name: `User`
4. Click **"Sign Up"**

### Verify User in Keycloak

1. Go to Keycloak Admin Console
2. Switch to the **foodi** realm (top-left dropdown)
3. Navigate to **Users** (left sidebar)
4. Click **"View all users"**
5. You should see `testuser` in the list

### Test SSO Flow

1. Open a new incognito window
2. Go to: **http://localhost:8080/realms/foodi/account**
3. Click **"Login with Foodi"**
4. Login with the Foodi credentials:
   - Username: `testuser`
   - Password: `password123`
5. You should be redirected back to Keycloak and logged in

---

## Realm Administration

### Managing Users

Users created in Foodi are automatically synced to the foodi realm. To manage them:

1. Switch to the **foodi** realm
2. Navigate to **Users**
3. Select a user to view/edit details
4. You can:
   - View user details
   - Reset passwords
   - Enable/disable users
   - Assign roles
   - View sessions

### Roles and Groups (Optional)

To implement role-based access control:

1. Navigate to **Realm roles**
2. Create roles:
   - `customer` (default for all users)
   - `admin` (for administrators)
   - `delivery` (for delivery personnel)

3. Assign roles to users:
   - Go to **Users** → Select user → **Role mapping**
   - Assign appropriate roles

---

## Troubleshooting

### Issue: "Invalid redirect URI" when testing SSO

**Solution:**
1. Check the Identity Provider redirect URIs
2. Ensure the pattern matches: `http://localhost:8080/realms/foodi/broker/foodi/endpoint`
3. The alias in the URL must match your identity provider alias

### Issue: Users not syncing from Foodi to Keycloak

**Solution:**
1. Check Foodi application logs: `docker-compose logs -f foodi-app`
2. Verify Keycloak is running: `docker-compose ps`
3. Ensure the realm name in `appsettings.json` is `foodi`
4. Check that the admin credentials are correct

### Issue: "Login with Foodi" button not appearing

**Solution:**
1. Ensure you're accessing the foodi realm, not master
2. Check that the identity provider is enabled
3. Clear browser cache and try again

### Issue: Token/UserInfo endpoint errors

**Solution:**
1. Ensure you're using the correct hostnames:
   - Browser URLs: `http://localhost:5000`
   - Server-to-server: `http://foodi-app:5000`
2. Check that the Foodi app is running and healthy

---

## Production Considerations

When deploying to production:

1. **Enable HTTPS**: Use SSL/TLS certificates for all endpoints
2. **Validate Signatures**: Enable signature validation in identity provider
3. **Strong Secrets**: Change default passwords and client secrets
4. **Email Configuration**: Replace MailHog with a real SMTP server
5. **Backup Strategy**: Implement regular backups of the PostgreSQL database
6. **Monitoring**: Set up logging and monitoring for Keycloak
7. **Separate Admin Realm**: Create a dedicated admin user in the foodi realm instead of using master realm credentials

---

## Realm Import/Export (Advanced)

### Export Realm Configuration

To backup your realm configuration:

```bash
docker exec -it keycloak-server /opt/keycloak/bin/kc.sh export \
  --dir /tmp/export \
  --realm foodi \
  --users realm_file
```

### Import Realm Configuration

To restore or clone realm configuration:

```bash
docker exec -it keycloak-server /opt/keycloak/bin/kc.sh import \
  --dir /tmp/export \
  --override false
```

---

## Next Steps

1. ✅ Foodi realm created and configured
2. ✅ Identity provider set up for SSO
3. ✅ User synchronization tested
4. 🔄 Configure roles and permissions (optional)
5. 🔄 Set up multi-factor authentication (optional)
6. 🔄 Customize realm login theme (optional)

---

## Summary

You now have:
- A dedicated **foodi** realm for the Foodi application
- Automatic user synchronization from Foodi to Keycloak
- **"Login with Foodi"** SSO capability in Keycloak
- Proper separation of concerns (master realm for admin, foodi realm for users)

The Foodi app will create all new users in the foodi realm, and users can authenticate to other Keycloak clients using their Foodi credentials.

---

## Support

For issues or questions:
- Check Keycloak logs: `docker-compose logs -f keycloak`
- Check Foodi logs: `docker-compose logs -f foodi-app`
- Refer to the main [SSO_SETUP_GUIDE.md](SSO_SETUP_GUIDE.md)
- Keycloak Documentation: https://www.keycloak.org/documentation

