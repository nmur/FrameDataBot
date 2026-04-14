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
2. Deliver US1 (`T016-T026`) including Bot runtime/container parity follow-up (`T075-T082`).
3. Deliver US2 (`T027-T036`) as MVP ingestion backbone.
4. Deliver refinements in order: US3 -> US4 -> US5 -> US6.
5. Complete polish tasks (`T065-T070`) after desired story set is done.
6. At each step, consult the reference list above for requirements and contracts.

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

- [X] T008 Create PostgreSQL schema bootstrap and migration baseline in `src/FrameData.Infrastructure/Persistence/Migrations/0001_Initial.sql`
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

**Goal**: Provide exact canonical move-name lookup via `/framedata` command and API response.

**Independent Test**: Query a supported character + exact canonical move and receive correct frame data; unknown character/move inputs return clear not-found or unsupported responses.

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
- [X] T025 [US1] Implement Discord `/framedata` exact-query handler in `src/FrameData.Bot/Commands/MoveCommandHandler.cs`
- [X] T026 [US1] Implement not-found/unsupported-character response mapper in `src/FrameData.Bot/Formatting/MoveResponseFormatter.cs`

### Scope Simplification Follow-Up (Single-Game Interface)

- [X] T071 [P] [US1] Remove `game` input from Discord command parser/handler flow and update affected unit tests in `src/FrameData.Bot/Commands/MoveCommandParser.cs`, `src/FrameData.Bot/Commands/MoveCommandHandler.cs`, and `tests/unit/FrameData.Bot.Tests/Commands/MoveCommandParserTests.cs`
- [X] T072 [P] [US1] Remove `game` query parameter and unsupported-game path from move query endpoint and API integration tests in `src/FrameData.Api/Endpoints/MoveQueryEndpoint.cs` and `tests/integration/FrameData.Api.IntegrationTests/MoveQueryExactTests.cs`
- [X] T073 [US1] Remove game-discriminator handling from exact lookup service/repository interface and related domain tests in `src/FrameData.Domain/MoveLookup/ExactMoveLookupService.cs`, `src/FrameData.Domain/MoveLookup/IMoveQueryRepository.cs`, `src/FrameData.Infrastructure/Persistence/Repositories/MoveRepository.cs`, and `tests/unit/FrameData.Domain.Tests/MoveLookup/ExactMoveLookupServiceTests.cs`
- [X] T074 [US1] Update contract tests and response formatting for single-game behavior in `tests/contract/FrameData.Contracts.Tests/MoveQueryContractTests.cs` and `src/FrameData.Bot/Formatting/MoveResponseFormatter.cs`

### Runtime Containerization Follow-Up (Bot Service Parity)

- [X] T075 [P] [US1] Add unit tests for bot host bootstrap and required configuration validation in `tests/unit/FrameData.Bot.Tests/Hosting/BotHostBootstrapTests.cs`
- [X] T076 [P] [US1] Add integration test coverage for bot-to-API client wiring assumptions in `tests/integration/FrameData.Api.IntegrationTests/BotService/BotApiConnectivityConfigTests.cs`
- [X] T077 [US1] Implement production bot runtime bootstrap in `src/FrameData.Bot/Program.cs`
- [X] T078 [P] [US1] Add bot container build definition in `Dockerfile.bot`
- [X] T079 [US1] Add Bot service wiring and environment mapping in `docker-compose.yml` and `.env.example`
- [X] T080 [US1] Extend image publishing to include Bot image in `.github/workflows/release.yml`
- [X] T081 [US1] Remove provider-specific bot deployment coupling and keep host-agnostic deployment configuration in `.github/workflows/release.yml` and `specs/001-build-3s-frame-bot/quickstart.md`
- [X] T082 [US1] Update deployment verification documentation for four-service topology in `specs/001-build-3s-frame-bot/quickstart.md`

**Checkpoint**: US1 fully testable and deployable as MVP.

---

## Phase 4: User Story 2 - Source Ingestion and Persistence (Priority: P1)

**Goal**: Ingest source sections (Normals/Specials/Super Arts/Misc), persist to PostgreSQL, and export one JSON file per character.

**Independent Test**: Run ingestion and verify database updates + one JSON export per character; partial failures preserve successful updates and report retry-required scopes.

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

**Checkpoint**: US1+US2 operational with refreshable data pipeline.

---

## Phase 5: User Story 3 - Notation and Alias Resolution (Priority: P2)

**Goal**: Support shorthand/numpad/colloquial input with fuzzy matching and safe disambiguation.

**Independent Test**: Inputs such as `cr.HK`, `2hk`, and `sweep` resolve correctly or return disambiguation candidates when ambiguous.

### Tests for User Story 3 (MANDATORY) ⚠️

