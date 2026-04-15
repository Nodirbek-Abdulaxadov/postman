# Backend Architecture

## Technology Baseline

- .NET 8
- ASP.NET Core Web API
- SignalR for realtime messaging
- PostgreSQL for durable data
- Redis for caching and scale-out
- Dapper for data access

## Runtime Topology

V1 should stay operationally small and explicit:

- one ASP.NET Core application hosting APIs, SignalR hubs, and hosted workers
- one PostgreSQL instance for transactional and historical data
- one Redis instance for cache, latest courier locations, and SignalR backplane
- one reverse proxy for TLS termination and routing when deployed publicly

This keeps the system production-capable without turning it into a microservice-heavy platform too early.

## Logical Modules

### Auth Module

- login and refresh token flow
- role resolution
- session revocation and permission enforcement

### Users Module

- branches, admins, couriers, and customers
- branch-scoped user administration
- account activation and deactivation

### Orders Module

- order creation and retrieval
- courier assignment
- lifecycle transitions and history tracking

### Tracking Module

- courier location ingestion
- throttling and deduplication
- latest location cache and realtime publishing

### Notification Module

- status notifications
- operational alerts
- retry-capable external messaging integration

## Project Structure

Recommended solution layout:

- `src/Api`: controllers, endpoint contracts, auth configuration, dependency setup
- `src/Application`: use cases, command and query handlers, validation, interfaces
- `src/Domain`: entities, enums, value objects, business rules
- `src/Infrastructure`: Dapper repositories, SQL, Redis, SignalR hubs, external integrations
- `src/Shared.Contracts`: DTOs, enums, common response models
- `tests`: unit and integration tests

## Layer Responsibilities

### API Layer

- accept HTTP and SignalR requests
- authenticate caller identity
- validate input shape
- map transport models to application commands and queries

### Application Layer

- orchestrate use cases
- enforce business workflow rules
- coordinate transactions
- publish domain-level events to infrastructure concerns

### Domain Layer

- define roles, statuses, and invariants
- prevent illegal order transitions
- remain free from infrastructure dependencies

### Infrastructure Layer

- execute SQL through Dapper
- handle Redis caching and pub/sub concerns
- host SignalR hubs
- integrate with external notification providers

## Request Lifecycle

Standard backend request flow:

1. controller or hub validates identity and basic payload shape
2. application service checks business permissions and current state
3. repository executes SQL through Dapper
4. latest operational data may be mirrored into Redis
5. realtime and notification events are emitted after the state is committed

## Data Access Rules

- keep SQL explicit and close to the use case it serves
- use parameterized queries only
- avoid generic repositories that hide business intent
- wrap multi-step write operations in explicit transactions
- use optimistic concurrency for status transitions when possible

## Performance Rules

- use async I/O end to end
- do not introduce polling for realtime screens
- cache only data that is proven to be read-heavy or latency-sensitive
- keep tracking writes isolated from standard order reads
- project database queries to exact DTOs instead of loading broad objects

## Operational Components

The backend may include a small set of hosted services:

- stale courier detector
- token cleanup worker
- archival or partition maintenance job
- notification retry processor

These background jobs should remain inside the monolith until operational pressure justifies extraction.

## Database Strategy

### Design Principles

- keep transactional business data in PostgreSQL
- optimize write-heavy tracking tables separately from order tables
- use `TIMESTAMPTZ` for all important event times
- index only proven query paths
- keep only low-latency operational views in Redis

### Core Tables

