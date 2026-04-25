# Implementation Plan: Discord 3s Frame Data Bot

**Branch**: `001-build-3s-frame-bot` | **Date**: 2026-04-25 | **Spec**: `/specs/001-build-3s-frame-bot/spec.md`
**Input**: Feature specification from `/specs/001-build-3s-frame-bot/spec.md`, plus 2026-04-25 planning refinement: make ingestion real by replacing in-memory ingestion/query storage with a Postgres-backed worker/API path.

## Summary

Close the remaining US2 gap between the planned ingestion story and the current scaffold. The next implementation slice turns `FrameData.Ingestion` into a real one-shot worker, adds a full supported-character/source-id catalog, replaces in-memory repositories with Npgsql/PostgreSQL implementations, proves persistence with Testcontainers, and verifies the API/bot query path reads the same persisted data produced by ingestion. The production stack also includes a Seq container for centralized structured logs from the Bot, API, and Ingestion services so operators can follow Discord requests, API queries, ingestion runs, and per-character scrape details end to end. Existing live Discord slash-command handling remains unchanged and continues to call the API; rich Discord embeds remain a later US6 follow-up.

## Technical Context

- **Language/Version**: .NET 10 (C#) for bot/API/ingestion/scraper services and shared libraries
- **Primary Dependencies**: Discord.Net WebSocket/Interactions, ASP.NET Core Minimal APIs, AngleSharp, FuzzySharp, Serilog, Serilog.Sinks.Seq, Seq Docker container, NSubstitute, Shouldly, xUnit, Testcontainers for .NET, Npgsql
- **Storage**: PostgreSQL is the source of truth for `characters`, `moves`, and `ingestion_runs`; JSON exports remain a generated disk artifact mounted from container storage
- **Testing**: xUnit + Shouldly + NSubstitute for unit tests; Testcontainers PostgreSQL integration tests for repositories, ingestion worker, API query reads, and schema bootstrap; deterministic Discord boundary tests stay separate from live Discord
- **Target Platform**: Linux Docker containers on local Compose and Unraid/self-hosted Docker; ingestion runs as an explicit one-shot container or API-triggered background run; Seq runs as a persistent central logging container in both local and production Compose topologies
- **Project Type**: Multi-service backend (`FrameData.Bot`, `FrameData.Api`, `FrameData.Ingestion`, scraper + shared libraries)
- **Performance Goals**: SC-001 remains: >=95% valid exact-name queries complete in <3 seconds across API latency and bot end-to-end latency on a fixed representative sample
- **Constraints**: .NET-only scraper scope; no Moq/FluentAssertions; no live Discord in CI; ingestion must support partial success while replacing the stored dataset with successfully ingested character scopes; API and ingestion must use the same Postgres schema/repository implementations
- **Scale/Scope**: Single-game Street Fighter III: 3rd Strike scope; full supported-roster source catalog; Normals/Specials/Super Arts/Misc ingestion only for this slice; no hitbox media, metadata enrichment, fuzzy lookup, or rich Discord embed work in this slice

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Pre-Phase 0 gate:
- [x] Small, single-purpose functions: persistence, catalog, schema bootstrap, worker host, scraper/orchestrator, and API trigger concerns are planned as separate services/classes.
- [x] Descriptive naming/intent comments: new names must describe Postgres repository behavior, ingestion worker options, and catalog entries directly; comments are only for external source/schema constraints.
- [x] TDD order: the new task block adds unit/contract/integration tests before implementation tasks.
- [x] Comprehensive automated testing: Testcontainers is required to prove actual rows are inserted/read from PostgreSQL; in-memory repository tests are not sufficient for this slice.
- [x] Reproducible integration testing: source HTML is supplied by deterministic fixtures/fake HTTP handlers in automated tests; live source scraping is covered by manual smoke validation.
- [x] Focused scope/performance: the slice only makes US2 persistence/worker behavior real and preserves the existing Discord/API command surface.

Post-Phase 1 re-check:
- [x] Research resolves the repository, schema bootstrap, worker execution, catalog, and API trigger strategy.
- [x] Data model identifies the full character source catalog and per-character ingestion status needed for retry visibility.
- [x] API contract documents optional ingestion run scoping while keeping full-catalog ingestion as the default.
- [x] Quickstart documents real worker invocation, Postgres verification, JSON export verification, and API/bot query validation from persisted rows.

## Core Implementation Sequence

### Step 1: Prove the Current Gap With Tests

- Add Testcontainers coverage that fails against the current in-memory repositories by asserting `characters`, `moves`, and `ingestion_runs` rows exist in PostgreSQL after repository/orchestrator calls.
- Add API integration coverage where a move inserted through the real repository is queryable through `GET /v1/moves/query`.
- Add worker host coverage that verifies the ingestion executable wires configuration, catalog, repositories, source client, and export path instead of printing `Hello, World!`.

### Step 2: Centralize Schema Bootstrap

- Add a small schema bootstrap service that applies the current SQL schema from `src/FrameData.Infrastructure/Persistence/Migrations/0001_Initial.sql` to a configured PostgreSQL database.
- Use the same bootstrap path in API startup, ingestion worker startup, and integration test setup.
- Keep the schema bootstrap deterministic and idempotent; the dataset itself is replaced wholesale by ingestion runs instead of being evolved incrementally.

### Step 3: Replace In-Memory Repositories

- Implement `CharacterRepository`, `MoveRepository`, and `IngestionRunRepository` with Npgsql-backed SQL operations.
- `MoveRepository` must satisfy both ingestion upsert needs and `IMoveQueryRepository` exact lookup needs.
- Remove hardcoded Makoto seed data from runtime repositories; tests must seed required data explicitly or run ingestion.

### Step 4: Add Full Supported Character Catalog

- Introduce a single catalog of enabled 3s characters with internal character id, display name, source character id, aliases, and stable display order.
- Use the catalog as the default scope for the worker and API-triggered ingestion runs.
- Allow explicit scoped ingestion by character id for tests/manual retries without changing the default full-catalog behavior.

### Step 5: Wire the Ingestion Worker Host

- Replace the console template program with a .NET host that loads configuration, validates `POSTGRES_CONNECTION_STRING`, configures `Ingestion:SourceBaseUrl` and `Ingestion:ExportPath`, bootstraps the schema, runs the orchestrator for the requested scope, logs the run result, and exits with an explicit status code.
- Preserve partial-success visibility: successful character scopes become the replacement dataset and failed scopes are visible in run status for retry.
- Keep scheduling outside the worker for this slice; Unraid/Compose/GitHub Actions can invoke the one-shot container.

### Step 6: Wire API to the Same Persistent Store

- Update API DI so query endpoints, ingestion endpoints, and repository implementations all use the same Postgres-backed services.
- Update `POST /v1/ingestion/runs` so omitted scope means full catalog and optional scope means targeted retry.
- Verify the bot path indirectly by keeping the bot API client unchanged and proving API queries return rows from PostgreSQL.

### Step 7: Update Operational Validation

- Document `docker compose run --rm ingestion` or equivalent Unraid one-shot execution.
- Document how to verify database rows, JSON exports, ingestion run status, and Discord `/framedata` after ingestion.
- Keep rich Discord response work (`T096-T100`) deferred.

### Step 8: Add JSON Backup and Restore Utility Mode

- Extend the one-shot ingestion executable with `backup` and `restore` modes so the same image can export/import the current dataset without a separate service.
- Export a `manifest.json` plus one JSON file per character under `characters/`, making backups inspectable and easy to copy from the mounted data volume.
- Restore by validating the manifest and character files, bootstrapping the schema, and transactionally replacing the stored `characters` and `moves` dataset with the backup contents.
- Keep ingestion run history out of the default backup; the portable backup represents the queryable frame-data dataset.

### Step 9: Centralize Structured Logs with Seq

- Add a persistent Seq container to local and production Compose so API, Bot, and Ingestion logs are searchable from one UI.
- Configure all three .NET services with shared Serilog bootstrap logic that always writes console logs and writes to Seq when `SEQ_SERVER_URL` is configured.
- Default application categories to Debug-level logging while keeping noisy framework categories at safer levels, with `SEQ_MINIMUM_LEVEL` available for operational overrides.
- Add request and interaction logging for Discord command handling, Bot-to-API calls, API move queries, API-triggered ingestion runs, and ingestion status lookups.
- Add detailed ingestion run logs for source page fetches, per-character parse/export status, per-section move counts, per-move frame data at Debug level, dataset replacement, and failures.

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
│   ├── Api/
│   ├── Commands/
│   ├── Discord/
│   ├── Formatting/
│   └── Hosting/
├── FrameData.Api/
│   └── Endpoints/
├── FrameData.Ingestion/
│   ├── Backup/           # JSON backup/export and restore/import utilities
│   ├── Catalog/          # supported character/source-id catalog
│   ├── Hosting/          # worker options/bootstrap
│   └── Services/
├── FrameData.Scraper/
│   ├── Parsing/
│   └── Source/
├── FrameData.Domain/
├── FrameData.Infrastructure/
│   ├── Persistence/
│   │   ├── Migrations/
│   │   └── Repositories/ # Npgsql-backed repositories
│   └── Storage/
└── FrameData.Shared/

tests/
├── unit/
│   ├── FrameData.Bot.Tests/
│   ├── FrameData.Domain.Tests/
│   └── FrameData.Ingestion.Tests/
├── integration/
│   ├── FrameData.Api.IntegrationTests/
│   ├── FrameData.Bot.IntegrationTests/
│   └── FrameData.Ingestion.IntegrationTests/
└── contract/
    └── FrameData.Contracts.Tests/
```

**Structure Decision**: Keep the existing layered .NET multi-service structure. Add ingestion catalog/hosting code under `FrameData.Ingestion`, shared schema bootstrap and Npgsql repositories under `FrameData.Infrastructure`, and reuse API/Bot boundaries so the bot continues to access data only through the API.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |
