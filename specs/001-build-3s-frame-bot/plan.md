# Implementation Plan: Discord 3s Frame Data Bot

**Branch**: `001-build-3s-frame-bot` | **Date**: 2026-03-31 | **Spec**: [/home/nmur/code/3sFrameDataBot/specs/001-build-3s-frame-bot/spec.md](/home/nmur/code/3sFrameDataBot/specs/001-build-3s-frame-bot/spec.md)
**Input**: Feature specification from `/specs/001-build-3s-frame-bot/spec.md`

## Summary

Deliver an iterative Discord frame-data platform with a strict .NET-only implementation
scope for this feature: 1) exact move lookup MVP, 2) source ingestion/persistence,
3) fuzzy alias resolution, 4) last active-frame hitbox image support,
5) storage-impact analysis, and 6) advanced metadata/media expansion.

Command surface is generalized for future game expansion: `/framedata` with required
`character` and `move`, plus optional `game` (defaulting to current 3s dataset in this
feature iteration).

## Technical Context

**Language/Version**: .NET 10 (C#) for bot/API/ingestion/scraper services and shared libraries  
**Primary Dependencies**: Discord.Net, ASP.NET Core Minimal APIs, AngleSharp, FuzzySharp, Serilog, NSubstitute, Shouldly, xUnit, Testcontainers for .NET, Npgsql  
**Storage**: PostgreSQL (JSONB for nested move/frame structures), character JSON exports on disk  
**Testing**: xUnit + Shouldly + NSubstitute (unit); Testcontainers for .NET (integration); contract tests for API payloads  
**Target Platform**: Linux Docker containers on cloud host (Render baseline)  
**Project Type**: Multi-service backend (Bot, API, Ingestion, Scraper + shared libs)  
**Performance Goals**: SC-001 validation run: >=95% valid exact-name queries complete in <3 seconds, measured with fixed-size representative samples across API query and bot end-to-end latency; ingestion completes full roster within maintenance window  
**Constraints**: .NET-only implementation for this feature iteration; no Moq; no FluentAssertions; secure least-privilege container deployment; CI/CD via GitHub + cloud deploy integration  
**Scale/Scope**: Single-bot deployment for initial 3s dataset with optional `game` parameter reserved for forward-compatible expansion

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Pre-Phase 0 Gate:
- [x] Functions are planned as small, single-purpose units.
- [x] Comment policy is intent-only (why, not what).
- [x] TDD order is explicit in task sequencing.
- [x] Comprehensive unit + integration strategy is defined.
- [x] Integration tests use reliable/reproducible infra (Testcontainers).
- [x] Scope is constrained to independent user stories.
- [x] Performance targets and validation are defined.

Post-Phase 1 Re-check:
- [x] Research decisions resolve implementation ambiguity.
- [x] Data model and contracts align with story boundaries.
- [x] Quickstart and tasks preserve constitution quality gates.

## Core Implementation Sequence

### Step 1: Foundation Bootstrap

- Execute Setup + Foundational tasks first (`T001-T015`).
- Output: runnable solution skeleton, dependency wiring, DB baseline, CI baseline.
- References:
  - Research stack decisions: [research.md](/home/nmur/code/3sFrameDataBot/specs/001-build-3s-frame-bot/research.md)
  - Structure + dependencies: [tasks.md](/home/nmur/code/3sFrameDataBot/specs/001-build-3s-frame-bot/tasks.md)

### Step 2: MVP Query Path (US1)

- Implement `/framedata` query flow via `GET /v1/moves/query` (`T016-T026`).
- Output: exact behavior + clear handling for unsupported characters/moves and optional unsupported `game` values.
- References:
  - Story behavior: [spec.md](/home/nmur/code/3sFrameDataBot/specs/001-build-3s-frame-bot/spec.md)
  - API contract: [bot-api.yaml](/home/nmur/code/3sFrameDataBot/specs/001-build-3s-frame-bot/contracts/bot-api.yaml)
  - Command contract: [discord-command.md](/home/nmur/code/3sFrameDataBot/specs/001-build-3s-frame-bot/contracts/discord-command.md)
  - Core entities: [data-model.md](/home/nmur/code/3sFrameDataBot/specs/001-build-3s-frame-bot/data-model.md)

### Step 3: Ingestion Backbone (US2)

- Implement scraper + ingest + DB persistence + JSON exports (`T027-T036`).
- Output: refreshable dataset and ingestion status endpoints with explicit partial-failure reporting.
- References:
  - Parser + hosting decisions: [research.md](/home/nmur/code/3sFrameDataBot/specs/001-build-3s-frame-bot/research.md)
  - Ingestion entities and statuses: [data-model.md](/home/nmur/code/3sFrameDataBot/specs/001-build-3s-frame-bot/data-model.md)
  - Operational flow: [quickstart.md](/home/nmur/code/3sFrameDataBot/specs/001-build-3s-frame-bot/quickstart.md)

### Step 4: Usability Refinement (US3)

- Add alias normalization + fuzzy ranking + disambiguation (`T037-T045`).
- Output: shorthand/numpad/colloquial input support with safe ambiguity handling.
- References:
  - Matching requirements: [spec.md](/home/nmur/code/3sFrameDataBot/specs/001-build-3s-frame-bot/spec.md)
  - Fuzzy library decision: [research.md](/home/nmur/code/3sFrameDataBot/specs/001-build-3s-frame-bot/research.md)

### Step 5: Visual Data Layer (US4)

- Add last active-frame image scraping/storage/response enrichment (`T046-T053`).
- Output: optional media references in query responses.
- References:
  - Image behavior: [spec.md](/home/nmur/code/3sFrameDataBot/specs/001-build-3s-frame-bot/spec.md)
  - Media entities: [data-model.md](/home/nmur/code/3sFrameDataBot/specs/001-build-3s-frame-bot/data-model.md)

### Step 6: Capacity Governance (US5)

- Implement storage assessment workflow (`T054-T058`).
- Output: report-driven decision gate before full image archival.
- References:
  - Storage-governance requirements: [spec.md](/home/nmur/code/3sFrameDataBot/specs/001-build-3s-frame-bot/spec.md)
  - StorageAssessment entity: [data-model.md](/home/nmur/code/3sFrameDataBot/specs/001-build-3s-frame-bot/data-model.md)

### Step 7: Metadata Expansion (US6)

- Add advanced move metadata mapping/serialization (`T059-T064`).
- Output: enriched responses without regressing baseline fields.
- References:
  - Metadata behavior: [spec.md](/home/nmur/code/3sFrameDataBot/specs/001-build-3s-frame-bot/spec.md)
  - Response contract: [bot-api.yaml](/home/nmur/code/3sFrameDataBot/specs/001-build-3s-frame-bot/contracts/bot-api.yaml)

### Step 8: Cross-Cutting Hardening

- Execute polish tasks (`T065-T070`) after selected story completion.
- Output: observability, deploy pipeline, performance validation, security-gate verification, implementation report.
- References:
  - Deployment flow: [quickstart.md](/home/nmur/code/3sFrameDataBot/specs/001-build-3s-frame-bot/quickstart.md)
  - Quality gates: [tasks.md](/home/nmur/code/3sFrameDataBot/specs/001-build-3s-frame-bot/tasks.md)

## Project Structure

### Documentation (this feature)

```text
specs/001-build-3s-frame-bot/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── bot-api.yaml
│   └── discord-command.md
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── FrameData.Bot/
├── FrameData.Api/
├── FrameData.Ingestion/
├── FrameData.Scraper/
├── FrameData.Domain/
├── FrameData.Infrastructure/
└── FrameData.Shared/

tests/
├── unit/
├── integration/
└── contract/
```

**Structure Decision**: Layered .NET multi-service architecture with clear test separation and explicit story-aligned delivery.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |
