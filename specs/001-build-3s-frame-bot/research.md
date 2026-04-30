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

## Decision 3: Runtime Storage Choice

- Decision: Use versioned static dataset directories as the runtime source of truth.
  Each dataset contains `manifest.json`, per-character JSON files under `characters/`,
  and optional media files under `media/`. The API loads the active dataset at startup.
- Rationale: The current closed plan prioritizes a portable dataset artifact that works
  cleanly on local Docker and Unraid without a long-running database dependency.
- Alternatives considered:
  - PostgreSQL with JSONB: implemented as an intermediate persistence slice, then
    superseded by the static dataset refactor to reduce runtime operational overhead.
  - SQLite JSON: lightweight, but less directly useful once the dataset directory is the
    portable artifact.
  - LiteDB: viable for small deployments, but unnecessary for the finalized runtime
    architecture.

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

- Decision: Defer formal sample-based API/bot latency benchmark evidence to a future
  plan by maintainer-approved closeout exception.
- Rationale: The current plan closes after completed functional, dataset, contract,
  Compose, and Discord/media work. A later operational hardening plan can define and
  preserve benchmark tooling without reopening this feature.
- Alternatives considered:
  - Reopen the current plan for benchmark automation: rejected because the maintainer
    requested plan closeout and a fresh plan for future work.

## Decision 9: Security Release Gate

- Decision: Defer automated pre-production security gate workflow and evidence to a
  future operational hardening plan by maintainer-approved closeout exception.
- Rationale: The current plan keeps secure-containerization guidance and Compose
  topology work, while leaving formal dependency/image/secrets/least-privilege gate
  automation for the next plan.
- Alternatives considered:
  - Reopen the current plan for security-gate automation: rejected because the
    maintainer requested closing this plan and starting future work separately.
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

## Decision 18: Static Dataset Repository Is the Runtime Default

- Decision: The production API resolves a static dataset loader/query repository rather
  than a database-backed repository. Ingestion publishes the versioned dataset directory,
  and Bot continues to query the API.
- Rationale: This preserves the Bot/API boundary while removing database lifecycle,
  schema bootstrap, and connection-string requirements from the always-running stack.
- Alternatives considered:
  - Keep the intermediate database-backed runtime path: superseded because the dataset
    directory is already the portable artifact operators need.
  - Maintain separate database and static-file query paths: rejected because it creates
    drift between refresh and runtime behavior.

## Decision 19: One-Shot Dataset Publisher

- Decision: `FrameData.Ingestion` runs as a one-shot worker that loads configuration,
  scrapes the requested scope or full catalog, writes a staged static dataset, validates
  the output, atomically publishes it, logs status, and exits.
- Rationale: This fits Unraid/Compose scheduling, avoids an always-on scheduler, and
  keeps the refresh artifact directly inspectable on disk.
- Alternatives considered:
  - Always-on background scheduler: rejected for this slice because scheduling policy is
    host-specific and can be handled by Unraid/cron/Compose initially.
  - API-triggered ingestion in the runtime stack: rejected because ingestion is now a
    separate controlled refresh job.

## Decision 20: Full Character Catalog as Worker Data

- Decision: Define a single supported-character catalog containing internal id,
  display name, source character id, aliases, enabled flag, and display order. Full
  catalog ingestion is the default; scoped ingestion is available for tests and manual
  retries.
- Rationale: A catalog makes source scope explicit, testable, and reusable by dataset
  publishing and media capture.
- Alternatives considered:
  - Inline default scopes in worker code: rejected because it duplicates source-id
    knowledge and makes full-roster validation harder.
  - Store the catalog in a runtime database: rejected because the closed plan removes
    runtime database dependency.

## Decision 21: Static Dataset Integration Tests Must Assert Files

- Decision: Integration tests prove `manifest.json`, `characters/*.json`, media files,
  active dataset preservation on failed publish, and API query behavior against fixture
  datasets.
- Rationale: The production boundary is now the file-system dataset plus API startup
  load, so file assertions provide the relevant completion evidence.
- Alternatives considered:
  - Keep database-row assertions as completion evidence: retained only as historical
    coverage for the superseded intermediate slice.
  - Live source-site integration tests in CI: rejected because external network/content
    availability would make CI nondeterministic.
