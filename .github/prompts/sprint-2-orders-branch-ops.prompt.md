---
name: "Sprint 2 Orders and Branch Operations"
description: "Use when: implementing Sprint 2 orders, status history, branch-scoped admin operations, and courier assignment workflow"
argument-hint: "Current repo state, constraints, or blockers"
agent: "agent"
model: "GPT-5 (copilot)"
---
Implement Sprint 2 for the Postal Delivery System.

Use these documents as the source of truth:
- [README](../../README.md)
- [Postal Delivery System Spec](../../POSTAL_DELIVERY_SYSTEM.md)
- [Backend Architecture](../../backend_architecture.md)
- [UI Architecture](../../ui_architecture.md)
- [Workspace Instructions](../copilot-instructions.md)

Sprint 2 goal:
- make branch admins operational on the core order flow

Sprint 2 must deliver:
- orders schema and order status history
- order CRUD and filtering APIs
- courier assignment workflow for branch admins
- branch-scoped authorization for order and courier operations
- initial admin web shell for login, orders, couriers, and users navigation

Execution rules:
- preserve Sprint 1 auth and structure decisions
- persist status history for every transition
- use optimistic concurrency where order state can race
- keep branch scope enforcement in backend logic, not only UI
- do not present disconnected admin scaffolding as completed Sprint 2 work

Definition of done:
- admin can create and assign orders inside the correct branch
- order history is persisted and queryable
- illegal branch access is rejected

While working:
- implement only the minimum UI needed to support real branch operations
- validate the admin-to-order flow end to end
- call out anything that belongs to Sprint 3 or later instead of silently pulling it in