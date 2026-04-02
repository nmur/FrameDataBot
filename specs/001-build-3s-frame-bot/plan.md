# Implementation Plan: Discord 3s Frame Data Bot

**Branch**: `001-build-3s-frame-bot` | **Date**: 2026-04-02 | **Spec**: `/specs/001-build-3s-frame-bot/spec.md`
**Input**: Feature specification from `/specs/001-build-3s-frame-bot/spec.md`

## Summary

Deliver the existing 3s frame-data platform scope with an explicit simplification:
remove `game` parameter support from all public interfaces and backend logic for the
current implementation cycle. Query behavior remains `/framedata` with required
`character` and `move` only, while preserving all existing story goals for ingestion,
fuzzy matching, media enrichment, and operational hardening.

## Technical Context

**Language/Version**: .NET 10 (C#) for bot/API/ingestion/scraper services and shared libraries  
**Primary Dependencies**: Discord.Net, ASP.NET Core Minimal APIs, AngleSharp, FuzzySharp, Serilog, NSubstitute, Shouldly, xUnit, Testcontainers for .NET, Npgsql  
**Storage**: PostgreSQL (JSONB for nested move/frame structures), character JSON exports on disk  
**Testing**: xUnit + Shouldly + NSubstitute (unit); Testcontainers for .NET (integration); contract tests for API payloads  
**Target Platform**: Linux Docker containers on cloud host (Render baseline)  
**Project Type**: Multi-service backend (Bot, API, Ingestion, Scraper + shared libs)  
**Performance Goals**: SC-001 validation run: >=95% valid exact-name queries complete in <3 seconds, measured with fixed-size representative samples across API query and bot end-to-end latency  
**Constraints**: .NET-only implementation for this feature iteration; no Moq; no FluentAssertions; secure least-privilege container deployment; remove multi-game request handling from current interfaces and backend logic  
**Scale/Scope**: Single-game (3s) deployment scope with no caller-provided game selector

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Pre-Phase 0 Gate:
- [x] Functions are planned as small, single-purpose units.
- [x] Comment policy is intent-only (why, not what).
- [x] TDD order is explicit in task sequencing.
- [x] Comprehensive unit + integration strategy is defined.
- [x] Integration tests use reliable/reproducible infra (Testcontainers).
- [x] Scope is constrained to independent user stories and explicit de-scope cleanup.
- [x] Performance targets and validation are defined.

Post-Phase 1 Re-check:
- [x] Research decisions resolve implementation ambiguity for game-parameter removal.
- [x] Data model and contracts align with single-game public interface.
- [x] Quickstart and tasks preserve constitution quality gates.

## Core Implementation Sequence

### Step 1: Foundation Bootstrap

- Execute Setup + Foundational tasks first (`T001-T015`).
- Output: runnable solution skeleton, dependency wiring, DB baseline, CI baseline.

### Step 2: MVP Query Path (US1)

- Implement `/framedata` query flow via `GET /v1/moves/query` (`T016-T026`).
- Apply single-game de-scope updates (`T071-T074`) to remove `game` parameter handling
  from parser, endpoint contracts, and backend lookup flow.
- Output: exact behavior with clear handling for unsupported character/move only.

### Step 3: Ingestion Backbone (US2)

- Implement scraper + ingest + DB persistence + JSON exports (`T027-T036`).
- Output: refreshable dataset and ingestion status endpoints with explicit partial-failure reporting.

### Step 4: Usability Refinement (US3)

- Add alias normalization + fuzzy ranking + disambiguation (`T037-T045`).
- Output: shorthand/numpad/colloquial input support with safe ambiguity handling.

### Step 5: Visual Data Layer (US4)

- Add last active-frame image scraping/storage/response enrichment (`T046-T053`).
- Output: optional media references in query responses.

### Step 6: Capacity Governance (US5)

- Implement storage assessment workflow (`T054-T058`).
- Output: report-driven decision gate before full image archival.

### Step 7: Metadata Expansion (US6)

- Add advanced move metadata mapping/serialization (`T059-T064`).
- Output: enriched responses without regressing baseline fields.

### Step 8: Cross-Cutting Hardening

- Execute polish tasks (`T065-T070`) after selected story completion.
- Output: observability, deploy pipeline, performance validation, security-gate verification, implementation report.

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

**Structure Decision**: Keep the existing layered .NET multi-service structure and apply
single-game interface simplification without introducing new services or abstractions.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |
