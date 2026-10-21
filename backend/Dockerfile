# Backend Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0@sha256:738f72aff955d5c5690e06b6fc8ecd1b90ead564099a257ac528ff720b8da85f AS build
WORKDIR /src
COPY backend/src/CrBrowser.Api/CrBrowser.Api.csproj backend/src/CrBrowser.Api/
RUN dotnet restore backend/src/CrBrowser.Api/CrBrowser.Api.csproj
COPY backend/ ./backend/
RUN dotnet publish backend/src/CrBrowser.Api/CrBrowser.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0@sha256:74a1d7c07211a7e8a2e35ff3c70ed8e4e1d6451a1ff7cf7bdf526f9eafa23023 AS final
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
# Expose default port used in launchSettings (5214) but inside container we use 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet","CrBrowser.Api.dll"]
