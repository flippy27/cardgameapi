#!/bin/bash

# Load environment variables
if [ -f .env ]; then
    export $(cat .env | xargs)
fi

# Ensure ASPNETCORE_ENVIRONMENT is set
export ASPNETCORE_ENVIRONMENT=Development

echo "Starting CardDuel API with environment: $ASPNETCORE_ENVIRONMENT"
dotnet run
