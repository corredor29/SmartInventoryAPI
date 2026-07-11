# SmartInventoryAPI

Backend .NET del sistema **SmartInventory AI**: gestión de inventario, ventas, facturación y un chatbot con IA para una tienda de tecnología.

## Tabla de contenido

- [Descripción general](#descripción-general)
- [Arquitectura](#arquitectura)
- [Stack tecnológico](#stack-tecnológico)
- [Requisitos previos](#requisitos-previos)
- [Configuración inicial](#configuración-inicial)
- [Levantar la base de datos con Docker](#levantar-la-base-de-datos-con-docker)
- [Restaurar dependencias y aplicar migraciones](#restaurar-dependencias-y-aplicar-migraciones)
- [Correr el proyecto](#correr-el-proyecto)
- [Probar la API](#probar-la-api)
- [Correr los tests](#correr-los-tests)
- [Estructura de endpoints principales](#estructura-de-endpoints-principales)
- [Roles del sistema](#roles-del-sistema)
- [Integración con otros servicios](#integración-con-otros-servicios)
- [Variables de entorno / configuración completa](#variables-de-entorno--configuración-completa)
- [Troubleshooting común](#troubleshooting-común)
- [Estructura de carpetas](#estructura-de-carpetas)
- [Notas conocidas](#notas-conocidas)

## Descripción general

SmartInventoryAPI es el backend de un sistema de inventario y ventas para una tienda de tecnología, con soporte para un chatbot conversacional que permite a los clientes comprar productos por chat. Expone la gestión de productos, categorías, inventario (con sus movimientos), clientes, ventas, facturas y usuarios, todo protegido con autenticación JWT y control de acceso por roles.

El componente diferenciador es la integración con un chatbot con IA: este backend recibe los mensajes del cliente, los reenvía a un servicio externo construido en FastAPI (que interpreta la conversación y decide acciones como buscar productos o generar una venta), y persiste tanto la conversación como sus resultados. Cuando el chatbot no puede resolver una consulta, la sesión se puede escalar a un asesor humano, que recibe la notificación en tiempo real vía SignalR.

Este repositorio contiene **únicamente el backend .NET**. El motor del chatbot (FastAPI/Python) y el frontend (React) son proyectos independientes que consumen esta API; no están incluidos aquí.

## Arquitectura

El proyecto sigue una arquitectura por capas clásica, con las dependencias fluyendo en una sola dirección:

```text
┌─────────────────────────────────────────────┐
│                     Api                      │  Controllers, middlewares, filtros,
│   (Controllers, Hubs, Middleware, Filters)   │  hub de SignalR, configuración de arranque
└───────────────────┬───────────────────────────┘
                     │ depende de
                     ▼
┌─────────────────────────────────────────────┐
│                 Application                  │  Casos de uso / servicios de negocio,
│  (Services, Contracts, DTOs, Validators)     │  contratos (interfaces), DTOs, validadores
└───────────────────┬───────────────────────────┘
                     │ depende de
                     ▼
┌─────────────────────────────────────────────┐
│                    Domain                    │  Entidades y Value Objects con
│      (Entities, ValueObjects, Common)        │  reglas de negocio e invariantes
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│                Infrastructure                │  EF Core, repositorios, Unit of Work,
│ (Repositories, UnitOfWork, Migrations, Data) │  seeder, clientes HTTP externos
└───────────────────┬───────────────────────────┘
      depende de Application y Domain, y es
      consumida (por inyección de dependencias)
      únicamente desde Api

┌─────────────────────────────────────────────┐
│                    Tests                     │  Pruebas unitarias (xUnit + Moq)
│         (SmartInventory.Tests)               │  de la capa Application
└─────────────────────────────────────────────┘
```

- **Domain**: entidades (`Product`, `Sale`, `Inventory`, `Invoice`, `User`, etc.) y Value Objects inmutables con validación en el constructor o en factories estáticas (`ProductName`, `StockQuantity`, `SaleOriginName`, ...). No depende de ninguna otra capa del proyecto.
- **Application**: servicios de negocio (`SaleService`, `InventoryService`, `AuthService`, `ProductService`, ...), contratos de repositorios y servicios, DTOs de entrada/salida, validadores con FluentValidation y excepciones de dominio de aplicación (`NotFoundException`, `BusinessException`). Depende solo de Domain.
- **Infrastructure**: `AppDbContext` (EF Core + Npgsql + pgvector), implementaciones de los repositorios, `EfUnitOfWork`, migraciones, el `DbSeeder` y el cliente HTTP hacia el chatbot (`FastApiChatbotClient`). Depende de Application y Domain.
- **Api**: proyecto ASP.NET Core Web API — controllers, el hub de SignalR (`ChatHub`), middlewares (manejo global de excepciones, logging de requests), el filtro de respuesta unificada, y toda la configuración de arranque (JWT, CORS, rate limiting, DI) en `Program.cs` y `Api/Extensions`.
- **Tests**: proyecto `SmartInventory.Tests` con pruebas unitarias de la capa Application usando xUnit y Moq.

## Stack tecnológico

| Categoría | Tecnología / paquete | Versión |
|---|---|---|
| Framework | .NET | `net10.0` (todos los proyectos) |
| Web framework | ASP.NET Core (`Microsoft.NET.Sdk.Web`) | 10 |
| ORM | Microsoft.EntityFrameworkCore / .Design | 10.0.7 |
| Base de datos | Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.1 |
| Búsqueda vectorial | Pgvector / Pgvector.EntityFrameworkCore | 0.3.2 / 0.3.0 |
| Autenticación | Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.7 |
| Tokens JWT | System.IdentityModel.Tokens.Jwt | 8.19.1 |
| Hashing de contraseñas | BCrypt.Net-Next | 4.2.0 |
| Validación | FluentValidation / FluentValidation.AspNetCore | 12.1.1 / 11.3.1 |
| Mapeo objeto-objeto | Mapster | 10.0.7 |
| Tiempo real | SignalR (incluido en ASP.NET Core) | — |
| Documentación de API | `Microsoft.AspNetCore.OpenApi` (sin UI visual) | 10.0.7 |
| Tests | xUnit / xunit.runner.visualstudio | 2.9.3 / 3.1.4 |
| Mocking en tests | Moq | 4.20.72 |
| Cobertura de tests | coverlet.collector | 6.0.4 |

## Requisitos previos

- **.NET SDK 10.0** (verificado con `dotnet --version`; el proyecto usa `net10.0` en todos los `.csproj`).
- **Docker Desktop**, para levantar PostgreSQL con la extensión `pgvector`.
- **dotnet-ef** (herramienta global), para aplicar migraciones:

  ```bash
  dotnet tool install --global dotnet-ef
  ```

## Configuración inicial

1. Clonar el repositorio:

   ```bash
   git clone <url-del-repositorio>
   cd SmartInventoryAPI
   ```

2. Crear/editar `Api/appsettings.Development.json` con tus propios valores (este archivo está en `.gitignore`, así que no se sube al repo). Como plantilla, usa esta estructura — reemplaza cada placeholder por tus valores reales:

   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     },
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5433;Database=smartinventory;Username=postgres;Password=<TU_PASSWORD>"
     },
     "Jwt": {
       "Secret": "<CADENA_ALEATORIA_DE_AL_MENOS_32_CARACTERES>",
       "Issuer": "SmartInventoryAPI",
       "Audience": "SmartInventoryClient",
       "ExpiryMinutes": "120"
     },
     "Chatbot": {
       "BaseUrl": "http://localhost:8000"
     },
     "Frontend": {
       "Url": "http://localhost:5173"
     },
     "RateLimiting": {
       "Global": { "PermitLimit": 100, "WindowMinutes": 1 },
       "Auth": { "PermitLimit": 5, "WindowMinutes": 1 },
       "Chatbot": { "PermitLimit": 30, "WindowMinutes": 1 }
     }
   }
   ```

   > ⚠️ **`Jwt:Secret` debe ser una cadena aleatoria de al menos 32 caracteres.** La API la usa como clave simétrica (`SymmetricSecurityKey`) para firmar y validar los tokens; si es demasiado corta, la validación del token puede fallar. **Nunca subas un valor real de este secreto (ni de la contraseña de la base de datos) al repositorio.**

## Levantar la base de datos con Docker

El proyecto usa la imagen `pgvector/pgvector:pg17`, que es PostgreSQL 17 con la extensión `pgvector` preinstalada. Esta extensión es necesaria porque los productos almacenan un embedding vectorial (`Product.Embedding`) usado para búsqueda semántica desde el chatbot; una imagen estándar de `postgres` no la soporta.

```bash
docker compose up -d
```

Esto levanta un contenedor `smartinventory-db` con PostgreSQL escuchando en el puerto **5433** del host (mapeado al 5432 del contenedor) y persistencia en el volumen `smartinventory_data`. El puerto y las credenciales exactas están definidos en `docker-compose.yml`; asegúrate de que tu `ConnectionStrings:DefaultConnection` use el mismo puerto (`5433`) y las mismas credenciales que configuraste ahí.

## Restaurar dependencias y aplicar migraciones

```bash
dotnet restore
dotnet ef database update --project Infrastructure --startup-project Api
```

Al arrancar, la API siembra datos iniciales automáticamente (`DbSeeder`, invocado desde `Program.cs` vía `app.SeedDatabaseAsync()`), incluyendo catálogos base y un usuario administrador:

- **Roles**: Administrador, Asesor, Cliente
- **Estados de producto**: Activo, Inactivo
- **Categorías**: Laptops, Periféricos, Componentes, Accesorios
- **Tipos de movimiento de inventario**: Entrada, Salida, Ajuste
- **Orígenes de venta**: Manual, Chatbot
- **Estados de venta**: Pendiente, Completada, Cancelada
- **Estados de sesión de chat**: Activa, Escalada, Cerrada
- **Tipos de remitente de chat**: Bot, Cliente, Asesor
- **Estados de escalamiento**: Pendiente, En Progreso, Resuelto

Usuario administrador sembrado (**solo para desarrollo, cambiar en producción**):

- Email: `admin@smartinventory.com`
- Password: `Admin123!`

Usuarios asesor sembrados:

| Nombre | Email | Password |
|---|---|---|
| Danny Velasco | `danny.velasco@smartinventory.com` | `Asesor123!` |
| Andres Navas | `andres.navas@smartinventory.com` | `Asesor123!` |

El seeder es idempotente: cada sección verifica si ya existen registros antes de insertar, así que es seguro reiniciar la API varias veces sin duplicar datos.

## Correr el proyecto

```bash
dotnet run --project Api
```

El puerto real depende de tu entorno local; revisa `Api/Properties/launchSettings.json` para confirmarlo (actualmente define el perfil `http` en `http://localhost:5299` y el perfil `https` en `https://localhost:7209` / `http://localhost:5299`, ambos con `ASPNETCORE_ENVIRONMENT=Development`).

## Probar la API

El proyecto usa `AddOpenApi()`/`MapOpenApi()` nativo de ASP.NET Core (sin Swagger UI ni interfaz visual). En Development, el documento OpenAPI en JSON queda disponible normalmente en `/openapi/v1.json`, pero para explorar y probar los endpoints se recomienda Postman, Insomnia o `curl`.

Flujo típico de autenticación:

1. **Login** para obtener un token:

   ```http
   POST /api/auth/login
   Content-Type: application/json

   {
     "email": "admin@smartinventory.com",
     "password": "Admin123!"
   }
   ```

   La respuesta incluye `token`, `name`, `email` y `role`.

2. **Usar el token** en las peticiones a endpoints protegidos, en el header `Authorization`:

   ```http
   GET /api/products
   Authorization: Bearer {token}
   ```

Con `curl`:
```bash
TOKEN=$(curl -s -X POST http://localhost:5299/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@smartinventory.com","password":"Admin123!"}' \
  | jq -r '.data.token')

curl http://localhost:5299/api/users -H "Authorization: Bearer $TOKEN"
```

Todas las respuestas de la API se envuelven en un formato unificado (`UnifiedResponseFilter`): `{ "success": true, "data": ... }` en éxito, o `{ "success": false, "message": "..." }` en error (manejado por `ExceptionMiddleware`).

## Correr los tests

```bash
dotnet test
```

El proyecto `Tests/SmartInventory.Tests` sí tiene contenido real: 4 archivos de pruebas unitarias (xUnit + Moq) que cubren, dentro de la capa Application, los servicios `SaleService`, `InventoryService`, `AuthService` y `ProductService` (creación de ventas con validación de stock y rollback transaccional, ajuste de inventario, login con BCrypt, alta de productos, etc.). El resto de servicios de Application (`CustomerService`, `UserService`, `InvoiceService`, servicios de catálogos, servicios de chat) y las capas Infrastructure y Api no tienen pruebas todavía.

## Estructura de endpoints principales

Auth requerido = necesita header `Authorization: Bearer {token}` válido. "Roles" indica los roles permitidos por `[Authorize(Roles = "...")]`; "Anónimo" indica `[AllowAnonymous]`.

### Auth (`/api/auth`)

| Método | Ruta | Descripción | Acceso |
|---|---|---|---|
| POST | `/api/auth/login` | Login, devuelve JWT | Anónimo (rate limit `auth`) |

### Products (`/api/products`)

| Método | Ruta | Descripción | Acceso |
|---|---|---|---|
| GET | `/api/products` | Lista todos los productos | Anónimo |
| GET | `/api/products/{id}` | Detalle de un producto | Anónimo |
| GET | `/api/products/search?q=` | Búsqueda semántica (pgvector) con fallback por texto | Anónimo (rate limit `chatbot`) |
| POST | `/api/products` | Crear producto (genera embedding si hay API key) | Administrador |
| PUT | `/api/products/{id}` | Actualizar producto | Administrador |
| PATCH | `/api/products/{id}/status` | Cambiar estado del producto | Administrador |
| DELETE | `/api/products/{id}` | Eliminar producto | Administrador |
| POST | `/api/products/reindex-embeddings` | Regenera embeddings de todos los productos | Administrador |

### Categories (`/api/categories`) y Product Statuses (`/api/product-statuses`)

Mismo patrón en ambos: `GET` (lista y por id) es anónimo; `POST`, `PUT`, `DELETE` requieren rol Administrador.

### Inventory (`/api/inventory`)

| Método | Ruta | Descripción | Acceso |
|---|---|---|---|
| GET | `/api/inventory` | Lista todo el inventario | Administrador, Asesor |
| GET | `/api/inventory/{id}` | Inventario por id | Administrador, Asesor |
| GET | `/api/inventory/{productId}/stock` | Stock actual de un producto (usado por el chatbot) | Anónimo (rate limit `chatbot`) |
| PATCH | `/api/inventory/{productId}/adjust` | Ajustar stock (entrada/salida) | Administrador, Asesor |

### Inventory Movements (`/api/inventory-movements`) y Movement Types (`/api/movement-types`)

| Método | Ruta | Descripción | Acceso |
|---|---|---|---|
| GET | `/api/inventory-movements` | Lista movimientos | Administrador, Asesor |
| GET | `/api/inventory-movements/{id}` | Movimiento por id | Administrador, Asesor |
| GET | `/api/inventory-movements/by-inventory/{inventoryId}` | Movimientos de un inventario | Administrador, Asesor |
| GET | `/api/movement-types` (y `/{id}`) | Catálogo de tipos de movimiento | Administrador, Asesor |
| POST/PUT/DELETE | `/api/movement-types` | Gestión del catálogo | Administrador |

### Sales (`/api/sales`)

| Método | Ruta | Descripción | Acceso |
|---|---|---|---|
| GET | `/api/sales` | Lista ventas | Administrador, Asesor |
| GET | `/api/sales/{id}` | Detalle de venta | Administrador, Asesor |
| POST | `/api/sales` | Registrar una venta (manual o vía chatbot) | Anónimo (rate limit `chatbot`) |
| PATCH | `/api/sales/{id}/status` | Cambiar estado de una venta | Administrador, Asesor |

### Sale Origins (`/api/sale-origins`) y Sale Statuses (`/api/sale-statuses`)

`GET` (lista y por id) requiere Administrador o Asesor; `POST`, `PUT`, `DELETE` requieren Administrador.

### Invoices (`/api/invoices`)

| Método | Ruta | Descripción | Acceso |
|---|---|---|---|
| GET | `/api/invoices` | Lista facturas | Administrador, Asesor |
| GET | `/api/invoices/{id}` | Factura por id | Administrador, Asesor |
| GET | `/api/invoices/number/{invoiceNumber}` | Factura por número (ej. `FAC-000001`) | Anónimo |

### Chat (`/api/chat`, `/api/chat-sessions`, `/api/chat-session-statuses`, `/api/chat-messages`, `/api/sender-types`)

| Método | Ruta | Descripción | Acceso |
|---|---|---|---|
| POST | `/api/chat/message` | Enviar mensaje del cliente, obtener respuesta del bot | Anónimo (rate limit `chatbot`) |
| GET | `/api/chat-sessions` (y `/{id}`) | Consultar sesiones de chat | Administrador, Asesor |
| PATCH | `/api/chat-sessions/{id}/status` | Cambiar estado de una sesión | Administrador, Asesor |
| GET | `/api/chat-session-statuses` (y `/{id}`) | Catálogo de estados de sesión | Administrador, Asesor |
| POST/PUT/DELETE | `/api/chat-session-statuses` | Gestión del catálogo | Administrador |
| GET | `/api/chat-messages/by-session/{chatSessionId}` | Mensajes de una sesión | Administrador, Asesor |
| GET | `/api/sender-types` (y `/{id}`) | Catálogo de tipos de remitente | Administrador, Asesor |
| POST/PUT/DELETE | `/api/sender-types` | Gestión del catálogo | Administrador |

### Chat Escalations (`/api/chat/escalations`) y Notificaciones (`/api/chat/notify-advisor`)

| Método | Ruta | Descripción | Acceso |
|---|---|---|---|
| GET | `/api/chat/escalations/pending` | Escalamientos pendientes | Administrador, Asesor |
| GET | `/api/chat/escalations/{id}` | Detalle de escalamiento | Administrador, Asesor |
| POST | `/api/chat/escalations` | Crear escalamiento (notifica a asesores vía SignalR) | Anónimo (rate limit `chatbot`) |
| PATCH | `/api/chat/escalations/{id}/assign` | Asignar escalamiento a un asesor | Administrador, Asesor |
| PATCH | `/api/chat/escalations/{id}/resolve` | Resolver escalamiento | Administrador, Asesor |
| GET | `/api/escalation-statuses` (y `/{id}`) | Catálogo de estados de escalamiento | Administrador, Asesor |
| POST/PUT/DELETE | `/api/escalation-statuses` | Gestión del catálogo | Administrador |
| POST | `/api/chat/notify-advisor` | Disparar notificación manual a asesores vía SignalR | Anónimo (rate limit `chatbot`) |

### Customers (`/api/customers`)

`GET`, `POST`, `PUT`, `DELETE` — todos requieren rol Administrador o Asesor.

### Users (`/api/users`) y Roles (`/api/roles`)

`GET`, `POST`, `PUT`, `DELETE` en ambos — todos requieren rol Administrador.

## Roles del sistema

El seeder define tres roles: **Administrador**, **Asesor** y **Cliente**. A partir de los `[Authorize(Roles = ...)]` reales en los controllers:

- **Administrador**: acceso completo. Es el único rol que puede crear, actualizar o eliminar productos, catálogos (categorías, estados de producto, tipos de movimiento, orígenes/estados de venta, tipos de remitente, estados de escalamiento), usuarios y roles. También comparte con Asesor la operación diaria (inventario, ventas, facturas, clientes, chat).
- **Asesor**: rol operativo. Puede consultar y operar inventario (incluyendo ajustes de stock), ventas, facturas, clientes, sesiones de chat, mensajes y escalamientos (asignar/resolver), y leer los catálogos de solo-lectura para su rol. No puede crear/editar/eliminar catálogos, productos, usuarios ni roles — esas operaciones están reservadas a Administrador.
- **Cliente**: existe como rol sembrado en la base de datos, pero **ningún controller lo referencia explícitamente** en sus reglas de autorización. En la práctica, el cliente final interactúa con el sistema de forma anónima: búsqueda de productos, consulta de stock, chat con el bot, creación de ventas y consulta de facturas por número son todos endpoints `[AllowAnonymous]`. Ver la nota correspondiente en [Notas conocidas](#notas-conocidas).

## Integración con otros servicios

- **Chatbot (FastAPI externo)**: `POST /api/chat/message` guarda el mensaje del cliente, lo reenvía al servicio externo configurado en `Chatbot:BaseUrl` (`FastApiChatbotClient`, que hace `POST {BaseUrl}/chat/message`), y persiste la respuesta del bot. La URL por defecto es `http://localhost:8000`. Si el bot no responde (timeout / caída), el API devuelve un mensaje amigable con `state: ERROR` en lugar de fallar en 500. Cuando el bot responde `WAITING_HUMAN_AGENT`, el API crea (o reutiliza) la escalación y notifica a asesores por SignalR. Alias Python: `POST /api/chat/escalate`.
- **SignalR**: el hub `ChatHub` se expone en `/hubs/chat` con `[AllowAnonymous]` para que clientes sin login (FAB / chatbot) puedan `JoinSession` tras una escalación. Los asesores se unen al grupo `Advisors` (`JoinAsAdvisor`) para `NewEscalation` / `AdvisorNotification`. JWT opcional vía query `access_token`.
- **Frontend (React)**: consumido vía HTTP/JSON y SignalR. El origen permitido por CORS se configura con `Frontend:Url` (política `AllowFrontend`, con `AllowCredentials`), con valor por defecto `http://localhost:5173` si la clave no está configurada.

### Arranque local del stack (3 apps + DB)

| Proceso | Puerto | Cómo |
|---|---|---|
| PostgreSQL | `5433` | `docker compose up -d` en este repo |
| API .NET | `5299` | `cd Api && dotnet run` |
| Chatbot Python | `8000` | repo `smartinventory-chatbot` → `uvicorn app.main:app --port 8000` |
| Frontend | `5173` | repo `smartinventory-frontend` → `npm run dev` |

Config chatbot: `Chatbot:BaseUrl=http://localhost:8000`, `Chatbot:TimeoutSeconds=60`. En Python: `DOTNET_API_BASE_URL=http://localhost:5299/api`.

## Variables de entorno / configuración completa

| Clave | Descripción | Obligatoria / default |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | Cadena de conexión a PostgreSQL | Obligatoria (sin default; falla si falta) |
| `Jwt:Secret` | Clave simétrica para firmar/validar JWT (mínimo 32 caracteres recomendado) | Obligatoria — la app lanza `InvalidOperationException` al arrancar si falta |
| `Jwt:Issuer` | Emisor esperado del token | Opcional, default `SmartInventoryAPI` |
| `Jwt:Audience` | Audiencia esperada del token | Opcional, default `SmartInventoryClient` |
| `Jwt:ExpiryMinutes` | Minutos de validez del token, usada por `TokenService` al generar el JWT | Opcional, default `120` (parseada como texto en `TokenService.GenerateToken`) |
| `Chatbot:BaseUrl` | URL base del servicio FastAPI del chatbot | Opcional, default `http://localhost:8000` |
| `Chatbot:TimeoutSeconds` | Timeout de la llamada HTTP al chatbot | Opcional, default `60` |
| `OpenAI:ApiKey` / `OPENAI_API_KEY` | API key para embeddings (búsqueda semántica pgvector) | Opcional — sin key, `GET /api/products/search` usa fallback por texto |
| `OpenAI:EmbeddingModel` | Modelo de embeddings | Opcional, default `text-embedding-3-small` (vector 1536) |
| `Frontend:Url` | Origen permitido por CORS para el frontend | Opcional, default `http://localhost:5173` (no está definida en ningún `appsettings*.json` del repo) |
| `RateLimiting:Global:PermitLimit` / `WindowMinutes` | Límite global de requests por IP | Opcional, default 100 / 1 min |
| `RateLimiting:Auth:PermitLimit` / `WindowMinutes` | Límite específico para `/api/auth/login` | Opcional, default 5 / 1 min |
| `RateLimiting:Chatbot:PermitLimit` / `WindowMinutes` | Límite específico para endpoints usados por el chatbot | Opcional, default 30 / 1 min |

## Troubleshooting común

- **Error de conexión a PostgreSQL al arrancar la API**: verifica que el contenedor esté corriendo con `docker ps` (debe aparecer `smartinventory-db`) y que `ConnectionStrings:DefaultConnection` use el puerto `5433` (no el `5432` por defecto de Postgres), tal como está mapeado en `docker-compose.yml`.
- **`la extensión "vector" no está disponible` / error de pgvector**: asegúrate de estar usando la imagen `pgvector/pgvector:pg17` definida en `docker-compose.yml`, no una imagen `postgres` estándar. Si ya tenías un volumen creado con otra imagen, elimínalo (`docker compose down -v`) y vuelve a levantar el contenedor para que se reinicialice con la extensión disponible.
- **Búsqueda semántica sin resultados / solo texto**: configura `OpenAI:ApiKey` (o `OPENAI_API_KEY`), reinicia la API y llama `POST /api/products/reindex-embeddings` con un JWT de Administrador para poblar la columna `embedding`. Sin key, el search sigue funcionando con tokens de texto.
- **Conflicto de puertos con un PostgreSQL nativo**: el contenedor usa el puerto `5433` del host precisamente para evitar chocar con una instancia local de PostgreSQL en el `5432` por defecto. Si aun así `5433` está ocupado, cambia el mapeo de puertos en `docker-compose.yml` y actualiza `ConnectionStrings:DefaultConnection` en consecuencia.
- **`401 Unauthorized` en endpoints protegidos**: confirma que el header sea `Authorization: Bearer {token}` (con el prefijo `Bearer` y un espacio), que el token no haya expirado, y que `Jwt:Secret`/`Issuer`/`Audience` sean los mismos con los que se generó el token (si cambiaste el secreto entre reinicios, los tokens emitidos antes dejan de ser válidos).
- **`dotnet ef` no reconocido**: instala la herramienta global con `dotnet tool install --global dotnet-ef` y asegúrate de que la carpeta de herramientas de .NET esté en tu `PATH`.

## Estructura de carpetas

```text
SmartInventoryAPI/
├── Api/                          # Proyecto Web API (punto de entrada)
│   ├── Controllers/               # 22 controllers REST (auth, catálogos, ventas, chat, ...)
│   ├── Extensions/                # Configuración de DI, JWT, CORS, rate limiting, middlewares
│   ├── Filters/                   # UnifiedResponseFilter (envoltura { success, data })
│   ├── Hubs/                      # ChatHub (SignalR) en /hubs/chat
│   ├── Middleware/                # ExceptionMiddleware, RequestLoggingMiddleware
│   ├── Properties/                # launchSettings.json (perfiles y puertos locales)
│   ├── appsettings.json           # Configuración base (versionada)
│   └── appsettings.Development.json  # Configuración local (ignorada por git)
├── Application/                  # Casos de uso / lógica de negocio
│   ├── Contracts/                 # Interfaces de repositorios y servicios
│   ├── DTOs/                      # Objetos de entrada/salida por recurso
│   ├── Exceptions/                # BusinessException, NotFoundException
│   ├── Services/                  # Implementación de los servicios de negocio
│   └── Validators/                # Validadores FluentValidation por recurso
├── Domain/                        # Núcleo de negocio, sin dependencias externas
│   ├── Common/                    # BaseEntity
│   ├── Entities/                  # Entidades (Product, Sale, Inventory, User, ...)
│   └── ValueObjects/               # Value Objects inmutables con reglas de validación
├── Infrastructure/                # Acceso a datos e integraciones externas
│   ├── Configurations/             # Configuración de EF Core (Fluent API) por entidad
│   ├── Data/Seeder/                # DbSeeder (catálogos base + usuario admin)
│   ├── ExternalServices/           # FastApiChatbotClient (HTTP hacia el chatbot)
│   ├── Migrations/                 # Migraciones de EF Core
│   ├── Repositories/                # Implementación de los repositorios (EF Core)
│   └── UnitOfWork/                  # EfUnitOfWork
├── Tests/SmartInventory.Tests/     # Pruebas unitarias (xUnit + Moq) de la capa Application
├── docker-compose.yml              # Contenedor de PostgreSQL con pgvector
├── base_de_datos.sql               # Script SQL de referencia del esquema
├── SmartInventoryAPI.slnx          # Archivo de solución (.NET, formato slnx)
└── LICENSE                         # MIT
```

## Notas conocidas

- **`Api/appsettings.json` está versionado en git y contiene actualmente valores reales** de `ConnectionStrings:DefaultConnection` (contraseña incluida) y `Jwt:Secret`. El `.gitignore` sí excluye `appsettings.Development.json` y `appsettings.Local.json`, pero **no** excluye `appsettings.json`, que es justamente el archivo con los secretos reales. Se recomienda rotar esas credenciales, mover los valores sensibles a `dotnet user-secrets`, variables de entorno o un vault, y dejar solo placeholders en el `appsettings.json` versionado.
- **`Frontend:Url` no está definida en ningún `appsettings*.json` del repositorio**, aunque `AddCorsPolicy` la lee (`configuration["Frontend:Url"]`). Hoy en día siempre cae al valor por defecto `http://localhost:5173` embebido en el código; si el frontend corre en otro puerto/host, hay que agregar esta clave explícitamente.
- **El rol `Cliente`** se siembra en la base de datos pero ningún controller lo usa en `[Authorize(Roles = ...)]`. Los flujos pensados para clientes finales (búsqueda de productos, consulta de stock, chat, creación de ventas, consulta de facturas) están todos marcados `[AllowAnonymous]`, así que en la práctica el backend no distingue actualmente un "Cliente" autenticado de un visitante anónimo.
- **`Jwt:ExpiryMinutes`** está presente en `appsettings.json`, pero `AddJwtAuthentication` en `ServiceCollectionExtensions.cs` no la lee (no se usa para configurar la expiración de los tokens en la validación); confirma en `TokenService` cómo se está usando (o no) ese valor si necesitas ajustar la duración real de los tokens.
- **Dependencias sin uso aparente**: el paquete `MediatR` está referenciado en `Application.csproj` pero no se encontró ningún uso de `IMediator`/`IRequest` en el código. `Infrastructure.csproj` referencia además `Microsoft.AspNetCore.App` (metapaquete legado) y `Microsoft.Extensions.Options.ConfigurationExtensions`, ambos señalados como innecesarios por el propio `dotnet restore` (warnings `NETSDK1080` y `NU1510`) en un proyecto .NET moderno.
- **No hay Swagger UI ni interfaz visual de documentación**: el proyecto usa `AddOpenApi()`/`MapOpenApi()` nativo de ASP.NET Core, que solo expone el documento OpenAPI en JSON (sin UI) y únicamente en el entorno de Development.
- **Cobertura de tests parcial**: `Tests/SmartInventory.Tests` cubre `SaleService`, `InventoryService`, `AuthService` y `ProductService`. El resto de la capa Application (catálogos, chat, facturas, usuarios, clientes) y las capas Infrastructure/Api no tienen pruebas automatizadas todavía.
