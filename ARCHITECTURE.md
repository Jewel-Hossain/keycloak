# 🏗️ Architecture Diagram

## System Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                           USER'S BROWSER                            │
│                                                                     │
│  ┌──────────────────┐              ┌──────────────────┐           │
│  │  Foodi Web UI    │              │  Keycloak UI     │           │
│  │ localhost:5000   │              │ localhost:8080   │           │
│  └────────┬─────────┘              └────────┬─────────┘           │
│           │                                  │                     │
└───────────┼──────────────────────────────────┼─────────────────────┘
            │                                  │
            │ HTTP/HTTPS                       │ HTTP/HTTPS
            │                                  │
┌───────────▼──────────────────────────────────▼─────────────────────┐
│                     DOCKER NETWORK (keycloak-network)              │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │                    FOODI APP (.NET 8)                       │  │
│  │                    Container: foodi-app                     │  │
│  │                    Port: 5000                               │  │
│  │                                                             │  │
│  │  ┌──────────────────────────────────────────────────────┐  │  │
│  │  │              Controllers Layer                       │  │  │
│  │  │  • AccountController (Login, Register, Logout)      │  │  │
│  │  │  • HomeController (Menu, Orders)                    │  │  │
│  │  │  • AuthorizationController (OIDC Endpoints)         │  │  │
│  │  └──────────────────┬───────────────────────────────────┘  │  │
│  │                     │                                       │  │
│  │  ┌──────────────────▼───────────────────────────────────┐  │  │
│  │  │              Services Layer                          │  │  │
│  │  │  • KeycloakSyncService (User Sync)                  │  │  │
│  │  │  • OpenIddict (OIDC Server)                         │  │  │
│  │  └──────────────────┬───────────────────────────────────┘  │  │
│  │                     │                                       │  │
│  │  ┌──────────────────▼───────────────────────────────────┐  │  │
│  │  │              Data Layer                              │  │  │
│  │  │  • ApplicationDbContext (EF Core)                   │  │  │
│  │  │  • User, FoodItem, Order Models                     │  │  │
│  │  └──────────────────┬───────────────────────────────────┘  │  │
│  │                     │                                       │  │
│  │  ┌──────────────────▼───────────────────────────────────┐  │  │
│  │  │              SQLite Database                         │  │  │
│  │  │  • Users table                                       │  │  │
│  │  │  • FoodItems table                                   │  │  │
│  │  │  • Orders table                                      │  │  │
│  │  │  • OpenIddict tables                                 │  │  │
│  │  │  Volume: foodi_data:/app/data                       │  │  │
│  │  └──────────────────────────────────────────────────────┘  │  │
│  └─────────────────────────────────────────────────────────────┘  │
│                                                                     │
│                     │                                               │
│                     │ HTTP (Admin API)                             │
│                     │                                               │
│  ┌─────────────────▼───────────────────────────────────────────┐  │
│  │                    KEYCLOAK SERVER                           │  │
│  │                    Container: keycloak-server                │  │
│  │                    Port: 8080                                │  │
│  │                                                              │  │
│  │  ┌──────────────────────────────────────────────────────┐   │  │
│  │  │              Identity & Access Management            │   │  │
│  │  │  • User Authentication                               │   │  │
│  │  │  • Identity Brokering (Foodi Provider)              │   │  │
│  │  │  • Admin API                                         │   │  │
│  │  │  • OIDC/SAML Support                                 │   │  │
│  │  └──────────────────┬───────────────────────────────────┘   │  │
│  │                     │                                         │  │
│  └─────────────────────┼─────────────────────────────────────────┘  │
│                        │ JDBC                                      │
│                        │                                            │
│  ┌─────────────────────▼─────────────────────────────────────────┐  │
│  │                    POSTGRESQL DATABASE                        │  │
│  │                    Container: keycloak-postgres               │  │
│  │                    Port: 5432 (internal)                      │  │
│  │                                                               │  │
│  │  • Keycloak schema                                           │  │
│  │  • Users (synced from Foodi)                                 │  │
│  │  • Realms, Clients, Roles                                    │  │
│  │  • Sessions, Events                                          │  │
│  │  Volume: postgres_data:/var/lib/postgresql/data             │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │                    MAILHOG (DEV SMTP)                        │  │
│  │                    Container: mailhog-server                 │  │
│  │                    SMTP: 1025, Web UI: 8025                  │  │
│  │                                                              │  │
│  │  • Email capture for development                            │  │
│  │  • Used for Email OTP (if enabled)                          │  │
│  │  • Web UI for viewing emails                                │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## User Registration Flow

