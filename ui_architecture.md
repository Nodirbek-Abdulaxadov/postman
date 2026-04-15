# UI Architecture

## Goals

- keep the frontend stack mostly in C# and Razor
- maximize reuse without coupling unrelated workflows
- support realtime delivery tracking cleanly across web and mobile

The UI documentation is intentionally consolidated here so application boundaries, shared code rules, and role-specific experiences stay in one place.

## Applications

### Admin Web

- technology: Blazor Server
- users: `SuperAdmin`, `Admin`
- strengths: server-driven UI, easy SignalR integration, rapid internal tooling

### Courier App

- technology: .NET MAUI Blazor Hybrid
- users: `Courier`
- strengths: native device access, background GPS, one codebase with shared Razor components

### Customer Web

- technology: responsive Blazor Web App or PWA
- users: `Customer`
- strengths: low distribution friction, fast access from shared links, easy realtime order tracking

## Shared Layer

- shared DTOs and enums
- shared API clients
- shared realtime client helpers
- shared visual components and state widgets

## Admin Web

### Users

- `SuperAdmin`
- `Admin`

### Main Pages

- `/dashboard`: KPIs, active deliveries, and branch health summary
- `/orders`: order list, filters, detail panel, and assignment actions
- `/couriers`: courier availability, active tasks, and performance view
- `/tracking`: live branch map with selected order details
- `/users`: branch admins, couriers, and account status management

### Capabilities

- create and manage branches and branch admins for `SuperAdmin`
- create and assign orders inside one branch for `Admin`
- monitor courier movement and delivery exceptions
- handle cancellations and operational overrides

### Core Components

- `KpiCards`
- `OrderTable`
- `OrderFilters`
- `CourierList`
- `TrackingMap`
- `StatusBadge`
- `UserManagementPanel`

## Courier Mobile App

### Product Direction

The courier application is the primary native mobile client. It must optimize for speed, reliability, and background tracking rather than rich navigation.

### Primary Screens

- login and session restore
- assigned orders list
- order detail with status actions
- active delivery screen
- permissions and device health screen

### Core Features

- accept assigned order
- mark pickup and delivery milestones
- send background GPS updates during active delivery
- queue and retry updates briefly during connectivity loss

## Customer Experience

### V1 Platform

- responsive Blazor Web App or PWA

### Primary Screens

- create order form
- order list or order detail page
- live tracking page with map and status timeline

### Core Features

- track current order status
- view latest courier position when available
- see the latest update time and delivery progress

## Realtime UI Behavior

### Admin Behavior

- subscribe to branch-level SignalR updates
- update rows, badges, and map markers in place
- keep selected order context during incoming updates
- show a visible degraded-state banner when the live connection drops

### Customer Behavior

- subscribe only to the current order stream
- update the map and timeline from `location_update` and `status_update`
- show stale-data messaging if no live event arrives in the expected window

### Courier Behavior

- courier app is primarily a sender of tracking events
- realtime may be used for assignment refresh or operational notices only

## UI Communication Model

- commands and queries use HTTP APIs
- live updates use SignalR
- authentication uses the same JWT-based backend rules across all clients

## State Management Rule

Server state is authoritative. Clients may keep temporary local state for UX, but they should reconcile against the latest server snapshot after reconnects or conflicts.

## Shared UI Boundaries

### Good Candidates for Reuse

- `StatusBadge`
- `OrderCard`
- `TrackingMap` wrapper
- loading, empty, and error states
- common form inputs and validation summaries

### What Not to Over-Share

- full admin pages
- courier task flow pages
- customer checkout pages
- role-specific layout shells

### UX Rules

- courier actions must be large, fast, and one-hand friendly
- customer tracking should be map-first and status-first
- admin workflows should favor dense operational views over visual noise
- debounce expensive map redraws and reconcile from the server after reconnects