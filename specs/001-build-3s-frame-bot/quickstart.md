# Quickstart: Discord 3s Frame Data Bot

## Prerequisites

- Docker and Docker Compose (for local parity builds)
- .NET 10 SDK (for local non-container development)
- Discord bot token and guild/application IDs
- Container image registry account (for example GHCR)

## Configuration

1. Create `.env` (do not commit):
   - `DISCORD_BOT_TOKEN`
   - `BOT_GUILD_ID`
   - `API_BIND_URL`
   - `POSTGRES_CONNECTION_STRING`
   - `BOT_API_BASE_URL` (Bot -> API internal base URL, for example `http://api:8080`)
2. Configure ingestion schedule and source base URL.
3. Configure data volume path for JSON exports.

## Deploy Services (Container Host)

1. Build and test locally:
   - `docker compose build`
   - `dotnet test tests/unit`
2. Configure your target Docker host:
   - Deploy separate Bot service using `Dockerfile.bot`
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
   - API health endpoint responds
   - Ingestion run can be triggered and observed
   - Bot container can resolve and call API service over compose network
4. Run explicit Bot config validation check:
   - Ensure `DISCORD_BOT_TOKEN`, `BOT_GUILD_ID`, and `BOT_API_BASE_URL` are set

## Run Tests

1. Unit tests:
   - `dotnet test tests/unit`
2. Integration tests (Testcontainers required):
   - `dotnet test tests/integration`
3. Contract tests:
   - `dotnet test tests/contract`

## MVP Validation Flow

1. Trigger ingestion and confirm one JSON export per character.
2. Query known exact move names via Discord command:
   - `/framedata makoto 2mk`
3. Confirm expected frame-data fields are returned.
4. Confirm unknown move/character responses are clear.
5. Confirm partial-ingestion behavior: successful character updates are committed while failed scopes are marked for retry in run status.
6. Confirm released image set includes Bot, API, and Ingestion images with matching version tags.

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
   - Bot end-to-end latency measurements.
3. Pass criteria: at least 95% of valid exact-name queries complete in under 3 seconds.
