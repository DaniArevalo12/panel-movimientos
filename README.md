# Panel de Movimientos

Aplicación Blazor Web App (.NET 10, modo interactivo Server) que consume una API REST y presenta el catálogo de movimientos de un sistema de inventario en una grilla moderna, con búsqueda, ordenamiento, auto-refresco y tema claro/oscuro.

> **Nota sobre la versión de .NET:** el enunciado original pedía .NET 9. Este equipo solo tiene instalado el SDK de .NET 10, por lo que el proyecto se dirige a `net10.0`. El código no usa ninguna característica exclusiva de .NET 10; migrar a `net9.0` es cambiar el `TargetFramework` en ambos `.csproj` con el SDK correspondiente instalado.

## Índice

- [Arquitectura](#arquitectura)
- [Tecnologías](#tecnologías)
- [Estructura del repositorio](#estructura-del-repositorio)
- [Cómo ejecutar](#cómo-ejecutar)
- [Conectar con la API real](#conectar-con-la-api-real)
- [Decisiones técnicas](#decisiones-técnicas)
- [Buenas prácticas implementadas](#buenas-prácticas-implementadas)
- [Posibles mejoras futuras](#posibles-mejoras-futuras)

## Arquitectura

La solución tiene dos proyectos:

```
PruebaTecnica.sln
├── src/PruebaTecnica.Web       → Aplicación Blazor Web App (el entregable de la prueba)
└── src/PruebaTecnica.MockApi   → API REST mínima que simula el backend externo
```

**`PruebaTecnica.Web` no es un CRUD ni un multi-proyecto de Clean Architecture "de libro".** Es un único proyecto organizado con fronteras internas estrictas, porque la superficie funcional real (consumir un endpoint, mostrar una grilla) no justifica repartir Domain/Application/Infrastructure en ensamblados separados — eso sería complejidad accidental. En su lugar, Clean Architecture se aplica como **regla de dependencia**, no como estructura de carpetas:

```
Components/Pages   → UI. Solo orquesta estado (cargando/error/datos) y renderiza.
Components/Shared  → Componentes de presentación puros (badge, skeleton, estados vacíos).
Services/           → Contratos + implementación de acceso a datos. Única capa que conoce HTTP.
Models/              → DTOs fuertemente tipados que reflejan el contrato de la API.
Configuration/       → Opciones fuertemente tipadas (Options Pattern).
Common/              → Primitivas transversales (Result<T>).
```

La regla que se respeta en todo el proyecto: **ningún componente Razor llama a `HttpClient` directamente**. Todo pasa por `IMovimientoService`, inyectado por constructor/`@inject`. Esto permite sustituir la implementación (por ejemplo, por una versión con caché o por un mock en tests) sin tocar una sola línea de UI.

```
Movimientos.razor  →  IMovimientoService  →  MovimientoService  →  HttpClient (HttpClientFactory)
     (UI)                (abstracción)          (implementación)        (transporte)
```

## Tecnologías

| Tecnología | Uso |
|---|---|
| .NET 10 / Blazor Web App (Interactive Server) | Framework principal |
| `IHttpClientFactory` | Ciclo de vida y pooling de `HttpClient` |
| `Microsoft.Extensions.Http.Resilience` | Retry con backoff, timeout por intento y circuit breaker sobre el cliente HTTP |
| Options Pattern (`IOptions<T>` + `ValidateOnStart`) | Configuración fuertemente tipada y validada al arrancar |
| `System.Text.Json` | Serialización, con contrato de propiedades explícito (`JsonPropertyName`) |
| `ILogger<T>` | Logging estructurado de inicio/fin/duración/errores de cada consulta |
| CSS puro (variables, grid, flexbox) | UI empresarial sin dependencias de terceros |

**Por qué no se usó un UI kit (MudBlazor, etc.):** la app tiene una sola pantalla con una tabla, un buscador y algunos botones. Un design system completo de ~2 MB para eso es sobreingeniería. Con CSS moderno (custom properties, `color-scheme`, animaciones CSS) se logra la misma calidad visual sin la dependencia.

**Por qué sí se usó `Microsoft.Extensions.Http.Resilience`:** es un paquete first-party de Microsoft (no una librería de terceros), se integra en una línea (`AddStandardResilienceHandler`) y resuelve un requisito real — resiliencia ante fallos de red — sin reinventar retry/backoff a mano, que es fácil de hacer mal (reintentos sin jitter, sin límite, etc.).

## Estructura del repositorio

```
.editorconfig            Convenciones de estilo/nombres para dotnet format, el IDE y los analizadores.
Directory.Build.props    Configuración MSBuild común a todos los proyectos (Nullable, analizadores, CA rules).
src/
├── PruebaTecnica.Web/
│   ├── Components/
│   │   ├── Layout/            MainLayout, ReconnectModal
│   │   ├── Pages/              Movimientos (página principal, ruta "/"), Error, NotFound
│   │   └── Shared/              EstadoBadge, EmptyState, ErrorState, TableSkeleton, ThemeToggle
│   ├── Services/                IMovimientoService, MovimientoService
│   ├── Models/                  Movimiento
│   ├── Configuration/           ApiSettings
│   ├── Common/                  Result<T>
│   ├── wwwroot/                  app.css, js/theme.js
│   ├── appsettings.json
│   └── Program.cs
├── PruebaTecnica.Web.Tests/     Tests unitarios (xUnit) de MovimientoService y Result<T>
├── PruebaTecnica.MockApi/
│   ├── MovimientosDataSource.cs  Datos de ejemplo (20 movimientos, contrato PascalCase exacto)
│   ├── MovimientoDto.cs
│   └── Program.cs
└── Shared/
    └── RenderHostingExtensions.cs  Archivo .cs enlazado (no un proyecto) en ambos .csproj: boilerplate
                                     de hosting para Render (variable PORT), no lógica de negocio.
```

## Cómo ejecutar

Requisitos: SDK de .NET 10 (`dotnet --version`).

```bash
# Desde la raíz del repositorio

# 1. Levantar la API mock (simula el backend externo)
dotnet run --project src/PruebaTecnica.MockApi --launch-profile http
# → http://localhost:5206/api/movimientos

# 2. En otra terminal, levantar la aplicación Blazor
dotnet run --project src/PruebaTecnica.Web --launch-profile http
# → http://localhost:5212
```

`appsettings.json` de `PruebaTecnica.Web` ya apunta al mock (`http://localhost:5206`), así que con ambos procesos corriendo la grilla carga datos reales de un servicio HTTP independiente — no datos en memoria del propio proceso.

Para correr los tests:

```bash
dotnet test
```

## Conectar con la API real

Cuando la URL oficial de la prueba técnica esté disponible, el único cambio necesario es `appsettings.json` (o `appsettings.Production.json` / variables de entorno en el hosting elegido):

```json
{
  "ApiSettings": {
    "BaseUrl": "https://api-real.example.com",
    "Endpoint": "/api/movimientos",
    "TimeoutSeconds": 15
  }
}
```

No hace falta tocar código: `ApiSettings` se valida con `DataAnnotations` al arrancar (`ValidateOnStart`), así que una URL faltante o inválida hace fallar el arranque con un mensaje claro en lugar de un error confuso en tiempo de ejecución. `PruebaTecnica.MockApi` puede eliminarse del `.sln` sin afectar a `PruebaTecnica.Web`.

## Decisiones técnicas

- **`Result<T>` en vez de excepciones para control de flujo.** `MovimientoService` captura `HttpRequestException`, `TaskCanceledException`/`TimeoutRejectedException` (timeout, ya sea del propio `HttpClient` o de la estrategia de resiliencia de Polly), `BrokenCircuitException` (circuit breaker abierto), `JsonException` y, como red de seguridad final, `Exception` genérica — ningún fallo, ni siquiera uno no anticipado, debe escapar del servicio y tumbar el circuito de Blazor Server. Todo se loggea con `ILogger` (vía métodos `[LoggerMessage]` generados en tiempo de compilación, no interpolación clásica) y se traduce a `Result<T>.Failure(mensaje)`. La UI nunca hace `try/catch` de HTTP: reacciona a un valor tipado.
- **Contrato JSON explícito con `[JsonPropertyName]`.** El modelo `Movimiento` fija los nombres exactos (`Codigo`, `Descripcion`, `VActiva`) en vez de depender de una política de naming implícita. Si el backend cambia el contrato, la deserialización falla de forma visible (los campos quedan en su valor por defecto y el log lo refleja) en vez de fallar en silencio.
- **Resiliencia configurada en la composición raíz, no en el servicio.** `MovimientoService` no sabe nada de reintentos ni circuit breakers — eso es una responsabilidad transversal de infraestructura, configurada una sola vez en `Program.cs` vía `AddStandardResilienceHandler`. El servicio solo sabe pedir datos y traducir errores a `Result<T>`.
- **Auto-refresco con `PeriodicTimer`, no `Timer` ni `setInterval` vía JS.** `PeriodicTimer` es cancelable de forma cooperativa con `CancellationToken`, evita reentrancia (no dispara un tick nuevo si el anterior sigue corriendo) y se libera correctamente en `DisposeAsync`, evitando fugas de memoria en un circuito de Blazor Server de larga duración.
- **Cambio de tema sin flash (FOUC).** La preferencia se aplica con un script inline en `<head>` **antes** del primer render (leyendo `localStorage` o `prefers-color-scheme`), y el componente `ThemeToggle` solo sincroniza el estado después de `OnAfterRenderAsync`. Es la única interacción con `IJSRuntime` de todo el proyecto — deliberadamente, porque es el único caso que Blazor Server no puede resolver sin un round-trip.

## Buenas prácticas implementadas

✔ **SOLID**
- **SRP** — cada clase tiene una única razón para cambiar: `MovimientoService` traduce HTTP→dominio, `ApiSettings` es solo configuración, cada componente `Shared` renderiza un único estado visual.
- **OCP/DIP** — la UI depende de `IMovimientoService`, no de `MovimientoService` ni de `HttpClient`. Sustituir la fuente de datos no requiere tocar componentes.
- **ISP** — `IMovimientoService` expone un único método coherente con su nombre; no hay interfaces "bolsa" con métodos que algunos consumidores no usan.

✔ **Clean Code** — nombres descriptivos en español (dominio del negocio) y en inglés donde es idiomático (tipos de infraestructura); métodos pequeños con una responsabilidad; sin regiones; sin `var` cuando el tipo no es evidente en la lectura; cero duplicación entre los dos servicios desplegables (el boilerplate de hosting para Render vive en `src/Shared/`, no copiado dos veces).

✔ **Rendimiento** — todo el flujo de datos es `async`/`await` de punta a punta; no hay bloqueos (`.Result`, `.Wait()`); no hay solicitudes HTTP solapadas (una guarda de reentrancia evita que un tick de auto-refresco pise una petición manual en curso); el filtrado/ordenamiento client-side opera sobre listas ya materializadas en memoria (dataset pequeño por diseño del dominio) y se calcula una sola vez por render, no una vez por cada lugar donde se lee; el logging usa `[LoggerMessage]` (CA1848) en vez de las llamadas clásicas a `ILogger.LogXxx`; las transiciones CSS usan `transform`/`opacity` para no forzar reflow.

✔ **Seguridad** — la `BaseUrl` de la API nunca está hardcodeada; se valida con `[Url]` al arrancar, con la misma validación aplicada de forma síncrona antes de configurar la resiliencia HTTP (para que un `TimeoutSeconds` inválido falle igual de rápido). Cabeceras `X-Content-Type-Options`, `Referrer-Policy`, `X-Frame-Options` y `Permissions-Policy` en cada respuesta. No se interpola HTML sin escapar (Razor escapa por defecto). Los paquetes de terceros se verifican con `dotnet list package --vulnerable` (limpio) y `--outdated`; `Microsoft.OpenApi` se mantiene deliberadamente en 2.x (no en el 3.x disponible) porque es un salto de versión mayor sin CVEs pendientes en la versión actual, y `Microsoft.AspNetCore.OpenApi` de ASP.NET Core 10 está construido contra esa superficie de API.

✔ **Accesibilidad** — encabezados de columna ordenables implementados como `<button>` real dentro del `<th>` (no `role="button"` sobre el propio `<th>`, que perdería su semántica de `columnheader`) con `aria-sort` reflejando el estado actual; estados de error con `role="alert"` para que un lector de pantalla los anuncie sin depender de que el usuario los encuentre visualmente; botón de tema y buscador con `aria-label` explícito (un `title` o un `placeholder` no son un nombre accesible confiable).

✔ **Tests** — proyecto `PruebaTecnica.Web.Tests` (xUnit) cubre `MovimientoService` con un `HttpMessageHandler` falso: éxito, lista vacía, error de conexión, JSON malformado, timeout (propio y de Polly), circuit breaker abierto, cancelación del llamador y excepción no anticipada; más los invariantes de `Result<T>`.

✔ **Analizadores / CA rules / IDE rules** — `EnableNETAnalyzers` + `AnalysisLevel=latest-Recommended` + `EnforceCodeStyleInBuild` activos en todo el repo vía `Directory.Build.props`; `.editorconfig` con convenciones de nombres (campos privados `_camelCase`, tipos `PascalCase`, interfaces con prefijo `I`) y de estilo; los warnings de nulabilidad se tratan como error. Las supresiones puntuales (`CA1000` en `Result<T>`, `CA1707` en el proyecto de tests) están documentadas con `Justification`, no silenciadas a ciegas.

✔ **Escalabilidad/Mantenibilidad** — agregar una nueva fuente de datos, un nuevo campo en la grilla o un nuevo estado de UI no requiere tocar más de un archivo por cambio, gracias a la separación Services/Models/Components.

## Posibles mejoras futuras

- Tests de componente (`bUnit`) para `Movimientos.razor` (los tests actuales cubren la capa de servicio; falta la interacción usuario→UI: búsqueda, ordenamiento, auto-refresco).
- CSP con nonce por request (hoy se añaden cabeceras de seguridad de bajo riesgo; una CSP completa requiere inyectar un nonce en el script inline de `App.razor` y no se implementó para no arriesgar una regresión sin poder validarla visualmente en este entorno).
- Paginación server-side si el catálogo real crece más allá de unos cientos de registros (hoy el dataset es pequeño y paginar en memoria sería ruido).
- Métricas de resiliencia (`Microsoft.Extensions.Http.Resilience` ya emite telemetría vía `OpenTelemetry`) conectadas a un panel de observabilidad.
- Autenticación/autorización si la API real la requiere (hoy no está en el alcance del enunciado).
