# Postal Delivery System

Real-time postal delivery platform for branch-based order management, courier coordination, and live customer tracking.

## Purpose

This repository currently contains implementation guidance and architecture notes for building the first production version of the system. The documents below are aligned around a single V1 strategy so backend, database, realtime, and UI decisions do not conflict.

## V1 Scope

- Role-based access for SuperAdmin, Admin, Courier, and Customer
- Order creation, assignment, acceptance, pickup, delivery, and cancellation
- Courier live location updates with throttling rules
- Admin dashboard with live branch monitoring
- Customer order tracking page with realtime status updates

## Core Technical Decisions

- Architecture: modular monolith
- Backend: .NET 8 Web API
- Data access: Dapper
- Primary database: PostgreSQL
- Cache and scale-out support: Redis
- Realtime transport: SignalR over WebSockets with Redis backplane
- Admin UI: Blazor Server
- Courier app: .NET MAUI Blazor Hybrid
- Customer UI: responsive Blazor Web App or PWA
- Shared code: Razor Class Library, shared DTOs, shared API clients

## Documentation Map

- POSTAL_DELIVERY_SYSTEM.md: master product, scope, roles, lifecycle, and delivery plan
- backend_architecture.md: runtime topology, modules, backend layering, database, order engine, tracking, realtime, and security
- ui_architecture.md: admin web, courier mobile, customer tracking, realtime UI behavior, and shared UI boundaries
- prompt.txt: implementation prompt aligned with the documents above

## Suggested Delivery Order

1. Authentication, roles, and branch model
2. Order management and admin assignment flow
3. Courier app with status transitions
4. Tracking ingestion and Redis-backed realtime updates
5. Admin live dashboard and customer tracking page
6. Hardening: rate limits, audit logs, monitoring, and deployment

## Milestones Snapshot

- `M1 - Secure Platform Skeleton`: solution structure, auth, roles, branches, health checks
- `M2 - Branch Operations Ready`: orders, assignment flow, status history, admin shell
- `M3 - End-to-End Delivery Flow`: courier mobile execution and validated status transitions
- `M4 - Realtime Backend Ready`: tracking ingestion, Redis, SignalR groups, live event publishing
- `M5 - Realtime User Experience Ready`: admin live dashboard and customer tracking page
- `M6 - Staging Release Candidate`: hardening, observability, tests, staging deployment

Detailed sprint-level backlog and exit criteria live in `POSTAL_DELIVERY_SYSTEM.md`.

## Implementation Principles

- Prefer simple modules over early microservices.
- Use async I/O throughout the stack.
- Keep write-heavy tracking data isolated from transactional order data.
- Treat Redis as a fast operational store, not the system of record.
- Every state change must be validated, authorized, and auditable.

## Current Status

**M6 — Staging Release Candidate** is complete. All six sprints are implemented.

Implemented artifacts:

- modular monolith solution with `Api`, `Application`, `Domain`, `Infrastructure`, `Shared.Contracts`, and tests
- JWT access token and refresh token flow with rotation and revoke endpoint
- role and branch-scoped authorization baseline (`SuperAdmin`, `Admin`, `Courier`, `Customer`)
- one-time initial `SuperAdmin` bootstrap endpoint
- global exception middleware and health checks for PostgreSQL and Redis
- Dapper repositories for branches, users, and refresh tokens
- orders schema and order status history persistence with optimistic assignment transition (`Created -> Assigned`)
- order CRUD and filtering APIs with branch-scope enforcement
- courier assignment workflow API with branch and role validation
- admin web shell project (Blazor Server) with login, orders, couriers, and users pages
- courier order API for assigned list, detail, and legal transitions (`Assigned -> Accepted -> PickedUp -> OnTheWay -> Delivered`)
- courier service transition validation with optimistic concurrency conflict handling
- courier mobile app (.NET MAUI Blazor Hybrid) with login, refresh-based session restore, assigned orders list, order detail, and action commands
- tracking ingestion API with validation and throttling rules (minimum interval, movement threshold, heartbeat)
- latest-location persistence in PostgreSQL plus Redis latest-location cache keys for courier and order
- SignalR tracking hub with authorized order, branch, and courier groups
- realtime `location_update` publishing from accepted tracking points
- realtime `status_update` publishing from assignment and courier status transitions
- Sprint 1 through Sprint 4 SQL migrations
- admin live dashboard with KPI cards and live order status updates via SignalR
- admin live tracking page with Leaflet map, active order list, and real-time courier markers
- `ConnectionBanner` and `StatusBadge` shared Razor components
- `TrackingRealtimeClient` registered as scoped DI service in AdminWeb
- customer web app (`CustomerWeb`) with login, order list, and live tracking page
- customer live tracking page with Leaflet map, status timeline, and real-time courier position
- ASP.NET Core rate limiting: fixed-window on auth endpoints (10 req/min per IP), sliding-window on tracking ingestion (120 req/min per user)
- `CorrelationIdMiddleware` — reads or generates `X-Correlation-Id` and echoes it on every response
- `RefreshTokenCleanupWorker` — background service that deletes expired and old-revoked tokens hourly
- `StaleCourierDetectorWorker` — background service that logs a warning every 2 minutes when a courier on an active delivery has no recent location ping
- SQL migration 0004: `audit_logs` table with indexes, `idx_refresh_tokens_cleanup` index, and `courier_locations` monthly partitions for 2026
- 5 unit tests for background workers using in-memory fakes and a recording logger
- 6 HTTP-level smoke tests using `WebApplicationFactory<Program>`: liveness probe, 401 on protected endpoints, rate-limit 429, `Retry-After` header, `X-Correlation-Id` echo
- full Docker Compose with health-checked services for PostgreSQL, Redis, API, AdminWeb, and CustomerWeb
- Dockerfiles for API, AdminWeb, and CustomerWeb using multi-stage builds
- `/health/live` (always healthy), `/health/ready` (checks DB and Redis), `/health` (full report)

