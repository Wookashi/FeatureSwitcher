# --- FRONT BUILD ---
FROM node:22-alpine AS webbuild
WORKDIR /web
COPY Web/package*.json ./
RUN npm ci
COPY Web .
RUN npm run build

# --- API BUILD & PUBLISH ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Api/*.csproj ./Api/
RUN dotnet restore ./Api/Manager.Api.csproj
COPY Api ./Api
RUN dotnet publish ./Api/Manager.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# --- RUNTIME ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
ENV TZ=Europe/Warsaw ASPNETCORE_URLS=http://+:8080 DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
RUN apt-get update && apt-get install -y tzdata && rm -rf /var/lib/apt/lists/*
COPY --from=webbuild /web/dist ./wwwroot
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Wookashi.FeatureSwitcher.Manager.Api.dll"]
