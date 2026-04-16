# DongQiDB Text-to-SQL API
# Multi-stage build for optimized production image

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first (for better layer caching)
COPY DongQiDB.sln .
COPY DongQiDB.Domain/DongQiDB.Domain.csproj DongQiDB.Domain/
COPY DongQiDB.Application/DongQiDB.Application.csproj DongQiDB.Application/
COPY DongQiDB.Infrastructure/DongQiDB.Infrastructure.csproj DongQiDB.Infrastructure/
COPY DongQiDB.Api/DongQiDB.Api.csproj DongQiDB.Api/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build and publish
WORKDIR /src/DongQiDB.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN groupadd -r dongqi && useradd -r -g dongqi dongqi

# Copy published files
COPY --from=build /app/publish .

# Set ownership
RUN chown -R dongqi:dongqi /app

# Switch to non-root user
USER dongqi

# Expose port
EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:5000 \
    DOTNET_EnableDiagnostics=0

# Entry point
ENTRYPOINT ["dotnet", "DongQiDB.Api.dll"]