```sql
CREATE TABLE branches (
	id UUID PRIMARY KEY,
	code VARCHAR(32) NOT NULL UNIQUE,
	name TEXT NOT NULL,
	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE users (
	id UUID PRIMARY KEY,
	branch_id UUID NULL REFERENCES branches(id),
	role SMALLINT NOT NULL,
	full_name TEXT NOT NULL,
	phone VARCHAR(20) NOT NULL UNIQUE,
	password_hash TEXT NOT NULL,
	is_active BOOLEAN NOT NULL DEFAULT TRUE,
	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE orders (
	id UUID PRIMARY KEY,
	order_code VARCHAR(32) NOT NULL UNIQUE,
	customer_id UUID NOT NULL REFERENCES users(id),
	branch_id UUID NOT NULL REFERENCES branches(id),
	courier_id UUID NULL REFERENCES users(id),
	status SMALLINT NOT NULL,
	recipient_name TEXT NOT NULL,
	recipient_phone VARCHAR(20) NOT NULL,
	address TEXT NOT NULL,
	lat DOUBLE PRECISION NOT NULL,
	lng DOUBLE PRECISION NOT NULL,
	assigned_at TIMESTAMPTZ NULL,
	delivered_at TIMESTAMPTZ NULL,
	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE order_status_history (
	id BIGSERIAL PRIMARY KEY,
	order_id UUID NOT NULL REFERENCES orders(id),
	from_status SMALLINT NULL,
	to_status SMALLINT NOT NULL,
	changed_by_user_id UUID NOT NULL REFERENCES users(id),
	note TEXT NULL,
	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE courier_locations (
	id BIGSERIAL NOT NULL,
	courier_id UUID NOT NULL REFERENCES users(id),
	order_id UUID NULL REFERENCES orders(id),
	lat DOUBLE PRECISION NOT NULL,
	lng DOUBLE PRECISION NOT NULL,
	accuracy_meters REAL NULL,
	speed_mps REAL NULL,
	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	PRIMARY KEY (id, created_at)
) PARTITION BY RANGE (created_at);

CREATE TABLE refresh_tokens (
	id BIGSERIAL PRIMARY KEY,
	user_id UUID NOT NULL REFERENCES users(id),
	token_hash TEXT NOT NULL,
	expires_at TIMESTAMPTZ NOT NULL,
	revoked_at TIMESTAMPTZ NULL,
	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Recommended Indexes

```sql
CREATE INDEX idx_users_branch_role
ON users(branch_id, role)
WHERE branch_id IS NOT NULL;

CREATE INDEX idx_orders_branch_status_created
ON orders(branch_id, status, created_at DESC);

CREATE INDEX idx_orders_courier_status
ON orders(courier_id, status)
WHERE courier_id IS NOT NULL;

CREATE INDEX idx_order_status_history_order_time
ON order_status_history(order_id, created_at DESC);

CREATE INDEX idx_courier_locations_courier_time
ON courier_locations(courier_id, created_at DESC);

