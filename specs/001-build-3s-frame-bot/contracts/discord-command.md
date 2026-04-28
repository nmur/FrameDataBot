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
  - Reply to the Discord interaction with an embed-first move result or actionable error.
- The handler should defer/acknowledge the interaction when needed so API latency does
  not exceed Discord's initial response window.

## Lookup Behavior

- Exact canonical move-name lookup is required for MVP.
- Later phases enable alias/notation/fuzzy matching.
- Ambiguous fuzzy matches return candidate options for user confirmation.
- Input validation errors are limited to character/move correctness and format.

## Response Shapes

### Successful Match Embed

The default successful `/framedata` response uses a Discord embed with:

- Embed title: `{Character} - {Matched move}`
- Color accent chosen by response type or move category
- Section field
- Startup / Active / Recovery fields
- On-Hit / On-Block fields
- Optional notes or advanced properties section when metadata exists
- Optional image/media attachment when available in a later media slice
- Concise plain-text fallback content summarizing the same move

Example fallback content:

```text
Makoto Hayate (Specials) | Startup 12 Active 3 Recovery 21 OnHit +2 OnBlock -6
```

### Ambiguous Match

- Embed or fallback content with a short explanation.
- Ordered candidate list with move name, section, and score when available.
- No silent low-confidence final match

### Not Found

- Error embed or fallback content explaining no match found.
- Suggestion to provide exact move name or clearer notation.

### Unsupported Character

- Error embed or fallback content explaining the character is not supported.
- Suggestion to provide a supported character name.

### Fallback Rules

- The bot should always provide concise `content` alongside embeds for accessibility,
  logging, and graceful fallback.
- Validation failures may remain content-only if no move query result exists.
- Future media responses attach local files and reference them from the embed with
  `attachment://...`; they do not require a public CDN.
