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

## Decision 7: Cloud Host Recommendation and Security Baseline

- Decision: Use Render as the default initial host; deploy containerized services from
  GitHub with either Render auto-deploy or GitHub Actions invoking deploy hooks.
- Rationale: Lowest operational friction, straightforward GitHub integration, and a
  usable free entry path for early iterations.
- Alternatives considered:
  - Railway: strong DX but free usage is less predictable for always-on workloads.
  - Fly.io: capable for containers but free allowance is less favorable for sustained
    always-on usage.

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
