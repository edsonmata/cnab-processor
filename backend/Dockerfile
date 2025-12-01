# ========================================
# File: backend/src/CnabProcessor.Api/Dockerfile
# ========================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files
COPY ["src/CnabProcessor.Api/CnabProcessor.Api.csproj", "src/CnabProcessor.Api/"]
COPY ["src/CnabProcessor.Domain/CnabProcessor.Domain.csproj", "src/CnabProcessor.Domain/"]
COPY ["src/CnabProcessor.Infrastructure/CnabProcessor.Infrastructure.csproj", "src/CnabProcessor.Infrastructure/"]

# Restore
RUN dotnet restore "src/CnabProcessor.Api/CnabProcessor.Api.csproj"

# Copy everything
COPY . .

# Build
WORKDIR "/src/src/CnabProcessor.Api"
RUN dotnet build "CnabProcessor.Api.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "CnabProcessor.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CnabProcessor.Api.dll"]