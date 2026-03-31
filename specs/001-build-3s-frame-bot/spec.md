# Feature Specification: Discord 3s Frame Data Bot

**Feature Branch**: `001-build-3s-frame-bot`  
**Created**: 2026-03-21  
**Status**: Approved  
**Input**: User description: "Create a Discord bot which fetches and displays Street Fighter III: 3rd Strike (3s) frame data, along with other associate information. The Discord user should be able to call the bot with a command and a parameter for the desired character and the name of the move, and the bot should return a rich display of the moves frame data. First iterations of the bot will return just simple frame data numbers (ie active frames, frame advantages), but later iterations should be able to return more advanced details (ie special cancellable?) and also display an image of the move (potentially an animated gif of the move). The data will be sourced from http://ensabahnur.free.fr/BastonNew/index.php. Each characters frame data is located in URLs such as http://ensabahnur.free.fr/BastonNew/index.php?id=1. Create a tool which will scrape the data directly from the each characters page, specifically the Normals, Specials, Super Arts, and Misc sections. The data should be pulled into json files (one per character), but should also be made available in a database for our bot service to access. Ideally I would like to run the bots backend service(s) locally on my Unraid home server in Docker container(s), but only if I can do this in a secure way."

## Clarifications

### Session 2026-03-31

- Q: Which security validation gate should define "secure containerized hosting" readiness? → A: Mandatory pre-production checklist gate: dependency scan + container image scan + secrets scan + manual least-privilege review, all with zero critical findings.
- Q: How should SC-001 performance be validated? → A: Measure both API query latency and bot end-to-end latency using a fixed representative dataset and fixed sample size per run.
- Q: What sampling standard should be used for sampled success criteria? → A: Minimum 100 samples per criterion, stratified across characters and move categories, with consistent methodology each run.
- Q: What should ingestion do when some characters/sections fail? → A: Allow partial success; commit successful updates and mark failed characters/sections with explicit run status indicating retry is needed.
- Q: What is the scraper implementation scope for this feature? → A: .NET-only scraper scope for this feature, with no Python fallback included.

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

---

### User Story 2 - Source Ingestion and Persistence (Priority: P1)

As a maintainer, I want a repeatable ingest process from source pages so bot answers are
backed by current stored data.

**Why this priority**: Lookup quality depends on data quality and refreshability.

**Independent Test**: Running ingestion creates one JSON export per character and updates
persistent records for Normals, Specials, Super Arts, and Misc.

**Acceptance Scenarios**:

1. **Given** source pages are reachable, **When** ingestion runs, **Then** all supported
   characters are ingested from the designated sections.
2. **Given** ingestion completes successfully, **When** data is checked, **Then** one
   JSON file exists per character and persistent records are queryable by character and
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

### User Story 4 - Last Active-Frame Hitbox Image (Priority: P2)

As a Discord user, I want an image for the move's last active frame when available so I
can visually confirm the hitbox state.

**Why this priority**: Adds immediate visual value while keeping storage requirements
manageable.

**Independent Test**: For a move with a hitbox display page, the service stores and can
return the last active-frame image including hitbox boxes.

**Acceptance Scenarios**:

1. **Given** a move with an accessible hitbox display page, **When** image ingestion
   runs, **Then** the system captures and stores the last active-frame image when
   derivable.
2. **Given** image data exists for the requested move, **When** the bot returns frame
   data, **Then** the response includes the stored image reference.
3. **Given** the last active frame cannot be determined, **When** ingestion runs,
   **Then** the system records the failure and preserves text frame-data functionality.

---

### User Story 5 - Full Frame-Image Expansion Decision (Priority: P3)

As a maintainer, I want a storage impact evaluation for saving all move frame images so
I can decide whether full image archival is viable.

**Why this priority**: Prevents uncontrolled disk growth before enabling full-image
capture.

**Independent Test**: A report exists estimating storage requirements and operational
impact for full per-frame image retention across all characters and moves.

**Acceptance Scenarios**:

1. **Given** representative image samples, **When** storage analysis runs, **Then** the
   system produces estimated per-move, per-character, and full-roster storage needs.
2. **Given** the storage estimate, **When** maintainers review deployment limits,
   **Then** they can approve or reject full-image archival based on defined thresholds.

---

### User Story 6 - Expanded Move Details and Media (Priority: P3)

As an advanced player, I want extra move properties and optional media references so I
can access deeper move context beyond basic numbers.

**Why this priority**: Useful enhancement after core lookup, ingestion, and image
foundations are stable.

**Independent Test**: For moves with enriched metadata, responses include advanced
properties without breaking MVP fields.

**Acceptance Scenarios**:

1. **Given** enriched metadata exists, **When** a move is requested, **Then** the
   response includes advanced properties in a dedicated section.
2. **Given** enriched metadata is unavailable, **When** a move is requested, **Then**
   basic frame data still returns successfully.

## Out of Scope *(mandatory)*

