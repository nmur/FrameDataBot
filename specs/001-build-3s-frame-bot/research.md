# Phase 0 Research: Discord 3s Frame Data Bot

## Decision 1: Service Language Baseline

- Decision: Use .NET 10 for all long-running services (bot, API, ingestion) and shared
  libraries.
- Rationale: Matches explicit project preference, keeps operational/tooling consistency,
  and simplifies deployment/monitoring in Docker on a single cloud host.
- Alternatives considered:
  - Mixed .NET + Node services: rejected due to unnecessary runtime sprawl.
  - Python-first backend: rejected because preference is .NET unless strongly justified.

## Decision 2: Web Scraper Approach

- Decision: Use .NET `HttpClient` + `AngleSharp` for scraper and parser implementation.
  This feature iteration is explicitly .NET-only with no Python fallback scope.
- Rationale: Preserves single-runtime operational simplicity, aligns with approved scope,
  and keeps implementation consistent with the current plan/tasks.
- Alternatives considered:
  - .NET `Microsoft.Playwright`: rejected due to heavier runtime/dependencies and slower
    execution for expected static/semi-structured source pages.
  - Python sidecar scraper: rejected because multi-runtime support is out of scope for
    this approved feature iteration.

## Decision 3: Database Choice

Recommended database options:
1. PostgreSQL with JSONB (Recommended)
2. SQLite with JSON columns
3. LiteDB (stable v5 line)

- Decision: Use PostgreSQL for v1, with JSONB columns for nested move/frame structures.
- Rationale: Suitable for current requirements, production-proven, strong .NET support,
  and aligned with goal of gaining practical PostgreSQL experience.
- Alternatives considered:
  - SQLite JSON: lightweight, but weaker concurrency and migration ergonomics for future
    service growth.
  - LiteDB: still viable for small deployments, but not selected due to preference for
    PostgreSQL experience and stronger managed-host ecosystem support.

## Decision 4: Testing Stack and Licensing Constraints

- Decision: Use xUnit + NSubstitute + Shouldly for unit tests; avoid Moq and
  FluentAssertions.
- Rationale: Aligns with stated licensing concerns while preserving readable assertions
  and practical mocking.
- Alternatives considered:
  - NUnit: acceptable but no strong advantage over xUnit for this project.
  - FakeItEasy: acceptable substitute framework but not preferred baseline.

## Decision 5: Integration Testing Infrastructure

- Decision: Use Testcontainers for .NET for integration tests.
- Rationale: Reliable, reproducible dependency orchestration aligned with both user
  preference and constitution testing rigor.
- Alternatives considered:
  - In-memory fakes only: rejected for insufficient boundary realism.
  - Shared external test instance: rejected for non-deterministic test environment risk.

## Decision 6: Fuzzy Matching Library Strategy

- Decision: Start with `FuzzySharp` for fuzzy scoring; keep a pluggable matcher
  abstraction to allow replacement if precision/performance is insufficient.
- Rationale: Mature .NET option for token and ratio-based matching, suitable for inputs
  like `cr.HK`, `2hk`, and colloquial aliases such as `sweep`.
- Alternatives considered:
  - SimMetrics.Net: robust but less commonly adopted in current .NET examples.
  - Custom Levenshtein-only logic: too limited for notation/alias diversity.

## Decision 7: Deployment Host Strategy and Security Baseline

- Decision: Keep deployment host-agnostic and rely on portable container artifacts
  (Dockerfiles + compose + GHCR image publishing) so the stack can run on self-hosted
  Docker (NAS) or managed container platforms without platform-specific coupling.
- Rationale: Minimizes lock-in and keeps deployment paths flexible while preserving a
  single build/release pipeline.
- Alternatives considered:
  - Provider-specific deployment integration in the release workflow: rejected to avoid
    coupling CI/CD logic to one host.
  - Source-only/manual runtime startup: rejected due to weaker reproducibility.

Security baseline controls:
- Run containers as non-root user where possible.
- Use read-only root filesystem where practical; mount writable data paths explicitly.
- Store tokens/secrets in environment files or mounted secret files outside git.
- Restrict exposed ports to only required bot/API endpoints.
- Pin image versions and perform periodic dependency updates.

## Decision 8: Performance Validation Strategy

- Decision: Validate SC-001 with fixed-size samples on a representative dataset, and
  measure both API query latency and bot end-to-end latency each run.
- Rationale: Makes performance outcomes reproducible and auditable across releases.
- Alternatives considered:
  - No explicit performance checks in early phases: rejected by constitution.

## Decision 9: Security Release Gate

- Decision: Require a mandatory pre-production security checklist gate including
  dependency scan, container image scan, secrets scan, and manual least-privilege
  review. All categories must have zero critical findings before production use.
- Rationale: Converts security intent into objective release criteria aligned with
  approved specification success criteria.
- Alternatives considered:
  - Informal qualitative review only: rejected due to ambiguous acceptance standards.
  - External penetration testing as mandatory gate: deferred for later scale.

