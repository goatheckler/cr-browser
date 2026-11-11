# Backend Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0@sha256:1b955f179b02df4f0eb5fadedaa898cbe44d620a64aab52eec72c8e91d1355b5 AS build
WORKDIR /src
COPY backend/src/CrBrowser.Api/CrBrowser.Api.csproj backend/src/CrBrowser.Api/
RUN dotnet restore backend/src/CrBrowser.Api/CrBrowser.Api.csproj
COPY backend/ ./backend/
RUN dotnet publish backend/src/CrBrowser.Api/CrBrowser.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0@sha256:dccacae9e0e103b5d3ba5fbe442dd8d3316b990cd8f2ec9be0a0ec9db0c0e520 AS final
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
# Expose default port used in launchSettings (5214) but inside container we use 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet","CrBrowser.Api.dll"]