- [ ] T037 [P] [US3] Add unit tests for alias normalization rules in `tests/unit/FrameData.Domain.Tests/MoveLookup/AliasNormalizerTests.cs`
- [ ] T038 [P] [US3] Add unit tests for fuzzy ranking and threshold behavior in `tests/unit/FrameData.Domain.Tests/MoveLookup/FuzzyMatcherTests.cs`
- [ ] T039 [P] [US3] Add API integration tests for ambiguous and no-match responses in `tests/integration/FrameData.Api.IntegrationTests/MoveQueryFuzzyTests.cs`
- [ ] T040 [P] [US3] Add contract tests for ambiguous response payload in `tests/contract/FrameData.Contracts.Tests/MoveQueryAmbiguousContractTests.cs`

### Implementation for User Story 3

- [ ] T041 [P] [US3] Implement MoveAlias and MatchCandidate domain models in `src/FrameData.Domain/MoveLookup/MoveAlias.cs` and `src/FrameData.Domain/MoveLookup/MatchCandidate.cs`
- [ ] T042 [P] [US3] Implement alias normalization service in `src/FrameData.Domain/MoveLookup/AliasNormalizer.cs`
- [ ] T043 [US3] Implement fuzzy matcher service using FuzzySharp in `src/FrameData.Domain/MoveLookup/FuzzyMoveMatcher.cs`
- [ ] T044 [US3] Implement disambiguation response builder in `src/FrameData.Api/Responses/MoveDisambiguationResponseFactory.cs`
- [ ] T045 [US3] Update Discord response flow for candidate selection prompts in `src/FrameData.Bot/Formatting/MoveResponseFormatter.cs`

**Checkpoint**: US3 adds robust user-friendly lookup behavior without breaking US1.

---

## Phase 6: User Story 4 - Last Active-Frame Hitbox Image (Priority: P2)

**Goal**: Capture and serve last active-frame hitbox image references when derivable.

**Independent Test**: For moves with supported hitbox display pages, image references are stored and returned with query responses.

### Tests for User Story 4 (MANDATORY) ⚠️

- [ ] T046 [P] [US4] Add unit tests for hitbox display parsing and frame detection in `tests/unit/FrameData.Ingestion.Tests/Scraping/HitboxFrameParserTests.cs`
- [ ] T047 [P] [US4] Add unit tests for move image persistence logic in `tests/unit/FrameData.Ingestion.Tests/Media/MoveImagePersistenceTests.cs`
- [ ] T048 [P] [US4] Add integration tests for image capture and retrieval in `tests/integration/FrameData.Ingestion.IntegrationTests/MoveImageFlowTests.cs`
- [ ] T049 [P] [US4] Add contract tests for media field in query response in `tests/contract/FrameData.Contracts.Tests/MoveMediaContractTests.cs`

### Implementation for User Story 4

- [ ] T050 [P] [US4] Implement MoveImage domain model in `src/FrameData.Domain/Media/MoveImage.cs`
- [ ] T051 [P] [US4] Implement hitbox display scraper for last active frame in `src/FrameData.Scraper/Parsing/HitboxDisplayParser.cs`
- [ ] T052 [US4] Implement image storage and metadata repository in `src/FrameData.Infrastructure/Storage/MoveImageStorageService.cs` and `src/FrameData.Infrastructure/Persistence/Repositories/MoveImageRepository.cs`
- [ ] T053 [US4] Integrate media enrichment into move query endpoint in `src/FrameData.Api/Endpoints/MoveQueryEndpoint.cs`

**Checkpoint**: US4 adds optional visual data while preserving text-only functionality.

---

## Phase 7: User Story 5 - Full Frame-Image Expansion Decision (Priority: P3)

**Goal**: Produce a storage-impact assessment before any full per-frame image archival.

**Independent Test**: Generate report with per-move/per-character/full-roster storage estimates and a recommended retention policy.

### Tests for User Story 5 (MANDATORY) ⚠️

- [ ] T054 [P] [US5] Add unit tests for storage estimation calculations in `tests/unit/FrameData.Domain.Tests/Media/StorageAssessmentServiceTests.cs`
- [ ] T055 [P] [US5] Add integration test for storage assessment report generation in `tests/integration/FrameData.Ingestion.IntegrationTests/StorageAssessmentReportTests.cs`

### Implementation for User Story 5

- [ ] T056 [P] [US5] Implement StorageAssessment domain model in `src/FrameData.Domain/Media/StorageAssessment.cs`
- [ ] T057 [US5] Implement storage assessment service in `src/FrameData.Ingestion/Services/StorageAssessmentService.cs`
- [ ] T058 [US5] Implement assessment report exporter in `src/FrameData.Ingestion/Reporting/StorageAssessmentReportWriter.cs`

