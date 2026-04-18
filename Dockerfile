# ── Stage 1: Build frontend ───────────────────────────────────────────────────────
FROM node:20-alpine AS frontend-build
WORKDIR /app/frontend
COPY backend/PROHUB.API/frontend/package*.json ./
RUN npm ci
COPY backend/PROHUB.API/frontend/ ./
RUN npm run build

# ── Stage 2: Build backend ────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /src

COPY backend/PROHUB.API/PROHUB.API.Server/PROHUB.API.Server.csproj PROHUB.API.Server/
RUN dotnet restore PROHUB.API.Server/PROHUB.API.Server.csproj

COPY backend/PROHUB.API/PROHUB.API.Server/ PROHUB.API.Server/
WORKDIR /src/PROHUB.API.Server
RUN dotnet publish PROHUB.API.Server.csproj -c Release -o /app/publish /p:UseAppHost=false

# Copy compiled frontend into wwwroot
COPY --from=frontend-build /app/frontend/dist /app/publish/wwwroot

# ── Stage 3: Runtime ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=backend-build /app/publish .

ENTRYPOINT ["dotnet", "PROHUB.API.Server.dll"]