- Matchup advice, combo recommendations, and strategy coaching.
- User account systems, ranking systems, or moderation features.
- Real-time game telemetry or emulator integration.
- Full video hosting.
- Automatic capture of all frame images for all moves before storage analysis and
  approval.

### Edge Cases

- Source page is unreachable during refresh.
- Source page structure changes and one or more sections cannot be parsed.
- Character or move names contain inconsistent punctuation/casing.
- User input matches multiple moves with similar confidence.
- User input does not match any move above minimum confidence.
- Hitbox display page exists but last active frame cannot be identified.
- Hitbox display page references missing or broken image assets.
- Duplicate refresh runs are triggered concurrently.
- Partial ingestion failures for some characters/sections still preserve successful updates, while failed scopes are explicitly marked for retry.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Users MUST be able to request move data by providing a character and move
  identifier in a bot command.
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
- **FR-008**: The system MUST store ingested move data in a persistent queryable store
  used by the bot service.
- **FR-009**: The system MUST provide a repeatable refresh process that updates stored
  data without manual row-level edits; successful character updates MAY be committed
  when some characters/sections fail, and failures MUST be recorded with explicit run
  status indicating retry is needed.
- **FR-010**: After MVP, lookup MUST support shorthand, numpad notation, and colloquial
  aliases with scored best-guess matching.
- **FR-011**: The system MUST return disambiguation options when no single match exceeds
  the ambiguity threshold.
- **FR-012**: The system MUST ingest move hitbox-display data and attempt to capture the
  last active-frame image (including displayed hitbox boxes) when available.
- **FR-013**: The system MUST store a reference to captured move images so responses can
  include image data when present.
- **FR-014**: The system MUST produce a storage-impact report for full per-frame image
  archival before enabling that archival behavior.
- **FR-015**: Deployment MUST support secure containerized hosting on a cloud platform
  with CI/CD integration, including a mandatory pre-production security checklist gate
  (dependency scan, container image scan, secrets scan, and manual least-privilege
  review) with zero critical findings.
- **FR-016**: The bot MUST return a clear and actionable response when character, move,
  or requested data is unavailable.
- **FR-017**: Implementation MUST use small, single-responsibility functions with
  descriptive names.
- **FR-018**: Comments in production code MUST explain why decisions were made, not
  restate behavior.

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
  name and frame data values.
- **MoveAlias**: A normalized alternate input form for a move, including shorthand,
  numpad notation, and colloquial terms.
- **MatchCandidate**: A scored move candidate containing normalized query,
  candidate move, confidence score, and rank.
- **MoveFrameData**: Structured timing and advantage attributes for a move.
- **MoveImage**: Stored artifact for move visuals, including image location, source
  reference, and captured frame designation.
- **StorageAssessment**: Periodic estimate of disk requirements for image archival
  scopes (last-active-only vs full per-frame capture).
- **IngestionRun**: A refresh event record containing run time, source scope, outcome,
  and per-character status.

## Assumptions

- Initial release prioritizes exact canonical move-name matching before fuzzy/alias
  resolution.
- Bot command usage is limited to one move lookup per request.
- Basic frame-data fields are authoritative from source unless explicitly overridden by
  maintainers.
- Last active-frame image capture is attempted only when frame sequencing is available
  and derivable from source data.
- Full per-frame image capture remains disabled until storage impact is evaluated and
  explicitly approved.
- Cloud deployment security includes least-privilege defaults, controlled secrets, and
  restricted service exposure to only necessary ports.
- Scraper implementation scope for this feature is .NET-only; alternate runtime
  fallbacks are out of scope for this feature iteration.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At least 95% of valid exact-name move queries return a complete response
  in under 3 seconds, validated each run using fixed-size samples across both API query
  latency and bot end-to-end latency on a representative dataset.
- **SC-002**: At least 99% of sampled exact canonical move-name queries resolve to the
  intended move, measured on at least 100 stratified samples per run.
- **SC-003**: 100% of supported characters have exactly one exported JSON file after a
  successful refresh.
- **SC-004**: 99% of sampled stored move entries match latest ingested source values
  after each refresh, measured on at least 100 stratified samples per run.
- **SC-005**: At least 95% of sampled shorthand/notation/colloquial inputs resolve to
  the intended canonical move after alias/fuzzy release, measured on at least 100
  stratified samples per run.
- **SC-006**: 100% of ambiguous fuzzy matches return disambiguation options rather than
  low-confidence silent final matches.
- **SC-007**: For moves with derivable hitbox frame data, at least 90% have a stored
  last active-frame image reference after image ingestion, measured on at least 100
  stratified samples per run.
- **SC-008**: A storage-impact report for full per-frame image archival is produced and
  approved or rejected before full-image archival is enabled.
- **SC-009**: Before production use, all security checklist categories (dependency scan,
  container image scan, secrets scan, manual least-privilege review) complete with zero
  critical findings.
