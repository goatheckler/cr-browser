# Backend Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0@sha256:df1aebc5fd72a1315f34eda24206f195d5ca00ccf2e3009947a74c5a67166cbb AS build
WORKDIR /src
COPY backend/src/CrBrowser.Api/CrBrowser.Api.csproj backend/src/CrBrowser.Api/
RUN dotnet restore backend/src/CrBrowser.Api/CrBrowser.Api.csproj
COPY backend/ ./backend/
RUN dotnet publish backend/src/CrBrowser.Api/CrBrowser.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0@sha256:ebdd28e9ee54ea5032a390500d37bb1b6d45c36c6ba51e10f3ddfcdc746f3e28 AS final
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
# Expose default port used in launchSettings (5214) but inside container we use 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet","CrBrowser.Api.dll"]
