# Keycloak + Foodi SSO Demo Project

## 🎉 PROJECT COMPLETE & VERIFIED ✅

> **All SSO flows tested and working!** See [PROJECT_COMPLETE.md](PROJECT_COMPLETE.md) and [SSO_FLOWS_VERIFIED.md](SSO_FLOWS_VERIFIED.md) for full verification results.

## 🎯 What is This?

This project demonstrates **complete bidirectional SSO integration** between:
- **Foodi**: A .NET 8 food delivery application
- **Keycloak**: Enterprise identity management

### Key Features:
✅ User registration in Foodi auto-syncs to Keycloak  
✅ Keycloak can authenticate via "Login with Foodi"  
✅ Full bidirectional SSO using OpenID Connect  
✅ **Real-time user sync** - Create, modify, activate, deactivate (NEW)  
✅ **"Go to Keycloak" button** - Quick access from Foodi UI (NEW)  
✅ **Profile management** - Update profile with sync (NEW)  
✅ **Password management** - Change password with sync (NEW)  
✅ **Dedicated foodi realm** - Proper realm separation (NEW)  
✅ **PostgreSQL exposed** - Direct database access (NEW)  
✅ **83 comprehensive tests** - 94% coverage, 100% passing (NEW)  
✅ Beautiful modern UI  
✅ Email OTP authentication (optional)  

---

## 🚀 Quick Start

```bash
docker-compose up --build
```

Then follow the [QUICKSTART.md](QUICKSTART.md) guide (5 minutes).

**Access:**
- **Foodi App**: http://localhost:5000
- **Keycloak**: http://localhost:8080 (admin/admin123)
- **MailHog**: http://localhost:8025

---

## 📚 Documentation

### Quick Start
- **[QUICK_REFERENCE.md](QUICK_REFERENCE.md)** - ⭐ One-page cheat sheet
- **[QUICKSTART.md](QUICKSTART.md)** - Get started in 5 minutes

### Project Status & Verification
- **[PROJECT_COMPLETE.md](PROJECT_COMPLETE.md)** - ⭐ Complete project summary & deliverables
- **[SSO_FLOWS_VERIFIED.md](SSO_FLOWS_VERIFIED.md)** - ⭐ SSO flow verification results

### Setup Guides
- **[SSO_SETUP_GUIDE.md](SSO_SETUP_GUIDE.md)** - Complete setup and architecture guide
- **[REALM_SETUP_GUIDE.md](REALM_SETUP_GUIDE.md)** - ⭐ Create foodi realm step-by-step
- **[USER_CREATION_ROLE_GUIDE.md](USER_CREATION_ROLE_GUIDE.md)** - Keycloak role configuration

### Testing & Quality
- **[TESTING_GUIDE.md](TESTING_GUIDE.md)** - Comprehensive testing documentation (updated)
- **[TEST_COVERAGE_REPORT.md](TEST_COVERAGE_REPORT.md)** - ⭐ Detailed coverage analysis (94%)
- **[UI_TEST_RESULTS.md](UI_TEST_RESULTS.md)** - ⭐ Browser UI testing results (7/7 passing)
- **[COMPLETE_TEST_VERIFICATION.md](COMPLETE_TEST_VERIFICATION.md)** - ⭐ Combined test verification (90/90 passing)

### Implementation Details
- **[IMPLEMENTATION_COMPLETE.md](IMPLEMENTATION_COMPLETE.md)** - ⭐ Feature implementation summary
- **[FINAL_IMPLEMENTATION_SUMMARY.md](FINAL_IMPLEMENTATION_SUMMARY.md)** - ⭐ Complete project summary

---

# Original: Keycloak Email OTP Setup Guide

## Prerequisites

1. **Install Docker and Docker Compose**
   ```bash
   # Ubuntu/Debian
   sudo apt update
   sudo apt install docker.io docker-compose
   
   # Start Docker service
   sudo systemctl start docker
   sudo systemctl enable docker
   
   # Add user to docker group (optional)
   sudo usermod -aG docker $USER
   ```