```
┌─────────┐
│ Browser │
└────┬────┘
     │
     │ 1. Fill registration form
     │    (username, email, password, etc.)
     │
     ▼
┌──────────────────────┐
│ Foodi: AccountCtrl   │
│ Register Action      │
└────┬─────────────────┘
     │
     │ 2. Validate input
     │ 3. Hash password (SHA256)
     │ 4. Create User object
     │
     ▼
┌──────────────────────┐
│ Foodi: SQLite DB     │
│ Save user record     │
└────┬─────────────────┘
     │
     │ 5. User saved in Foodi DB
     │
     ▼
┌──────────────────────┐
│ KeycloakSyncService  │
└────┬─────────────────┘
     │
     │ 6. Get admin token
     │    POST /realms/master/protocol/openid-connect/token
     │
     ▼
┌──────────────────────┐
│ Keycloak Admin API   │
└────┬─────────────────┘
     │
     │ 7. Return access token
     │
     ▼
┌──────────────────────┐
│ KeycloakSyncService  │
└────┬─────────────────┘
     │
     │ 8. Create user in Keycloak
     │    POST /admin/realms/master/users
     │    { username, email, password, firstName, lastName }
     │
     ▼
┌──────────────────────┐
│ Keycloak Admin API   │
└────┬─────────────────┘
     │
     │ 9. User created in Keycloak
     │    Return Location header with user ID
     │
     ▼
┌──────────────────────┐
│ KeycloakSyncService  │
└────┬─────────────────┘
     │
     │ 10. Extract Keycloak user ID
     │
     ▼
┌──────────────────────┐
│ Foodi: SQLite DB     │
│ Update user record   │
└────┬─────────────────┘
     │
     │ 11. Set SyncedToKeycloak = true
     │     Set KeycloakUserId = <id>
     │
     ▼
┌──────────────────────┐
│ Browser              │
│ Success message!     │
└──────────────────────┘

RESULT: User exists in BOTH Foodi and Keycloak ✅
```

---

## SSO Authentication Flow (Keycloak → Foodi)

