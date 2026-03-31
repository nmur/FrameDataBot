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