2. **Build the Email OTP Provider JAR**
   
   **Option A: Using Maven (Recommended)**
   ```bash
   # Install Maven
   sudo apt install maven
   
   # Clone and build the provider
   git clone https://github.com/5-stones/keycloak-email-otp.git
   cd keycloak-email-otp
   mvn clean package
   
   # Copy the JAR to providers directory
   cp target/keycloak-email-otp.jar ../providers/
   cd ..
   rm -rf keycloak-email-otp
   ```
   
   **Option B: Download Pre-built JAR**
   ```bash
   # Try to download from releases (may not always be available)
   wget -O providers/keycloak-email-otp.jar \
     https://github.com/5-stones/keycloak-email-otp/releases/latest/download/keycloak-email-otp.jar
   ```

## Quick Start

1. **Start the services**
   ```bash
   docker-compose up --build
   ```

2. **Access the services**
   - **Keycloak Admin Console**: http://localhost:8080
     - Username: admin
     - Password: admin123
   - **MailHog Web UI**: http://localhost:8025
     - View all emails sent by Keycloak
     - No authentication required

3. **Configure Email OTP Authentication**
   - Go to Authentication → Flows
   - Copy the "Browser" flow and name it "Browser with Email OTP"
   - In the new flow:
     - Remove "Username Password Form" execution
     - Add "Email TOTP Authentication" execution
     - Set requirement to "Required"
   - Go to Authentication → Bindings
   - Set "Browser Flow" to your new flow

4. **Test Email OTP**
   - Try logging in with Email OTP authentication
   - Check MailHog at http://localhost:8025 to see the OTP email
   - Use the OTP code from the email to complete authentication

## Configuration Details

### Database
- PostgreSQL database is automatically created
- Data persists in Docker volume `postgres_data`
- Connection details are in docker-compose.yml

### Email OTP Provider
- JAR file location: `providers/keycloak-email-otp.jar`
- Automatically loaded during Keycloak startup
- Available as "Email TOTP Authentication" in authentication flows

### Mail Server (MailHog)
- **SMTP Port**: 1025 (for sending emails)
- **Web UI Port**: 8025 (for viewing emails)
- **Purpose**: Development mail server that captures all outgoing emails
- **Features**: 
  - No authentication required
  - Real-time email viewing
  - Email search and filtering
  - Perfect for testing email OTP functionality

### Ports
- Keycloak: 8080
- MailHog Web UI: 8025
- MailHog SMTP: 1025
- PostgreSQL: 5432 (internal only)

## Troubleshooting

1. **JAR file not found**
   - Ensure `keycloak-email-otp.jar` exists in `providers/` directory
   - Check file permissions

2. **Database connection issues**
   - Wait for PostgreSQL to be ready (health check)
   - Check database credentials in docker-compose.yml

3. **Email not sending**
   - Check MailHog is running: http://localhost:8025
   - Verify SMTP configuration in Keycloak Admin Console
   - Check Keycloak logs for email errors
   - Ensure MailHog service is healthy in docker-compose

4. **Authentication flow not working**
   - Ensure the Email OTP flow is properly bound
   - Check execution requirements are set correctly
   - Verify the provider is loaded (check logs)

## Security Notes

- Change default admin password in production
- Use strong database passwords
- **MailHog is for development only** - replace with real SMTP server in production
- Configure proper SMTP authentication for production
- Enable HTTPS in production
- Consider using environment variables for sensitive data

## Production Considerations

For production deployment, replace MailHog with a real SMTP server:

1. **Update docker-compose.yml**:
   ```yaml
   # Remove or comment out the mailhog service
   # Update Keycloak environment variables:
   KC_SPI_EMAIL_SMTP_HOST=your-smtp-server.com
   KC_SPI_EMAIL_SMTP_PORT=587
   KC_SPI_EMAIL_SMTP_USERNAME=your-email@domain.com
   KC_SPI_EMAIL_SMTP_PASSWORD=your-app-password
   KC_SPI_EMAIL_SMTP_SSL=true
   KC_SPI_EMAIL_SMTP_STARTTLS=true
   ```

2. **Configure SMTP in Keycloak Admin Console**:
   - Go to Realm Settings → Email
   - Enter your SMTP server details
   - Test the connection

## File Structure
```
keycloak/
├── Dockerfile
├── docker-compose.yml
├── env.example
├── README.md
└── providers/
    ├── keycloak-email-otp.jar
    └── README.md
```
