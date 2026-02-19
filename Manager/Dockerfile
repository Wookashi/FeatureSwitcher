# --- FRONT BUILD ---
FROM node:22-alpine AS webbuild
WORKDIR /web
COPY Manager/Web/package*.json ./
RUN npm ci
COPY Manager/Web .
RUN npm run build

# --- API BUILD & PUBLISH ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Manager/Api/*.csproj Manager/Api/
COPY Manager/Abstraction/*.csproj Manager/Abstraction/
COPY Manager/Database/*.csproj Manager/Database/
COPY Shared/Abstraction/*.csproj Shared/Abstraction/

RUN dotnet restore Manager/Api/Manager.Api.csproj

COPY Manager/Api Manager/Api
COPY Manager/Abstraction Manager/Abstraction
COPY Manager/Database Manager/Database
COPY Shared/Abstraction Shared/Abstraction
RUN dotnet publish Manager/Api/Manager.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# --- RUNTIME ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV TZ=Europe/Warsaw ASPNETCORE_URLS=http://+:5033 ASPNETCORE_ENVIRONMENT=Production DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
RUN apt-get update && apt-get install -y tzdata wget && rm -rf /var/lib/apt/lists/*
COPY --from=webbuild /web/dist ./wwwroot
COPY --from=build /app/publish .
EXPOSE 5033
ENTRYPOINT ["dotnet", "Wookashi.FeatureSwitcher.Manager.Api.dll"]
