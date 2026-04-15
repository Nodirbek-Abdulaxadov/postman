---
name: "Sprint 3 Courier Execution Flow"
description: "Use when: implementing Sprint 3 courier mobile flow, session restore, order actions, and validated status transitions"
argument-hint: "Current repo state, constraints, or blockers"
agent: "agent"
model: "GPT-5 (copilot)"
---
Implement Sprint 3 for the Postal Delivery System.

Use these documents as the source of truth:
- [README](../../README.md)
- [Postal Delivery System Spec](../../POSTAL_DELIVERY_SYSTEM.md)
- [Backend Architecture](../../backend_architecture.md)
- [UI Architecture](../../ui_architecture.md)
- [Workspace Instructions](../copilot-instructions.md)

Sprint 3 goal:
- complete the non-realtime courier workflow

Sprint 3 must deliver:
- courier mobile login and session restore
- assigned order list and order detail screens
- actions for `Accepted`, `PickedUp`, `OnTheWay`, and `Delivered`
- validation and optimistic concurrency for status changes

Execution rules:
- keep courier actions limited to assigned orders only
- preserve the canonical lifecycle: `Created`, `Assigned`, `Accepted`, `PickedUp`, `OnTheWay`, `Delivered`, `Cancelled`
- optimize courier UI for speed, clarity, and one-hand use
- implement real API or app integration points, not dummy local state

Definition of done:
- courier can complete the legal delivery lifecycle for assigned work
- invalid or unauthorized transitions are blocked
- admin can observe resulting status changes through normal reads

While working:
- verify whether Sprint 2 prerequisites are complete before extending the flow
- keep offline handling minimal but real where needed for session continuity
- report anything deferred to tracking or realtime sprints explicitly