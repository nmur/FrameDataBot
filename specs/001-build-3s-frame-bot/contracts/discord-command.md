# Discord Command Contract

## Command

- `/framedata`

## Parameters

- `character` (required, string)
- `move` (required, string)

## Behavior

- Exact canonical move-name lookup is required for MVP.
- Later phases enable alias/notation/fuzzy matching.
- Ambiguous fuzzy matches return candidate options for user confirmation.
- Input validation errors are limited to character/move correctness and format.

## Response Shapes

### Successful Match

- Character name
- Matched move name
- Move section
- Startup / Active / Recovery / On-Hit / On-Block values (when available)
- Optional last active-frame image link (when available)

### Ambiguous Match

- Short explanation
- Ordered candidate list (move name + section)

### Not Found

- Error message explaining no match found
- Suggestion to provide exact move name or clearer notation
