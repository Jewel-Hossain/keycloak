# 🚀 Quick Start - Foodi + Keycloak SSO

## One-Command Setup

```bash
docker-compose up --build
```

## What You Get

After running the command above, you'll have:

1. **Foodi App**: http://localhost:5000 - Food delivery app with SSO
2. **Keycloak**: http://localhost:8080 - Identity management (admin/admin123)
3. **MailHog**: http://localhost:8025 - Email testing interface

## 5-Minute Test

### Step 1: Create a User in Foodi (2 min)

1. Open http://localhost:5000
2. Click **"Sign Up"**
3. Fill the form:
   ```
   Username: testuser
   Email: test@example.com
   Password: password123
   First Name: Test
   Last Name: User
   ```
4. Click **"Create Account"**
5. See success message: **"Account created successfully and synced with Keycloak!"**

### Step 2: Verify Sync (1 min)

1. Open http://localhost:8080
2. Login: `admin` / `admin123`
3. Click **"Users"** → **"View all users"**
4. See **testuser** in the list ✅
5. This user was auto-created by Foodi!

### Step 3: Configure "Login with Foodi" (2 min)

1. In Keycloak Admin, click **"Identity Providers"**
2. Click **"Add provider"** → **"OpenID Connect v1.0"**
3. Fill in:
   - **Alias**: `foodi`
   - **Display name**: `Login with Foodi`
   - **Authorization URL**: `http://localhost:5000/connect/authorize`
   - **Token URL**: `http://foodi-app:5000/connect/token`
   - **User Info URL**: `http://foodi-app:5000/connect/userinfo`
   - **Client ID**: `keycloak-client`
   - **Client Secret**: `foodi-secret-key-2024`
4. Click **"Add"**

### Step 4: Test SSO (30 sec)

1. Logout from Keycloak
2. Go to http://localhost:8080
3. See **"Login with Foodi"** button 🎉
4. Click it → Login with `testuser` / `password123`
5. You're now logged into Keycloak via Foodi!

## 🎯 What Just Happened?

✅ **User sync**: Foodi → Keycloak  
✅ **SSO**: Keycloak authenticates via Foodi  
✅ **Full integration**: One account, two systems  

## Next Steps

👉 Read the full guide: [SSO_SETUP_GUIDE.md](SSO_SETUP_GUIDE.md)

## Stop Services

```bash
docker-compose down
```

## Reset Everything

```bash
docker-compose down -v
docker-compose up --build
```

## 🆘 Issues?

Check logs:
```bash
docker-compose logs -f
```

See [SSO_SETUP_GUIDE.md](SSO_SETUP_GUIDE.md) for troubleshooting.

