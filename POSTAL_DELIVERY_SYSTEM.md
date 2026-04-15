# Postal Delivery System Specification

## 1. Product Goal

Postal Delivery System is a branch-based delivery platform for managing orders, assigning couriers, and showing live delivery progress to admins and customers.

The first production version must solve four business problems clearly:

- centralize branch operations
- control courier assignment and delivery execution
- provide realtime order visibility
- stay simple enough to build quickly and scale later

## 2. Roles and Responsibilities

### SuperAdmin

- creates and manages branches
- creates and manages branch admins
- sees global operational metrics

### Admin

- works inside one branch
- creates and assigns orders
- manages couriers in the same branch
- monitors live deliveries and exceptions

### Courier

- sees only assigned orders
- accepts and completes delivery tasks
- sends live location while on an active delivery

### Customer

- creates or views own orders
- tracks order status and live courier position

## 3. V1 Order Lifecycle

The canonical order statuses for V1 are:

| Code | Status | Description |
| --- | --- | --- |
| 0 | Created | Order exists but no courier is assigned yet |
| 1 | Assigned | Admin assigned a courier |
| 2 | Accepted | Courier accepted responsibility |
| 3 | PickedUp | Parcel was picked up |
| 4 | OnTheWay | Courier is moving toward destination |
| 5 | Delivered | Delivery completed successfully |
| 6 | Cancelled | Order cancelled before completion |

`Delivered` and `Cancelled` are final states. There is no separate `Failed` state in V1; failure reasons are stored as cancellation reasons.

## 4. Architecture Strategy

V1 uses a modular monolith.

This is the correct tradeoff because it keeps deployment and debugging simple while preserving clear module boundaries for future extraction if load or team size grows.

Core logical modules:

- Auth
- Users
- Orders
- Tracking
- Notifications

## 5. Core Business Flow

1. Customer creates an order.
2. Admin reviews the order and assigns a courier.
3. Courier accepts the task.
4. Courier picks up the parcel and starts delivery.
5. Courier app sends location updates under tracking rules.
6. Tracking module updates Redis and publishes realtime events.
7. Admin and customer screens receive live updates through SignalR.
8. Courier marks the order as delivered.

## 6. Technology Decisions

### Backend

- .NET 8 Web API
- Dapper for explicit and fast SQL access
- PostgreSQL as the system of record
- Redis for caching, latest courier locations, and realtime scale-out

### Realtime

- SignalR as the application-level realtime protocol
- WebSockets as the primary transport
- Redis backplane for multi-instance broadcasting

### User Interfaces

- Admin dashboard: Blazor Server
- Courier app: .NET MAUI Blazor Hybrid
- Customer tracking: responsive Blazor Web App or PWA
- Shared UI and DTOs: Razor Class Library and shared contracts

## 7. Non-Functional Requirements

- Realtime fan-out target: under 300 ms after an accepted location update
- Concurrent viewers target: 10,000+
- Active couriers target: 1,000+
- No polling-based live tracking in the main flow
- All I/O paths must be asynchronous
- Every state change must be authorized and auditable

## 8. Data Strategy

- transactional data stays in PostgreSQL
- latest courier position stays in Redis for fast reads
- full location history is append-only and partitioned in PostgreSQL
- order status history is stored for audit and analytics

## 9. Delivery Plan

### Phase 1

Authentication, branch structure, role model

### Phase 2

Order CRUD, branch filtering, admin assignment flow

### Phase 3

Courier app, status transitions, and validation rules

### Phase 4

Tracking ingestion, Redis integration, and SignalR broadcasting

### Phase 5

Admin live dashboard, customer tracking page, and hardening

## 10. Development Backlog

The backlog below is intentionally grouped by production epics rather than by small technical tasks. This keeps planning aligned with the actual business flow and the module boundaries defined in the backend and UI documents.

### Epic A. Platform Foundation

- create the .NET solution and project boundaries for `Api`, `Application`, `Domain`, `Infrastructure`, and `Shared.Contracts`
- configure environment settings, secret loading, logging, global exception handling, and health checks
- prepare PostgreSQL and Redis local development setup
- create the initial database schema baseline and migration strategy
- add CI baseline for build, test, and formatting checks

### Epic B. Identity and Organization

- implement branch model and role model
- implement login, refresh token flow, and token revocation
- add user management for `SuperAdmin` and `Admin`
- enforce branch-scoped authorization rules across APIs and SignalR
- seed baseline roles and test data for non-production environments

### Epic C. Order Management

- implement order creation, retrieval, filtering, and detail queries
- implement courier assignment workflow for branch admins
- implement order status history persistence
- enforce legal status transitions and optimistic concurrency
- expose admin-ready order views for tables and dashboards

