# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

# Copy project file
COPY AuricruxBackend.csproj .

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build and publish
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

# Copy published app from build stage
COPY --from=build /app/publish .

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/api/auricrux/health || exit 1

ENTRYPOINT ["dotnet", "auricrux-backend-oss.dll"]
