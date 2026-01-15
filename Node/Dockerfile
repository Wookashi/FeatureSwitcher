# --- API BUILD & PUBLISH ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Node/Api/*.csproj Node/Api/
COPY Node/Abstraction/*.csproj Node/Abstraction/
COPY Node/Database/*.csproj Node/Database/
COPY Shared/Abstraction/*.csproj Shared/Abstraction/

RUN dotnet restore Node/Api/Node.Api.csproj

COPY Node/Api Node/Api
COPY Node/Abstraction Node/Abstraction
COPY Node/Database Node/Database
COPY Shared/Abstraction Shared/Abstraction
RUN dotnet publish Node/Api/Node.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# --- RUNTIME ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV TZ=Europe/Warsaw ASPNETCORE_URLS=http://+:5216 DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
RUN apt-get update && apt-get install -y tzdata wget && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
EXPOSE 5216
HEALTHCHECK --interval=10s --timeout=3s --retries=5 CMD wget -qO- http://localhost:5216/health || exit 1
ENTRYPOINT ["dotnet", "Wookashi.FeatureSwitcher.Node.Api.dll"]
