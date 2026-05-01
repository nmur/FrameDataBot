# Feature Specification: Discord 3s Frame Data Bot

**Feature Branch**: `001-build-3s-frame-bot`  
**Created**: 2026-03-21  
**Status**: Closed

**Input**: User description: "Create a Discord bot which fetches and displays Street Fighter III: 3rd Strike (3s) frame data, along with other associate information. The Discord user should be able to call the bot with a command and a parameter for the desired character and the name of the move, and the bot should return a rich display of the moves frame data. First iterations of the bot will return just simple frame data numbers (ie active frames, frame advantages), but later iterations should be able to return more advanced details (ie special cancellable?) and also display an image of the move (potentially an animated gif of the move). The data will be sourced from http://ensabahnur.free.fr/BastonNew/index.php. Each characters frame data is located in URLs such as http://ensabahnur.free.fr/BastonNew/index.php?id=1. Create a tool which will scrape the data directly from the each characters page, specifically the Normals, Specials, Super Arts, and Misc sections. The data should be pulled into json files (one per character), but should also be made available in a database for our bot service to access. Ideally I would like to run the bots backend service(s) locally on my Unraid home server in Docker container(s), but only if I can do this in a secure way."

## Clarifications

### Session 2026-03-31

- Q: Which security validation gate should define "secure containerized hosting" readiness? → A: Mandatory pre-production checklist gate: dependency scan + container image scan + secrets scan + manual least-privilege review, all with zero critical findings. Superseded for this closed plan by the 2026-04-29 deferral.
- Q: How should performance be validated? → A: Measure both API query latency and bot end-to-end latency using a fixed representative dataset and fixed sample size per run. Superseded for this closed plan by the 2026-04-29 deferral.
- Q: What sampling standard should be used for sampled success criteria? → A: Minimum 100 samples per criterion, stratified across characters and move categories, with consistent methodology each run.
- Q: What should ingestion do when some characters/sections fail? → A: Allow partial success; replace the stored dataset with successful character scopes from the run and mark failed characters/sections with explicit run status indicating retry is needed.
- Q: What is the scraper implementation scope for this feature? → A: .NET-only scraper scope for this feature, with no Python fallback included.
- Q: What should the canonical Discord command surface be for the current lean scope? → A: Use `/framedata` with required `character` and `move` parameters only.

### Session 2026-04-25

- Q: Does MVP completion require actual Discord gateway/slash-command handling, not only command handler logic? → A: Yes. The bot runtime must connect to Discord, register the `/framedata` slash command globally by default, receive slash-command interactions in Discord channels, query the API using the provided `character` and `move` values, and reply in-channel. Guild-scoped registration remains available for beta/test deployments. The original first pass allowed primitive text; the 2026-04-28 clarification supersedes that response-format direction.
- Q: Does US2 completion require a real ingestion worker and persistent PostgreSQL read/write path, not only parser/orchestrator scaffolding? → A: Yes for the 2026-04-25 intermediate slice. This was superseded by the completed static dataset refactor, where ingestion publishes versioned JSON/media datasets and API/bot lookup reads the active dataset.

### Session 2026-04-28

- Q: Should the next Discord response implementation continue with primitive text before rich formatting? → A: No. The bot should use Discord embeds as the default `/framedata` response format for move results now; the later 2026-04-28 clarification removes duplicate basic text from normal embed responses.
- Q: Should ingestion preserve non-frame source columns such as Specials/Super Arts Motion plus Damage and Stun? → A: Yes. These values should be optional move attributes that are parsed from source tables when present and retained through the stored dataset and lookup response path.
- Q: Should normal embed responses keep the old basic text response as message content? → A: No. Formatted move, ambiguous, and query-error results should be sent as embed-only Discord responses; content-only text remains acceptable for validation or operational failures where no embed result exists.

### Session 2026-04-29 Closeout