## Decision 10: Command Namespace and Single-Game Scope

- Decision: Use `/framedata` as the canonical Discord command with required
  `character` and `move` parameters only.
- Rationale: Keeps the implementation lean for the current 3s-only scope and removes
  unnecessary request-path complexity from interfaces and backend logic.
- Alternatives considered:
  - Keep `/3s move`: rejected because it hardcodes one game into command naming.
  - Keep optional `game`: rejected because it introduces unused branching and
    unsupported-game handling overhead while only one dataset is active.

## Decision 11: `/v1/moves/query` Method (`GET` vs `POST`)

### Option A: `GET /v1/moves/query?character=...&moveInput=...`

Pros:
- Aligns with HTTP semantics for read-only operations (safe/idempotent intent).
- Improves operational clarity in logs/metrics and standard API tooling.
- Enables straightforward caching behavior (client/proxy/CDN where applicable).
- Easier ad-hoc/manual invocation from browser/curl without JSON body.
- Better contract discoverability for simple query inputs.

Cons:
- Query-string payloads become awkward as request shape grows (extra knobs/options).
- Less ergonomic for nested/complex query criteria in future expansions.
- URL length and encoding constraints can become a concern for richer inputs.
- Can expose query values in URL surfaces more broadly (history/proxy logs), which may be undesirable for sensitive inputs.

### Option B: `POST /v1/moves/query` with JSON body

Pros:
- Flexible request envelope for future expansion without query-string churn.
- Cleaner transport for optional/structured fields and future advanced options.
- Avoids URL-length/encoding constraints.
- Keeps transport shape consistent with other command-like API operations.

Cons:
- Weaker semantic fit for a read-only query endpoint.
- Less cache-friendly by default and less intuitive for “query” behavior.
- Harder to inspect quickly in basic tooling compared with query params.
- Can obscure idempotent read intent unless explicitly documented.

### Recommendation

- Use `GET` as the canonical method for the current exact lookup endpoint because the current request shape is simple (`character`, `moveInput`) and behavior is read-only.
- Keep `POST` only if/when advanced query payloads are needed (for example, richer matching options or future structured query criteria), potentially as a separate advanced endpoint to keep semantics clear.

Rationale:
- This balances correctness of HTTP semantics and observability simplicity today while preserving a clean path for future complexity.

## Decision 12: Bot Runtime Containerization Strategy

- Decision: Treat `FrameData.Bot` as a first-class deployable runtime and containerize it
  explicitly with its own Dockerfile, image tags, and compose service definition.
- Rationale: The feature already defines Bot as a runtime service and measures bot
  end-to-end behavior; without a bot container artifact, deployment parity and
  environment reproducibility remain incomplete.
- Alternatives considered:
  - Keep Bot as source-only/non-containerized process: rejected due to inconsistent
    deployment paths and weaker local/cloud parity.
  - Merge Bot into API process: rejected because it blurs service boundaries and creates
    unnecessary coupling between Discord gateway handling and HTTP API concerns.

## Decision 13: Deployment Artifact Parity Across Services

- Decision: Publish Bot, API, and Ingestion images together in release flow and keep
  docker-compose topology aligned with those same runtime boundaries.
- Rationale: Unified release metadata and image versioning simplifies rollback,
  cross-service compatibility tracking, and NAS/cloud deployment automation.
- Alternatives considered:
  - Publish only API/Ingestion images: rejected because it leaves Bot deployment outside
    the same controlled CI/CD release path.

## Decision 14: Discord Gateway Runtime Approach

- Decision: Use Discord.Net WebSocket gateway handling in `FrameData.Bot`, with
  `DiscordSocketClient` for connection lifecycle and interaction events.
- Rationale: The project already depends on Discord.Net and the bot must receive live
  slash-command interactions from Discord channels. Keeping gateway handling in
  `FrameData.Bot` preserves the current Bot/API service boundary and avoids routing
  Discord events through the API host.
- Alternatives considered:
  - Discord outgoing interaction webhooks hosted by the API: rejected for this slice
    because it would shift Discord ingress to the API service, add public endpoint
    exposure, and bypass the existing Bot service runtime.
  - Polling or command simulation: rejected because it does not satisfy actual Discord
    channel invocation.

## Decision 15: Slash Command Registration Scope

- Decision: Register `/framedata` as a guild-scoped slash command on bot startup using
  the configured `BOT_GUILD_ID`.
- Rationale: Guild command registration propagates quickly, is easier to validate during
  local/self-hosted development, and matches the current single-guild/home-server
  deployment assumptions.
- Alternatives considered:
  - Global slash command registration: rejected for the first gateway slice because
    propagation is slower and rollout mistakes are harder to correct quickly.
  - Manual command registration outside the bot runtime: rejected because it leaves the
    deployed service and command contract vulnerable to drift.

## Decision 16: Interaction Response Strategy

- Decision: Use Discord embeds as the default public move lookup response, without
  duplicate plain-text message content for formatted move, ambiguous, or query-error
  results. Content-only text remains acceptable for validation or operational failures
  where no embed result exists.
