# Foodi + Keycloak SSO Integration Guide

## 🎯 Overview

This project demonstrates a **complete bidirectional SSO integration** between:
- **Foodi**: A .NET 8 food delivery application
- **Keycloak**: An open-source identity and access management solution

### What's Implemented:

✅ **User Registration in Foodi** → Automatically syncs to Keycloak  
✅ **Foodi as Identity Provider** → Keycloak can authenticate users via "Login with Foodi"  
✅ **OpenID Connect (OIDC)** → Industry-standard SSO protocol  
✅ **Seamless User Experience** → Single account across both systems  
✅ **Real-time User Sync** → Profile updates, password changes, and account status sync instantly  
✅ **Dedicated Foodi Realm** → Separate realm for Foodi users with proper isolation  
✅ **"Go to Keycloak" Button** → Easy access to Keycloak admin console from Foodi UI  

> **📚 New to Realm Setup?** Check out the [REALM_SETUP_GUIDE.md](REALM_SETUP_GUIDE.md) for step-by-step instructions on creating and configuring the foodi realm.

---

## 🚀 Quick Start

### Prerequisites

- Docker and Docker Compose installed
- Ports 5000, 8080, 8025 available

### Step 1: Start All Services

```bash
docker-compose up --build
```

This will start:
- **Keycloak** on http://localhost:8080
- **Foodi App** on http://localhost:5000
- **MailHog** (email testing) on http://localhost:8025
- **PostgreSQL** database (internal)

Wait for all services to be healthy (check logs for "ready" messages).

---

## 📝 Testing the SSO Flow

### Test 1: User Registration & Auto-Sync

1. **Open Foodi App**: http://localhost:5000
2. **Click "Sign Up"** and create a new account:
   - Username: `testuser`
   - Email: `test@example.com`
   - Password: `password123`
   - First Name: `Test`
   - Last Name: `User`

3. **Submit the form**

4. **Verify the sync**:
   - You should see a success message: "Account created successfully and synced with Keycloak!"
   - The user now exists in **both** Foodi and Keycloak

5. **Verify in Keycloak**:
   - Go to http://localhost:8080
   - Login: `admin` / `admin123`
   - **Important**: Switch to the **foodi** realm (dropdown in top-left corner)
   - Navigate to: **Users** → Click "View all users"
   - You should see `testuser` in the list
   - This user was automatically created by Foodi in the foodi realm!

### Test 2: Login to Foodi

1. **Logout from Foodi** (if logged in)
2. **Click "Login"**
3. **Enter credentials**:
   - Username or Email: `testuser`
   - Password: `password123`
4. **Click "Login"**
5. **Success!** You should be logged in and see the menu
6. **Notice the new "🔐 Go to Keycloak" button** in the navbar - click it to open Keycloak admin console

### Test 2.5: Profile Management & Real-time Sync

Test the new profile management features:

1. **While logged in to Foodi**, click on **"Profile"** in the navbar
2. **Update your profile**:
   - Change your first or last name
   - Update your email address
   - Click "💾 Update Profile"
3. **Verify Keycloak sync**:
   - You should see: "Profile updated successfully and synced with Keycloak!"
   - Open Keycloak admin console (use the "Go to Keycloak" button)
   - Switch to **foodi** realm and check **Users**
   - Your changes should be reflected immediately!
4. **Test password change**:
   - From the Profile page, click "🔐 Change Password"
   - Enter current password and new password
   - The password will sync to Keycloak in real-time

### Test 3: Configure "Login with Foodi" in Keycloak

This is the most exciting part - making Keycloak authenticate users via Foodi!

#### Step 3.1: Add Foodi as Identity Provider

1. **Open Keycloak Admin Console**: http://localhost:8080
2. **Login**: `admin` / `admin123`
3. **Navigate to**: **Identity Providers** (left sidebar)
4. **Click**: "Add provider" → Select **"OpenID Connect v1.0"**

#### Step 3.2: Configure the Identity Provider

Fill in these details:

| Field | Value |
|-------|-------|
| **Alias** | `foodi` |
| **Display name** | `Login with Foodi` |
| **Enabled** | ON (toggle) |
| **Authorization URL** | `http://localhost:5000/connect/authorize` |
| **Token URL** | `http://foodi-app:5000/connect/token` |
| **Logout URL** | `http://localhost:5000/connect/logout` |
| **User Info URL** | `http://foodi-app:5000/connect/userinfo` |
| **Client ID** | `keycloak-client` |
| **Client Secret** | `foodi-secret-key-2024` |
| **Default Scopes** | `openid profile email` |
| **Validate Signatures** | OFF (for dev only) |

**Important Notes:**
- Use `http://localhost:5000` for browser-accessible URLs (Authorization, Logout)
- Use `http://foodi-app:5000` for server-to-server URLs (Token, UserInfo) - this is the Docker network hostname
- In production, use HTTPS and proper certificate validation

#### Step 3.3: Configure Mappers (Optional but Recommended)

1. **In the Foodi identity provider**, go to **"Mappers"** tab
2. **Click**: "Add mapper"
3. **Create mapper for email**:
   - Name: `email`
   - Mapper Type: `Attribute Importer`
   - Claim: `email`
   - User Attribute Name: `email`
