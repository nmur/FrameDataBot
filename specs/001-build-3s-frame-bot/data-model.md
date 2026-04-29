# Data Model: Discord 3s Frame Data Bot

## Character

- Description: Playable 3s character with source linkage and lookup metadata.
- Fields:
  - `id` (string, required): stable internal identifier.
  - `sourceCharacterId` (int, required): source-site character ID.
  - `name` (string, required): canonical display name.
  - `aliases` (string[], optional): alternate character names.
  - `updatedAt` (datetime, required): last successful ingest update timestamp.
- Relationships:
  - One-to-many with `Move`.
  - Created/updated from one `SourceCharacterCatalogEntry`.
- Validation:
  - `name` unique per game.
  - `sourceCharacterId` unique.

## SourceCharacterCatalogEntry

- Description: Runtime catalog entry defining a supported 3s character and its source
  page mapping. This is required input to ingestion and may be stored as character
  metadata after a successful upsert.
- Fields:
  - `id` (string, required): stable internal character id used by queries and exports.
  - `sourceCharacterId` (int, required): source-site `id` query parameter.
  - `displayName` (string, required): canonical display name.
  - `aliases` (string[], optional): alternate character inputs.
  - `enabled` (bool, required): whether this character participates in default
    full-catalog ingestion.
  - `displayOrder` (int, required): stable ordering for exports and reports.
- Relationships:
  - One-to-one bootstrap source for a persisted `Character`.
- Validation:
  - `id` unique, non-empty, lowercase kebab/snake-safe identifier.
  - `sourceCharacterId` unique across enabled entries.
  - `displayOrder` unique across enabled entries.

## Move

- Description: Canonical move record for a character and section.
- Fields:
  - `id` (string, required)
  - `characterId` (string, required)
  - `section` (enum, required): `Normals | Specials | SuperArts | Misc`
  - `canonicalName` (string, required)
  - `displayOrder` (int, optional)
  - `sourceMoveId` (string, optional)
  - `motion` (string, optional): source command/input notation, primarily present
    for Specials and Super Arts.
  - `damage` (string, optional): source damage column value preserved as text.
  - `stun` (string, optional): source stun column value preserved as text.
  - `frameData` (MoveFrameData, required)
  - `metadata` (MoveMetadata, optional)
- Relationships:
  - Many-to-one with `Character`.
  - One-to-many with `MoveAlias` and `MoveImage`.
- Validation:
  - Unique constraint: `characterId + section + canonicalName`.
  - `motion`, `damage`, and `stun` are optional because source sections do not all
    expose the same columns.

## MoveFrameData

- Description: Numeric and textual frame values used for responses.
- Fields:
  - `startup` (string, optional)
  - `active` (string, optional)
  - `recovery` (string, optional)
  - `onHit` (string, optional)
  - `onBlock` (string, optional)
  - `frameAdvantage` (string, optional)
  - `notes` (string, optional)
- Validation:
  - At least one key timing/advantage field must be present.

## MoveAlias

- Description: Alternate query forms mapping to a canonical move.
- Fields:
  - `id` (string, required)
  - `moveId` (string, required)
  - `alias` (string, required)
  - `aliasType` (enum, required): `Canonical | Abbreviation | Numpad | Colloquial | Derived`
  - `normalizedAlias` (string, required)
- Validation:
  - Unique per move on `normalizedAlias`.

## MatchCandidate

- Description: Ranked candidate produced during fuzzy lookup.
- Fields:
  - `moveId` (string, required)
  - `canonicalName` (string, required)
  - `matchedAlias` (string, optional)
  - `score` (decimal, required)
  - `rank` (int, required)
  - `thresholdPassed` (bool, required)
- Validation:
  - Scores sorted descending by rank.

## MoveImage

- Description: Stored move image asset metadata.
- Fields:
  - `id` (string, required)
  - `moveId` (string, required)
  - `imageType` (enum, required): `RepresentativeActiveFrame | Other`
  - `storagePath` (string, required)
  - `sourceUrl` (string, required)
  - `sourceFrameImageUrl` (string, optional): source PNG URL for the selected frame
    when an image was available.
  - `selectedFrame` (string, optional): zero-padded source frame identifier selected
    for capture, such as `006`.
  - `selectionStrategy` (string, required): selector identifier, initially
    `largest-active-hitbox-area`.
  - `activeHitboxArea` (int, optional): summed active hitbox rectangle area used by
    the default selector.
  - `overlayHitboxes` (string[], required): rendered overlay set. Initial value is
    `P1_P`, `P1_V`, `P1_A`, `P1_T`, and `P1_TA`.
  - `fallbackReason` (string, optional): sanitized reason when a dummy image was stored.
  - `capturedAt` (datetime, required)
  - `captureStatus` (enum, required): `Success | DummyFallback | Failed | NotDerivable`
- Validation:
  - One `RepresentativeActiveFrame` record per move.
  - `storagePath` is required even for `DummyFallback` so Discord responses can attach
    a stable placeholder image.
  - Successful representative captures require `selectedFrame` and `sourceFrameImageUrl`.
  - `DummyFallback`, `Failed`, and `NotDerivable` statuses require `fallbackReason`.

