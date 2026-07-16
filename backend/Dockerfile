# Backend Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0@sha256:89ce6291bde9acdf59594e79fb8277c6d84c46e4b1f5bf126a4f18766e4bd597 AS build
WORKDIR /src
COPY backend/src/CrBrowser.Api/CrBrowser.Api.csproj backend/src/CrBrowser.Api/
RUN dotnet restore backend/src/CrBrowser.Api/CrBrowser.Api.csproj
COPY backend/ ./backend/
RUN dotnet publish backend/src/CrBrowser.Api/CrBrowser.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0@sha256:9822a6201c64c9f38b8a190832a7ae21cd30150d509b3ea170fd83b4f4e6166a AS final
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
# Expose default port used in launchSettings (5214) but inside container we use 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet","CrBrowser.Api.dll"]
