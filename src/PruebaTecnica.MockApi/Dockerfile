# Build context: raíz del repositorio (docker build -f src/PruebaTecnica.MockApi/Dockerfile .)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/PruebaTecnica.MockApi/PruebaTecnica.MockApi.csproj src/PruebaTecnica.MockApi/
RUN dotnet restore src/PruebaTecnica.MockApi/PruebaTecnica.MockApi.csproj

COPY src/PruebaTecnica.MockApi/ src/PruebaTecnica.MockApi/
RUN dotnet publish src/PruebaTecnica.MockApi/PruebaTecnica.MockApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "PruebaTecnica.MockApi.dll"]
