# Panel de Movimientos

Blazor Web App (.NET 10, modo interactivo Server) que consume una API REST y muestra el catálogo de movimientos de un sistema de inventario en una grilla: búsqueda, ordenamiento por columna, auto-refresco opcional y tema claro/oscuro.

## Demo en vivo

- **Aplicación**: https://pruebatecnica-web.onrender.com
- **API consumida** (mock, ver nota abajo): https://pruebatecnica-mockapi.onrender.com/api/movimientos

Las dos corren en el plan gratuito de Render, que duerme el servicio tras ~15 minutos sin tráfico. Si la primera carga tarda 30-60 segundos, es el contenedor reactivándose, no un fallo.

No recibí una URL real de la API para esta prueba, solo el formato de JSON esperado. Por eso el repo incluye `PruebaTecnica.MockApi`, un backend propio que reproduce ese mismo contrato, para poder entregar la aplicación funcionando contra un servicio HTTP real en vez de datos en memoria. Si la URL oficial aparece más adelante, el cambio para conectarla está descrito en [Conectar con la API real](#conectar-con-la-api-real) y no toca una sola línea de código.

> **Sobre la versión de .NET:** el enunciado pedía .NET 9. Esta máquina solo tenía el SDK de .NET 10 instalado, así que el proyecto apunta a `net10.0`. No usa nada exclusivo de esa versión — migrar a `net9.0` es cambiar el `TargetFramework` en los `.csproj` con el SDK correspondiente ya instalado.

## Índice

- [Demo en vivo](#demo-en-vivo)
- [Arquitectura](#arquitectura)
- [Tecnologías](#tecnologías)
- [Estructura del repositorio](#estructura-del-repositorio)
- [Cómo ejecutar](#cómo-ejecutar)
- [Despliegue](#despliegue)
- [Conectar con la API real](#conectar-con-la-api-real)
- [Decisiones técnicas](#decisiones-técnicas)
- [Buenas prácticas implementadas](#buenas-prácticas-implementadas)
- [Posibles mejoras futuras](#posibles-mejoras-futuras)

## Arquitectura

La solución tiene tres proyectos:

```
PruebaTecnica.sln
├── src/PruebaTecnica.Web        → Aplicación Blazor Web App (el entregable de la prueba)
├── src/PruebaTecnica.MockApi    → API REST mínima que simula el backend externo
└── src/PruebaTecnica.Web.Tests  → Tests unitarios (xUnit) de la capa de servicio
```

`PruebaTecnica.Web` no está partido en Domain/Application/Infrastructure al estilo Clean Architecture "de libro". Para una sola pantalla que consume un endpoint y muestra una tabla, repartir eso en varios ensamblados es complejidad que no se paga sola. En vez de eso, la separación se aplica como regla de dependencia dentro de un único proyecto:

```
Components/Pages   → UI. Solo orquesta estado (cargando/error/datos) y renderiza.
Components/Shared  → Componentes de presentación puros (badge, skeleton, estados vacíos).
Services/           → Contratos + implementación de acceso a datos. Única capa que conoce HTTP.
Models/              → DTOs fuertemente tipados que reflejan el contrato de la API.
Configuration/       → Opciones fuertemente tipadas (Options Pattern).
Common/              → Primitivas transversales (Result<T>).
```

La regla que se mantiene en todo el proyecto: ningún componente Razor llama a `HttpClient` directamente. Todo pasa por `IMovimientoService`, inyectado por `@inject`. Eso deja la puerta abierta a cambiar la implementación (una versión con caché, un mock en tests) sin tocar la UI:

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
| CSS puro (variables, grid, flexbox) | UI sin dependencias de terceros |

No usé un UI kit como MudBlazor: la app tiene una tabla, un buscador y unos botones, y arrastrar un design system completo para eso no se justifica. Con CSS moderno (custom properties, `color-scheme`, un par de animaciones) se llega a la misma calidad visual sin la dependencia.

`Microsoft.Extensions.Http.Resilience` sí lo sumé, porque es first-party de Microsoft, se integra en una línea (`AddStandardResilienceHandler`) y resuelve algo real — resiliencia ante fallos de red — sin reinventar retry/backoff a mano, que es fácil de dejar mal hecho (sin jitter, sin límite de reintentos, etc.).

## Estructura del repositorio

```
.editorconfig            Convenciones de estilo/nombres para dotnet format, el IDE y los analizadores.
.dockerignore             Excluye bin/obj del contexto de build de Docker.
Directory.Build.props    Configuración MSBuild común a todos los proyectos (Nullable, analizadores, CA rules).
render.yaml               Blueprint de Render: despliega Web y MockApi ya conectados entre sí.
DEPLOY.md                 Guía paso a paso del despliegue en Render.
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
│   ├── Dockerfile
│   └── Program.cs
├── PruebaTecnica.Web.Tests/     Tests unitarios (xUnit) de MovimientoService y Result<T>
├── PruebaTecnica.MockApi/
│   ├── MovimientosDataSource.cs  Datos de ejemplo (20 movimientos, contrato PascalCase exacto)
│   ├── MovimientoDto.cs
│   ├── Dockerfile
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

`appsettings.json` de `PruebaTecnica.Web` ya apunta al mock (`http://localhost:5206`), así que con ambos procesos corriendo la grilla carga datos reales de un servicio HTTP independiente, no datos en memoria del propio proceso.

Para correr los tests:

```bash
dotnet test
```

## Despliegue

Ambos servicios corren en Render (plan gratuito) desde `render.yaml`: el blueprint crea `pruebatecnica-mockapi` y `pruebatecnica-web` y los conecta entre sí solo. La guía paso a paso está en [DEPLOY.md](DEPLOY.md).

## Conectar con la API real

Cuando aparezca la URL oficial de la prueba, el único cambio necesario es en `appsettings.json` (o `appsettings.Production.json` / variables de entorno del hosting):

```json
{
  "ApiSettings": {
    "BaseUrl": "https://api-real.example.com",
    "Endpoint": "/api/movimientos",
    "TimeoutSeconds": 15
  }
}
```

No hay que tocar código. `ApiSettings` se valida con `DataAnnotations` al arrancar (`ValidateOnStart`), así que una URL faltante o mal escrita hace fallar el arranque con un mensaje claro en vez de un error confuso más adelante. `PruebaTecnica.MockApi` se puede quitar de la solución sin que `PruebaTecnica.Web` se entere.

## Decisiones técnicas

**`Result<T>` en vez de excepciones para control de flujo.** `MovimientoService` captura todo lo que puede salir mal en una llamada HTTP: `HttpRequestException`, timeout (tanto del `HttpClient` como de la estrategia de resiliencia de Polly), circuit breaker abierto, JSON malformado, y como red de seguridad final, cualquier excepción no anticipada — nada de eso debería escapar del servicio y tumbar el circuito de Blazor Server. Cada caso se loggea (con métodos `[LoggerMessage]` generados en compilación, no interpolación clásica) y se traduce a un `Result<T>.Failure(mensaje)`. La UI nunca hace `try/catch` de HTTP, solo reacciona a un valor tipado.

**Contrato JSON explícito con `[JsonPropertyName]`.** El modelo `Movimiento` fija los nombres exactos (`Codigo`, `Descripcion`, `VActiva`) en vez de confiar en una política de naming implícita. Si el backend cambia el contrato, la deserialización falla de forma visible en vez de fallar en silencio con campos en su valor por defecto.

**La resiliencia se configura en `Program.cs`, no dentro del servicio.** `MovimientoService` no sabe nada de reintentos ni circuit breakers — es una responsabilidad de infraestructura, configurada una sola vez vía `AddStandardResilienceHandler`. El servicio solo pide datos y traduce errores.

**Auto-refresco con `PeriodicTimer`, no con un `Timer` clásico ni un `setInterval` en JS.** Se cancela de forma cooperativa con `CancellationToken`, no dispara un tick nuevo si el anterior sigue en curso, y se libera correctamente en `DisposeAsync` — importante en un circuito de Blazor Server que puede vivir minutos u horas.

**Cambio de tema sin parpadeo.** La preferencia se aplica con un script inline en `<head>` antes del primer render (lee `localStorage` o `prefers-color-scheme`), y `ThemeToggle` solo sincroniza su estado después de `OnAfterRenderAsync`. Es la única interacción con `IJSRuntime` en todo el proyecto, y es deliberada: es el único caso que Blazor Server no puede resolver sin un round-trip al navegador.

## Buenas prácticas implementadas

**SOLID.** `MovimientoService` traduce HTTP a dominio y nada más, `ApiSettings` es solo configuración, cada componente de `Shared` renderiza un único estado visual (SRP). La UI depende de `IMovimientoService`, no de la implementación ni de `HttpClient` (DIP), y esa interfaz expone un único método coherente con su nombre, sin métodos que unos consumidores usan y otros no (ISP).

**Clean Code.** Nombres descriptivos en español para el dominio del negocio y en inglés donde es lo idiomático (tipos de infraestructura). Métodos pequeños, sin regiones, sin `var` cuando el tipo no es evidente. El boilerplate de hosting para Render vive una sola vez en `src/Shared/`, no copiado en los dos proyectos que se despliegan.

**Rendimiento.** Todo el flujo de datos es `async`/`await` de punta a punta, sin bloqueos (`.Result`, `.Wait()`). Una guarda de reentrancia evita que un tick de auto-refresco y un clic manual disparen peticiones solapadas. El filtrado y ordenamiento de la tabla se calcula una sola vez por render, no una vez por cada lugar del markup que lo usa. El logging usa `[LoggerMessage]` (CA1848) en vez de las llamadas clásicas a `ILogger`.

**Seguridad.** La `BaseUrl` de la API nunca está hardcodeada — se valida con `[Url]` al arrancar, y esa misma validación corre de forma síncrona antes de configurar la resiliencia HTTP, para que un `TimeoutSeconds` inválido falle igual de rápido. Cada respuesta lleva cabeceras `X-Content-Type-Options`, `Referrer-Policy`, `X-Frame-Options` y `Permissions-Policy`. Razor escapa HTML por defecto, así que no hay interpolación cruda. Los paquetes de terceros se revisaron con `dotnet list package --vulnerable` (limpio) y `--outdated`; `Microsoft.OpenApi` se quedó deliberadamente en 2.x en vez del 3.x disponible, porque es un salto de versión mayor sin CVEs pendientes en la versión actual, y `Microsoft.AspNetCore.OpenApi` de ASP.NET Core 10 está construido contra esa superficie de API.

**Accesibilidad.** Los encabezados de columna ordenables son un `<button>` real dentro del `<th>`, no un `role="button"` puesto sobre el propio `<th>` (eso le habría quitado su semántica de `columnheader`), con `aria-sort` reflejando el estado actual. Los estados de error usan `role="alert"` para que un lector de pantalla los anuncie sin que el usuario tenga que encontrarlos visualmente. El botón de tema y el buscador llevan `aria-label` explícito, porque un `title` o un `placeholder` no son un nombre accesible confiable.

**Tests.** `PruebaTecnica.Web.Tests` cubre `MovimientoService` con un `HttpMessageHandler` falso: éxito, lista vacía, error de conexión, JSON malformado, timeout propio y de Polly, circuit breaker abierto, cancelación del llamador, excepción no anticipada. También los invariantes de `Result<T>`.

**Analizadores y reglas de estilo.** `EnableNETAnalyzers`, `AnalysisLevel=latest-Recommended` y `EnforceCodeStyleInBuild` activos en todo el repo vía `Directory.Build.props`, más un `.editorconfig` con convenciones de nombres (campos privados `_camelCase`, tipos `PascalCase`, interfaces con prefijo `I`). Los warnings de nulabilidad se tratan como error. Las dos supresiones puntuales que hay (`CA1000` en `Result<T>`, `CA1707` en el proyecto de tests) están documentadas con su `Justification`, no silenciadas sin explicación.

**Escalabilidad y mantenibilidad.** Agregar una fuente de datos nueva, un campo más en la grilla o un estado de UI distinto no debería tocar más de un archivo por cambio — es la consecuencia directa de mantener separados Services, Models y Components.

## Posibles mejoras futuras

- Tests de componente (`bUnit`) para `Movimientos.razor`. Los tests actuales cubren la capa de servicio; falta la interacción usuario→UI (búsqueda, ordenamiento, auto-refresco).
- CSP con nonce por request. Hoy hay cabeceras de seguridad de bajo riesgo, pero una CSP completa requiere inyectar un nonce en el script inline de `App.razor`, y no quise arriesgar una regresión sin poder validarla visualmente en este entorno.
- Paginación server-side, si el catálogo real llega a crecer más allá de unos cientos de registros — hoy el dataset es pequeño y paginar en memoria sería ruido innecesario.
- Métricas de resiliencia conectadas a un panel de observabilidad (`Microsoft.Extensions.Http.Resilience` ya emite telemetría vía `OpenTelemetry`, solo falta conectarla).
- Autenticación/autorización, si la API real llega a requerirla — no está en el alcance del enunciado actual.