- Rationale: The gateway, API, and query pipeline are already in place, so the next
  response slice can improve readability directly. Keeping embed construction in the
  Bot service avoids coupling the API to Discord-specific presentation while allowing
  later media attachments to reuse the same response path.
- Alternatives considered:
  - Continue primitive text as the default: rejected because it delays the requested
    rich Discord experience even though the live interaction path now exists.
  - Ephemeral responses by default: rejected for the first pass because the requested UX
    is invocation in a Discord channel and shared channel answers are useful for frame
    data lookup.

## Decision 17: Automated Testing Boundary for Discord

- Decision: Test Discord command definition, interaction mapping, handler orchestration,
  and host wiring with deterministic adapters/fakes; keep live Discord validation as a
  manual quickstart smoke test.
- Rationale: CI cannot safely or reliably depend on live Discord gateway state, tokens,
  guild permissions, or command propagation timing. Adapter-based tests still verify the
  owned logic and service wiring while preserving reproducibility.
- Alternatives considered:
  - Live Discord integration tests in CI: rejected because they require secrets and
    external SaaS state, making tests flaky and unsafe.
  - Unit tests only: rejected because host-level wiring and lifecycle registration still
    need integration coverage.

## Decision 18: Postgres-Backed Repositories Are the Runtime Default

- Decision: Replace in-memory `CharacterRepository`, `MoveRepository`, and
  `IngestionRunRepository` runtime behavior with Npgsql/PostgreSQL implementations.
  Tests may still use fakes where useful, but production API and ingestion hosts must
  resolve the Postgres-backed services.
- Rationale: FR-008 requires a persistent queryable store used by the bot service. The
  current in-memory repository scaffold proves service contracts but does not survive
  process restarts, does not share data between API and ingestion containers, and cannot
  satisfy production ingestion.
- Alternatives considered:
  - Keep in-memory repositories with JSON exports as the source of truth: rejected
    because API/bot reads would not use PostgreSQL.
  - Maintain separate API and ingestion persistence implementations: rejected because it
    creates drift between write and read behavior.

## Decision 19: Shared Schema Bootstrap

- Decision: Add a small idempotent schema bootstrap that applies the current SQL schema
  from `src/FrameData.Infrastructure/Persistence/Migrations/0001_Initial.sql` before
  API or ingestion work touches repositories. Do not track migration history for the
  v1 dataset.
- Rationale: Both API and ingestion containers need the same schema, and Testcontainers
  integration tests must create a real database from the same bootstrap path used in
  production. The upstream data changes rarely, so replacing the dataset on refresh is
  simpler than supporting incremental data migrations.
- Alternatives considered:
  - Manual schema provisioning outside the app: rejected for local/Unraid ergonomics and
    repeatable tests.
  - Full migration tracking: rejected for this slice because current SQL schema is small
    and the dataset can be refreshed wholesale.

## Decision 20: One-Shot Ingestion Worker

- Decision: `FrameData.Ingestion` runs as a one-shot worker: load configuration,
  bootstrap the schema, run the orchestrator for a requested scope or full catalog,
  replace the persisted character/move dataset, write JSON
  exports, log run status, and exit with an explicit success/partial/failure code.
- Rationale: This fits Unraid/Compose scheduling, avoids an always-on scheduler before
  it is needed, and makes manual refresh/retry behavior easy to observe.
- Alternatives considered:
  - Always-on background scheduler: rejected for this slice because scheduling policy is
    host-specific and can be handled by Unraid/cron/Compose initially.
  - API-only ingestion trigger: rejected as the only path because the separate ingestion
    container already exists and should be deployable as a controlled refresh job.

## Decision 21: Full Character Catalog as Runtime Data

- Decision: Define a single supported-character catalog containing internal id,
  display name, source character id, aliases, enabled flag, and display order. Full
  catalog ingestion is the default; scoped ingestion is available for tests and manual
  retries.
- Rationale: Hardcoding only Makoto in API endpoints does not satisfy US2. A catalog
  makes source scope explicit, testable, and reusable by both worker and API-triggered
  ingestion.
- Alternatives considered:
  - Inline default scopes in API/worker code: rejected because it duplicates source-id
    knowledge and makes full-roster validation harder.
  - Store the catalog only in PostgreSQL before ingestion: rejected for the first slice
    because bootstrap would need seed management before the ingestion path can run.

## Decision 22: Persistent Integration Tests Must Assert Database Rows

- Decision: Add Testcontainers tests that query PostgreSQL directly after repository,
  orchestrator, worker-host, and API operations. In-memory assertions alone are not
  sufficient to close US2.
- Rationale: The current tests pass while the worker still prints `Hello, World!` and
  repositories are in-memory. Direct row assertions prove the production persistence
  boundary.
- Alternatives considered:
  - Keep current fake-source/in-memory tests: retained as unit-level coverage only, but
    rejected as completion evidence for real ingestion.
  - Live source-site integration tests in CI: rejected because external network/content
    availability would make CI nondeterministic.
