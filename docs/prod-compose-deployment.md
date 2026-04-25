# Production Compose Deployment

Use `docker-compose.prod.yml` for registry-based deployments on a Docker host. The local
`docker-compose.yml` remains intended for development builds from source.

## Files

- `docker-compose.prod.yml`: pulls published images from a registry.
- `.env.prod.example`: copy into the deployment environment and fill in secrets.
- Seq runs from `datalust/seq` in the production Compose file and stores data under
  `FRAMEDATA_SEQ_DATA`.

## Registry Authentication

For private GHCR images, create a GitHub personal access token classic with `read:packages`,
then log in on the Docker host:

```bash
printf '%s' 'YOUR_GITHUB_PAT' | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin
```

## Configuration

Create an environment file from the example and set real values:

```bash
cp .env.prod.example .env.prod
```

For the default stable release channel:

```env
FRAMEDATA_IMAGE_TAG=stable
```

For a rolling feature branch build:

```env
FRAMEDATA_IMAGE_TAG=dev-001-build-3s-frame-bot-latest
```

For an exact feature branch build:

```env
FRAMEDATA_IMAGE_TAG=dev-001-build-3s-frame-bot-b817d4b
```

For an immutable release rollback:

```env
FRAMEDATA_IMAGE_TAG=v0.1.0
```

Set the Seq values before first start:

```env
SEQ_ADMIN_PASSWORD=replace-with-a-strong-password
SEQ_PORT=5341
SEQ_SERVER_URL=http://seq
SEQ_MINIMUM_LEVEL=Debug
FRAMEDATA_SEQ_DATA=/srv/framedatabot/seq
```

## Deploy Or Update

Run from the directory containing `docker-compose.prod.yml` and `.env.prod`:

```bash
docker compose --env-file .env.prod -f docker-compose.prod.yml pull
docker compose --env-file .env.prod -f docker-compose.prod.yml up -d --remove-orphans
```

`pull_policy: always` ensures Compose checks the registry when Compose runs. It does not update
already-running containers by itself, so schedule the commands above on the Docker host if you
want unattended updates.

## Logging

Open Seq at `http://<docker-host>:${SEQ_PORT:-5341}`. The API, Bot, and Ingestion services all
write structured logs to Seq when `SEQ_SERVER_URL` is set. Useful starting filters:

```text
ServiceName = 'FrameData.Api'
ServiceName = 'FrameData.Bot'
ServiceName = 'FrameData.Ingestion'
```

Use `SEQ_MINIMUM_LEVEL=Information` to reduce application log volume after the deployment is
stable. Leave it at `Debug` while validating ingestion because per-character and per-move details
are emitted at Debug level.

## Notes

- Postgres is not exposed outside the Compose network in the production file.
- API is exposed on `${API_PORT:-8080}` for host access.
- Seq is exposed on `${SEQ_PORT:-5341}` for host access; keep it behind your trusted network or
  reverse proxy.
- Persistent paths are controlled by `FRAMEDATA_POSTGRES_DATA`, `FRAMEDATA_SEQ_DATA`, and
  `FRAMEDATA_EXPORTS_PATH`.