```
┌─────────┐
│ Browser │
└────┬────┘
     │
     │ 1. User visits Keycloak login page
     │    http://localhost:8080
     │
     ▼
┌──────────────────────┐
│ Keycloak Login Page  │
│ Shows:               │
│ • Username/Password  │
│ • "Login with Foodi" │ ← Identity Provider button
└────┬─────────────────┘
     │
     │ 2. User clicks "Login with Foodi"
     │
     ▼
┌──────────────────────┐
│ Keycloak             │
│ Identity Broker      │
└────┬─────────────────┘
     │
     │ 3. Redirect to Foodi authorization endpoint
     │    GET /connect/authorize
     │    ?client_id=keycloak-client
     │    &redirect_uri=http://localhost:8080/realms/master/broker/foodi/endpoint
     │    &response_type=code
     │    &scope=openid profile email
     │
     ▼
┌──────────────────────┐
│ Foodi:               │
│ AuthorizationCtrl    │
│ Authorize()          │
└────┬─────────────────┘
     │
     │ 4. Check authentication
     │    Is user logged into Foodi?
     │
     ├─ NO ──────┐
     │           ▼
     │      ┌──────────────────────┐
     │      │ Foodi Login Page     │
     │      └────┬─────────────────┘
     │           │
     │           │ 5. User enters credentials
     │           │    username, password
     │           │
     │           ▼
     │      ┌──────────────────────┐
     │      │ Foodi: AccountCtrl   │
     │      │ Login()              │
     │      └────┬─────────────────┘
     │           │
     │           │ 6. Validate credentials
     │           │    Check SQLite database
     │           │    Verify password hash
     │           │
     │           ▼
     │      ┌──────────────────────┐
     │      │ Foodi: Cookie Auth   │
     │      │ Create session       │
     │      └────┬─────────────────┘
     │           │
     │           │ 7. User authenticated
     │           │
     └─ YES ─────┤
                 │
                 ▼
            ┌──────────────────────┐
            │ Foodi:               │
            │ AuthorizationCtrl    │
            │ Authorize()          │
            └────┬─────────────────┘
                 │
                 │ 8. Create authorization code
                 │    Generate unique code
                 │    Associate with user
                 │
                 ▼
            ┌──────────────────────┐
            │ Browser              │
            │ Redirect to Keycloak │
            └────┬─────────────────┘
                 │
                 │ 9. GET http://localhost:8080/realms/master/broker/foodi/endpoint
                 │    ?code=<authorization_code>
                 │
                 ▼
            ┌──────────────────────┐
            │ Keycloak             │
            │ Identity Broker      │
            └────┬─────────────────┘
                 │
                 │ 10. Exchange code for token
                 │     POST http://foodi-app:5000/connect/token
                 │     code=<authorization_code>
                 │     client_id=keycloak-client
                 │     client_secret=foodi-secret-key-2024
                 │     grant_type=authorization_code
                 │
                 ▼
            ┌──────────────────────┐
            │ Foodi:               │
            │ AuthorizationCtrl    │
            │ Exchange()           │
            └────┬─────────────────┘
                 │
                 │ 11. Validate authorization code
                 │     Validate client credentials
                 │     Generate access token
                 │
                 ▼
            ┌──────────────────────┐
            │ Keycloak             │
            │ Receives tokens      │
            └────┬─────────────────┘
                 │
                 │ 12. {
                 │       "access_token": "eyJ...",
                 │       "token_type": "Bearer",
                 │       "expires_in": 3600
                 │     }
                 │
                 ▼
            ┌──────────────────────┐
            │ Keycloak             │
            │ Identity Broker      │
            └────┬─────────────────┘
                 │
                 │ 13. Get user info
                 │     GET http://foodi-app:5000/connect/userinfo
                 │     Authorization: Bearer <access_token>
                 │
                 ▼
            ┌──────────────────────┐
            │ Foodi:               │
            │ AuthorizationCtrl    │
            │ Userinfo()           │
            └────┬─────────────────┘
                 │
                 │ 14. Validate access token
                 │     Retrieve user from database
                 │     Build user profile
                 │
                 ▼
            ┌──────────────────────┐
            │ Keycloak             │
            │ Receives user info   │
            └────┬─────────────────┘
                 │
                 │ 15. {
                 │       "sub": "123",
                 │       "email": "test@example.com",
                 │       "name": "testuser",
                 │       "given_name": "Test",
                 │       "family_name": "User"
                 │     }
                 │
                 ▼
            ┌──────────────────────┐
            │ Keycloak             │
            │ Create/update user   │
            │ Create session       │
            └────┬─────────────────┘
                 │
                 │ 16. User authenticated in Keycloak
                 │
                 ▼
            ┌──────────────────────┐
            │ Browser              │
            │ Keycloak Admin       │
            │ User logged in! ✅   │
            └──────────────────────┘

RESULT: User authenticated in Keycloak via Foodi SSO ✅
```

---

## Data Synchronization

```
┌──────────────────┐              ┌──────────────────┐
│   FOODI DATABASE │              │ KEYCLOAK DATABASE│
│     (SQLite)     │              │   (PostgreSQL)   │
└────────┬─────────┘              └────────┬─────────┘
         │                                 │
         │  User Registration              │
         │  ────────────────────────►      │
         │                                 │
         │  Keycloak creates user          │
         │                                 │
         │                                 │
         │  SSO Authentication             │
         │  ◄────────────────────────      │
         │                                 │
         │  Keycloak queries Foodi         │
         │  for user information           │
         │                                 │
         │                                 │
         │  Both databases maintain        │
         │  user records, but Foodi        │
         │  is source of truth for         │
         │  authentication                 │
         │                                 │
         │                                 │
         ▼                                 ▼
┌──────────────────┐              ┌──────────────────┐
│  User Record:    │              │  User Record:    │
│  • id: 1         │              │  • id: abc-123   │
│  • username      │              │  • username      │
│  • email         │              │  • email         │
│  • password_hash │              │  • credentials   │
│  • first_name    │              │  • first_name    │
│  • last_name     │              │  • last_name     │
│  • synced: true  │              │  • source: foodi │
│  • kc_id: abc... │              │                  │
└──────────────────┘              └──────────────────┘
```

