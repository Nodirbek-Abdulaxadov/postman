---
name: "Sprint 4 Tracking and Realtime Backend"
description: "Use when: implementing Sprint 4 tracking ingestion, throttling, Redis latest-location cache, SignalR hub, and realtime backend delivery"
argument-hint: "Current repo state, constraints, or blockers"
agent: "agent"
model: "GPT-5 (copilot)"
---
Implement Sprint 4 for the Postal Delivery System.

Use these documents as the source of truth:
- [README](../../README.md)
- [Postal Delivery System Spec](../../POSTAL_DELIVERY_SYSTEM.md)
- [Backend Architecture](../../backend_architecture.md)
- [Workspace Instructions](../copilot-instructions.md)

Sprint 4 goal:
- make live tracking reliable at the backend and transport level

Sprint 4 must deliver:
- tracking endpoint and payload validation
- distance and interval throttling rules
- PostgreSQL location persistence and partition-aware design
- Redis latest-location cache
- SignalR hub, authorized groups, and live event publishing

Execution rules:
- treat PostgreSQL as the durable store and Redis as operational support only
- reject unauthorized courier tracking updates
- keep reconnect and degraded-mode behavior explicit
- avoid polling or transport designs that contradict the documented realtime architecture

Definition of done:
- accepted courier location updates are persisted and broadcast
- reconnecting clients have a clear recovery path
- Redis failure degrades gracefully without corrupting durable state

While working:
- keep SQL explicit and close to the use case
- include edge cases such as duplicate sends, invalid coordinates, and stale client sessions
- report anything left for Sprint 5 UI consumption instead of blurring the boundary