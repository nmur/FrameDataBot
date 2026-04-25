# Quickstart: Discord 3s Frame Data Bot

## Prerequisites

- Docker and Docker Compose (for local parity builds)
- .NET 10 SDK (for local non-container development)
- Discord application with bot token, application ID, and target guild ID
- Bot invited to the target guild with application command permissions
- Container image registry account (for example GHCR)

## Configuration

1. Create `.env` (do not commit):
   - `DISCORD_BOT_TOKEN`
   - `BOT_GUILD_ID`
   - `API_BIND_URL`
   - `POSTGRES_CONNECTION_STRING`
   - `BOT_API_BASE_URL` (Bot -> API internal base URL, for example `http://api:8080`)
   - `INGESTION_SOURCE_BASE_URL` (defaults to `http://ensabahnur.free.fr/BastonNew/index.php`)
   - `FRAMEDATA_EXPORTS_PATH` for persisted JSON exports.
2. Configure ingestion execution on the host. The ingestion image is a one-shot worker;
   schedule it with Unraid/cron/Compose when a refresh is desired.
3. Configure data volume paths for PostgreSQL and JSON exports.

## Discord Application Setup

1. In the Discord Developer Portal, create or select the bot application.
2. Enable the bot user and copy the bot token into `DISCORD_BOT_TOKEN`.
3. Use the application ID to invite the bot to the target guild with scopes:
   - `bot`
   - `applications.commands`
4. Grant the bot permission to view/send messages in the test channel.
5. Use the numeric target guild ID as `BOT_GUILD_ID`.

## Deploy Services (Container Host)

1. Build and test locally:
   - `docker compose build`
   - `dotnet test tests/unit`
2. Configure your target Docker host:
   - Use `docker-compose.prod.yml` for registry-based deployment
   - Copy `.env.prod.example` to the host and fill in deployment values
   - Set required env vars/secrets
   - Provision PostgreSQL and wire connection string
   - Configure persistent disk for JSON exports where needed
3. Deploy:
   - Pull tagged images from registry and restart services, or
   - Trigger host deployment through GitHub Actions + self-hosted runner
4. Verify health:
   - Bot connected to Discord
   - API health endpoint responds
   - Ingestion run can be triggered and observed
   - Bot can query API successfully in deployed environment

## Optional GitHub Actions Deployment

1. Add registry credentials and deployment host secrets to GitHub.
2. On successful CI test workflow, publish images and trigger deployment.
3. Confirm deployment status from your host and action logs.

## Run Services (Local, Optional)

1. Build images:
   - `docker compose build`
2. Start stack:
   - `docker compose up -d`
3. Verify health:
   - Bot connected to Discord
   - Bot logs show `Registered Discord slash command /framedata` for the configured guild
   - API health endpoint responds
   - Ingestion run can be triggered and observed
   - Bot container can resolve and call API service over compose network
4. Run explicit Bot config validation check:
   - Ensure `DISCORD_BOT_TOKEN`, numeric `BOT_GUILD_ID`, and `BOT_API_BASE_URL` are set

## Run Tests

1. Unit tests:
   - `dotnet test tests/unit`
2. Integration tests (Testcontainers required):
   - `dotnet test tests/integration`
3. Contract tests:
   - `dotnet test tests/contract`

## MVP Validation Flow

1. Apply migrations through API or ingestion startup and confirm PostgreSQL has the
   expected `characters`, `moves`, and `ingestion_runs` tables.
2. Trigger full-catalog ingestion:
   - Local Compose: `docker compose run --rm ingestion`
   - API trigger: `POST /v1/ingestion/runs` with no body, then poll
     `GET /v1/ingestion/runs/{runId}`
3. Confirm one JSON export exists per enabled character under the configured export
   volume.
4. Confirm PostgreSQL contains rows for every enabled character and move rows from
   Normals, Specials, Super Arts, and Misc where source data exists.
5. Query the API directly for an ingested move:
   - `GET /v1/moves/query?character=makoto&moveInput=2mk`
6. Start the bot service and confirm it connects to Discord Gateway.
7. Confirm `/framedata` appears in the configured guild.
8. Query known exact move names via Discord command:
   - `/framedata character:makoto move:2mk`
9. Confirm the bot posts primitive frame-data text from the API-backed PostgreSQL row.
10. Confirm unknown move/character responses are clear.
11. Confirm partial-ingestion behavior: successful character updates are committed while
    failed scopes are marked for retry in run status.
12. Confirm released image set includes Bot, API, and Ingestion images with matching
    version tags.

## Rich Response Follow-Up Validation

1. Query a move with complete frame data.
2. Confirm Discord response uses a structured embed with character, move, section, and
   frame-data fields.
3. Query a move with optional media once image ingestion exists.
4. Confirm the embed includes the media reference when available and still sends a text
   fallback when media is unavailable.

## Security Baseline

- Run containers as non-root where possible.
- Mount secrets from host files; avoid hardcoded credentials.
- Expose only required ports.
- Keep images pinned and regularly updated.
- Use isolated Docker network for internal service communication.

## Pre-Production Security Gate (Required)

Before production use, complete all checks with zero critical findings:

1. Dependency vulnerability scan.
2. Container image vulnerability scan.
3. Secrets scan for repository and build artifacts.
4. Manual least-privilege review (runtime user, ports, filesystem/write paths, secret handling).

## Performance Validation Protocol (SC-001)

1. Use a fixed representative dataset and fixed sample size for each run.
2. Run and record both:
   - API query latency measurements.
   - Bot end-to-end latency measurements from slash interaction receipt through Discord response send.
3. Pass criteria: at least 95% of valid exact-name queries complete in under 3 seconds.
