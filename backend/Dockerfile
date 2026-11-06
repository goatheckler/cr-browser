# Backend Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0@sha256:341aa6e8bbaceb1194732328ca27c15bfa9c531687db83a66af3c0c1833f5534 AS build
WORKDIR /src
COPY backend/src/CrBrowser.Api/CrBrowser.Api.csproj backend/src/CrBrowser.Api/
RUN dotnet restore backend/src/CrBrowser.Api/CrBrowser.Api.csproj
COPY backend/ ./backend/
RUN dotnet publish backend/src/CrBrowser.Api/CrBrowser.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0@sha256:b14a41431d252c640aae69b52320e512050fc53bafcb5012b97ccb9ca173c2d5 AS final
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
# Expose default port used in launchSettings (5214) but inside container we use 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet","CrBrowser.Api.dll"]
