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

The repository is documentation-first. It is ready to be used as the baseline for creating the actual solution structure and implementation backlog.

The documentation has been intentionally consolidated so the project stays serious and implementation-oriented without scattering key decisions across too many small files.