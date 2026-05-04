---

description: "Task list for implementing Discord 3s frame data bot"
---

# Tasks: Discord 3s Frame Data Bot

**Input**: Design documents from `/specs/001-build-3s-frame-bot/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Tests are REQUIRED. Every user story includes unit, integration, and contract coverage where applicable, with test-first sequencing.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on incomplete tasks)
- **[Story]**: User story label (US1, US2, ...)
- Include exact file paths in every task description

## Path Conventions

- Source: `src/FrameData.*`
- Tests: `tests/unit/`, `tests/integration/`, `tests/contract/`
- Feature docs: `specs/001-build-3s-frame-bot/`

## Implementation Detail References

- Core sequence and task ordering authority:
  `specs/001-build-3s-frame-bot/plan.md` (Core Implementation Sequence section)
- Requirements and story acceptance criteria:
  `specs/001-build-3s-frame-bot/spec.md`
- Technology decisions and tradeoffs:
  `specs/001-build-3s-frame-bot/research.md`
- Entity definitions, validations, transitions:
  `specs/001-build-3s-frame-bot/data-model.md`
- Interface and payload contracts:
  `specs/001-build-3s-frame-bot/contracts/bot-api.yaml`,
  `specs/001-build-3s-frame-bot/contracts/discord-command.md`
- Deployment/test execution flow:
  `specs/001-build-3s-frame-bot/quickstart.md`

## Core Implementation Runbook

1. Execute `T001-T015` before starting any story work.
2. Deliver US1 (`T016-T026`) including Bot runtime/container parity follow-up (`T075-T082`), live Discord gateway/slash-command follow-up (`T083-T095`), and Discord embed response follow-up (`T096-T100`).
3. Deliver US2 (`T027-T036`) as MVP ingestion backbone.
4. Historical note: the Postgres persistence follow-up (`T101-T118`) and JSON backup/restore follow-up (`T119-T125`) were completed, but are superseded by the static dataset storage refactor (`T136-T150`).
5. Deliver Seq centralized logging follow-up (`T126-T135`) before storage/media refactors so production diagnostics are available.
6. Deliver static dataset storage refactor (`T136-T150`) and source column expansion (`T151-T156`) before US4 image work so move data and media share one persistent dataset bundle with all currently required source columns.
7. Completed refinement scope: US3 alias/fuzzy lookup, static dataset refactor/source column expansion, and US4 representative active-frame media. Embed response formatting is part of the US1 live Discord response path.
8. At each step, consult the reference list above for requirements and contracts.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initialize .NET 10 solution and baseline project layout.

- [X] T001 Create .NET 10 solution file at `FrameDataBot.sln`
- [X] T002 Create service projects in `src/FrameData.Bot/`, `src/FrameData.Api/`, `src/FrameData.Ingestion/`, and `src/FrameData.Scraper/`
- [X] T003 [P] Create shared library projects in `src/FrameData.Domain/`, `src/FrameData.Infrastructure/`, and `src/FrameData.Shared/`
- [X] T004 [P] Create test projects in `tests/unit/FrameData.Domain.Tests/`, `tests/unit/FrameData.Bot.Tests/`, `tests/unit/FrameData.Ingestion.Tests/`, `tests/integration/FrameData.Api.IntegrationTests/`, `tests/integration/FrameData.Ingestion.IntegrationTests/`, and `tests/contract/FrameData.Contracts.Tests/`
- [X] T005 Add project references and NuGet dependencies in `FrameDataBot.sln` and all `*.csproj` files
- [X] T006 [P] Add repository-level SDK/global settings in `Directory.Build.props` and `.editorconfig`
- [X] T007 [P] Add container and local orchestration baseline in `docker-compose.yml` and `.env.example`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core cross-story infrastructure that MUST exist before story implementation.

**⚠️ CRITICAL**: No user story work starts before this phase is complete.

- [X] T008 Create PostgreSQL schema bootstrap baseline in `src/FrameData.Infrastructure/Persistence/Migrations/0001_Initial.sql`
- [X] T009 [P] Implement database connectivity and unit-of-work abstractions in `src/FrameData.Infrastructure/Persistence/DbConnectionFactory.cs` and `src/FrameData.Infrastructure/Persistence/UnitOfWork.cs`
- [X] T010 [P] Implement JSON export storage service in `src/FrameData.Infrastructure/Storage/CharacterJsonExportService.cs`
- [X] T011 [P] Implement shared domain primitives and result/error model in `src/FrameData.Domain/Common/` and `src/FrameData.Shared/Contracts/`
- [X] T012 Implement API host bootstrap with health endpoint and DI wiring in `src/FrameData.Api/Program.cs`
- [X] T013 [P] Implement ingestion run tracking entity/repository baseline in `src/FrameData.Domain/Ingestion/IngestionRun.cs` and `src/FrameData.Infrastructure/Persistence/Repositories/IngestionRunRepository.cs`
- [X] T014 [P] Create Testcontainers PostgreSQL fixture for integration tests in `tests/integration/FrameData.Api.IntegrationTests/Fixtures/PostgresContainerFixture.cs`
- [X] T015 [P] Add CI build/test baseline workflow in `.github/workflows/ci.yml`

**Checkpoint**: Foundation complete; user stories can proceed.

---

## Phase 3: User Story 1 - Exact Move Lookup MVP (Priority: P1) 🎯

**Goal**: Provide exact canonical move-name lookup via live Discord `/framedata` slash command and API response.

**Independent Test**: Query a supported character + exact canonical move through the Discord slash command and receive correct frame data; unknown character/move inputs return clear not-found or unsupported responses.

### Tests for User Story 1 (MANDATORY) ⚠️

- [X] T016 [P] [US1] Add unit tests for exact move query service in `tests/unit/FrameData.Domain.Tests/MoveLookup/ExactMoveLookupServiceTests.cs`
- [X] T017 [P] [US1] Add unit tests for `/framedata` input validation for required `character` and `move` inputs in `tests/unit/FrameData.Bot.Tests/Commands/MoveCommandParserTests.cs`
- [X] T018 [P] [US1] Add API integration tests for `GET /v1/moves/query` exact and not-found cases in `tests/integration/FrameData.Api.IntegrationTests/MoveQueryExactTests.cs`
- [X] T019 [P] [US1] Add contract tests for exact-match and error response schemas for `GET /v1/moves/query` in `tests/contract/FrameData.Contracts.Tests/MoveQueryContractTests.cs`

### Implementation for User Story 1

- [X] T020 [P] [US1] Implement Character and Move aggregate models in `src/FrameData.Domain/Characters/Character.cs` and `src/FrameData.Domain/Moves/Move.cs`
- [X] T021 [P] [US1] Implement MoveFrameData model in `src/FrameData.Domain/Moves/MoveFrameData.cs`
- [X] T022 [US1] Implement exact lookup domain service in `src/FrameData.Domain/MoveLookup/ExactMoveLookupService.cs`
- [X] T023 [US1] Implement move query repository in `src/FrameData.Infrastructure/Persistence/Repositories/MoveRepository.cs`
- [X] T024 [US1] Implement API endpoint for `GET /v1/moves/query` exact mode in `src/FrameData.Api/Endpoints/MoveQueryEndpoint.cs`
- [X] T025 [US1] Implement Discord `/framedata` exact-query handler in `src/FrameData.Bot/Discord/FramedataInteractionHandler.cs`
- [X] T026 [US1] Implement not-found/unsupported-character response mapper in `src/FrameData.Bot/Formatting/MoveEmbedResponseFactory.cs`

### Scope Simplification Follow-Up (Single-Game Interface)

- [X] T071 [P] [US1] Remove `game` input from Discord command parser/handler flow and update affected unit tests in `src/FrameData.Bot/Commands/MoveCommandParser.cs`, `src/FrameData.Bot/Discord/FramedataInteractionHandler.cs`, and `tests/unit/FrameData.Bot.Tests/Commands/MoveCommandParserTests.cs`
- [X] T072 [P] [US1] Remove `game` query parameter and unsupported-game path from move query endpoint and API integration tests in `src/FrameData.Api/Endpoints/MoveQueryEndpoint.cs` and `tests/integration/FrameData.Api.IntegrationTests/MoveQueryExactTests.cs`
- [X] T073 [US1] Remove game-discriminator handling from exact lookup service/repository interface and related domain tests in `src/FrameData.Domain/MoveLookup/ExactMoveLookupService.cs`, `src/FrameData.Domain/MoveLookup/IMoveQueryRepository.cs`, `src/FrameData.Infrastructure/Persistence/Repositories/MoveRepository.cs`, and `tests/unit/FrameData.Domain.Tests/MoveLookup/ExactMoveLookupServiceTests.cs`
- [X] T074 [US1] Update contract tests and response formatting for single-game behavior in `tests/contract/FrameData.Contracts.Tests/MoveQueryContractTests.cs` and `src/FrameData.Bot/Formatting/MoveEmbedResponseFactory.cs`

### Runtime Containerization Follow-Up (Bot Service Parity)

- [X] T075 [P] [US1] Add unit tests for bot host bootstrap and required configuration validation in `tests/unit/FrameData.Bot.Tests/Hosting/BotHostBootstrapTests.cs`
- [X] T076 [P] [US1] Add integration test coverage for bot-to-API client wiring assumptions in `tests/integration/FrameData.Api.IntegrationTests/BotService/BotApiConnectivityConfigTests.cs`
- [X] T077 [US1] Implement production bot runtime bootstrap in `src/FrameData.Bot/Program.cs`
- [X] T078 [P] [US1] Add bot container build definition in `Dockerfile.bot`
- [X] T079 [US1] Add Bot service wiring and environment mapping in `docker-compose.yml` and `.env.example`
- [X] T080 [US1] Extend image publishing to include Bot image in `.github/workflows/release.yml`
- [X] T081 [US1] Remove provider-specific bot deployment coupling and keep host-agnostic deployment configuration in `.github/workflows/release.yml` and `specs/001-build-3s-frame-bot/quickstart.md`
- [X] T082 [US1] Update deployment verification documentation for four-service topology in `specs/001-build-3s-frame-bot/quickstart.md`

### Discord Gateway Slash-Command Follow-Up

- [X] T083 [P] [US1] Add unit tests for `/framedata` slash command definition in `tests/unit/FrameData.Bot.Tests/Discord/FramedataSlashCommandDefinitionTests.cs`
- [X] T084 [P] [US1] Add unit tests for slash interaction option extraction and validation in `tests/unit/FrameData.Bot.Tests/Discord/SlashCommandInteractionMapperTests.cs`
- [X] T085 [P] [US1] Add unit tests for interaction handler success and error responses in `tests/unit/FrameData.Bot.Tests/Discord/FramedataInteractionHandlerTests.cs`
- [X] T086 [US1] Add deterministic bot integration test project and gateway wiring tests in `tests/integration/FrameData.Bot.IntegrationTests/FrameData.Bot.IntegrationTests.csproj` and `tests/integration/FrameData.Bot.IntegrationTests/DiscordGatewayWiringTests.cs`
- [X] T087 [P] [US1] Add contract tests for the Discord slash command schema in `tests/contract/FrameData.Contracts.Tests/DiscordCommandContractTests.cs`
- [X] T088 [P] [US1] Implement slash command definition builder in `src/FrameData.Bot/Discord/FramedataSlashCommandDefinition.cs`
- [X] T089 [P] [US1] Implement slash interaction option mapper in `src/FrameData.Bot/Discord/SlashCommandInteractionMapper.cs`
- [X] T090 [US1] Implement Discord interaction handler that calls `IMoveQueryApiClient` and `MoveEmbedResponseFactory` in `src/FrameData.Bot/Discord/FramedataInteractionHandler.cs`
- [X] T091 [US1] Implement guild command registration service in `src/FrameData.Bot/Discord/DiscordCommandRegistrar.cs`
- [X] T092 [US1] Replace bot keepalive loop with Discord gateway login/start/stop lifecycle in `src/FrameData.Bot/Hosting/BotRuntimeService.cs`
- [X] T093 [US1] Wire Discord.Net socket client, interaction service, registrar, and handler into DI in `src/FrameData.Bot/Program.cs`
- [X] T094 [US1] Update bot runtime option validation for Discord gateway configuration in `src/FrameData.Bot/Hosting/BotRuntimeOptions.cs`, `src/FrameData.Bot/Hosting/BotRuntimeOptionsLoader.cs`, and `tests/unit/FrameData.Bot.Tests/Hosting/BotHostBootstrapTests.cs`
- [X] T095 [US1] Update Discord gateway smoke-test instructions and environment examples in `.env.example`, `.env.prod.example`, and `specs/001-build-3s-frame-bot/quickstart.md`

### Discord Embed Response Follow-Up

- [X] T096 [P] [US1] Add unit tests for successful, ambiguous, and error embed formatting in `tests/unit/FrameData.Bot.Tests/Formatting/MoveEmbedResponseFactoryTests.cs`
- [X] T097 [P] [US1] Add unit tests for interaction handler embed-only sending in `tests/unit/FrameData.Bot.Tests/Discord/FramedataInteractionHandlerEmbedResponseTests.cs`
- [X] T098 [P] [US1] Update contract tests for embed-only `/framedata` responses in `tests/contract/FrameData.Contracts.Tests/DiscordCommandContractTests.cs`
- [X] T099 [P] [US1] Implement Discord move response model and embed factory in `src/FrameData.Bot/Formatting/DiscordMoveResponse.cs` and `src/FrameData.Bot/Formatting/MoveEmbedResponseFactory.cs`
- [X] T100 [US1] Extend Discord responder abstraction and interaction handler to send Discord.Net embeds without duplicate message content in `src/FrameData.Bot/Discord/IDiscordInteractionResponder.cs`, `src/FrameData.Bot/Discord/SocketSlashCommandResponder.cs`, and `src/FrameData.Bot/Discord/FramedataInteractionHandler.cs`

**Checkpoint**: US1 fully testable and deployable as a live Discord MVP with structured embed responses.

---

## Phase 4: User Story 2 - Source Ingestion and Persistence (Priority: P1)

**Goal**: Ingest source sections (Normals/Specials/Super Arts/Misc), publish a versioned static dataset directory, and make the API queryable from JSON files without PostgreSQL.

**Independent Test**: Run ingestion and verify `manifest.json`, one JSON file per character, and the active dataset pointer are produced; the API loads the active dataset from disk and answers move queries without PostgreSQL.

### Tests for User Story 2 (MANDATORY) ⚠️

- [X] T027 [P] [US2] Add unit tests for source section parsing in `tests/unit/FrameData.Ingestion.Tests/Scraping/CharacterSectionParserTests.cs`
- [X] T028 [P] [US2] Add unit tests for JSON export generation in `tests/unit/FrameData.Ingestion.Tests/Export/CharacterJsonExportServiceTests.cs`
- [X] T029 [P] [US2] Add ingestion integration tests for success and partial-success persistence in `tests/integration/FrameData.Ingestion.IntegrationTests/IngestionPersistenceTests.cs`
- [X] T030 [P] [US2] Add contract tests for ingestion run statuses (`Running|Succeeded|PartiallySucceeded|Failed`) in `tests/contract/FrameData.Contracts.Tests/IngestionRunContractTests.cs`

### Implementation for User Story 2

- [X] T031 [P] [US2] Implement scraper HTTP client and HTML document loader in `src/FrameData.Scraper/Source/SourceHttpClient.cs`
- [X] T032 [P] [US2] Implement section parsers for Normals/Specials/Super Arts/Misc in `src/FrameData.Scraper/Parsing/CharacterSectionParser.cs`
- [X] T033 [US2] Implement ingestion orchestrator with partial-success status handling in `src/FrameData.Ingestion/Services/IngestionOrchestrator.cs`
- [X] T034 [US2] Implement persistence mappers for character/move upserts in `src/FrameData.Infrastructure/Persistence/Repositories/CharacterRepository.cs` and `src/FrameData.Infrastructure/Persistence/Repositories/MoveRepository.cs`
- [X] T035 [US2] Implement character JSON export workflow in `src/FrameData.Ingestion/Services/CharacterExportWorkflow.cs`
- [X] T036 [US2] Implement ingestion trigger/status endpoints with explicit retry-required failure reporting in `src/FrameData.Api/Endpoints/IngestionEndpoints.cs`

**Checkpoint**: US1 live lookup plus US2 ingestion scaffold complete; historical Postgres persistence follows in `T101-T118` and is superseded by the static dataset refactor in `T136-T150`.

---

### Real Ingestion Persistence Follow-Up (Historical - Superseded)

**Goal**: Replace the ingestion scaffold with a real one-shot worker and Postgres-backed read/write path shared by API and bot.

**Independent Test**: Run the ingestion worker against fixture source HTML and Testcontainers PostgreSQL, then verify database rows, JSON exports, ingestion run status, and `GET /v1/moves/query` reads the persisted move through the API.

#### Tests for Real Ingestion Persistence (MANDATORY) ⚠️

- [X] T101 [P] [US2] Add unit tests for full supported character catalog entries and uniqueness in `tests/unit/FrameData.Ingestion.Tests/Catalog/SupportedCharacterCatalogTests.cs`
- [X] T102 [P] [US2] Add unit tests for ingestion worker configuration validation and exit-code mapping in `tests/unit/FrameData.Ingestion.Tests/Hosting/IngestionWorkerOptionsTests.cs`
- [X] T103 [P] [US2] Add contract tests for optional ingestion run scope and per-character status payloads in `tests/contract/FrameData.Contracts.Tests/IngestionRunContractTests.cs`
- [X] T104 [P] [US2] Add Testcontainers repository persistence tests proving character, move, and ingestion run rows are inserted/upserted in `tests/integration/FrameData.Ingestion.IntegrationTests/PostgresRepositoryPersistenceTests.cs`
- [X] T105 [P] [US2] Add Testcontainers orchestrator tests proving fixture HTML ingestion writes Postgres rows and JSON exports for success and partial-success cases in `tests/integration/FrameData.Ingestion.IntegrationTests/PostgresIngestionOrchestratorTests.cs`
- [X] T106 [P] [US2] Add API integration tests proving `GET /v1/moves/query` reads persisted Postgres rows inserted by ingestion/repositories in `tests/integration/FrameData.Api.IntegrationTests/MoveQueryPostgresPersistenceTests.cs`
- [X] T107 [P] [US2] Add ingestion worker host integration tests proving the executable wires catalog, repositories, schema bootstrap, source client, and export path instead of the console template in `tests/integration/FrameData.Ingestion.IntegrationTests/IngestionWorkerHostTests.cs`

#### Implementation for Real Ingestion Persistence

- [X] T108 [US2] Implement shared schema bootstrap service for the current SQL schema in `src/FrameData.Infrastructure/Persistence/Migrations/` and `src/FrameData.Infrastructure/Persistence/SchemaBootstrapper.cs`
- [X] T109 [US2] Adjust PostgreSQL bootstrap SQL for source character IDs and per-character ingestion status persistence in `src/FrameData.Infrastructure/Persistence/Migrations/0001_Initial.sql`
- [X] T110 [US2] Replace in-memory `CharacterRepository` with Npgsql-backed upsert/query implementation in `src/FrameData.Infrastructure/Persistence/Repositories/CharacterRepository.cs`
- [X] T111 [US2] Replace in-memory `MoveRepository` and hardcoded Makoto seed data with Npgsql-backed exact query and character move upsert implementation in `src/FrameData.Infrastructure/Persistence/Repositories/MoveRepository.cs`
- [X] T112 [US2] Replace in-memory `IngestionRunRepository` with Npgsql-backed run/status persistence in `src/FrameData.Infrastructure/Persistence/Repositories/IngestionRunRepository.cs`
- [X] T113 [US2] Implement full supported 3s character/source-id catalog in `src/FrameData.Ingestion/Catalog/SupportedCharacterCatalog.cs`
- [X] T114 [US2] Update `IngestionOrchestrator` to use catalog scopes, persist per-character statuses, and replace the stored dataset with successful character scopes in `src/FrameData.Ingestion/Services/IngestionOrchestrator.cs`
- [X] T115 [US2] Replace `Hello, World!` ingestion console template with a hosted one-shot worker in `src/FrameData.Ingestion/Program.cs` and `src/FrameData.Ingestion/Hosting/`
- [X] T116 [US2] Wire API startup to schema bootstrap and Postgres-backed repositories so API queries and ingestion endpoints use the shared store in `src/FrameData.Api/Program.cs`
- [X] T117 [US2] Update ingestion trigger/status endpoints to default to full catalog, accept optional scoped retries, and return per-character status details in `src/FrameData.Api/Endpoints/IngestionEndpoints.cs`
- [X] T118 [US2] Update compose/env/quickstart documentation for one-shot ingestion execution, export volume verification, and Postgres-backed API/bot validation in `docker-compose.yml`, `docker-compose.prod.yml`, `.env.example`, `.env.prod.example`, and `specs/001-build-3s-frame-bot/quickstart.md`

**Checkpoint**: US2 is real in production terms: ingestion worker scrapes configured source pages, writes PostgreSQL rows, exports JSON files, records retryable failures, and API/bot lookup reads the persisted store.

---

### JSON Backup and Restore Follow-Up (Historical - Superseded)

**Goal**: Export and restore the queryable frame-data dataset using portable JSON files.

**Independent Test**: Seed PostgreSQL with a dataset, export a backup directory containing `manifest.json` and one character file per character, replace the database with different data, restore the backup, and verify `GET /v1/moves/query` can read the restored move.

#### Tests for JSON Backup and Restore (MANDATORY) ⚠️

- [X] T119 [P] [US2] Add unit tests for ingestion worker backup/restore command parsing and required path validation in `tests/unit/FrameData.Ingestion.Tests/Hosting/IngestionWorkerOptionsTests.cs`
- [X] T120 [P] [US2] Add Testcontainers backup/restore round-trip tests for manifest plus per-character JSON files in `tests/integration/FrameData.Ingestion.IntegrationTests/BackupRestoreTests.cs`

#### Implementation for JSON Backup and Restore

- [X] T121 [US2] Add dataset read and replacement support for backup/restore in `src/FrameData.Infrastructure/Persistence/Repositories/FrameDataDatasetRepository.cs`
- [X] T122 [US2] Implement JSON backup manifest and per-character file export/import service in `src/FrameData.Ingestion/Backup/FrameDataBackupService.cs`
- [X] T123 [US2] Add `backup` and `restore` worker command modes in `src/FrameData.Ingestion/Hosting/` and `src/FrameData.Ingestion/Program.cs`
- [X] T124 [US2] Wire backup service into ingestion DI in `src/FrameData.Ingestion/Hosting/IngestionWorkerServiceCollectionExtensions.cs`
- [X] T125 [US2] Update compose/env/quickstart documentation for backup and restore commands in `docker-compose.yml`, `docker-compose.prod.yml`, `.env.example`, `.env.prod.example`, and `specs/001-build-3s-frame-bot/quickstart.md`

**Checkpoint**: Superseded by the static dataset storage refactor. Operators no longer need separate JSON backup/restore commands once the active dataset directory is the portable JSON/media artifact.

---

### Seq Centralized Logging Follow-Up

**Goal**: Run Seq as the central structured log store for local and production deployments and emit richer diagnostics from the Bot, API, and Ingestion services.

**Independent Test**: Start the Compose stack, browse Seq on the configured host port, run a Bot/API query and a one-shot ingestion, then verify Seq contains searchable events with `ServiceName`, request/interaction details, ingestion run IDs, character IDs, section counts, and per-move Debug entries.

#### Implementation for Seq Centralized Logging

- [X] T126 [P] [US2] Add shared Serilog/Seq logging bootstrap and Seq package dependency in `src/FrameData.Shared/Logging/FrameDataLogging.cs` and `src/FrameData.Shared/FrameData.Shared.csproj`
- [X] T127 [US2] Wire API startup to shared logging, Seq, and HTTP request logging in `src/FrameData.Api/Program.cs` and `src/FrameData.Api/appsettings.json`
- [X] T128 [US2] Wire Bot startup and Bot API client/Discord interaction diagnostics to shared logging in `src/FrameData.Bot/Program.cs`, `src/FrameData.Bot/Api/MoveQueryApiClient.cs`, and `src/FrameData.Bot/Discord/FramedataInteractionHandler.cs`
- [X] T129 [US2] Wire Ingestion worker startup to shared logging in `src/FrameData.Ingestion/Hosting/IngestionWorkerProgram.cs`
- [X] T130 [US2] Add detailed ingestion progress logs for per-character source fetches, section counts, parsed move data, exports, dataset replacement, and failures in `src/FrameData.Ingestion/Services/IngestionOrchestrator.cs`
- [X] T131 [US2] Add detailed API ingestion and move-query logs in `src/FrameData.Api/Endpoints/MoveQueryEndpoint.cs` and `src/FrameData.Api/Endpoints/IngestionEndpoints.cs`
- [X] T132 [US2] Add Seq service and `SEQ_*` environment wiring for local and production Compose in `docker-compose.yml`, `docker-compose.prod.yml`, `.env.example`, and `.env.prod.example`
- [X] T133 [US2] Document Seq access and log-verification workflow in `specs/001-build-3s-frame-bot/quickstart.md` and `docs/prod-compose-deployment.md`
- [X] T134 [US2] Validate service build and Compose configuration for logging changes with `dotnet build FrameDataBot.sln --no-restore /m:1 /p:UseSharedCompilation=false`, `docker compose config`, and `docker compose -f docker-compose.prod.yml --env-file .env.prod.example config`
- [X] T135 [US2] Run unit tests with `dotnet test tests/unit/FrameData.Domain.Tests/FrameData.Domain.Tests.csproj --no-build`, `dotnet test tests/unit/FrameData.Bot.Tests/FrameData.Bot.Tests.csproj --no-build`, and `dotnet test tests/unit/FrameData.Ingestion.Tests/FrameData.Ingestion.Tests.csproj --no-build` when the environment permits the VSTest local socket

**Checkpoint**: API, Bot, and Ingestion logs are centralized in Seq for local and production deployments, with enough detail to trace requests and ingestion work end to end.

---

### Static Dataset Storage Refactor Follow-Up

**Goal**: Replace the Postgres-backed runtime store with a versioned static JSON/media dataset on disk, remove separate JSON backup/restore modes, and move ingestion into a separate on-demand Compose stack.

**Independent Test**: Build a fixture dataset directory, start the API against it, verify exact/fuzzy move queries resolve from JSON files without PostgreSQL, run ingestion publishing into a staging dataset, and verify atomic active-dataset replacement while the main Compose stack contains only always-running services.

#### Tests for Static Dataset Storage Refactor (MANDATORY) ⚠️

- [X] T136 [P] [US2] Add unit tests for static dataset manifest validation and active-path option validation in `tests/unit/FrameData.Ingestion.Tests/Dataset/StaticDatasetOptionsTests.cs`
- [X] T137 [P] [US2] Add unit tests for static dataset file writing, manifest generation, and atomic publish semantics in `tests/unit/FrameData.Ingestion.Tests/Dataset/StaticDatasetPublisherTests.cs`
- [X] T138 [P] [US2] Add API integration tests proving `GET /v1/moves/query` reads exact and alias/fuzzy matches from fixture JSON files without PostgreSQL in `tests/integration/FrameData.Api.IntegrationTests/MoveQueryStaticDatasetTests.cs`
- [X] T139 [P] [US2] Add ingestion integration tests proving the worker publishes `manifest.json`, `characters/*.json`, and preserves the previous active dataset on failed publish in `tests/integration/FrameData.Ingestion.IntegrationTests/StaticDatasetPublishingTests.cs`
- [X] T140 [P] [US2] Add Compose configuration validation tests or scripted checks for runtime and ingestion Compose separation in `tests/integration/FrameData.Ingestion.IntegrationTests/StaticDatasetComposeConfigTests.cs`

#### Implementation for Static Dataset Storage Refactor

- [X] T141 [US2] Define static dataset manifest and character file contracts in `src/FrameData.Shared/Contracts/StaticDatasetContracts.cs` and `src/FrameData.Domain/Datasets/StaticFrameDataDataset.cs`
- [X] T142 [US2] Implement static dataset reader and in-memory move query repository in `src/FrameData.Infrastructure/Dataset/StaticFrameDataDatasetLoader.cs` and `src/FrameData.Infrastructure/Dataset/StaticMoveQueryRepository.cs`
- [X] T143 [US2] Wire API startup to `FRAMEDATA_ACTIVE_DATASET_PATH`, static dataset loading, and `StaticMoveQueryRepository` in `src/FrameData.Api/Program.cs`
- [X] T144 [US2] Remove API runtime dependency on PostgreSQL, schema bootstrap, Npgsql repositories, and ingestion trigger/status endpoints in `src/FrameData.Api/Program.cs`, `src/FrameData.Api/Endpoints/IngestionEndpoints.cs`, and `src/FrameData.Api/FrameData.Api.csproj`
- [X] T145 [US2] Implement dataset publisher for ingestion output in `src/FrameData.Ingestion/Publishing/StaticDatasetPublisher.cs` and update `src/FrameData.Ingestion/Services/IngestionOrchestrator.cs` to write versioned JSON datasets instead of database rows
- [X] T146 [US2] Replace ingestion worker backup/restore command modes with ingest/publish-only options in `src/FrameData.Ingestion/Hosting/IngestionWorkerCommand.cs`, `src/FrameData.Ingestion/Hosting/IngestionWorkerOptions.cs`, `src/FrameData.Ingestion/Hosting/IngestionWorker.cs`, and `src/FrameData.Ingestion/Program.cs`
- [X] T147 [US2] Remove JSON backup/restore service and Postgres dataset repository from runtime code in `src/FrameData.Ingestion/Backup/FrameDataBackupService.cs` and `src/FrameData.Infrastructure/Persistence/Repositories/FrameDataDatasetRepository.cs`
- [X] T148 [US2] Remove `postgres` and `ingestion` services from `docker-compose.yml` and `docker-compose.prod.yml`, and add one-shot ingestion topology in `docker-compose.ingestion.yml` with the shared dataset root mounted read-write
- [X] T149 [US2] Update dataset environment variables and examples in `.env.example`, `.env.prod.example`, and `.env.ingestion.example` using `FRAMEDATA_DATASET_ROOT` and `FRAMEDATA_ACTIVE_DATASET_PATH`
- [X] T150 [US2] Update static dataset operations documentation in `specs/001-build-3s-frame-bot/quickstart.md` and `docs/prod-compose-deployment.md`, including local Unraid dataset paths, active dataset switching, and validation commands

**Checkpoint**: Runtime Bot/API/Seq stack reads a static active dataset without PostgreSQL; ingestion runs only through the separate ingestion Compose file and publishes portable JSON/media dataset directories.

---

### Source Column Expansion Follow-Up

**Goal**: Preserve Motion, Damage, and Stun source table columns as optional move attributes through ingestion, static dataset publishing/loading, API contracts, and Discord move responses.

**Independent Test**: Parse fixture Specials and Super Arts source tables that include Motion, Damage, and Stun columns, publish/load a static dataset, query the API, and verify the Discord response includes those values when present while existing moves without those columns still work.

#### Tests for Source Column Expansion (MANDATORY) ⚠️

- [X] T151 [P] [US2] Add parser coverage for Motion, Damage, and Stun columns in `tests/unit/FrameData.Ingestion.Tests/Scraping/CharacterSectionParserTests.cs`
- [X] T152 [P] [US2] Add static dataset/API/Discord response coverage for Motion, Damage, and Stun in `tests/unit/FrameData.Ingestion.Tests/Dataset/StaticDatasetPublisherTests.cs`, `tests/integration/FrameData.Api.IntegrationTests/MoveQueryStaticDatasetTests.cs`, `tests/unit/FrameData.Bot.Tests/Formatting/MoveEmbedResponseFactoryTests.cs`, and `tests/contract/FrameData.Contracts.Tests/MoveQueryContractTests.cs`

#### Implementation for Source Column Expansion

- [X] T153 [P] [US2] Add optional Motion, Damage, and Stun properties to move contracts/models in `src/FrameData.Domain/Moves/Move.cs`, `src/FrameData.Shared/Contracts/StaticDatasetContracts.cs`, and `src/FrameData.Shared/Contracts/MoveQueryContracts.cs`
- [X] T154 [US2] Parse and map Motion, Damage, and Stun source columns in `src/FrameData.Scraper/Parsing/CharacterSectionParser.cs` and `src/FrameData.Ingestion/Services/IngestionOrchestrator.cs`
- [X] T155 [US2] Persist and load Motion, Damage, and Stun through the static dataset and API response in `src/FrameData.Ingestion/Publishing/StaticDatasetPublisher.cs`, `src/FrameData.Infrastructure/Dataset/StaticFrameDataDatasetLoader.cs`, and `src/FrameData.Api/Endpoints/MoveQueryEndpoint.cs`
- [X] T156 [US2] Display optional Motion plus Damage and Stun values in Discord embed formatting in `src/FrameData.Bot/Formatting/MoveEmbedResponseFactory.cs`

**Checkpoint**: Source Motion, Damage, and Stun columns survive ingestion and are visible in lookup responses when present.

---

## Phase 5: User Story 3 - Notation and Alias Resolution (Priority: P2)

**Goal**: Support shorthand/numpad/colloquial input with fuzzy matching and safe disambiguation.

**Independent Test**: Inputs such as `cr.HK`, `2hk`, and `sweep` resolve correctly or return disambiguation candidates when ambiguous.

### Tests for User Story 3 (MANDATORY) ⚠️

- [X] T037 [P] [US3] Add unit tests for alias normalisation rules in `tests/unit/FrameData.Domain.Tests/MoveLookup/AliasNormaliserTests.cs`
- [X] T038 [P] [US3] Add unit tests for fuzzy ranking and threshold behavior in `tests/unit/FrameData.Domain.Tests/MoveLookup/FuzzyMatcherTests.cs`
- [X] T039 [P] [US3] Add API integration tests for ambiguous and no-match responses in `tests/integration/FrameData.Api.IntegrationTests/MoveQueryFuzzyTests.cs`
- [X] T040 [P] [US3] Add contract tests for ambiguous response payload in `tests/contract/FrameData.Contracts.Tests/MoveQueryAmbiguousContractTests.cs`

### Implementation for User Story 3

- [X] T041 [P] [US3] Implement MoveAlias and MatchCandidate domain models in `src/FrameData.Domain/MoveLookup/MoveAlias.cs` and `src/FrameData.Domain/MoveLookup/MatchCandidate.cs`
- [X] T042 [P] [US3] Implement alias normalisation service in `src/FrameData.Domain/MoveLookup/AliasNormaliser.cs`
- [X] T043 [US3] Implement fuzzy matcher service using FuzzySharp in `src/FrameData.Domain/MoveLookup/FuzzyMoveMatcher.cs`
- [X] T044 [US3] Implement disambiguation response builder in `src/FrameData.Api/Responses/MoveDisambiguationResponseFactory.cs`
- [X] T045 [US3] Update Discord response flow for candidate selection prompts in `src/FrameData.Bot/Formatting/MoveEmbedResponseFactory.cs`

**Checkpoint**: US3 adds robust user-friendly lookup behavior without breaking US1.

---

## Phase 6: User Story 4 - Representative Active-Frame Hitbox Image (Priority: P2)

**Goal**: Capture and serve representative active-frame hitbox image references when derivable.

**Independent Test**: For scoped pilot moves with supported hitbox display pages, representative active-frame PNG files and media metadata are stored inside the static dataset `media/` tree and returned with query responses as relative media references. The default selector chooses the earliest frame with the largest summed active hitbox rectangle area, rendered images include `P1_P`, `P1_V`, `P1_A`, `P1_T`, and `P1_TA`, P2 hitboxes are excluded, and unavailable frame images store a dummy fallback image plus fallback metadata.

### Tests for User Story 4 (MANDATORY) ⚠️

- [X] T046 [P] [US4] Add unit tests for hitbox display parsing, active rectangle area scoring, earliest-frame tie breaking, object active hitbox inclusion, and P2 exclusion in `tests/unit/FrameData.Ingestion.Tests/Scraping/HitboxFrameParserTests.cs` and `tests/unit/FrameData.Domain.Tests/Media/RepresentativeFrameSelectorTests.cs`
- [X] T047 [P] [US4] Add unit tests for static dataset move image pathing, representative media metadata, scoped pilot policy, per-move overrides, and dummy fallback persistence in `tests/unit/FrameData.Ingestion.Tests/Media/MoveImageDatasetStorageTests.cs` and `tests/unit/FrameData.Ingestion.Tests/Media/RepresentativeFrameSelectionPolicyTests.cs`
- [X] T048 [P] [US4] Add integration tests for scoped Ken pilot image capture, configured P1-only overlay rendering, dummy fallback behavior, static dataset media publish, and API media retrieval in `tests/integration/FrameData.Ingestion.IntegrationTests/MoveImageStaticDatasetFlowTests.cs`
- [X] T049 [P] [US4] Add contract tests for representative media fields in query responses, including `representativeFrameImageUrl`, `selectedFrame`, `selectionStrategy`, `captureStatus`, and `fallbackReason` in `tests/contract/FrameData.Contracts.Tests/MoveMediaContractTests.cs`

### Implementation for User Story 4

- [X] T050 [P] [US4] Implement MoveImage and representative-frame selection policy domain models in `src/FrameData.Domain/Media/MoveImage.cs` and `src/FrameData.Domain/Media/RepresentativeFrameSelectionPolicy.cs`
- [X] T051 [P] [US4] Implement hitbox display parsing and largest-active-hitbox-area representative frame selection in `src/FrameData.Scraper/Parsing/HitboxDisplayParser.cs` and `src/FrameData.Domain/Media/RepresentativeFrameSelector.cs`
- [X] T052 [US4] Implement canvas/image capture, P1-only hitbox overlay rendering, scoped pilot move filtering, per-move override handling, generated/configured dummy fallback images, and media metadata writing in `src/FrameData.Ingestion/Media/HitboxCanvasRenderer.cs`, `src/FrameData.Ingestion/Media/MoveImageDatasetStorageService.cs`, `src/FrameData.Ingestion/Hosting/IngestionWorkerOptions.cs`, and `src/FrameData.Shared/Contracts/MoveMediaContracts.cs`
- [X] T053 [US4] Integrate representative media references into static dataset loading, move query responses, ingestion configuration examples, and Discord media attachment flow in `src/FrameData.Infrastructure/Dataset/StaticFrameDataDatasetLoader.cs`, `src/FrameData.Api/Endpoints/MoveQueryEndpoint.cs`, `src/FrameData.Bot/Formatting/MoveEmbedResponseFactory.cs`, `.env.ingestion.example`, and `docker-compose.ingestion.yml`

**Checkpoint**: US4 adds optional visual data while preserving text-only functionality.

---

## Dependencies & Execution Order

### Phase Dependencies

- Setup (Phase 1): starts immediately.
- Foundational (Phase 2): depends on Setup and blocks all user stories.
- User Stories:
  - US1 (Phase 3) and US2 (Phase 4) start after Foundational.
  - US1 deployment parity follow-up (`T075-T082`) was completed before live Discord gateway work.
  - US1 Discord gateway follow-up (`T083-T095`) and embed response follow-up (`T096-T100`) complete before US3 response disambiguation work, because US3 changes the live command response path.
  - US2 real persistence follow-up (`T101-T118`) was completed as the first production persistence slice, but is superseded by the static dataset storage refactor.
  - Static dataset storage refactor (`T136-T150`) plus source column expansion (`T151-T156`) must complete before US4 or final MVP validation, because move data and media should share a portable JSON/media dataset instead of PostgreSQL and must retain all currently required source columns.
  - US3 (Phase 5) depends on US1 baseline lookup behavior and a query repository implementation; its completed matcher work must be preserved when the static repository replaces the Postgres repository.
  - US4 (Phase 6) depends on static dataset storage so representative image metadata and files can be published beside move JSON. Full media ingestion remains gated behind successful scoped Ken pilot validation.
- Current plan closure: all retained story tasks are complete.

### User Story Completion Order

1. US1 + runtime parity follow-up + Discord gateway follow-up + embed response follow-up + US2 scaffold
2. Historical US2 Postgres persistence/worker follow-up (`T101-T118`) and backup/restore follow-up (`T119-T125`)
3. US3 (fuzzy/alias usability)
4. Static dataset storage refactor (`T136-T150`)
5. US4 (representative active-frame image in the static media dataset)

### Within Each User Story

- Write failing tests first.
- Implement models and repositories before service orchestration.
- Implement endpoint/command handlers after service logic.
- Verify integration and contract tests before closing the story.

---

## Parallel Execution Examples

### User Story 1

```bash
# Run in parallel:
T016, T017, T018, T019
T020, T021
T075, T076
T083, T084, T085, T087
T088, T089
T096, T097, T098, T099
```

### User Story 2

```bash
# Run in parallel:
T027, T028, T029, T030
T031, T032
T101, T102, T103, T104, T105, T106, T107
T110, T111, T112, T113
T136, T137, T138, T139, T140
T151, T152, T153
```

### User Story 3

```bash
# Run in parallel:
T037, T038, T039, T040
T041, T042
```

### User Story 4

```bash
# Run in parallel:
T046, T047, T048, T049
T050, T051
```

## Implementation Strategy

### MVP First (US1 + US2)

1. Complete Setup and Foundational phases.
2. Deliver US1 exact lookup path and Bot runtime/container parity follow-up.
3. Deliver US1 Discord gateway/slash-command follow-up so `/framedata` works in a real Discord channel.
4. Deliver US1 Discord embed response follow-up so `/framedata` returns structured frame-data embeds without duplicate message text.
5. Deliver the static dataset storage refactor and source column expansion so the worker writes versioned JSON/media datasets and the API reads all required move attributes from the active dataset on disk.
6. Validate API and bot queries against the mounted static dataset.
7. Validate and demo MVP.

### Incremental Delivery

1. Preserve US3 fuzzy/alias support while swapping the query repository to static dataset loading.
2. Complete the static dataset storage refactor (`T136-T150`) and source column expansion (`T151-T156`).
3. Complete US4 representative active-frame image support inside the static media dataset, starting with the scoped Ken pilot media run.

### Quality Gates

1. Every story requires passing unit + integration + contract tests.
2. No story closes without independent test criteria passing.
3. Preserve backward compatibility for previously delivered story behavior.
4. Formal sample-based performance benchmark evidence and security-gate automation are
   deferred to future plans by maintainer-approved closeout exception.
