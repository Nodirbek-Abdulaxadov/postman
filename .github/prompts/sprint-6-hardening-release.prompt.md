---
name: "Sprint 6 Hardening and Release Candidate"
description: "Use when: implementing Sprint 6 hardening, observability, maintenance jobs, critical-flow testing, and staging release readiness"
argument-hint: "Current repo state, constraints, or blockers"
agent: "agent"
model: "GPT-5 (copilot)"
---
Implement Sprint 6 for the Postal Delivery System.

Use these documents as the source of truth:
- [README](../../README.md)
- [Postal Delivery System Spec](../../POSTAL_DELIVERY_SYSTEM.md)
- [Backend Architecture](../../backend_architecture.md)
- [UI Architecture](../../ui_architecture.md)
- [Workspace Instructions](../copilot-instructions.md)

Sprint 6 goal:
- turn the working product into a staging-ready release candidate

Sprint 6 must deliver:
- rate limiting and audit completeness for critical flows
- cleanup and maintenance workers such as stale courier detection and token cleanup
- metrics, logs, health probes, and operational alert hooks
- integration, smoke, and load testing for auth, orders, tracking, and realtime
- staging deployment readiness and rollback notes

Execution rules:
- do not use hardening as a reason to re-architect the system away from the documented monolith
- prioritize observability and recoverability on the highest-risk flows
- keep release-readiness artifacts concrete and runnable
- treat missing verification as unfinished work, not as an optional extra

Definition of done:
- critical flows are testable and observable
- staging deployment is repeatable
- operational debugging paths are clear

While working:
- validate security controls, audit coverage, and degraded-mode behavior
- report remaining production risks honestly
- keep any follow-up work clearly separated from the release-candidate baseline