CREATE INDEX idx_refresh_tokens_user_active
ON refresh_tokens(user_id, expires_at)
WHERE revoked_at IS NULL;
```

### Retention and Partitioning

- partition `courier_locations` by time, preferably daily under heavy load
- archive or compress old partitions
- keep status history for audit and reporting
- clean expired and revoked refresh tokens on a schedule

## Order Engine

### Canonical Statuses

| Code | Status | Changed By | Notes |
| --- | --- | --- | --- |
| 0 | Created | Customer or Admin | Order exists but is not assigned |
| 1 | Assigned | Admin | Courier was assigned |
| 2 | Accepted | Courier | Courier accepted the assignment |
| 3 | PickedUp | Courier | Parcel was collected |
| 4 | OnTheWay | Courier | Courier is in delivery route |
| 5 | Delivered | Courier | Final successful state |
| 6 | Cancelled | Admin or Customer | Final unsuccessful state |

### Allowed Transitions

- `Created -> Assigned`
- `Created -> Cancelled`
- `Assigned -> Accepted`
- `Assigned -> Cancelled`
- `Accepted -> PickedUp`
- `Accepted -> Cancelled` only through an admin override
- `PickedUp -> OnTheWay`
- `OnTheWay -> Delivered`

No transition is allowed out of `Delivered` or `Cancelled`.

### Business and Concurrency Rules

- only an `Admin` from the same branch can assign a courier
- a courier can act only on orders assigned to that courier
- a courier cannot skip directly from `Assigned` to `Delivered`
- cancellation after pickup is exceptional and must be audited
- every status change must append a row to `order_status_history`
- status updates should use optimistic concurrency on the current expected status

Example:

```sql
UPDATE orders
SET status = @nextStatus
WHERE id = @orderId AND status = @expectedStatus;
```

## Tracking Engine

### Ingestion Rules

- accept tracking only from authenticated couriers
- accept tracking only when the courier has an active order
- ignore movement below 10 meters unless the heartbeat window is reached
- minimum accepted interval is 2 seconds
- maximum heartbeat interval is 5 seconds

### Endpoint

`POST /api/tracking/location`

Example payload:

```json
{
	"courierId": "6b7f7d02-64b9-4e72-a9ef-4f31b456f68e",
	"orderId": "3c4f62cd-f52d-4c5b-974f-9f3d5d21b99e",
	"lat": 41.311081,
	"lng": 69.240562,
	"accuracyMeters": 8.2,
	"deviceTime": "2026-04-15T10:30:00Z"
}
```

### Processing Flow

1. authenticate the courier
2. verify courier and order relationship
3. validate coordinates and time skew
4. apply throttling rules
5. write latest location to Redis
6. persist accepted point to PostgreSQL
7. publish live updates through SignalR

### Redis Keys

- `courier:{courierId}:latest-location`
- `order:{orderId}:latest-location`
- `tracking:courier:{courierId}`
- `tracking:order:{orderId}`

If Redis is temporarily unavailable, persist to PostgreSQL first and degrade live push gracefully.

## Realtime Transport

### Primary Choice

Use SignalR as the application-facing realtime protocol. It gives reconnection, group management, and .NET client support while still using WebSockets as the preferred transport underneath.

### Hub Endpoint

`/hubs/tracking`

### Groups

- `order:{orderId}` for customer order tracking
- `branch:{branchId}` for admin live dashboards
- `courier:{courierId}` for courier-specific operational messages when needed

### Event Types

`location_update`

```json
{
	"orderId": "3c4f62cd-f52d-4c5b-974f-9f3d5d21b99e",
	"courierId": "6b7f7d02-64b9-4e72-a9ef-4f31b456f68e",
	"lat": 41.311081,
	"lng": 69.240562,
	"recordedAt": "2026-04-15T10:30:01Z"
}
```

`status_update`

```json
{
	"orderId": "3c4f62cd-f52d-4c5b-974f-9f3d5d21b99e",
	"status": "Delivered",
	"statusCode": 5,
	"changedAt": "2026-04-15T10:45:32Z"
}
```

### Scale-Out and Recovery

- authenticate SignalR connections with the same JWT strategy as HTTP APIs
- enforce group join authorization on the server
- enable automatic reconnect on clients
- rejoin groups after reconnect and fetch one latest snapshot when necessary
- use Redis backplane for multi-instance fan-out

## Security and Hardening

### Authentication

- short-lived access token for API and SignalR calls
- refresh token rotation for durable sessions
- recommended defaults: 15 minute access token, 30 day refresh token

### Authorization Rules

- every query must be filtered by caller scope
- admins cannot mutate other branches
- couriers cannot touch orders outside their assignment
- customers can access only their own orders

### Protection Controls

- HTTPS only
- request validation on every endpoint
- rate limiting on login, refresh, order creation, and tracking endpoints
- strong password hashing with Argon2 or BCrypt
- audit login activity, assignment changes, status changes, cancellations, and admin operations
- keep secrets outside source control and rotate them operationally

### Abuse and Failure Controls

- slow or lock repeated failed login attempts
- reject impossible GPS jumps when they violate business rules
- monitor suspicious realtime group join attempts
- revoke sessions when role or branch access changes

## Scale Path

The expected growth path remains:

1. optimize SQL and indexes
2. cache only hot operational reads in Redis
3. run multiple app instances behind a load balancer
4. use Redis backplane for SignalR fan-out
5. extract modules only when operational cost or team structure requires it