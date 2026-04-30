<!--
Sync Impact Report
- Version change: 1.0.0 -> 2.0.0
- Modified principles:
  - IV. Comprehensive Automated Testing -> IV. Comprehensive Automated Testing
- Added sections:
  - None
- Removed sections:
  - None
- Templates requiring updates:
  - ✅ updated: .specify/templates/plan-template.md
  - ✅ updated: .specify/templates/spec-template.md
  - ✅ updated: .specify/templates/tasks-template.md
  - ⚠ pending (not present): .specify/templates/commands/*.md
- Follow-up TODOs:
  - None
-->
# 3sFrameDataBot Constitution

## Core Principles

### I. Small, Single-Purpose Functions
All production code MUST be composed of small, descriptively named functions that each
have one clear responsibility. Functions that combine unrelated concerns MUST be split.
Rationale: single-purpose functions reduce defect rate, improve testability, and speed
safe change.

### II. Descriptive Naming and Intent-Only Comments
Code MUST communicate behavior through names. Function and variable names MUST describe
what they do. Comments MUST explain why a decision exists, not what the code is doing,
except when documenting externally imposed constraints. Rationale: duplicated
code-as-comment drifts quickly and obscures intent.

### III. Test-Driven Development (NON-NEGOTIABLE)
Code changes MUST follow a red-green-refactor cycle whenever technically feasible:
write a failing test, implement the minimum change to pass, then refactor safely.
Changes that skip this order MUST be justified in the feature plan and reviewed.
Rationale: TDD constrains scope, prevents overbuilding, and protects behavior.

### IV. Comprehensive Automated Testing
Every feature and bug fix MUST include comprehensive unit tests for affected logic and
integration tests for component boundaries and external dependencies. Integration tests
MUST use reliable and reproducible test infrastructure suited to the system under test.
Alternatives MUST be justified in the plan. Rationale: fast unit coverage and
realistic integration checks jointly prevent regressions.

### V. Focused Scope and Performance Budgets
User stories and feature slices MUST be concise, independently valuable, and explicitly
limited in scope. Implementations MUST make only necessary changes to satisfy approved
requirements. Each feature MUST define measurable performance targets and verify they
are met before completion. Rationale: constrained scope preserves delivery speed;
performance budgets prevent late-stage degradation.

## Engineering Constraints

- Every planned change MUST map to an explicit requirement or acceptance scenario.
- Broad refactors unrelated to the feature are prohibited unless separately approved.
- New abstractions MUST be introduced only when they remove clear duplication or unlock
  required capability.
- Performance and optimization work MUST be evidence-driven with baseline and
  post-change measurements.

## Delivery Workflow & Quality Gates

- Feature specs MUST define focused user stories, out-of-scope items, measurable
  success criteria, and performance outcomes.
- Implementation plans MUST pass the Constitution Check before research/design and be
  re-validated before task generation.
- Task plans MUST include test-first execution order for each story: unit tests,
  integration tests, then implementation.
- Pull requests MUST include proof of passing unit and integration tests, plus
  performance verification for affected flows.
- Reviews MUST reject work that violates any Core Principle or omits required
  justifications.

## Governance

This constitution is the authoritative engineering policy for the repository. If any
workflow or template conflicts with it, this document takes precedence.

Amendment procedure:
1. Propose changes in a pull request that includes rationale, impact analysis,
   and template updates.
2. Obtain approval from project maintainers.
3. Update version and amendment date in this document.

Versioning policy:
- MAJOR: incompatible governance changes or principle removals/redefinitions.
- MINOR: new principle/section or materially expanded guidance.
- PATCH: clarifications and non-semantic wording improvements.

Compliance expectations:
- Every feature plan, spec, task list, and pull request MUST include an explicit
  constitution compliance check.
- Compliance is reviewed at planning, implementation, and review time.
- Violations MUST be corrected before merge unless a documented exception is approved.

**Version**: 2.0.0 | **Ratified**: 2026-03-21 | **Last Amended**: 2026-03-21
