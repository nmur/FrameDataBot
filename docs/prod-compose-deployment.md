# Production Compose Deployment

Use `docker-compose.prod.yml` for registry-based deployments on a Docker host. The local
`docker-compose.yml` remains intended for development builds from source.

## Files

- `docker-compose.prod.yml`: pulls published images from a registry.
- `docker-compose.ingestion.yml`: runs the ingestion worker on demand against the same
  static dataset root.
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
FRAMEDATA_DATASET_HOST_ROOT=/srv/framedatabot/dataset
FRAMEDATA_DATASET_ROOT=/data/framedata
FRAMEDATA_ACTIVE_DATASET_PATH=/data/framedata/active
```

`FRAMEDATA_DATASET_HOST_ROOT` is a host path. The runtime Compose file mounts it read-only at
`FRAMEDATA_DATASET_ROOT`; the ingestion Compose file mounts the same path read-write.

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

Open Seq at `http://<docker-host>:${SEQ_PORT:-5341}`. The API, Bot, and one-shot Ingestion
worker all write structured logs to Seq when `SEQ_SERVER_URL` is set. Useful starting filters:

```text
ServiceName = 'FrameData.Api'
ServiceName = 'FrameData.Bot'
ServiceName = 'FrameData.Ingestion'
```

Use `SEQ_MINIMUM_LEVEL=Information` to reduce application log volume after the deployment is
stable. Leave it at `Debug` while validating ingestion because per-character and per-move details
are emitted at Debug level.

## Static Dataset Refresh

Run ingestion only when a refresh is desired:

```bash
docker compose --env-file .env.prod -f docker-compose.ingestion.yml run --rm ingestion
```

For a scoped retry:

```bash
docker compose --env-file .env.prod -f docker-compose.ingestion.yml run --rm ingestion --characters=makoto,chun-li
```

The worker writes versioned directories under `${FRAMEDATA_DATASET_ROOT}/versions` inside the
container, which maps to `${FRAMEDATA_DATASET_HOST_ROOT}/versions` on the host. It updates
`${FRAMEDATA_DATASET_HOST_ROOT}/active`. Restart the API after a successful refresh so it reloads
the active dataset.

On Unraid, keep `FRAMEDATA_DATASET_HOST_ROOT` on persistent appdata or a protected share, for example
`/mnt/user/appdata/framedatabot/dataset`. Runtime containers only need read access; ingestion
needs write access.

## Notes

- API is exposed on `${API_PORT:-8080}` for host access.
- Seq is exposed on `${SEQ_PORT:-5341}` for host access; keep it behind your trusted network or
  reverse proxy.
- Persistent paths are controlled by `FRAMEDATA_DATASET_HOST_ROOT` and `FRAMEDATA_SEQ_DATA`.
