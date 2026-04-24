# Production Compose Deployment

Use `docker-compose.prod.yml` for registry-based deployments on a Docker host. The local
`docker-compose.yml` remains intended for development builds from source.

## Files

- `docker-compose.prod.yml`: pulls published images from a registry.
- `.env.prod.example`: copy into the deployment environment and fill in secrets.

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

## Deploy Or Update

Run from the directory containing `docker-compose.prod.yml` and `.env.prod`:

```bash
docker compose --env-file .env.prod -f docker-compose.prod.yml pull
docker compose --env-file .env.prod -f docker-compose.prod.yml up -d --remove-orphans
```

`pull_policy: always` ensures Compose checks the registry when Compose runs. It does not update
already-running containers by itself, so schedule the commands above on the Docker host if you
want unattended updates.

## Notes

- Postgres is not exposed outside the Compose network in the production file.
- API is exposed on `${API_PORT:-8080}` for host access.
- Persistent paths are controlled by `FRAMEDATA_POSTGRES_DATA` and `FRAMEDATA_EXPORTS_PATH`.
- The current ingestion executable and PostgreSQL-backed persistence are still implementation
  work items. The deployment shape is ready for published images, but production data behavior
  should be validated before relying on the stack for real users.