**Checkpoint**: US5 enables evidence-based decision on full-frame archival.

---

## Phase 8: User Story 6 - Expanded Move Details and Media (Priority: P3)

**Goal**: Support advanced move metadata in responses without regressing MVP fields.

**Independent Test**: Enriched moves return advanced properties; non-enriched moves still return valid baseline frame data.

### Tests for User Story 6 (MANDATORY) ⚠️

- [ ] T059 [P] [US6] Add unit tests for metadata mapping and optionality in `tests/unit/FrameData.Domain.Tests/Moves/MoveMetadataMappingTests.cs`
- [ ] T060 [P] [US6] Add integration tests for enriched query responses in `tests/integration/FrameData.Api.IntegrationTests/MoveMetadataResponseTests.cs`
- [ ] T061 [P] [US6] Add contract tests for advanced metadata schema in `tests/contract/FrameData.Contracts.Tests/MoveMetadataContractTests.cs`

### Implementation for User Story 6

- [ ] T062 [P] [US6] Implement MoveMetadata domain model in `src/FrameData.Domain/Moves/MoveMetadata.cs`
- [ ] T063 [US6] Implement metadata ingestion mapper in `src/FrameData.Ingestion/Mapping/MoveMetadataMapper.cs`
- [ ] T064 [US6] Integrate metadata serialization in API and bot formatters in `src/FrameData.Api/Responses/MoveQueryResponseFactory.cs` and `src/FrameData.Bot/Formatting/MoveResponseFormatter.cs`

**Checkpoint**: US6 delivers advanced detail extension while preserving backward compatibility.

---

## Final Phase: Polish & Cross-Cutting Concerns

**Purpose**: Hardening, documentation, and release readiness across all stories.

- [ ] T065 [P] Add structured logging and correlation IDs in `src/FrameData.Api/Observability/RequestCorrelationMiddleware.cs` and `src/FrameData.Ingestion/Observability/IngestionLogScope.cs`
- [ ] T066 [P] Add fixed-sample performance validation script for API and bot latency in `scripts/perf/run-benchmarks.sh`
- [ ] T067 Add mandatory pre-production security gate workflow (dependency, image, secrets, least-privilege checks) in `.github/workflows/security-gate.yml` and `scripts/security/`
- [ ] T068 Add deployment workflow for self-hosted Docker via GitHub Actions in `.github/workflows/deploy-selfhosted.yml`
- [ ] T069 [P] Update quickstart and operations runbook with security/performance gate execution in `specs/001-build-3s-frame-bot/quickstart.md` and `docs/operations.md`
- [ ] T070 Execute full test suite and document validation outcomes in `specs/001-build-3s-frame-bot/implementation-validation.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- Setup (Phase 1): starts immediately.
- Foundational (Phase 2): depends on Setup and blocks all user stories.
- User Stories:
  - US1 (Phase 3) and US2 (Phase 4) start after Foundational.
  - US1 deployment parity follow-up (`T075-T082`) completes before cross-story polish tasks.
  - US3 (Phase 5) depends on US1 baseline lookup behavior.
  - US4 (Phase 6) depends on US2 ingestion pipeline.
  - US5 (Phase 7) depends on US4 image-capture data.
  - US6 (Phase 8) depends on US2 ingestion and US1 response pipeline.
- Final Phase: depends on all desired stories being complete.

### User Story Completion Order

1. US1 + runtime parity follow-up + US2 (MVP data query + bot runtime + ingestion backbone)
2. US3 (fuzzy/alias usability)
3. US4 (last active-frame image)
4. US5 (storage impact decision)
5. US6 (advanced metadata)

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
```

### User Story 2

```bash
# Run in parallel:
T027, T028, T029, T030
T031, T032
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

### User Story 5

```bash
# Run in parallel:
T054, T055
T056
```

### User Story 6

```bash
# Run in parallel:
T059, T060, T061
T062
```

---

## Implementation Strategy

### MVP First (US1 + US2)

1. Complete Setup and Foundational phases.
2. Deliver US1 exact lookup path and Bot runtime/container parity follow-up.
3. Deliver US2 ingestion + persistence + JSON export.
4. Validate and demo MVP.

### Incremental Delivery

1. Add US3 fuzzy/alias support.
2. Add US4 last active-frame image support.
3. Add US5 storage assessment before any full-frame archival.
4. Add US6 advanced metadata support.

### Quality Gates

1. Every story requires passing unit + integration + contract tests.
2. No story closes without independent test criteria passing.
3. Preserve backward compatibility for previously delivered story behavior.
4. Production deployment requires passing mandatory security gate checks.
5. SC-001 validation must use fixed-size representative samples for both API and bot latency.
