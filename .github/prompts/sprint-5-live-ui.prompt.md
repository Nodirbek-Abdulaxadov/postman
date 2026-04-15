---
name: "Sprint 5 Live Admin and Customer Experience"
description: "Use when: implementing Sprint 5 admin live dashboard, customer tracking page, realtime UI behavior, and stable shared UI extraction"
argument-hint: "Current repo state, constraints, or blockers"
agent: "agent"
model: "GPT-5 (copilot)"
---
Implement Sprint 5 for the Postal Delivery System.

Use these documents as the source of truth:
- [README](../../README.md)
- [Postal Delivery System Spec](../../POSTAL_DELIVERY_SYSTEM.md)
- [Backend Architecture](../../backend_architecture.md)
- [UI Architecture](../../ui_architecture.md)
- [Workspace Instructions](../copilot-instructions.md)

Sprint 5 goal:
- expose realtime capabilities in operator and customer-facing UI

Sprint 5 must deliver:
- admin dashboard KPIs, orders table, courier list, and live tracking page
- customer order detail and live tracking page with map and status timeline
- realtime connection indicators, stale-state banners, and resilient map update behavior
- extraction of stable shared components and client helpers only where justified

Execution rules:
- admin UI should remain operationally dense and efficient
- customer UI should be map-first and status-first
- handle reconnects, stale states, and partial outages gracefully
- do not over-share full workflow pages between admin, courier, and customer apps

Definition of done:
- admin sees live status and map changes without polling
- customer can track their own order with a clear status timeline
- UI state stays stable during reconnects and partial outages

While working:
- keep realtime UI backed by actual SignalR flows from Sprint 4
- verify shared component extraction is warranted and stable
- call out visual or UX debt separately from sprint-complete behavior