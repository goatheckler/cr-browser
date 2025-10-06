# Backend Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0@sha256:ff8311847c54c04d1a14c488362807997d59b61372da5095a95f89cbcda7f9b7 AS build
WORKDIR /src
COPY backend/src/GhcrBrowser.Api/GhcrBrowser.Api.csproj backend/src/GhcrBrowser.Api/
RUN dotnet restore backend/src/GhcrBrowser.Api/GhcrBrowser.Api.csproj
COPY backend/ ./backend/
RUN dotnet publish backend/src/GhcrBrowser.Api/GhcrBrowser.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0@sha256:53f73fbe620361e5116f68752bf42958dfcda8699a94a785dcb4657bc571c8f3 AS final
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
# Expose default port used in launchSettings (5214) but inside container we use 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet","GhcrBrowser.Api.dll"]
