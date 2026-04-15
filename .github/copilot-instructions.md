# Postal Delivery System Guidelines

## Source of Truth

- Treat `README.md`, `POSTAL_DELIVERY_SYSTEM.md`, `backend_architecture.md`, `ui_architecture.md`, and `prompt.txt` as the authoritative project guidance.
- If a request conflicts with these documents, follow the documented architecture and call out the conflict clearly.

## Architecture

- Keep the system as a modular monolith.
- Keep backend boundaries explicit: `Api`, `Application`, `Domain`, `Infrastructure`, `Shared.Contracts`.
- Use .NET 8, ASP.NET Core Web API, Dapper, PostgreSQL, Redis, and SignalR.
- Use Blazor Server for admin web, .NET MAUI Blazor Hybrid for courier mobile, and responsive Blazor Web App or PWA for customer tracking.
- Do not introduce microservices, EF Core, polling-based realtime flows, or generic repository abstractions unless the project documents are explicitly changed.

## Sprint Execution

- Map implementation work to the sprint plan in `POSTAL_DELIVERY_SYSTEM.md`.
- Respect sprint boundaries, but do not leave a sprint with disconnected scaffolding presented as complete.
- If a later-sprint request depends on missing earlier-sprint prerequisites, implement the minimum correct prerequisite first.
- Prefer vertical slices that leave the system runnable and coherent.

## Domain Rules

- Roles are fixed for V1: `SuperAdmin`, `Admin`, `Courier`, `Customer`.
- Order lifecycle is fixed for V1: `Created`, `Assigned`, `Accepted`, `PickedUp`, `OnTheWay`, `Delivered`, `Cancelled`.
- `Delivered` and `Cancelled` are final states.
- Every state-changing action must be validated, authorized, and auditable.
- Courier actions must be limited to orders assigned to that courier.
- Branch-scoped access must be enforced for admin operations.

## Backend Conventions

- Use explicit, parameterized SQL and keep queries close to the use case they serve.
- Use async I/O throughout the stack.
- Use optimistic concurrency for order status transitions.
- Persist status history for every order transition.
- Treat PostgreSQL as the system of record and Redis as an operational cache or realtime support layer only.

## UI Conventions

- Admin UI should prioritize dense operational workflows over decorative layouts.
- Courier UI should optimize for speed, clarity, and one-hand operation.
- Customer tracking should be map-first and status-first.
- Realtime UI should handle reconnects, stale states, and partial outages gracefully.
- Share stable components, DTOs, and client helpers; do not over-share full workflow pages.

## Quality Bar

- Do not produce toy implementations, fake placeholders, or pseudo-finished scaffolding.
- Keep outputs implementation-ready, security-aware, performance-conscious, and consistent with the documented module boundaries.
- Include edge cases, failure handling, and operational consequences where they materially affect the design.
- Update documentation when architectural or sprint-level assumptions change.

## Validation

- Run the relevant build, test, or verification steps when code exists to validate the change.
- If the repository is still documentation-first, prefer creating real structure and actionable implementation artifacts over abstract examples.
