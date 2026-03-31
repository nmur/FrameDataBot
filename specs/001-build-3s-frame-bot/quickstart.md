# Quickstart: Discord 3s Frame Data Bot

## Prerequisites

- Docker and Docker Compose (for local parity builds)
- .NET 10 SDK (for local non-container development)
- Discord bot token and guild/application IDs
- Render account connected to GitHub repository

## Configuration

1. Create `.env` (do not commit):
   - `DISCORD_BOT_TOKEN`
   - `BOT_GUILD_ID`
   - `API_BIND_URL`
   - `POSTGRES_CONNECTION_STRING`
2. Configure ingestion schedule and source base URL.
3. Configure data volume path for JSON exports.

## Deploy Services (Cloud)

1. Build and test locally:
   - `docker compose build`
   - `dotnet test tests/unit`
2. Configure Render services from repository:
   - Create service(s) from repo using Docker runtime
   - Set required env vars/secrets
   - Provision PostgreSQL service and wire connection string
   - Configure persistent disk for JSON exports where needed
3. Deploy:
   - Use Render auto-deploy on push, or
   - Trigger deploy through GitHub Actions using Render deploy hook
4. Verify health:
   - Bot connected to Discord
   - API health endpoint responds
   - Ingestion run can be triggered and observed

## Optional GitHub Actions Deployment

1. Add Render deploy hook URL as repository secret.
2. On successful CI test workflow, call deploy hook.
3. Confirm deployment status in Render dashboard.

## Run Services (Local, Optional)

1. Build images:
   - `docker compose build`
2. Start stack:
   - `docker compose up -d`
3. Verify health:
   - Bot connected to Discord
   - API health endpoint responds
   - Ingestion run can be triggered and observed

## Run Tests

1. Unit tests:
   - `dotnet test tests/unit`
2. Integration tests (Testcontainers required):
   - `dotnet test tests/integration`
3. Contract tests:
   - `dotnet test tests/contract`

## MVP Validation Flow

1. Trigger ingestion and confirm one JSON export per character.
2. Query known exact move names via Discord command.
3. Confirm expected frame-data fields are returned.
4. Confirm unknown move/character responses are clear.
5. Confirm partial-ingestion behavior: successful character updates are committed while failed scopes are marked for retry in run status.

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
