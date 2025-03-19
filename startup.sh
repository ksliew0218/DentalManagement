#!/bin/bash

# Create necessary directories
mkdir -p /app/wwwroot/images/treatments
mkdir -p /app/wwwroot/images/profiles 
mkdir -p /app/wwwroot/images/Logo

# Set permissions
chmod -R 755 /app/wwwroot/images

# Start the application
dotnet DentalManagement.dll 