# TCGHit API Dockerfile
# Multi-stage build for optimized image size

# =============================================================================
# Stage 1: Build
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies (cached layer)
COPY ["TCGHit.Api.csproj", "."]
RUN dotnet restore "TCGHit.Api.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "TCGHit.Api.csproj" -c Release -o /app/build

# =============================================================================
# Stage 2: Publish
# =============================================================================
FROM build AS publish
RUN dotnet publish "TCGHit.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# =============================================================================
# Stage 3: Runtime (Final Image)
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos "" --uid 1000 appuser

# Copy published app
COPY --from=publish /app/publish .

# Set ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Azure Container Apps uses port 8080 by default
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Health check for container orchestration
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "TCGHit.Api.dll"]