- Q: Which remaining analysis findings should block closing this plan? → A: None. The maintainer approved deferring the full-frame storage assessment, expanded advanced metadata, formal sample-based performance benchmark evidence, and mandatory pre-production security gate automation to future plans.
- Q: What is the closing scope for this plan? → A: US1-US4 plus the static dataset, source-column, Discord gateway/embed, representative-media, and Seq logging follow-ups already marked complete in `tasks.md`.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Exact Move Lookup MVP (Priority: P1)

As a Discord user, I want to query by character and exact move name so I can quickly get
reliable frame data.

**Why this priority**: This is the smallest useful version and avoids early ambiguity
risk.

**Independent Test**: A user submits a known character and exact canonical move name and
receives frame data for the intended move.

**Acceptance Scenarios**:

1. **Given** a supported character and exact canonical move name, **When** the user
   submits a query command, **Then** the bot returns startup, active, recovery, and
   frame advantage values when available.
2. **Given** a supported character but an exact move name that does not exist,
   **When** the user submits a query, **Then** the bot returns a not-found response with
   guidance.
3. **Given** a character that is not supported, **When** the user submits a query,
   **Then** the bot returns a clear unsupported-character response.
4. **Given** the bot is running with valid Discord configuration and the API is
   reachable, **When** a Discord user invokes `/framedata` in a channel with
   `character` and `move`, **Then** the bot acknowledges the interaction and posts the
   corresponding structured embed frame-data response or actionable error.

---

### User Story 2 - Source Ingestion and Persistence (Priority: P1)

As a maintainer, I want a repeatable ingest process from source pages so bot answers are
backed by current stored data.

**Why this priority**: Lookup quality depends on data quality and refreshability.

**Independent Test**: Running ingestion creates one JSON file per character, publishes
an active static dataset, and makes Normals, Specials, Super Arts, and Misc queryable.

**Acceptance Scenarios**:

1. **Given** source pages are reachable, **When** ingestion runs, **Then** all supported
   characters are ingested from the designated sections.
2. **Given** ingestion completes successfully, **When** data is checked, **Then** one
   JSON file exists per character and the active dataset is queryable by character and
   move.
3. **Given** a source value changes, **When** the next ingestion completes, **Then**
   stored values are updated accordingly.

---

### User Story 3 - Notation and Alias Resolution (Priority: P2)

As a Discord user, I want shorthand and colloquial move input to resolve correctly so I
can query using familiar notation.

**Why this priority**: High-value usability improvement after exact-match reliability is
established.

**Independent Test**: Inputs like `cr.HK`, `2hk`, and `sweep` resolve to the intended
canonical move when confidence is sufficient.

**Acceptance Scenarios**:

1. **Given** an input using shorthand, numpad, or colloquial notation,
   **When** the user submits a query, **Then** the bot maps it to the most likely
   canonical move using scored matching.
2. **Given** multiple candidates with near-equal confidence, **When** the user submits
   a query, **Then** the bot returns top candidates for disambiguation instead of a
   silent low-confidence guess.
3. **Given** no acceptable candidate, **When** the user submits a query, **Then** the
   bot returns a no-match response and suggests better input.

---

### User Story 4 - Representative Active-Frame Hitbox Image (Priority: P2)

As a Discord user, I want an image for a representative active frame when available so
I can visually confirm the hitbox state.

**Why this priority**: Adds immediate visual value while keeping storage requirements
manageable.

**Independent Test**: For a move with a hitbox display page, the service stores and can
return a representative active-frame image including configured P1 hitbox boxes.

**Acceptance Scenarios**:

1. **Given** a move with an accessible hitbox display page, **When** image ingestion
   runs, **Then** the system captures and stores the representative active-frame image
   selected by the configured selector when derivable.
2. **Given** image data exists for the requested move, **When** the bot returns frame
   data, **Then** the response includes the stored image reference.
3. **Given** the representative frame cannot be determined or the frame image is
   unavailable, **When** ingestion runs, **Then** the system stores a configured or
   generated dummy image reference, records the fallback reason, and preserves text
   frame-data functionality.
