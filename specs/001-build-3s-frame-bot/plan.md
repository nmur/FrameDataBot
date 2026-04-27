# Implementation Plan: Discord 3s Frame Data Bot

**Branch**: `001-build-3s-frame-bot` | **Date**: 2026-04-25 | **Spec**: `/specs/001-build-3s-frame-bot/spec.md`
**Input**: Feature specification from `/specs/001-build-3s-frame-bot/spec.md`, plus 2026-04-25 planning refinement for real ingestion and 2026-04-27 planning refinement: simplify runtime storage by replacing Postgres-backed query persistence with a versioned static JSON/media dataset on disk.

## Summary

Revise the US2 persistence direction from a Postgres-backed runtime store to a static dataset model. Ingestion remains a Dockerized one-shot worker, but it is no longer part of the always-running application stack. The worker scrapes source pages, writes a versioned dataset directory containing `manifest.json`, per-character move JSON files, and later media files, then atomically publishes that directory under a shared local dataset path. The API and Bot stack mount the active dataset read-only; the API loads all move data into memory at startup and uses static-file-backed query indexes for exact, alias, and fuzzy lookup. JSON backup/restore utility modes are no longer needed because the dataset itself is already a portable JSON/media bundle.

The production/runtime stack keeps Bot, API, and Seq as long-running services. A separate ingestion Compose file runs the ingestion worker on demand against the same host dataset root so operators can refresh the static dataset without keeping ingestion or Postgres online. Later image work stores last-active screenshots beside the move data and sends Discord rich embed media as local file attachments from the Bot rather than requiring public media URLs.

## Technical Context

