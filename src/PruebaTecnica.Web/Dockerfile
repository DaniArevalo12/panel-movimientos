# Build context: raíz del repositorio (docker build -f src/PruebaTecnica.Web/Dockerfile .)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Se copia el proyecto completo (incluyendo wwwroot) ANTES de restaurar, en un solo paso.
# La optimización clásica de "copiar solo el .csproj, restaurar, y luego copiar el resto"
# rompe la resolución de los static web assets del framework (_framework/blazor.web.js
# terminaba ausente del publish): esos assets se calculan en el restore a partir del árbol
# completo del proyecto, no solo del .csproj. Directory.Build.props (raíz del repo) y
# src/Shared (archivo .cs enlazado, no un proyecto propio) también deben estar presentes.
COPY Directory.Build.props ./
COPY src/Shared/ src/Shared/
COPY src/PruebaTecnica.Web/ src/PruebaTecnica.Web/
RUN dotnet publish src/PruebaTecnica.Web/PruebaTecnica.Web.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "PruebaTecnica.Web.dll"]