4. **Given** image ingestion is first enabled, **When** an operator runs the worker,
   **Then** the image scope can be limited to a small pilot set such as selected Ken
   normals, selected Ken specials, and Ken SA3 before full-catalog media capture.

---

## Out of Scope *(mandatory)*

- Matchup advice, combo recommendations, and strategy coaching.
- User account systems, ranking systems, or moderation features.
- Real-time game telemetry or emulator integration.
- Full video hosting.
- Automatic capture of all frame images for all moves before storage analysis and
  approval.
- Full-frame storage impact reporting and approval workflow; this is deferred to a
  future plan before any full-frame archival is enabled.
- Expanded advanced move metadata beyond the retained source Motion, Damage, and Stun
  attributes; this is deferred to a future plan.
- Formal sample-based performance benchmark evidence and security-gate automation for
  production readiness; these are deferred to future operational hardening plans.

### Edge Cases

- Source page is unreachable during refresh.
- Source page structure changes and one or more sections cannot be parsed.
- Character or move names contain inconsistent punctuation/casing.
- User input matches multiple moves with similar confidence.
- User input does not match any move above minimum confidence.
- Hitbox display page exists but no representative active frame can be identified.
- Hitbox display page references missing or broken image assets.
- Representative-frame selector produces a tie; the earliest tied frame is used unless
  a move-specific override exists.
- Duplicate refresh runs are triggered concurrently.
- Partial ingestion failures for some characters/sections still store successful updates from that run, while failed scopes are explicitly marked for retry.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Users MUST be able to request move data via `/framedata` by providing
  required `character` and `move` values.
- **FR-002**: MVP lookup MUST support exact canonical move-name matching.
- **FR-003**: MVP responses MUST include startup, active, recovery, and frame advantage
  values when available for the requested move.
- **FR-004**: The response MUST identify matched character, matched move name, and move
  category (Normals, Specials, Super Arts, or Misc).
- **FR-005**: The system MUST ingest data from designated source pages for each
  supported character.
- **FR-006**: Ingestion MUST collect Normals, Specials, Super Arts, and Misc sections
  per character.
- **FR-007**: The system MUST produce one JSON export file per character after
  successful ingestion.
- **FR-008**: The system MUST store ingested move data in a persistent static dataset
  that is queryable through the API used by the bot service.
- **FR-009**: The system MUST provide a repeatable refresh process that updates stored
  data without manual row-level edits; each refresh replaces the stored dataset with
  successfully ingested character scopes from that run, failures MUST be recorded
  with explicit run status indicating retry is needed, and a fully failed run MUST
  leave the previous dataset intact.
- **FR-010**: After MVP, lookup MUST support shorthand, numpad notation, and colloquial
  aliases with scored best-guess matching.
- **FR-011**: The system MUST return disambiguation options when no single match exceeds
  the ambiguity threshold.
- **FR-012**: The system MUST ingest move hitbox-display data and attempt to capture a
  representative active-frame image when available. The default selector MUST choose
  the earliest frame with the largest summed active hitbox rectangle area.
- **FR-013**: The system MUST store a reference to captured move images so responses can
  include image data when present.
- **FR-013a**: Rendered hitbox images MUST include `P1_P`, `P1_V`, `P1_A`, `P1_T`,
  and `P1_TA` overlays and MUST exclude all P2 hitboxes.
- **FR-013b**: Image ingestion MUST support a scoped pilot move list so maintainers can
  validate selected Ken normals, selected Ken specials, and Ken SA3 before full media
  ingestion.
- **FR-013c**: Image ingestion MUST store a configured or generated empty/dummy image
  when the representative frame image is unavailable, while retaining structured
  fallback metadata.
- **FR-014**: The bot MUST return a clear and actionable response when character, move,
  or requested data is unavailable.
- **FR-015**: Implementation MUST use small, single-responsibility functions with
  descriptive names.
- **FR-016**: Comments in production code MUST explain why decisions were made, not
  restate behavior.
