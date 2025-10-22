#!/bin/bash

echo "🍔 Starting Foodi + Keycloak SSO Demo..."
echo ""
echo "This will start:"
echo "  - Foodi App (http://localhost:5000)"
echo "  - Keycloak (http://localhost:8080)"
echo "  - MailHog (http://localhost:8025)"
echo ""

# Start services
docker-compose up --build -d

echo ""
echo "⏳ Waiting for services to be ready..."
echo ""

# Wait for services
sleep 10

echo "✅ Services started!"
echo ""
echo "📝 Next steps:"
echo "  1. Open Foodi: http://localhost:5000"
echo "  2. Create an account (it will sync to Keycloak)"
echo "  3. Check Keycloak: http://localhost:8080 (admin/admin123)"
echo "  4. See the user you created!"
echo ""
echo "📚 Full guide: See QUICKSTART.md or SSO_SETUP_GUIDE.md"
echo ""
echo "🛑 To stop: docker-compose down"
echo ""

