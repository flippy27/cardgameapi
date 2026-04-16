#!/bin/bash

echo "🚀 CardDuel API - Dev Setup"
echo ""

# Check prerequisites
if ! command -v docker &> /dev/null; then
    echo "❌ Docker not found. Install Docker first."
    exit 1
fi

if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK not found. Install .NET SDK 8+ first."
    exit 1
fi

# Start Docker Compose
echo "📦 Starting PostgreSQL + Redis..."
docker-compose up -d

# Wait for DB
echo "⏳ Waiting for PostgreSQL to be ready..."
for i in {1..30}; do
    if docker exec $(docker-compose ps -q postgres) pg_isready -U postgres > /dev/null 2>&1; then
        echo "✅ PostgreSQL is ready"
        break
    fi
    echo -n "."
    sleep 1
done

# Run migrations
echo ""
echo "🔄 Running database migrations..."
dotnet ef database update

if [ $? -eq 0 ]; then
    echo "✅ Migrations completed"
else
    echo "⚠️  Migrations may have failed, continuing anyway..."
fi

echo ""
echo "🎮 Ready to start API!"
echo ""
echo "Run: ASPNETCORE_ENVIRONMENT=Development dotnet watch run"
echo "  or: dotnet run"
echo ""
echo "Then visit:"
echo "  - API:      http://localhost:5000"
echo "  - Swagger:  http://localhost:5000/swagger"
echo "  - Health:   http://localhost:5000/api/health"
echo ""
echo "Stop with: docker-compose down"