### Epic D. Courier Operations

- implement courier authentication and session restore in the mobile app
- build assigned-order list and order detail screens
- implement accept, pickup, on-the-way, and delivered actions
- handle basic offline retry for mobile commands and session continuity
- surface permission, connectivity, and device state feedback in the app

### Epic E. Tracking and Realtime

- implement tracking ingestion endpoint and payload validation
- apply movement and interval throttling rules
- persist accepted tracking points to PostgreSQL partitions
- mirror latest location into Redis
- implement SignalR hub, authorized groups, and realtime event publishing

### Epic F. Operational User Experience

- build admin dashboard pages for orders, couriers, tracking, and users
- implement live branch map and active delivery views
- build customer order detail and live tracking page
- add reconnect handling, stale-state messaging, and realtime status indicators
- extract stable shared components and client helpers where reuse is justified

### Epic G. Hardening and Release

- add rate limiting, audit coverage, and security reviews for critical endpoints
- implement stale courier detection and token cleanup jobs
- add operational metrics, structured logs, and alerting hooks
- run integration, smoke, and load tests on core flows
- prepare staging deployment, release checklist, and rollback plan

## 11. Sprint Plan and Milestones

Assumption: each sprint is 2 weeks and targets a staging-quality increment, not just partial code merges.

### Sprint 1. Foundation and Authentication

Goal: establish the runtime skeleton and secure identity flow.

Scope:

- solution and project structure
- configuration, logging, error handling, health checks
- branches, users, roles, JWT access token, refresh token
- initial schema for users, branches, refresh tokens

Exit criteria:

- API boots in all local environments
- login and refresh flow works end to end
- role-based authorization is enforceable in API endpoints

Milestone: `M1 - Secure Platform Skeleton`

### Sprint 2. Orders and Branch Operations

Goal: make branch admins operational on the core order flow.

Scope:

- orders schema and status history
- order CRUD and filtering APIs
- courier assignment workflow
- initial admin web shell for login, orders, couriers, and users pages

Exit criteria:

- admin can create and assign orders inside the correct branch
- order history is persisted and queryable
- illegal branch access is rejected

Milestone: `M2 - Branch Operations Ready`

### Sprint 3. Courier Execution Flow

Goal: complete the non-realtime delivery workflow for couriers.

Scope:

- courier mobile login and session restore
- assigned order list and order detail screens
- status transition commands: accept, picked up, on the way, delivered
- optimistic concurrency and validation on status changes

Exit criteria:

- courier can complete the full legal order lifecycle for assigned work
- unauthorized or invalid transitions are blocked
- admin can observe status changes through normal API reads

Milestone: `M3 - End-to-End Delivery Flow`

### Sprint 4. Tracking Pipeline and Realtime Backend

Goal: make live tracking reliable at the backend and transport level.

Scope:

- tracking endpoint and validation
- distance and interval throttling
- PostgreSQL location persistence and partition strategy
- Redis latest-location cache and SignalR backplane integration
- realtime group model and `location_update` or `status_update` event publishing

Exit criteria:

- accepted courier location updates are persisted and broadcast
- reconnecting clients can recover using latest snapshot plus live stream
- Redis failure degrades gracefully without losing durable order state

Milestone: `M4 - Realtime Backend Ready`

### Sprint 5. Live Admin and Customer Experience

Goal: expose the realtime capabilities in operator and customer-facing UI.

Scope:

- admin dashboard KPIs, orders table, courier list, and live tracking page
- customer order detail and live tracking page
- realtime connection indicators, stale-state banners, and map update behavior
- extraction of shared UI components and client helpers where stable

Exit criteria:

- admin sees live status and map changes without polling
- customer can track own order with a clear status timeline
- UI state remains stable during reconnects and partial outages

Milestone: `M5 - Realtime User Experience Ready`

### Sprint 6. Hardening, Observability, and Release Candidate

Goal: convert the working product into a staging-ready release candidate.

Scope:

- rate limiting, audit completeness, and secret handling review
- stale courier worker, token cleanup worker, and maintenance jobs
- metrics, logs, health probes, and operational alert hooks
- integration, smoke, and load tests for auth, orders, tracking, and realtime
- staging deployment pipeline and release checklist

Exit criteria:

- critical flows pass automated and manual smoke checks
- observability is sufficient to debug auth, order, and tracking failures
- staging deployment is repeatable and rollback steps are documented

Milestone: `M6 - Staging Release Candidate`

## 12. Reference Documents

- `backend_architecture.md` for runtime topology, backend layers, data model, order engine, tracking, realtime, and security
- `ui_architecture.md` for admin web, courier mobile, customer tracking, realtime UX, and shared UI boundaries
- `prompt.txt` for implementation prompting aligned to the documents above