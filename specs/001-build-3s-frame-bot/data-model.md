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
- Validation:
  - `name` unique per character.
  - `sourceCharacterId` unique.

## Move

- Description: Canonical move record for a character and section.
- Fields:
  - `id` (string, required)
  - `characterId` (string, required)
  - `section` (enum, required): `Normals | Specials | SuperArts | Misc`
  - `canonicalName` (string, required)
  - `displayOrder` (int, optional)
  - `sourceMoveId` (string, optional)
  - `frameData` (MoveFrameData, required)
  - `metadata` (MoveMetadata, optional)
- Relationships:
  - Many-to-one with `Character`.
  - One-to-many with `MoveAlias` and `MoveImage`.
- Validation:
  - Unique constraint: `characterId + section + canonicalName`.

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
  - `imageType` (enum, required): `LastActiveFrame | Other`
  - `storagePath` (string, required)
  - `sourceUrl` (string, required)
  - `capturedAt` (datetime, required)
  - `captureStatus` (enum, required): `Success | Failed | NotDerivable`
- Validation:
  - One `LastActiveFrame` record per move.

## StorageAssessment

- Description: Capacity planning output for image archival choices.
- Fields:
  - `id` (string, required)
  - `createdAt` (datetime, required)
  - `sampleSize` (int, required)
  - `estimatedBytesPerMove` (long, required)
  - `estimatedBytesPerCharacter` (long, required)
  - `estimatedBytesFullRoster` (long, required)
  - `recommendedPolicy` (enum, required): `LastActiveOnly | FullFrames | Defer`
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
- Validation:
  - `completedAt` required for terminal statuses.

## State Transitions

- `IngestionRun.status`:
  - `Running -> Succeeded`
  - `Running -> PartiallySucceeded`
  - `Running -> Failed`
- `MoveImage.captureStatus`:
  - `NotDerivable`/`Failed` may transition to `Success` on later refresh.

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
  - `content` (string, required for primitive response): plain text fallback.
  - `embedTitle` (string, optional): rich response title when embeds are enabled.
  - `embedFields` (collection, optional): structured frame data fields.
  - `imageUrl` (string, optional): optional media reference when available.
  - `isEphemeral` (bool, required): defaults to `false` for channel-visible answers.
- Validation:
  - Primitive response must fit Discord message content limits.
  - Rich response must preserve enough text fallback for clients or failures where
    embeds cannot be sent.
