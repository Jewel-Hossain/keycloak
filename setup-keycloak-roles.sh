#!/bin/bash

# Script to create demo roles in Keycloak foodi realm
# Run this after Keycloak is up and running

echo "Setting up Keycloak demo roles..."

# Keycloak configuration
KEYCLOAK_URL="http://localhost:8080"
REALM="foodi"
ADMIN_USER="admin"
ADMIN_PASS="admin123"

# Get admin token
echo "Getting admin token..."
TOKEN_RESPONSE=$(curl -s -X POST "$KEYCLOAK_URL/realms/master/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "username=$ADMIN_USER" \
  -d "password=$ADMIN_PASS" \
  -d "grant_type=password" \
  -d "client_id=admin-cli")

ACCESS_TOKEN=$(echo $TOKEN_RESPONSE | grep -o '"access_token":"[^"]*' | cut -d'"' -f4)

if [ -z "$ACCESS_TOKEN" ]; then
  echo "Error: Failed to get access token. Is Keycloak running?"
  exit 1
fi

echo "Access token obtained successfully"

# Create roles
echo "Creating roles in foodi realm..."

# Role 1: agent
echo "Creating 'agent' role..."
curl -s -X POST "$KEYCLOAK_URL/admin/realms/$REALM/roles" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "agent",
    "description": "Ticket and workspace permissions"
  }'

# Role 2: lead
echo "Creating 'lead' role..."
curl -s -X POST "$KEYCLOAK_URL/admin/realms/$REALM/roles" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "lead",
    "description": "All team tickets and workspace permissions"
  }'

# Role 3: admin
echo "Creating 'admin' role..."
curl -s -X POST "$KEYCLOAK_URL/admin/realms/$REALM/roles" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "admin",
    "description": "Administrative features and reporting"
  }'

# Role 4: head
echo "Creating 'head' role..."
curl -s -X POST "$KEYCLOAK_URL/admin/realms/$REALM/roles" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "head",
    "description": "Full system access and user management"
  }'

echo ""
echo "✅ Demo roles created successfully!"
echo ""
echo "Created roles:"
echo "  - agent: Ticket and workspace permissions"
echo "  - lead: All team tickets and workspace permissions"
echo "  - admin: Administrative features and reporting"
echo "  - head: Full system access and user management"
echo ""
echo "You can verify these roles in Keycloak Admin UI:"
echo "  $KEYCLOAK_URL → foodi realm → Roles"

