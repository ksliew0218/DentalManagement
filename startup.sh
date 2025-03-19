#!/bin/bash

# Create necessary directories
mkdir -p /app/wwwroot/images/treatments

# Set permissions
chmod -R 755 /app/wwwroot/images

# Start the application
dotnet DentalManagement.dll 