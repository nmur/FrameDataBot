# Implementation Plan: Discord 3s Frame Data Bot

**Branch**: `001-build-3s-frame-bot` | **Date**: 2026-04-25 | **Spec**: `/specs/001-build-3s-frame-bot/spec.md`
**Input**: Feature specification from `/specs/001-build-3s-frame-bot/spec.md`, plus 2026-04-25 planning refinement: actual Discord gateway/slash-command handling is the next implementation slice.

## Summary

Complete the live Discord-facing `/framedata` path that is implied by US1 but not yet implemented in the bot runtime. Existing exact lookup, API client, and primitive formatter work stays in place. This plan adds Discord.Net gateway startup, guild-scoped slash command registration, slash interaction option mapping, API-backed command execution, and a primitive channel response for the first pass. Rich Discord embed/media response work is planned as a later response-format enhancement so the first slice can prove the end-to-end Discord invocation path.

## Technical Context

- **Language/Version**: .NET 10 (C#) for bot/API/ingestion/scraper services and shared libraries
- **Primary Dependencies**: Discord.Net WebSocket/Interactions, ASP.NET Core Minimal APIs, AngleSharp, FuzzySharp, Serilog, NSubstitute, Shouldly, xUnit, Testcontainers for .NET, Npgsql
- **Storage**: No new storage for gateway handling; bot reads through API, API uses PostgreSQL JSONB and character JSON exports remain unchanged
- **Testing**: xUnit + Shouldly + NSubstitute for unit tests; bot integration tests use deterministic Discord client/interaction adapters rather than live Discord; existing API/Ingestion integration tests continue using Testcontainers
- **Target Platform**: Linux Docker containers on local Compose and self-hosted/managed container hosts; Discord Gateway over outbound WebSocket
- **Project Type**: Multi-service backend (`FrameData.Bot`, `FrameData.Api`, `FrameData.Ingestion`, scraper + shared libraries)
- **Performance Goals**: SC-001 remains: >=95% valid exact-name queries complete in <3 seconds across API latency and bot end-to-end latency on a fixed representative sample
- **Constraints**: .NET-only scope; no Moq/FluentAssertions; live Discord is excluded from automated CI for determinism and token safety; slash command registration is guild-scoped for fast iteration; bot response must preserve public channel command UX unless later product requirements choose ephemeral responses
- **Scale/Scope**: Single-game Street Fighter III: 3rd Strike scope; one `/framedata` command with required `character` and `move` string options; no game selector; no global command registration in the first gateway slice

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Pre-Phase 0 gate:
- [x] Small, single-purpose functions: gateway work is split into command definition, interaction mapping, handler orchestration, runtime lifecycle, and formatting.
- [x] Descriptive naming/intent comments: new Discord classes must name platform concepts directly and comments are limited to external Discord constraints.
- [x] TDD order: tasks add unit/contract/integration tests before implementation tasks.
- [x] Comprehensive automated testing: unit tests cover command schema, option extraction, handler behavior, and formatter behavior; integration tests cover bot host wiring without live Discord.
- [x] Reproducible integration testing: live Discord is not used in CI because it requires external SaaS state and secrets; deterministic adapters cover our boundary, and quickstart includes a manual smoke test for the real gateway.
- [x] Focused scope/performance: this slice only closes the `/framedata` gateway gap, with rich embeds deferred to a separate response-format task group.

Post-Phase 1 re-check:
- [x] Research resolves Discord.Net gateway and command-registration approach.
- [x] Data model adds only runtime interaction/response payload concepts; persistent domain entities remain unchanged.
- [x] Discord command contract now distinguishes primitive first-pass text responses from planned rich embed responses.
- [x] Quickstart contains local/deployed slash-command validation and the manual live Discord smoke path.

## Core Implementation Sequence

### Step 1: Freeze the Discord Command Boundary

- Keep `/framedata` as the only command.
- Keep required string options: `character` and `move`.
- Register commands to the configured guild on startup for fast propagation and low rollout risk.

### Step 2: Add Testable Discord Boundary Adapters

- Introduce small adapters around Discord.Net interaction data and response calls so command parsing and handler behavior can be tested without a live gateway.
- Unit test slash command definition, option extraction, invalid/missing option handling, API success mapping, and API error mapping.

### Step 3: Register the Slash Command

- Build the command definition from the contract in `contracts/discord-command.md`.
- Register the command when the Discord socket client reaches `Ready`.
- Log command registration success/failure without exposing token values.

### Step 4: Handle Slash Interactions

- Listen for Discord interaction creation events.
- Accept only `/framedata` slash command interactions for this handler.
- Extract `character` and `move`, call the existing `IMoveQueryApiClient`, and return the existing primitive formatter output.
- Use an interaction acknowledgement/defer path when API latency may exceed Discord's initial response window.

### Step 5: Replace the Keepalive Bot Loop

- Update `BotRuntimeService` to login, start the Discord socket client, register commands, keep the host alive through the client lifecycle, and stop/logout gracefully on cancellation.
- Preserve existing container/environment behavior.

### Step 6: Validate Primitive End-to-End Discord UX

- Confirm a user can invoke `/framedata character move` in a Discord channel and receive basic frame-data text or a clear error response.
- Keep response text intentionally simple for the first pass.

### Step 7: Plan Rich Response Follow-Up

- Add a Discord embed response builder after the primitive path works.
- Rich response should present character, matched move, section, startup/active/recovery/on-hit/on-block, optional advanced metadata, and optional image/media reference when available.
- Preserve text fallback for clients or failures where embeds/media cannot be sent.

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
│   ├── Discord/          # new gateway/interaction boundary classes
│   ├── Formatting/
│   └── Hosting/
├── FrameData.Api/
├── FrameData.Ingestion/
├── FrameData.Scraper/
├── FrameData.Domain/
├── FrameData.Infrastructure/
└── FrameData.Shared/

tests/
├── unit/
│   ├── FrameData.Bot.Tests/
│   └── FrameData.Domain.Tests/
├── integration/
│   ├── FrameData.Api.IntegrationTests/
│   ├── FrameData.Bot.IntegrationTests/       # new deterministic runtime wiring tests
│   └── FrameData.Ingestion.IntegrationTests/
└── contract/
    └── FrameData.Contracts.Tests/
```

**Structure Decision**: Keep the existing layered .NET multi-service structure. Add a narrow `FrameData.Bot/Discord` namespace for Discord.Net-specific gateway/interaction code, leaving API lookup and response formatting reusable. Add a bot integration test project only for host wiring and deterministic Discord boundary tests.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |
