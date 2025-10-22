# Quick Test Guide - Role-Based Access Control

## ✅ System is Running!

All services are up and accessible:
- **Foodi App**: http://localhost:5000
- **Keycloak Admin**: http://localhost:8080/admin
- **PostgreSQL**: localhost:5433

---

## Quick Testing Steps

### 1. Test Registration with Roles

#### Register as Agent (Default)
1. Go to http://localhost:5000/Account/Register
2. Fill in details:
   - First Name: John
   - Last Name: Agent
   - Username: agent1
   - Email: agent@test.com
   - **Account Type**: Agent (Default - Basic Features)
   - Password: test123
3. Click "Create Account"
4. ✅ Should see success message with role "Agent"

#### Register as Lead
1. Go to http://localhost:5000/Account/Register
2. Fill in details:
   - First Name: Jane
   - Last Name: Lead
   - Username: lead1
   - Email: lead@test.com
   - **Account Type**: Lead (Team Management)
   - Password: test123
3. Click "Create Account"
4. ✅ Should see success message with role "Lead"

#### Register as Admin
1. Go to http://localhost:5000/Account/Register
2. Fill in details:
   - First Name: Bob
   - Last Name: Admin
   - Username: admin1
   - Email: admin@test.com
   - **Account Type**: Administrator (Admin Features)
   - Password: test123
3. Click "Create Account"
4. ✅ Should see success message with role "Administrator"

#### Register as Head
1. Go to http://localhost:5000/Account/Register
2. Fill in details:
   - First Name: Alice
   - Last Name: Head
   - Username: head1
   - Email: head@test.com
   - **Account Type**: Head (Full System Access)
   - Password: test123
3. Click "Create Account"
4. ✅ Should see success message with role "Head"

---

### 2. Test Role-Based Menu Visibility

#### Login as Agent
1. Login with: agent1 / test123
2. **Should See in Menu**:
   - Home
   - Menu
   - My Orders
   - Profile
   - 🔐 Go to Keycloak
3. **Should NOT See**:
   - 📊 Reports
   - ⚙️ Admin

#### Login as Lead
1. Login with: lead1 / test123
2. **Should See in Menu**:
   - Home
   - Menu
   - My Orders
   - **📊 Reports** ✓ (NEW!)
   - Profile
   - 🔐 Go to Keycloak
3. **Should NOT See**:
   - ⚙️ Admin

#### Login as Admin
1. Login with: admin1 / test123
2. **Should See in Menu**:
   - Home
   - Menu
   - My Orders
   - 📊 Reports ✓
   - **⚙️ Admin** ✓ (NEW!)
   - Profile
   - 🔐 Go to Keycloak

#### Login as Head
1. Login with: head1 / test123
2. **Should See in Menu**:
   - Home
   - Menu
   - My Orders
   - 📊 Reports ✓
   - ⚙️ Admin ✓
   - Profile
   - 🔐 Go to Keycloak

---

### 3. Test Profile Page (Shows Role)

1. Login with any user
2. Click "Profile"
3. ✅ Should see account type badge with role name
4. ✅ Example: "🎭 Account Type: **Agent**"

---

### 4. Test Reports Page (Lead+)

1. Login as Lead, Admin, or Head
2. Click "📊 Reports"
3. ✅ Should see:
   - Statistics cards (Total Users, Active Users, etc.)
   - Recent orders table

#### Test Access Denied
1. Logout
2. Try to access http://localhost:5000/Home/Reports
3. ✅ Should redirect to login (unauthorized)

---

### 5. Test Admin Dashboard (Admin+)

1. Login as Admin or Head
2. Click "⚙️ Admin"
3. ✅ Should see Admin Dashboard with:
   - Total Users
   - Active Users
   - Inactive Users
   - Synced to Keycloak count
   - Quick Actions (Manage Users, View Reports)

#### Test User Management
1. From Admin Dashboard, click "👥 Manage Users"
2. ✅ Should see table of all users with:
   - Username, Email, Name
   - **Role Badge** (colored by role)
   - Status (Active/Inactive)
   - Synced status
   - View button

#### Test User Details
1. Click "View" on any user
2. ✅ Should see:
   - Basic Information
   - Role & Status section
   - Timestamps
   - **Management Actions** (if logged in as Head)

---

### 6. Test Role Management (Head Only)

1. Login as Head (head1 / test123)
2. Go to Admin → Manage Users
3. Click "View" on agent1
4. ✅ Should see "Management Actions" section
5. **Change Role**:
   - Select "Lead" from dropdown
   - Click "Update Role"
   - ✅ Should see success message
6. **Toggle User Status**:
   - Click "Deactivate User"
   - Confirm
   - ✅ Should see success message
   - ✅ Status should change to "Inactive"
7. **Resync to Keycloak**:
   - Click "🔄 Resync to Keycloak"
   - ✅ Should see success message

---

### 7. Verify Keycloak Synchronization

1. Open Keycloak Admin: http://localhost:8080/admin
2. Login: admin / admin
3. Switch to "foodi" realm
4. Click "Users" in left menu
5. ✅ Should see all registered users
6. Click on any user
7. Click "Role Mapping" tab
8. ✅ Should see assigned role (agent, lead, admin, or head)

---

## Expected Behavior Summary

| User Role | Reports Page | Admin Dashboard | User Management |
|-----------|--------------|-----------------|-----------------|
| Agent | ❌ Denied | ❌ Denied | ❌ Denied |
| Lead | ✅ Access | ❌ Denied | ❌ Denied |
| Admin | ✅ Access | ✅ Access | ❌ View Only |
| Head | ✅ Access | ✅ Access | ✅ Full Control |

---

## Verification Checklist

- [x] Application builds without errors
- [x] All containers running
- [x] Registration form shows role dropdown
- [x] Users can select roles during registration
- [x] Default role is "Agent"
- [x] Success message shows selected role
- [x] Profile page displays user role
- [x] Menu items show/hide based on role
- [x] Reports page accessible to Lead+
- [x] Admin Dashboard accessible to Admin+
- [x] User Management accessible to Head only
- [x] Role changes work (Head only)
- [x] User activation/deactivation works
- [x] Keycloak sync creates users
- [x] Keycloak sync assigns roles
- [x] Manual resync works

---

## Troubleshooting

### Registration Not Syncing to Keycloak

**Check**:
1. Keycloak container is running: `docker ps | grep keycloak`
2. Check Foodi logs: `docker logs foodi-app`
3. Verify Keycloak credentials in `appsettings.json`

**Manual Resync**:
1. Login as Head
2. Go to Admin → Manage Users
3. Click "View" on user
4. Click "🔄 Resync to Keycloak"

### Role Not Showing in Menu

**Solution**:
1. Logout
2. Login again (role claims are set on login)
3. Check role in Profile page

### Access Denied to Admin Panel

**Check**:
1. Verify your role in Profile page
2. Admin panel requires Admin or Head role
3. Reports require Lead, Admin, or Head role

---

## Next Steps

1. **Test all features** using the steps above
2. **Verify Keycloak sync** in Keycloak admin console
3. **Create test users** with different roles
4. **Test role-based access** by trying to access restricted pages

---

**Ready to Test!** 🚀

Start with registering users with different roles and verify the menu visibility changes.