## Local Setup

### Option A — Docker Compose (full stack)

```bash
# Copy and edit the signing key before starting
export JWT_SIGNING_KEY="your-production-signing-key-min-32-chars"

docker compose up -d
```

Apply migrations once the `postgres` container is healthy:

```bash
docker compose exec postgres psql -U postgres -d postal_delivery \
  -f /dev/stdin < src/Infrastructure/Database/Migrations/0001_sprint1_foundation.sql
docker compose exec postgres psql -U postgres -d postal_delivery \
  -f /dev/stdin < src/Infrastructure/Database/Migrations/0002_sprint2_orders.sql
docker compose exec postgres psql -U postgres -d postal_delivery \
  -f /dev/stdin < src/Infrastructure/Database/Migrations/0003_sprint4_tracking.sql
docker compose exec postgres psql -U postgres -d postal_delivery \
  -f /dev/stdin < src/Infrastructure/Database/Migrations/0004_sprint6_hardening.sql
```

Services after start:

| Service      | URL                    |
|--------------|------------------------|
| API          | http://localhost:5000  |
| AdminWeb     | http://localhost:5150  |
| CustomerWeb  | http://localhost:5181  |
| Swagger      | http://localhost:5000/swagger |

### Option B — Local dotnet run (development)

1. Start infrastructure only:

	`docker compose up postgres redis -d`

2. Apply SQL migrations:

	`psql -h localhost -p 57123 -U postgres -d postal_delivery -f src/Infrastructure/Database/Migrations/0001_sprint1_foundation.sql`

	`psql -h localhost -p 57123 -U postgres -d postal_delivery -f src/Infrastructure/Database/Migrations/0002_sprint2_orders.sql`

	`psql -h localhost -p 57123 -U postgres -d postal_delivery -f src/Infrastructure/Database/Migrations/0003_sprint4_tracking.sql`

	`psql -h localhost -p 57123 -U postgres -d postal_delivery -f src/Infrastructure/Database/Migrations/0004_sprint6_hardening.sql`

3. Run API:

	`dotnet run --project src/Api/PostalDeliverySystem.Api.csproj`

4. Run admin web (http://localhost:5150):

	`dotnet run --project src/AdminWeb/PostalDeliverySystem.AdminWeb.csproj`

5. Run customer web (http://localhost:5181):

	`dotnet run --project src/CustomerWeb/PostalDeliverySystem.CustomerWeb.csproj`

6. Run courier mobile app (Windows target):

	`dotnet build src/CourierMobile/PostalDeliverySystem.CourierMobile.csproj -f net9.0-windows10.0.19041.0`

	`dotnet build src/CourierMobile/PostalDeliverySystem.CourierMobile.csproj -t:Run -f net9.0-windows10.0.19041.0`

7. Bootstrap first SuperAdmin once:

	`POST /api/auth/bootstrap-superadmin`

	Example body:

	{
	  "fullName": "Root SuperAdmin",
	  "phone": "+998900000001",
	  "password": "SuperAdmin123!"
	}

### Running tests

```bash
dotnet test tests/PostalDeliverySystem.Tests/PostalDeliverySystem.Tests.csproj
```

The test suite does not require a running database — worker unit tests use in-memory fakes and smoke tests use the liveness probe and middleware pipeline only.

## API Reference

Auth endpoints (rate-limited: 10 req/min per IP):

- `POST /api/auth/bootstrap-superadmin`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/revoke`

Order endpoints:

- `POST /api/orders`
- `GET /api/orders`
- `GET /api/orders/{orderId}`
- `PUT /api/orders/{orderId}`
- `POST /api/orders/{orderId}/assign`
- `GET /api/orders/{orderId}/history`

Courier endpoints:

- `GET /api/courier/orders`
- `GET /api/courier/orders/{orderId}`
- `POST /api/courier/orders/{orderId}/accept`
- `POST /api/courier/orders/{orderId}/pickup`
- `POST /api/courier/orders/{orderId}/on-the-way`
- `POST /api/courier/orders/{orderId}/deliver`

Tracking endpoints and hub (tracking ingestion rate-limited: 120 req/min per user):

- `POST /api/tracking/location`
- `GET /api/tracking/orders/{orderId}/latest-location`
- `SignalR hub: /hubs/tracking`

Health probes:

- `GET /health/live` — liveness: always 200 if process is running
- `GET /health/ready` — readiness: checks PostgreSQL and Redis
- `GET /health` — full health report

Note: in this environment, the MAUI template generated `src/CourierMobile` with .NET 9 targets due to installed template constraints.