4. **Create mapper for username**:
   - Name: `username`
   - Mapper Type: `Username Template Importer`
   - Template: `${CLAIM.preferred_username}`
5. **Create mappers for first and last name** similarly

#### Step 3.4: Save Configuration

Click **"Add"** or **"Save"** to complete the setup.

### Test 4: Login to Keycloak via Foodi

Now test the complete SSO flow!

1. **Logout from Keycloak** (if logged in)
2. **Go to Keycloak login page**: http://localhost:8080
3. **You should now see**: A button that says **"Login with Foodi"** 🎉
4. **Click "Login with Foodi"**
5. **You'll be redirected to Foodi's login page**
6. **Login with your Foodi credentials**:
   - Username: `testuser`
   - Password: `password123`
7. **Authorize the request** (if prompted)
8. **You'll be redirected back to Keycloak** and logged in automatically!

**Success!** You've completed the SSO flow. The user authenticated via Foodi and Keycloak accepted it.

---

## 🔄 How It Works

### User Creation Flow

```
User fills form → Foodi creates user → Foodi syncs to Keycloak API
                                   ↓
                        User exists in both systems
```

**Code Location**: `foodi-app/Controllers/AccountController.cs` (Register action)

### SSO Authentication Flow (Keycloak → Foodi)

```
User clicks "Login with Foodi" in Keycloak
           ↓
Keycloak redirects to Foodi authorization endpoint
           ↓
User logs in to Foodi (if not already)
           ↓
Foodi generates authorization code
           ↓
Keycloak exchanges code for tokens
           ↓
Keycloak requests user info from Foodi
           ↓
User is logged into Keycloak
```

**Code Location**: `foodi-app/Controllers/AuthorizationController.cs`

---

## 🏗️ Architecture

### Services

1. **Foodi App (.NET 8)**
   - ASP.NET Core MVC
   - SQLite database (persisted in Docker volume)
   - OpenIddict (OIDC server)
   - Acts as both Service Provider and Identity Provider

2. **Keycloak**
   - Identity and Access Management
   - PostgreSQL database
   - OIDC/SAML support
   - User federation

3. **PostgreSQL**
   - Keycloak's database

4. **MailHog**
   - Development email server
   - For testing email OTP (if enabled)

### Network Diagram

```
┌─────────────────┐
│   Browser       │
│ localhost:5000  │ ← Foodi UI
│ localhost:8080  │ ← Keycloak UI
└────────┬────────┘
         │
    ┌────▼──────────────────────┐
    │  Docker Network           │
    │  (keycloak-network)       │
    │                           │
    │  ┌─────────────────┐      │
    │  │   foodi-app     │      │
    │  │   :5000         │      │
    │  └────────┬────────┘      │
    │           │               │
    │  ┌────────▼────────┐      │
    │  │   keycloak      │      │
    │  │   :8080         │      │
    │  └────────┬────────┘      │
    │           │               │
    │  ┌────────▼────────┐      │
    │  │   postgres      │      │
    │  │   :5432         │      │
    │  └─────────────────┘      │
    └───────────────────────────┘
```

---

## 🔧 Configuration Files

### Foodi App Configuration

**Location**: `foodi-app/appsettings.json`

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

**Note**: The Foodi app now uses the dedicated `foodi` realm for all user operations. The admin credentials are from the master realm, which is required for the Admin REST API.

### OpenIddict Client (Pre-configured)

The Keycloak client is automatically registered in Foodi on startup:

- **Client ID**: `keycloak-client`
- **Client Secret**: `foodi-secret-key-2024`
- **Redirect URIs**: 
  - `http://localhost:8080/realms/foodi/broker/foodi/endpoint`
  - `http://localhost:8080/realms/foodi/broker/foodi/endpoint/`
  - `http://localhost:8080/realms/master/broker/foodi/endpoint` (for master realm compatibility)
  - `http://localhost:8080/realms/master/broker/foodi/endpoint/`

**Code Location**: `foodi-app/Program.cs` (startup configuration)

---

## 📋 API Endpoints

### Foodi OIDC Endpoints

| Endpoint | Purpose |
|----------|---------|
| `/connect/authorize` | Authorization endpoint (browser) |
| `/connect/token` | Token endpoint (server-to-server) |
| `/connect/userinfo` | User information endpoint |
| `/connect/logout` | Logout endpoint |

### Keycloak Admin API

Foodi uses these endpoints to sync users:

- `POST /realms/master/protocol/openid-connect/token` - Get admin token
- `POST /admin/realms/master/users` - Create user

---

## 🐛 Troubleshooting

### Issue: "Failed to sync user to Keycloak"

**Possible Causes:**
1. Keycloak not fully started
2. Network connectivity issues
3. Invalid credentials

**Solution:**
```bash
# Check if Keycloak is healthy
docker-compose ps

# Check Foodi app logs
docker-compose logs foodi-app

# Restart services
docker-compose restart foodi-app
```

### Issue: "Login with Foodi" button not appearing

**Possible Causes:**
1. Identity Provider not configured
2. Identity Provider disabled

