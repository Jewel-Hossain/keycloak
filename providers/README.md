# Keycloak Email OTP Provider

This directory should contain the `keycloak-email-otp.jar` file.

## How to obtain the JAR file:

**Option A: Using Maven (Recommended)**
```bash
# Install Maven if not already installed
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

**Option C: Manual Download**
1. Go to https://github.com/5-stones/keycloak-email-otp/releases
2. Download the latest `keycloak-email-otp.jar` file
3. Place it in this `providers/` directory

## File location:
The JAR file should be placed directly in this `providers/` directory as `keycloak-email-otp.jar`

## Note:
The Docker setup will work without the JAR file, but the Email OTP functionality will not be available until you add the actual JAR file and rebuild the container.