## RepresentativeFrameSelectionPolicy

- Description: Configurable policy for selecting the frame to render for a move image.
- Fields:
  - `defaultStrategy` (string, required): initial value `largest-active-hitbox-area`.
  - `pilotMoveScope` (string[], optional): explicit move keys enabled for early media
    ingestion, such as selected Ken normals, selected Ken specials, and Ken SA3.
  - `moveOverrides` (map, optional): per-move override entries by stable move key.
  - `dummyImagePath` (string, optional): local PNG used when source frame images are
    unavailable. When omitted, ingestion generates a blank 384x224 PNG placeholder.
- Validation:
  - `pilotMoveScope` entries must resolve to known `characterId + moveId` keys before
    media capture starts.
  - A per-move override may specify either an explicit `selectedFrame` or an alternate
    selector strategy, but not both.
  - If `dummyImagePath` is provided, it must exist or media capture must fail
    validation before network calls begin.

## StorageAssessment

- Description: Capacity planning output for image archival choices.
- Fields:
  - `id` (string, required)
  - `createdAt` (datetime, required)
  - `sampleSize` (int, required)
  - `estimatedBytesPerMove` (long, required)
  - `estimatedBytesPerCharacter` (long, required)
  - `estimatedBytesFullRoster` (long, required)
  - `recommendedPolicy` (enum, required): `RepresentativeOnly | FullFrames | Defer`
  - `notes` (string, optional)

## IngestionRun

- Description: Execution log for ingestion/scraping runs.
- Fields:
  - `id` (string, required)
  - `startedAt` (datetime, required)
  - `completedAt` (datetime, optional)
  - `status` (enum, required): `Running | Succeeded | PartiallySucceeded | Failed`
  - `charactersProcessed` (int, required)
  - `movesProcessed` (int, required)
  - `errors` (string[], optional)
  - `characterStatuses` (IngestionRunCharacterStatus[], optional): per-character
    result details used to identify retry-required scopes.
- Validation:
  - `completedAt` required for terminal statuses.
  - `characterStatuses` required for any run that attempted at least one character.

## IngestionRunCharacterStatus

- Description: Per-character result captured during an ingestion run.
- Fields:
  - `characterId` (string, required)
  - `sourceCharacterId` (int, required)
  - `status` (enum, required): `Succeeded | Failed`
  - `movesProcessed` (int, required)
  - `error` (string, optional): sanitized failure reason for retry diagnostics.
- Relationships:
  - Belongs to one `IngestionRun`.
- Validation:
  - Failed statuses must include `error`.
  - Succeeded statuses must have `movesProcessed >= 0`.

## State Transitions

- `IngestionRun.status`:
  - `Running -> Succeeded`
  - `Running -> PartiallySucceeded`
  - `Running -> Failed`
- `MoveImage.captureStatus`:
  - `NotDerivable`/`Failed`/`DummyFallback` may transition to `Success` on later
    refresh.

## DiscordCommandInvocation

- Description: Runtime-only representation of a received `/framedata` slash command.
  This is not persisted.
- Fields:
  - `interactionId` (string, required): Discord interaction identifier used for
    logging/correlation only.
  - `guildId` (string, required): guild where the command was invoked.
  - `channelId` (string, required): channel where the command response will be sent.
  - `userId` (string, required): invoking user identifier for logging/correlation.
  - `commandName` (string, required): must equal `framedata`.
  - `character` (string, required): raw slash option value.
  - `move` (string, required): raw slash option value.
  - `receivedAt` (datetime, required): gateway receive time.
- Validation:
  - `commandName` must match the configured command contract.
  - `character` and `move` must be non-empty after trimming.
  - Interaction identifiers are used for logs only and must not be stored as user
    account data.

## DiscordCommandRegistration

- Description: Runtime command definition registered with Discord for the configured
  guild. This is not persisted.
- Fields:
  - `name` (string, required): `framedata`.
  - `description` (string, required): short user-facing Discord command description.
  - `guildId` (string, required): target guild for registration.
  - `options` (collection, required): required string options `character` and `move`.
  - `registeredAt` (datetime, optional): last successful startup registration time.
- Validation:
  - Command and option names must satisfy Discord slash command naming rules.
  - Registration must not expose token or secret values in logs.

## DiscordMoveResponse

- Description: Runtime response payload sent back to a Discord interaction.
- Fields:
  - `content` (string, optional): omitted for normal formatted embed responses;
    available for validation or operational failures where no embed result exists.
  - `embedTitle` (string, required for successful move responses): rich response title.
  - `embedColor` (integer, optional): Discord embed accent color.
  - `embedFields` (collection, required for successful move responses): structured
    frame-data fields such as section, startup, active, recovery, on-hit, and on-block.
  - `attachmentFileName` (string, optional): local Discord attachment name used by
    future media embeds with `attachment://...`.
  - `isEphemeral` (bool, required): defaults to `false` for channel-visible answers.
- Validation:
  - Embed content must fit Discord embed limits.
  - Validation failures may be content-only when no move result exists.
