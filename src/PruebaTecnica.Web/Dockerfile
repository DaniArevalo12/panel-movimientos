# Build context: raíz del repositorio (docker build -f src/PruebaTecnica.Web/Dockerfile .)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/PruebaTecnica.Web/PruebaTecnica.Web.csproj src/PruebaTecnica.Web/
RUN dotnet restore src/PruebaTecnica.Web/PruebaTecnica.Web.csproj

COPY src/PruebaTecnica.Web/ src/PruebaTecnica.Web/
RUN dotnet publish src/PruebaTecnica.Web/PruebaTecnica.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "PruebaTecnica.Web.dll"]