- **FR-017**: The bot runtime MUST connect to Discord, register the `/framedata` slash
  command globally by default, receive slash-command interactions, and route them
  through the move query flow.
- **FR-018**: The bot MUST use a rich Discord embed as the default move lookup response,
  including matched character, matched move, section, and frame-data fields, without
  sending duplicate plain-text message content for formatted move, ambiguous, or
  query-error results. Validation or operational failures where no embed result exists
  MAY remain content-only.
- **FR-019**: Ingestion MUST preserve optional source table values for Motion, Damage,
  and Stun as move attributes when those columns are present, including Motion values
  on Specials and Super Arts.

### Verification Requirements *(mandatory)*

- **VR-001**: Tests MUST follow TDD where feasible (failing tests before
  implementation).
- **VR-002**: Unit tests MUST comprehensively cover new and changed logic.
- **VR-003**: Integration tests MUST validate boundaries and external dependencies.
- **VR-004**: Integration tests MUST use reliable and reproducible dependency
  orchestration suitable for the system under test.

### Key Entities *(include if feature involves data)*

- **Character**: A playable 3s character with source-page identifier, display name,
  aliases, and associated move set.
- **Move**: A single attack entry tied to one character and one section, with canonical
  name, optional Motion/Damage/Stun source attributes, and frame data values.
- **MoveAlias**: A normalized alternate input form for a move, including shorthand,
  numpad notation, and colloquial terms.
- **MatchCandidate**: A scored move candidate containing normalized query,
  candidate move, confidence score, and rank.
- **MoveFrameData**: Structured timing and advantage attributes for a move.
- **MoveImage**: Stored artifact for move visuals, including image location, source
  reference, selected representative frame designation, selection strategy, rendered
  overlay set, and fallback status.
- **StaticDatasetManifest**: Metadata for a versioned static dataset directory,
  including schema version, generated timestamp, source metadata, character count,
  move count, and media count.
- **StaticCharacterFile**: Per-character JSON file containing character metadata and
  the move records loaded by the API at startup.
- **IngestionRun**: A refresh event record containing run time, source scope, outcome,
  and per-character status.

## Assumptions

- Initial release prioritizes exact canonical move-name matching before fuzzy/alias
  resolution.
- Bot command usage is limited to one move lookup per request.
- The current feature iteration targets Street Fighter III: 3rd Strike only and does
  not expose a game-selection parameter in bot or API query interfaces.
- Basic frame-data fields are authoritative from source unless explicitly overridden by
  maintainers.
- Representative active-frame image capture is attempted only when frame sequencing and
  active hitbox rectangles are available and derivable from source data.
- The default representative selector uses summed active hitbox rectangle area; future
  selectors and per-move overrides may replace the selected frame without changing the
  stored media contract.
- Full per-frame image capture remains disabled and outside this closing plan.
- Production security gate automation and formal benchmark evidence are deferred by
  maintainer-approved closeout exception on 2026-04-29.
- Scraper implementation scope for this feature is .NET-only; alternate runtime
  fallbacks are out of scope for this feature iteration.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At least 99% of sampled exact canonical move-name queries resolve to the
  intended move, measured on at least 100 stratified samples per run.
- **SC-002**: 100% of supported characters have exactly one exported JSON file after a
  successful refresh.
- **SC-003**: 99% of sampled stored move entries match latest ingested source values
  after each refresh, measured on at least 100 stratified samples per run.
- **SC-004**: At least 95% of sampled shorthand/notation/colloquial inputs resolve to
  the intended canonical move after alias/fuzzy release, measured on at least 100
  stratified samples per run.
- **SC-005**: 100% of ambiguous fuzzy matches return disambiguation options rather than
  low-confidence silent final matches.
- **SC-006**: For moves with derivable hitbox frame data in the enabled image-ingestion
  scope, at least 90% have a stored representative active-frame image reference after
  image ingestion, measured first against the configured pilot scope and later against
  at least 100 stratified samples per full media run.
