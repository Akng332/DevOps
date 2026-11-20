FROM mcr.microsoft.com/dotnet/sdk:10.0.404-noble AS build
WORKDIR /source

COPY global.json ./
COPY src/MusicStore.Api/MusicStore.Api.csproj src/MusicStore.Api/packages.lock.json src/MusicStore.Api/
RUN dotnet restore src/MusicStore.Api/MusicStore.Api.csproj --locked-mode

COPY src/MusicStore.Api/ src/MusicStore.Api/
RUN dotnet publish src/MusicStore.Api/MusicStore.Api.csproj \
    --configuration Release --output /publish --no-restore \
    -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.15-noble
WORKDIR /app
COPY --from=build --chown=1000:1000 /publish/ ./

USER 1000:1000
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=5s --start-period=30s --retries=3 \
    CMD ["dotnet", "MusicStore.Api.dll", "--healthcheck"]
ENTRYPOINT ["dotnet", "MusicStore.Api.dll"]
