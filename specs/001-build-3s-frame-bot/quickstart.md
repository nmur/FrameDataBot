# Quickstart: Discord 3s Frame Data Bot

## Prerequisites

- Docker and Docker Compose
- .NET 10 SDK for local non-container development
- Discord application with bot token, application ID, and target guild ID
- Bot invited to the target guild with application command permissions
- Container image registry account for production images
- Seq is included in Compose for centralized structured logs

## Configuration

1. Create `.env` from `.env.example` for the runtime stack:
   - `DISCORD_BOT_TOKEN`
   - `BOT_GUILD_ID`
   - `BOT_API_BASE_URL` (defaults to `http://api:8080`)
   - `FRAMEDATA_DATASET_HOST_ROOT` host path shared by runtime and ingestion
   - `FRAMEDATA_DATASET_ROOT` container path, usually `/data/framedata`
   - `FRAMEDATA_ACTIVE_DATASET_PATH` container path read by the API, usually `/data/framedata/active`
   - `SEQ_*` settings for local structured logs
2. Create `.env.ingestion` from `.env.ingestion.example` for one-shot ingestion runs.
3. Keep the host dataset root on persistent local storage. On Unraid, a practical default is a share such as `/mnt/user/appdata/framedatabot/dataset` mapped to `/data/framedata` in both Compose files.

## Discord Application Setup

1. In the Discord Developer Portal, create or select the bot application.
2. Enable the bot user and copy the bot token into `DISCORD_BOT_TOKEN`.
3. Invite the bot with scopes:
   - `bot`
   - `applications.commands`
4. Grant the bot permission to view/send messages in the test channel.
5. Use the numeric target guild ID as `BOT_GUILD_ID`.

## Static Dataset Flow

1. Run ingestion when you want to refresh data:
   - Full catalog: `docker compose --env-file .env.ingestion -f docker-compose.ingestion.yml run --rm ingestion`
   - Scoped retry: `docker compose --env-file .env.ingestion -f docker-compose.ingestion.yml run --rm ingestion --characters=makoto,chun-li`
2. Ingestion writes a versioned dataset under the mounted dataset root `versions/` directory and updates `active`.
3. Confirm the active dataset has:
   - `manifest.json`
   - `characters/*.json`
   - `media/` for later local attachment assets
4. Start or restart the runtime stack so the API loads the active dataset:
   - `docker compose up -d --build`
5. Query the API directly:
   - `GET /v1/moves/query?character=makoto&moveInput=2mk`

The dataset directory is the portable artifact. To roll back, repoint `active` to a previous directory under `versions/` or copy the desired version back into the active path before restarting the API.

## Deploy Services

1. Build and test locally:
   - `docker compose build`
   - `dotnet test tests/unit`
2. Configure the production host:
   - Copy `.env.prod.example` to `.env.prod`
   - Set `FRAMEDATA_DATASET_ROOT` to persistent host storage
   - Set Discord and Seq secrets
3. Deploy runtime services:
   - `docker compose --env-file .env.prod -f docker-compose.prod.yml pull`
   - `docker compose --env-file .env.prod -f docker-compose.prod.yml up -d --remove-orphans`
4. Run ingestion on demand with the separate ingestion Compose file using the same dataset root.

## Run Services Locally

1. Build images:
   - `docker compose build`
2. Run ingestion once:
   - `docker compose --env-file .env.ingestion -f docker-compose.ingestion.yml run --rm ingestion`
3. Start runtime stack:
   - `docker compose up -d`
4. Verify:
   - Seq UI opens at `http://localhost:5341`
   - API health endpoint responds
   - Bot connects to Discord and registers `/framedata`
   - Bot can query API successfully over the Compose network

## Run Tests

1. Unit tests:
   - `dotnet test tests/unit`
2. Integration tests:
   - `dotnet test tests/integration`
3. Contract tests:
   - `dotnet test tests/contract`

## MVP Validation Flow

1. Run one-shot ingestion and confirm `manifest.json` plus character JSON files exist under the active dataset.
2. Start the runtime stack.
3. Query the API:
   - `GET /v1/moves/query?character=makoto&moveInput=2mk`
4. Start the bot service and confirm it connects to Discord Gateway.
5. Confirm `/framedata` appears in the configured guild.
6. Query a known move via Discord:
   - `/framedata character:makoto move:2mk`
7. Confirm the Discord response is a structured embed with character, move, section,
   startup, active, recovery, on-hit, and on-block fields plus concise fallback content.
8. Confirm unknown move/character responses are clear.
9. Confirm Seq contains operational detail:
   - `ServiceName = 'FrameData.Bot'`
   - `ServiceName = 'FrameData.Api'`
   - `ServiceName = 'FrameData.Ingestion'`

## Media Attachment Follow-Up Validation

1. Query a move with optional media once image ingestion exists.
2. Confirm the existing embed sends local files as Discord attachments and references them with `attachment://...`.

## Security Baseline

- Run containers as non-root where possible.
- Mount secrets from host files; avoid hardcoded credentials.
- Expose only required ports.
- Keep images pinned and regularly updated.
- Use isolated Docker networks for internal service communication.
- Keep the dataset root writable only for ingestion; mount it read-only for API runtime.

## Performance Validation Protocol

1. Use a fixed representative dataset and fixed sample size for each run.
2. Run and record API query latency measurements and bot end-to-end latency measurements.
3. Pass criteria: at least 95% of valid exact-name queries complete in under 3 seconds.
