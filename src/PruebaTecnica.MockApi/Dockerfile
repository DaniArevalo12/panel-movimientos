# Build context: raíz del repositorio (docker build -f src/PruebaTecnica.MockApi/Dockerfile .)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Se copia el proyecto completo ANTES de restaurar (ver el comentario detallado en
# src/PruebaTecnica.Web/Dockerfile): separar "copiar .csproj, restaurar, copiar el resto"
# corrompe la resolución de static web assets del framework en proyectos Blazor. Este
# proyecto no sirve contenido estático propio, pero se mantiene el mismo patrón por
# consistencia entre ambos Dockerfile y para no depender de un orden de capas frágil.
COPY Directory.Build.props ./
COPY src/Shared/ src/Shared/
COPY src/PruebaTecnica.MockApi/ src/PruebaTecnica.MockApi/
RUN dotnet publish src/PruebaTecnica.MockApi/PruebaTecnica.MockApi.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "PruebaTecnica.MockApi.dll"]
