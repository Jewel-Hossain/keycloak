#!/bin/bash

echo "🧪 Running Foodi App Tests..."
echo ""

# Navigate to the app directory
cd "$(dirname "$0")"

# Restore dependencies
echo "📦 Restoring dependencies..."
dotnet restore FoodiApp.Tests/FoodiApp.Tests.csproj

# Run tests
echo ""
echo "🔬 Running all tests..."
dotnet test FoodiApp.Tests/FoodiApp.Tests.csproj --verbosity normal

# Check if tests passed
if [ $? -eq 0 ]; then
    echo ""
    echo "✅ All tests passed!"
else
    echo ""
    echo "❌ Some tests failed. Please review the output above."
    exit 1
fi

