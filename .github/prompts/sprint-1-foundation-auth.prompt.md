---
name: "Sprint 1 Foundation and Authentication"
description: "Use when: implementing Sprint 1 foundation, solution structure, auth, roles, branches, and health checks for the Postal Delivery System"
argument-hint: "Current repo state, constraints, or blockers"
agent: "agent"
model: "GPT-5 (copilot)"
---
Implement Sprint 1 for the Postal Delivery System.

Use these documents as the source of truth:
- [README](../../README.md)
- [Postal Delivery System Spec](../../POSTAL_DELIVERY_SYSTEM.md)
- [Backend Architecture](../../backend_architecture.md)
- [Workspace Instructions](../copilot-instructions.md)

Sprint 1 goal:
- establish the runtime skeleton and secure identity flow

Sprint 1 must deliver:
- solution structure and project boundaries for `Api`, `Application`, `Domain`, `Infrastructure`, and `Shared.Contracts`
- configuration, logging, exception handling, and health checks
- branches, users, roles, JWT access token, and refresh token flow
- initial schema for branches, users, roles, and refresh tokens

Execution rules:
- create real implementation artifacts, not pseudo-code
- stay inside the modular monolith architecture
- use .NET 8, ASP.NET Core Web API, PostgreSQL, Redis, and Dapper
- keep authentication and authorization production-oriented
- if prerequisites are missing, implement the minimum correct foundation first

Definition of done:
- application boots locally
- authentication flow works end to end
- role-based authorization is enforceable in APIs

While working:
- identify what already exists versus what is missing
- implement the sprint increment coherently
- validate with relevant build, test, or verification steps when possible
- report blockers, residual risks, and what remains outside Sprint 1