---

## Network Communication

```
EXTERNAL (Browser):
┌──────────────────────────────────┐
│ http://localhost:5000   → Foodi  │
│ http://localhost:8080   → Keycloak│
│ http://localhost:8025   → MailHog│
└──────────────────────────────────┘

INTERNAL (Docker Network):
┌──────────────────────────────────────────────┐
│ foodi-app:5000      → Foodi (container)      │
│ keycloak:8080       → Keycloak (container)   │
│ postgres:5432       → PostgreSQL (container) │
│ mailhog:1025,8025   → MailHog (container)    │
└──────────────────────────────────────────────┘

WHY TWO URLS?
• Browser: Uses localhost (host machine)
• Container-to-container: Uses service names
• Example:
  - Authorization URL: http://localhost:5000 (browser)
  - Token URL: http://foodi-app:5000 (Keycloak → Foodi)
```

---

## Security Layers

```
┌─────────────────────────────────────────┐
│           APPLICATION LAYER             │
│  • CSRF Protection                      │
│  • Input Validation                     │
│  • XSS Prevention                       │
│  • SQL Injection Prevention (EF Core)   │
└────────────────┬────────────────────────┘
                 │
┌────────────────▼────────────────────────┐
│         AUTHENTICATION LAYER            │
│  • Password Hashing (SHA256)            │
│  • Cookie-based Sessions                │
│  • OAuth 2.0 / OIDC                     │
│  • Token-based Authentication           │
└────────────────┬────────────────────────┘
                 │
┌────────────────▼────────────────────────┐
│          AUTHORIZATION LAYER            │
│  • [Authorize] Attributes               │
│  • Role-based Access Control            │
│  • Scope Validation                     │
└────────────────┬────────────────────────┘
                 │
┌────────────────▼────────────────────────┐
│           TRANSPORT LAYER               │
│  • HTTP (development)                   │
│  • HTTPS (production) - recommended     │
│  • Secure Cookies (HttpOnly)            │
└────────────────┬────────────────────────┘
                 │
┌────────────────▼────────────────────────┐
│           NETWORK LAYER                 │
│  • Docker Network Isolation             │
│  • Container Firewall                   │
│  • Internal Services (PostgreSQL)       │
└─────────────────────────────────────────┘
```

---

## Component Interaction Matrix

```
                    Foodi    Keycloak  PostgreSQL  MailHog
                    ─────    ────────  ──────────  ───────
Foodi               -        Admin API    -          -
Keycloak         OIDC        -          JDBC       SMTP
PostgreSQL         -         -           -          -
MailHog            -         -           -          -
Browser         HTTP        HTTP         -         HTTP
```

---

## Deployment Architecture

```
DEVELOPMENT (Current):
┌──────────────────────────────────────┐
│         Local Machine                │
│                                      │
│  ┌────────────────────────────────┐ │
│  │      Docker Compose            │ │
│  │                                │ │
│  │  ┌──────┐ ┌──────┐ ┌──────┐   │ │
│  │  │Foodi │ │Keycl.│ │Postgr│   │ │
│  │  └──────┘ └──────┘ └──────┘   │ │
│  └────────────────────────────────┘ │
└──────────────────────────────────────┘

PRODUCTION (Recommended):
┌──────────────────────────────────────┐
│         Cloud Provider               │
│                                      │
│  ┌────────────────────────────────┐ │
│  │   Load Balancer (HTTPS)        │ │
│  └──────┬─────────────────────────┘ │
│         │                            │
│  ┌──────▼──────┐  ┌──────────────┐  │
│  │  Foodi App  │  │  Keycloak    │  │
│  │  (Multiple) │  │  (Clustered) │  │
│  └──────┬──────┘  └──────┬───────┘  │
│         │                 │          │
│  ┌──────▼─────────────────▼───────┐ │
│  │   Managed PostgreSQL           │ │
│  │   (RDS / Azure DB)             │ │
│  └────────────────────────────────┘ │
└──────────────────────────────────────┘
```

---

This architecture provides:
✅ Scalability
✅ Security
✅ Maintainability
✅ High Availability (when clustered)
✅ Clear separation of concerns