**Solution:**
1. Check Keycloak → Identity Providers
2. Ensure "foodi" provider is enabled
3. Clear browser cache and reload

### Issue: "Invalid redirect URI"

**Possible Causes:**
1. Redirect URI mismatch between Keycloak and Foodi

**Solution:**
1. Check the identity provider redirect URI in Keycloak
2. Ensure it matches the pattern: `http://localhost:8080/realms/master/broker/foodi/endpoint`
3. The alias in the URL must match the identity provider alias

### Issue: SSL/TLS errors in logs

**Cause:** Development certificates

**Solution:** This is expected in development. In production, use proper SSL certificates.

### Issue: Can't connect to Foodi from Keycloak

**Possible Causes:**
1. Using wrong hostname in Token URL
2. Docker network issues

**Solution:**
- Browser-accessible URLs: Use `http://localhost:5000`
- Server-to-server URLs: Use `http://foodi-app:5000` (Docker hostname)

---

## 🔐 Security Considerations

### Development vs Production

This setup is for **DEVELOPMENT ONLY**. For production:

1. **Use HTTPS**: Configure SSL/TLS certificates
2. **Strong Secrets**: Change all default passwords and secrets
3. **Validate Signatures**: Enable signature validation in Keycloak
4. **Secure Database**: Use strong database passwords
5. **Network Security**: Restrict network access
6. **SMTP Server**: Replace MailHog with real SMTP service
7. **Password Hashing**: Consider using bcrypt or Argon2 instead of SHA256

### Current Security Features

✅ Password hashing (SHA256)  
✅ HTTPS-ready architecture  
✅ CSRF protection  
✅ Secure cookie settings  
✅ Environment-based configuration  

---

## 📊 Monitoring

### Health Checks

All services have health checks configured:

```bash
# Check service health
docker-compose ps

# View logs
docker-compose logs -f foodi-app
docker-compose logs -f keycloak
```

### Accessing Logs

```bash
# Foodi app logs
docker-compose logs -f foodi-app

# Keycloak logs
docker-compose logs -f keycloak

# All logs
docker-compose logs -f
```

---

## 🧪 Testing Checklist

- [ ] Start all services successfully
- [ ] Register a new user in Foodi
- [ ] Verify user appears in Keycloak
- [ ] Login to Foodi with the new user
- [ ] Configure Foodi as identity provider in Keycloak
- [ ] See "Login with Foodi" button in Keycloak
- [ ] Successfully login to Keycloak via Foodi
- [ ] Verify user profile data is transferred correctly

---

## 🚦 Service URLs

| Service | URL | Credentials |
|---------|-----|-------------|
| **Foodi App** | http://localhost:5000 | Register new account |
| **Keycloak Admin** | http://localhost:8080 | admin / admin123 |
| **MailHog** | http://localhost:8025 | No login required |
| **PostgreSQL** | localhost:5432 | keycloak / keycloak123 |

---

## 📚 Additional Resources

### Keycloak Documentation
- [Identity Brokering](https://www.keycloak.org/docs/latest/server_admin/#_identity_broker)
- [OpenID Connect](https://www.keycloak.org/docs/latest/securing_apps/#_oidc)

### OpenIddict Documentation
- [OpenIddict GitHub](https://github.com/openiddict/openiddict-core)
- [Getting Started](https://documentation.openiddict.com/guides/index.html)

### OAuth 2.0 / OIDC
- [OAuth 2.0 Specification](https://oauth.net/2/)
- [OpenID Connect](https://openid.net/connect/)

---

## 🎉 Success!

If you've followed this guide, you now have:

✅ A working .NET 8 Foodi application with a beautiful UI  
✅ User registration that syncs to Keycloak (in foodi realm)  
✅ Real-time sync for profile updates, password changes, and account status  
✅ Dedicated foodi realm for user management  
✅ Keycloak configured to use Foodi as an identity provider  
✅ Full bidirectional SSO between both systems  
✅ "Go to Keycloak" button for easy admin access  
✅ PostgreSQL database exposed for direct access  
✅ Comprehensive test coverage (unit + integration)  
✅ Understanding of how modern SSO works  

**Congratulations!** 🎊 You've successfully implemented enterprise-grade SSO integration with real-time user synchronization!

---

## 💡 Next Steps

1. **Add More Features**: Shopping cart, order placement, payment integration
2. **Multiple Realms**: Create separate realms for different environments
3. **Role-Based Access**: Implement authorization with Keycloak roles
4. **Social Login**: Add Google, Facebook, GitHub providers to Keycloak
5. **Multi-Factor Auth**: Enable 2FA with the Email OTP provider
6. **Production Deploy**: Set up proper SSL, secrets management, and monitoring

---

## 🆘 Need Help?

**Common Commands:**

```bash
# Start services
docker-compose up -d

# Stop services
docker-compose down

# Rebuild everything
docker-compose up --build --force-recreate

# Reset everything (WARNING: deletes data)
docker-compose down -v
docker-compose up --build

# View logs
docker-compose logs -f [service-name]
```

---

## 📝 License

This is a demo project for educational purposes. Feel free to use and modify as needed.

