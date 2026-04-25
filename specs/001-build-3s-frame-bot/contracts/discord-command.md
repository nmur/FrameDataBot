# Discord Command Contract

## Command

- Name: `/framedata`
- Registration scope: configured guild (`BOT_GUILD_ID`) for the first gateway slice
- Visibility: channel-visible response by default

## Parameters

- `character` (required, string)
  - Raw character query, for example `makoto`
  - Must be non-empty after trimming
- `move` (required, string)
  - Raw move query, for example `2mk`
  - Must be non-empty after trimming

## Gateway Behavior

- Bot runtime connects to Discord Gateway using `DISCORD_BOT_TOKEN`.
- On startup, the bot registers or updates the guild-scoped `/framedata` slash command.
- On `framedata` slash interaction:
  - Extract `character` and `move` option values.
  - Reject missing/blank values with a clear validation response.
  - Query the API with `GET /v1/moves/query?character={character}&moveInput={move}`.
  - Reply to the Discord interaction with the move result or actionable error.
- The handler should defer/acknowledge the interaction when needed so API latency does
  not exceed Discord's initial response window.

## Lookup Behavior

- Exact canonical move-name lookup is required for MVP.
- Later phases enable alias/notation/fuzzy matching.
- Ambiguous fuzzy matches return candidate options for user confirmation.
- Input validation errors are limited to character/move correctness and format.

## Response Shapes

### Primitive Successful Match

The first gateway implementation may return plain text with:

- Character name
- Matched move name
- Move section
- Startup / Active / Recovery / On-Hit / On-Block values when available

Example format:

```text
Makoto Hayate (Specials) | Startup 12 Active 3 Recovery 21 OnHit +2 OnBlock -6
```

### Rich Successful Match

Planned follow-up response using a Discord embed:

- Embed title: `{Character} - {Matched move}`
- Section field
- Startup / Active / Recovery fields
- On-Hit / On-Block fields
- Optional advanced properties section when metadata exists
- Optional image/media reference when available
- Plain text fallback content remains available

### Ambiguous Match

- Short explanation
- Ordered candidate list with move name and section
- No silent low-confidence final match

### Not Found

- Error message explaining no match found
- Suggestion to provide exact move name or clearer notation

### Unsupported Character

- Error message explaining the character is not supported
- Suggestion to provide a supported character name
