# 🚀 Quick Start - Dynamic Keycloak Roles Testing

## ✅ System Status

**All systems are ready for testing!**

- ✅ Docker containers running
- ✅ Foodi app rebuilt with new code
- ✅ Keycloak demo roles created (agent, lead, admin, head)
- ✅ Database migration applied (KeycloakRoles column)

---

## 🌐 Access URLs

| Service | URL | Login Credentials |
|---------|-----|-------------------|
| **Foodi App** | http://localhost:5000 | Register new account |
| **Keycloak Admin** | http://localhost:8080 | `admin` / `admin123` |
| **MailHog** | http://localhost:8025 | (No login required) |

---

## 🎯 Quick Test Steps

### 1. Register a New User (2 minutes)

**URL:** http://localhost:5000/Account/Register

**Fill in:**
- Name: `Test User`
- Username: `testuser`
- Email: `test@example.com`
- Password: `Test123!`

**Select Foodi Role:**
- Choose: `Lead - Team management and advanced features`

**Select Keycloak Roles:** (Check multiple)
- ☑ `agent`
- ☑ `lead`

**Click:** "Create Account"

**Expected:** ✅ Success message + Account created

---

### 2. View Profile

**URL:** http://localhost:5000/Account/Profile

**Expected to see:**

📦 **Foodi Role Box** (Blue)
- Shows: "Lead"
- Description about app permissions

📦 **Keycloak Roles Box** (Green)
- Shows badges: `agent`, `lead`
- Description about Keycloak sync

---

### 3. Create Admin/Head User for Testing

You need a user with Head role to manage other users.

**Option A - Direct Database (Quick)**
```bash
# Access Foodi container
docker exec -it foodi-app sqlite3 /app/data/foodi.db

# Update a user to Head role
UPDATE Users SET Roles = 'Head' WHERE Username = 'testuser';
.quit
```

**Option B - Register Another User**
- Register normally
- Select `Head` as Foodi Role
- Use this account for admin functions

---

### 4. Test Admin Panel (Head Role Required)

**Login as Head user, then visit:**

**URL:** http://localhost:5000/Admin/Users

**Expected:**
- ✅ Table showing all users
- ✅ Column "Foodi Role" with colored badges
- ✅ Column "Keycloak Roles" with blue badges
- ✅ "View" button for each user

**Click "View" on a user:**
- ✅ See Foodi Role section
- ✅ See Keycloak Roles section  
- ✅ Management forms (Head only)

---

### 5. Update User Roles

**On User Details page (Head only):**

**Change Foodi Role:**
- Select different role from dropdown
- Click "Update Foodi Role"
- ✅ Role updates immediately

**Change Keycloak Roles:**
- Check/uncheck role checkboxes
- Click "Update Keycloak Roles"
- ✅ Roles sync to Keycloak

---

### 6. Verify in Keycloak

**URL:** http://localhost:8080  
**Login:** `admin` / `admin123`

**Steps:**
1. Select `foodi` realm (top-left dropdown)
2. Go to **Roles** → **Realm roles**
3. Verify 4 roles exist: `agent`, `lead`, `admin`, `head`
4. Go to **Users** → Find your test user
5. Click on user → **Role mappings** tab
6. ✅ Verify assigned roles match what you set in Foodi

---

## 🎨 UI Features to Test

### Registration Page
- [ ] Two distinct sections: "Foodi Role" and "Keycloak Roles"
- [ ] Foodi Role = dropdown (single select)
- [ ] Keycloak Roles = checkboxes (multi-select)
- [ ] Clear descriptions for each section
- [ ] Graceful handling if Keycloak is down

### Admin Users List
- [ ] Table shows both role types
- [ ] Foodi roles have colored badges (Head=red, Admin=blue, Lead=purple, Agent=gray)
- [ ] Keycloak roles have blue badges
- [ ] Empty Keycloak roles show "—"

### User Details Page
- [ ] Separate sections for Foodi and Keycloak roles
- [ ] Role management forms (Head only)
- [ ] Independent update buttons
- [ ] Success/error messages

### User Profile
- [ ] Two colored info boxes
- [ ] Foodi role in blue box
- [ ] Keycloak roles in green box
- [ ] Read-only display

---

## 🔧 Troubleshooting

### "No Keycloak roles available"
**Solution:**
```bash
cd /home/jewel/workspace/keycloak
./setup-keycloak-roles.sh
```

### Foodi app not responding
**Solution:**
```bash
docker compose restart foodi-app
# Wait 10-15 seconds
```

### Need to rebuild after code changes
**Solution:**
```bash
docker compose up -d --build foodi-app
```

### Check logs
```bash
# Foodi logs
docker compose logs -f foodi-app

# Keycloak logs
docker compose logs -f keycloak
```

---

## 📊 Test Coverage Checklist

- [ ] Register user with both role types
- [ ] Register user with only Foodi role (no Keycloak roles)
- [ ] View user profile (both role types displayed)
- [ ] Admin: View users list
- [ ] Admin: View user details
- [ ] Admin: Update Foodi role
- [ ] Admin: Update Keycloak roles
- [ ] Verify roles in Keycloak Admin UI
- [ ] Test with Keycloak unavailable (graceful degradation)
- [ ] Create new role in Keycloak and verify it appears in Foodi

---

## 🎉 Success Criteria

✅ Dual role system working (Foodi + Keycloak)  
✅ Roles fetched dynamically from Keycloak  
✅ Clear visual separation between role types  
✅ Independent management of both role types  
✅ Roles properly synced to Keycloak  
✅ Graceful handling of Keycloak unavailability  
✅ User-friendly UI with clear labels  

---

## 📝 Quick Commands

```bash
# View running containers
docker compose ps

# Restart Foodi
docker compose restart foodi-app

# Rebuild Foodi
docker compose up -d --build foodi-app

# View logs
docker compose logs -f foodi-app

# Stop all
docker compose down

# Start all
docker compose up -d
```

---

**For detailed testing guide, see:** `DYNAMIC_ROLES_TESTING_GUIDE.md`

**Happy Testing! 🚀**

