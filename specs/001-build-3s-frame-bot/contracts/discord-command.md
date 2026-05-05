# Discord Command Contract

## Command

- Name: `/framedata`
- Registration scope: global by default; optional guild-scoped registration for beta/test deployments
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
- On startup, the bot registers or updates the global `/framedata` slash command by
  default, or the guild-scoped command in each configured guild when
  `DISCORD_COMMAND_REGISTRATION_SCOPE=guild`.
- On `framedata` slash interaction:
  - Extract `character` and `move` option values.
  - Reject missing/blank values with a clear validation response.
  - Query the API with `GET /v1/moves/query?character={character}&moveInput={move}`.
  - Reply to the Discord interaction with an embed-first move result or actionable error.
- The handler should defer/acknowledge the interaction when needed so API latency does
  not exceed Discord's initial response window.

## Lookup Behavior

- Exact canonical move-name lookup is required for MVP.
- Alias, notation, and fuzzy matching are supported after the lookup refinement.
- Ambiguous fuzzy matches return candidate options for user confirmation.
- Input validation errors are limited to character/move correctness and format.

## Response Shapes

### Successful Match Embed

The default successful `/framedata` response uses a Discord embed with:

- Embed title: `{Character} - {Matched move}`
- Color accent chosen by response type or move category
- Section field
- Startup / Active / Recovery fields
- On-Hit / Cr. On-Hit / On-Block fields
- Optional source Motion, Damage, Stun, and notes fields when present
- Optional image/media attachment when representative media exists
- No duplicate plain-text message content for formatted move results

### Ambiguous Match

- Embed with a short explanation.
- Ordered candidate list with move name, section, and score when available.
- No silent low-confidence final match

### Not Found

- Error embed explaining no match found.
- Suggestion to provide exact move name or clearer notation.

### Unsupported Character

- Error embed explaining the character is not supported.
- Suggestion to provide a supported character name.

### Content Rules

- Formatted move, ambiguous, and query-error results should send the embed by itself
  with no duplicate message `content`.
- Validation failures may remain content-only if no move query result exists.
- Media responses attach local files and reference them from the embed with
  `attachment://...`; they do not require a public CDN.
