# Implementation Plan: Discord 3s Frame Data Bot

**Branch**: `001-build-3s-frame-bot` | **Date**: 2026-04-14 | **Spec**: `/specs/001-build-3s-frame-bot/spec.md`
**Input**: Feature specification from `/specs/001-build-3s-frame-bot/spec.md`

## Summary

Deliver the approved 3s bot platform scope while explicitly completing service
containerization for the Bot runtime. The implementation cycle keeps single-game query
simplification (`/framedata` with required `character` + `move`) and extends deployment
artifacts so Bot runs alongside API, Ingestion, and PostgreSQL in Docker-based
environments (local Compose and cloud image pipelines).

## Technical Context

**Language/Version**: .NET 10 (C#) for bot/API/ingestion/scraper services and shared libraries  
**Primary Dependencies**: Discord.Net, ASP.NET Core Minimal APIs, AngleSharp, FuzzySharp, Serilog, NSubstitute, Shouldly, xUnit, Testcontainers for .NET, Npgsql  
**Storage**: PostgreSQL (JSONB for nested move/frame structures), character JSON exports on disk  
**Testing**: xUnit + Shouldly + NSubstitute (unit); Testcontainers for .NET (integration); contract tests for API payloads  
**Target Platform**: Linux Docker containers (local Compose, Render, and self-hosted NAS)  
**Project Type**: Multi-service backend (`FrameData.Bot`, `FrameData.Api`, `FrameData.Ingestion`, scraper + shared libraries)  
**Performance Goals**: SC-001 validation run: >=95% valid exact-name queries complete in <3 seconds across API and bot end-to-end latency, fixed representative sampling  
**Constraints**: .NET-only implementation scope; no Moq/FluentAssertions; secure least-privilege container deployment; Bot must have first-class container/deploy parity with API/Ingestion  
**Scale/Scope**: Single-game (3s) deployment scope with no caller-provided game selector

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Pre-Phase 0 gate:
- [x] Small, single-purpose functions remain the implementation baseline.
- [x] Naming/comment policy remains intent-focused (why, not what).
- [x] TDD order remains explicit in execution sequence.
- [x] Comprehensive unit + integration testing remains required for all changes.
- [x] Integration boundary testing remains reproducible via Testcontainers.
- [x] Scope remains focused: add bot deployment parity, no broad refactors.
- [x] Performance verification remains defined by existing measurable criteria.

Post-Phase 1 re-check:
- [x] Research resolves deployment/runtime ambiguity for Bot service containerization.
- [x] Data model and API contracts remain valid without conflicting shape changes.
- [x] Quickstart and deployment flow include Bot image/build/run parity and security constraints.

## Core Implementation Sequence

### Step 1: Foundation Bootstrap

- Execute Setup + Foundational tasks first (`T001-T015`).
- Output: runnable solution skeleton, dependency wiring, DB baseline, CI baseline.

### Step 2: MVP Query Path (US1)

- Implement `/framedata` query flow via `GET /v1/moves/query` (`T016-T026`).
- Apply single-game de-scope updates (`T071-T074`) removing `game` parameter handling.
- Output: exact lookup behavior and clear unsupported/missing input handling.

### Step 3: Ingestion Backbone (US2)

- Implement scraper + ingest + DB persistence + JSON exports (`T027-T036`).
- Output: refreshable dataset and explicit partial-failure ingestion status reporting.

### Step 4: Bot Service Runtime Parity (Deployment Amendment)

- Add Bot runtime host bootstrap and production-ready startup wiring in `FrameData.Bot`.
- Add `Dockerfile.bot` and compose wiring so Bot runs alongside API/Ingestion/Postgres.
- Extend release/deployment image publishing so Bot image tags are produced with API/Ingestion.
- Output: four-service runtime topology (bot, api, ingestion, postgres) with repeatable local/cloud deployment behavior.

### Step 5: Usability/Media/Metadata Enhancements

- Execute existing story slices in order: US3 (`T037-T045`), US4 (`T046-T053`),
  US5 (`T054-T058`), US6 (`T059-T064`).
- Output: fuzzy lookup, media enrichment, storage analysis, advanced metadata.

### Step 6: Cross-Cutting Hardening

- Execute polish tasks (`T065-T070`) with deployment automation updates and security gates.
- Output: observability, deploy pipeline parity, performance evidence, security checklist evidence.

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

repo root:
├── Dockerfile.api
├── Dockerfile.ingestion
└── Dockerfile.bot   # added in this planning cycle
```

**Structure Decision**: Keep the existing layered .NET multi-service structure and
complete deployment/runtime parity for all declared services by adding explicit Bot
container artifacts and release pipeline coverage.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |
