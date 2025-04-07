# Use the latest .NET 9.0 runtime as the base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the latest .NET 9.0 SDK for building the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["DentalManagement.csproj", "./"]
RUN dotnet restore "DentalManagement.csproj"
COPY . .
RUN dotnet publish "DentalManagement.csproj" -c Release -o /app

# Final runtime container
FROM base AS final
WORKDIR /app
COPY --from=build /app .

# Make sure to copy templates and configuration
COPY ./EmailTemplates/ /app/EmailTemplates/
COPY appsettings.json /app/appsettings.json
COPY appsettings.Development.json /app/appsettings.Development.json

# Copy and set permissions for the startup script
COPY startup.sh .
RUN chmod +x startup.sh

# Add bash for debugging
RUN apt-get update && apt-get install -y bash curl iputils-ping

ENTRYPOINT ["./startup.sh"]