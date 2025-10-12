# Deployment Configuration

This directory contains production deployment configuration for the cr-browser application.

## Files

- `docker-compose.yml` - Docker Swarm stack configuration for production deployment

## Deployment

### Automatic Deployment

Non-prerelease GitHub releases automatically deploy to production:

1. Create a release on GitHub (e.g., `v1.0.0`)
2. Mark it as a **release** (not a pre-release)
3. Build workflow automatically:
   - Builds and pushes images to Docker Hub
   - Deploys to production Swarm stack

### Manual Deployment

To manually deploy a specific version:

```bash
VERSION=v1.0.0 docker stack deploy -c deploy/docker-compose.yml cr-browser
```

To deploy latest:

```bash
docker stack deploy -c deploy/docker-compose.yml cr-browser
```

## Network Requirements

The `cr-browser-net` overlay network must exist before deployment:

```bash
docker network create --driver overlay cr-browser-net
```

## Prerelease Versions

Prereleases (e.g., `0.0.0.1-alpha-1`) are built and pushed to Docker Hub but **NOT** automatically deployed. They can be manually deployed using the manual deployment command above.