- **Language/Version**: .NET 10 (C#) for bot/API/ingestion/scraper services and shared libraries
- **Primary Dependencies**: Discord.Net WebSocket/Interactions, ASP.NET Core Minimal APIs, AngleSharp, FuzzySharp, Serilog, Serilog.Sinks.Seq, Seq Docker container, NSubstitute, Shouldly, xUnit
- **Storage**: Versioned static dataset directories on local disk are the source of truth. Each dataset contains `manifest.json`, one JSON file per character under `characters/`, and media files under `media/`; the API mounts the active dataset read-only and loads it into memory at startup. PostgreSQL/Npgsql are to be removed from runtime storage.
- **Testing**: xUnit + Shouldly + NSubstitute for unit tests; file-system integration tests for dataset publish/load/query behavior; deterministic Discord boundary tests stay separate from live Discord. Testcontainers PostgreSQL coverage becomes obsolete once the static dataset migration is complete.
- **Target Platform**: Linux Docker containers on local Compose and Unraid/self-hosted Docker; Bot/API/Seq run in the main stack; ingestion runs only as an explicit one-shot container from a separate Compose file sharing the same host dataset root.
- **Project Type**: Multi-service backend (`FrameData.Bot`, `FrameData.Api`, `FrameData.Ingestion`, scraper + shared libraries)
- **Performance Goals**: SC-001 remains: >=95% valid exact-name queries complete in <3 seconds across API latency and bot end-to-end latency on a fixed representative sample
- **Constraints**: .NET-only scraper scope; no Moq/FluentAssertions; no live Discord in CI; ingestion must publish static datasets atomically, preserve the prior active dataset if a full refresh fails, and support resumable media capture by skipping already-successful media files.
- **Scale/Scope**: Single-game Street Fighter III: 3rd Strike scope; full supported-roster source catalog; Normals/Specials/Super Arts/Misc ingestion plus static dataset publishing; last-active hitbox image capture writes local media files in a later slice; no public CDN is required for Discord embeds because the Bot can upload local files as embed attachments.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Pre-Phase 0 gate:
- [x] Small, single-purpose functions: dataset loading, dataset publishing, catalog, worker host, scraper/orchestrator, and media storage concerns are planned as separate services/classes.
- [x] Descriptive naming/intent comments: new names must describe static dataset behavior, ingestion worker options, media paths, and catalog entries directly; comments are only for external source/schema constraints.
- [x] TDD order: the new task block adds unit/contract/integration tests before implementation tasks.
- [x] Comprehensive automated testing: file-system integration tests are required to prove static datasets are published atomically, loaded by the API, and queryable without PostgreSQL.
- [x] Reproducible integration testing: source HTML is supplied by deterministic fixtures/fake HTTP handlers in automated tests; live source scraping is covered by manual smoke validation.
- [x] Focused scope/performance: the slice replaces the runtime storage substrate with static files while preserving the existing Discord/API command surface.

Post-Phase 1 re-check:
- [x] Research resolves the static dataset, worker execution, catalog, media storage, and API query-load strategy.
- [x] Data model identifies the full character source catalog, static dataset manifest, character file layout, and move media references needed for retry/resume visibility.
- [x] API contract keeps move-query behavior stable while removing runtime ingestion trigger/status endpoints from the always-running API target.
- [x] Quickstart must document separate ingestion Compose execution, active dataset verification, and API/bot query validation from mounted JSON files.

## Core Implementation Sequence

### Step 1: Define the Static Dataset Contract

- Introduce a versioned dataset directory shape:
  `manifest.json`, `characters/{characterId}.json`, `media/{characterId}/{moveId}/...`.
- Keep stable `characterId` and `moveId` values as the join between move JSON and media files.
- Include dataset metadata such as generated timestamp, source base URL, source page IDs, schema version, character count, move count, and media count.
- Treat the dataset directory itself as the portable backup/restore artifact; do not maintain separate JSON backup/import modes.

### Step 2: Replace Runtime PostgreSQL With Static Dataset Loading

- Implement a static dataset loader under `FrameData.Infrastructure` that validates the manifest, reads all character JSON, builds in-memory exact/alias/fuzzy lookup indexes, and exposes the existing `IMoveQueryRepository` behavior.
- Remove runtime schema bootstrap, Npgsql repository registration, API ingestion endpoints, and Postgres connection-string requirements from the API and Bot deployment path.
- Keep API response contracts stable for `/v1/moves/query`; only the storage backing changes.
- Preserve previous exact and fuzzy behavior by running existing query tests against a temporary static dataset fixture.

### Step 3: Convert Ingestion Into a Dataset Publisher

- Keep `FrameData.Ingestion` as a Dockerized one-shot worker, but make its output the canonical static dataset directory instead of Postgres rows plus JSON exports.
- The worker writes into a staging directory, validates the full dataset, then atomically publishes it as a new dataset version or updates the configured `active` pointer.
- Failed full-catalog runs must leave the prior active dataset intact.
- Scoped or resume runs may fill missing media for an existing dataset without re-scraping all move data.

### Step 4: Split Runtime and Ingestion Compose Topologies

- Remove `postgres` and `ingestion` services from the main local/production Compose files.
- Add a separate `docker-compose.ingestion.yml` for one-shot ingestion execution.
- Use a shared host dataset root across both Compose stacks. Runtime mounts the active dataset read-only; ingestion mounts the dataset root read-write.
- Update `.env.example`/`.env.prod.example` to use `FRAMEDATA_DATASET_ROOT` and `FRAMEDATA_ACTIVE_DATASET_PATH` instead of Postgres/export-specific settings.

### Step 5: Prepare Media Storage for Local Discord Attachments

- Store last-active PNGs under the dataset `media/` tree with metadata that links each file to a stable `moveId`.
- API query responses can expose relative media paths and metadata, but Discord rendering should be implemented later by having the Bot read local files and send them as rich embed attachments using `attachment://...`.
- No public CDN is required for the first media implementation.

### Step 6: Centralize Structured Logs With Seq

- Add a persistent Seq container to local and production Compose so API, Bot, and Ingestion logs are searchable from one UI.
- Configure all three .NET services with shared Serilog bootstrap logic that always writes console logs and writes to Seq when `SEQ_SERVER_URL` is configured.
- Default application categories to Debug-level logging while keeping noisy framework categories at safer levels, with `SEQ_MINIMUM_LEVEL` available for operational overrides.
- Add request and interaction logging for Discord command handling, Bot-to-API calls, API move queries, dataset load events, and ingestion worker runs.
- Add detailed ingestion run logs for source page fetches, per-character parse/export status, per-section move counts, per-move frame data at Debug level, dataset publish/replacement, media capture, and failures.

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
│   ├── Catalog/          # supported character/source-id catalog
│   ├── Hosting/          # worker options/bootstrap
│   ├── Media/            # hitbox image capture and dataset media metadata
│   ├── Publishing/       # static dataset writer, manifest, atomic publish
│   └── Services/
├── FrameData.Scraper/
│   ├── Parsing/
│   └── Source/
├── FrameData.Domain/
├── FrameData.Infrastructure/
│   ├── Dataset/          # static dataset reader and query index
│   └── Storage/          # file-system helpers for dataset/media paths
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

**Structure Decision**: Keep the existing layered .NET multi-service structure, but remove PostgreSQL as a runtime dependency. Add static dataset loading/query indexing under `FrameData.Infrastructure`, dataset publishing and media capture under `FrameData.Ingestion`, and reuse API/Bot boundaries so the bot continues to access move data through the API while reading local media files for Discord attachment embeds in the later rich-response slice.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |
