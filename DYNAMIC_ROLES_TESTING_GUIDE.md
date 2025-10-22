# Dynamic Keycloak Roles - UI Testing Guide

## Implementation Summary

✅ **Completed Features:**
- Dual role system: Foodi roles (for app permissions) + Keycloak roles (synced to Keycloak)
- Dynamic role fetching from Keycloak Admin API
- Role selection during registration
- Admin panel role management for both role types
- User profile displays both role types
- Database migration applied (`KeycloakRoles` column added)

---

## Prerequisites

### 1. Start Keycloak and Foodi App

```bash
# Start Docker containers
cd /home/jewel/workspace/keycloak
docker-compose up -d

# Wait ~30 seconds for Keycloak to be ready
# Then run the role setup script
./setup-keycloak-roles.sh
```

### 2. Verify Keycloak Roles Created

- Open: http://localhost:8080
- Login: `admin` / `admin`
- Select `foodi` realm (top-left dropdown)
- Navigate to: **Roles** → **Realm roles**
- Verify these 4 roles exist:
  - `agent` - Ticket and workspace permissions
  - `lead` - All team tickets and workspace permissions
  - `admin` - Administrative features and reporting
  - `head` - Full system access and user management

---

## Test Scenarios

### Test 1: User Registration with Both Role Types

**Steps:**
1. Open: http://localhost:5000/Account/Register
2. Fill in user details:
   - First Name: `John`
   - Last Name: `Doe`
   - Username: `johndoe`
   - Email: `john@example.com`
   - Password: `Password123`

3. **Select Foodi Role:**
   - In the "🎭 Foodi Role" section
   - Select: `Lead - Team management and advanced features`

4. **Select Keycloak Roles:**
   - In the "🔐 Keycloak Roles" section
   - Check: `agent` and `lead` (multi-select)

5. Click "Create Account"

**Expected Result:**
- ✅ Success message: "Account created successfully as Lead and synced with Keycloak!"
- ✅ User has Foodi role: `Lead`
- ✅ User has Keycloak roles: `agent, lead`

---

### Test 2: View User in Admin Panel

**Steps:**
1. Login as admin (or create a Head role user first)
2. Navigate to: http://localhost:5000/Admin/Users
3. Find the user `johndoe` in the list

**Expected Result:**
- ✅ Table shows:
  - **Foodi Role** column: Badge showing "Lead"
  - **Keycloak Roles** column: Two small badges showing "agent" and "lead"
  - Status: Active
  - Synced: ✓ Yes

4. Click "View" button for the user

---

### Test 3: View User Details

**Expected Result on User Details page:**

**Section 1: Foodi Role & Status**
- Foodi Role: Purple badge "Lead"
- Status: Active (green)
- Synced to Keycloak: ✓ Yes
- Keycloak User ID: (UUID shown)

**Section 2: Keycloak Roles**
- Current Keycloak Roles: Blue badges showing "agent" and "lead"

---

### Test 4: Update Foodi Role (Head User Only)

**Steps:**
1. Still on User Details page
2. Scroll to "Management Actions"
3. Under "Change Foodi Role":
   - Select: `Admin - Administrative features and reporting`
   - Click "Update Foodi Role"

**Expected Result:**
- ✅ Success message
- ✅ Foodi Role changes to "Administrator" (blue badge)
- ✅ Keycloak roles remain unchanged

---

### Test 5: Update Keycloak Roles (Head User Only)

**Steps:**
1. On User Details page
2. Under "Manage Keycloak Roles":
   - Uncheck: `agent`
   - Check: `admin` and `head`
   - Click "Update Keycloak Roles"

**Expected Result:**
- ✅ Success message: "Keycloak roles updated successfully"
- ✅ Keycloak Roles section shows: `admin, head, lead` (3 badges)
- ✅ Roles synced to Keycloak server

---

### Test 6: Verify in User Profile

**Steps:**
1. Logout from admin
2. Login as `johndoe` / `Password123`
3. Navigate to: http://localhost:5000/Account/Profile

**Expected Result:**

Two colored info boxes:

**🎭 Foodi Role:**
- Shows: "Administrator" (blue box)
- Description: "Your Foodi role determines which features and menu items you can access in the application."

**🔐 Keycloak Roles:**
- Shows badges: `admin`, `head`, `lead` (green box)
- Description: "These roles are synced to Keycloak and may be used by other integrated applications."

---

### Test 7: Verify Roles in Keycloak

**Steps:**
1. Open: http://localhost:8080
2. Login: `admin` / `admin`
3. Select `foodi` realm
4. Navigate to: **Users** → Find `johndoe` → **Role mappings** tab

**Expected Result:**
- ✅ Assigned roles show: `admin`, `head`, `lead`
- ✅ Roles match what was set in Foodi

---

### Test 8: Register New User Without Keycloak Roles

**Steps:**
1. Register a new user
2. Select Foodi Role: `Agent`
3. Don't check any Keycloak roles (leave all unchecked)
4. Create account

**Expected Result:**
- ✅ User created successfully
- ✅ Foodi Role: Agent
- ✅ Keycloak Roles: None (shows "—" or "No Keycloak roles assigned")

---

### Test 9: Dynamic Role Loading

**Steps:**
1. In Keycloak Admin UI, create a NEW role:
   - Name: `customer`
   - Description: `Customer role for external users`

2. Wait 5 minutes (cache expiry) OR restart Foodi app

3. Go to registration page or admin user details page

**Expected Result:**
- ✅ New `customer` role appears in the Keycloak Roles checklist
- ✅ Can be selected and assigned to users

---

### Test 10: Keycloak Not Available

**Steps:**
1. Stop Keycloak: `docker-compose stop keycloak-server`
2. Try to register a new user

**Expected Result:**
- ✅ Registration still works
- ✅ User created in Foodi
- ⚠️ Warning message: "Account created but could not sync with Keycloak"
- ✅ Keycloak roles section shows: "No Keycloak roles available"

---

## Quick Start Testing Script

```bash
# 1. Start everything
cd /home/jewel/workspace/keycloak
docker-compose up -d
sleep 30

# 2. Create Keycloak demo roles
./setup-keycloak-roles.sh

# 3. Open browser
echo "Open these URLs:"
echo "- Foodi: http://localhost:5000"
echo "- Keycloak: http://localhost:8080"
echo ""
echo "Test accounts:"
echo "- Create new users via registration"
echo "- Or use existing admin account if available"
```

---

## Key Features to Verify

### ✅ Foodi Roles (Controls App Permissions)
- Agent (default)
- Lead
- Admin
- Head

### ✅ Keycloak Roles (Metadata, Synced to Keycloak)
- Fetched dynamically from Keycloak
- Multi-select during registration
- Multi-select in admin panel
- Displayed separately from Foodi roles
- Independent from Foodi roles

### ✅ User Interface
- Clear visual separation between role types
- Foodi roles: Dropdown (single select)
- Keycloak roles: Checkboxes (multi-select)
- Color-coded badges
- Responsive design

---

## Troubleshooting

### Issue: "No Keycloak roles available"
**Solution:** 
1. Verify Keycloak is running: `docker-compose ps`
2. Run setup script: `./setup-keycloak-roles.sh`
3. Check Keycloak realm is `foodi` (not `master`)

### Issue: Roles not syncing to Keycloak
**Solution:**
1. Check Keycloak admin credentials in `appsettings.json`
2. Verify user has `KeycloakUserId` populated
3. Check logs for sync errors

### Issue: Cache not refreshing
**Solution:**
- Wait 5 minutes for cache to expire
- Or restart Foodi app: `docker-compose restart foodi-app`

---

## API Endpoints Used

- `GET /admin/realms/{realm}/roles` - Fetch all roles
- `GET /admin/realms/{realm}/users/{userId}/role-mappings/realm` - Get user roles
- `POST /admin/realms/{realm}/users/{userId}/role-mappings/realm` - Assign roles
- `DELETE /admin/realms/{realm}/users/{userId}/role-mappings/realm` - Remove roles

---

## Success Criteria

✅ Users can select both Foodi and Keycloak roles during registration
✅ Admin can view both role types in user listing
✅ Admin can update both role types independently
✅ User profile displays both role types
✅ Roles dynamically fetched from Keycloak
✅ Roles cached for performance (5 min TTL)
✅ Graceful handling when Keycloak is unavailable
✅ Clear visual distinction between role types
✅ Roles properly synced to Keycloak server

---

**Happy Testing! 🚀**

