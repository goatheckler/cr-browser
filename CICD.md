# CI/CD Strategy for ghcr-browser

**Last Updated**: 2025-10-06  
**Status**: Test and Build Workflows Complete - Ready for Deploy Workflow

## Overview

This document outlines the CI/CD strategy for the ghcr-browser monorepo, which contains a .NET backend API and a SvelteKit frontend, both deployed as separate container images.

## Architecture Decisions

### Monorepo Structure
**Decision**: Keep backend and frontend in single repository  
**Rationale**:
- Atomic commits across both applications
- Shared tooling, scripts, and infrastructure code
- Simplified local development setup
- Single source of truth for versioning
- E2E tests that span both services

### CI/CD Platform
**Decision**: GitHub Actions  
**Rationale**:
- Native GitHub integration (no external service)
- Free tier sufficient for project needs
- Docker Hub integration for container registry
- Simple YAML configuration
- Good monorepo support with path filtering
- Marketplace of pre-built actions

## Workflow Strategy

### Multiple Workflows Approach
We implement **three separate workflows** rather than one monolithic workflow:

1. **`test.yml`** - Testing (runs on all PRs) ✅ Complete
2. **`build.yml`** - Build & Push Images (runs on release creation) ✅ Complete
3. **`deploy.yml`** - Deployment (manual trigger or tag-based) 🔄 To Do

**Rationale**:
- Different trigger conditions (test on PRs, build on releases, deploy manually)
- Faster feedback loop (tests fail fast without building)
- Clear separation of concerns and failure points
- Different permission boundaries (test=none, build=registry write, deploy=production credentials)
- Reusability (deploy to staging vs production with same workflow)

## Workflow Details

### 1. Test Workflow (`test.yml`) ✅ Complete

**Trigger**: Pull requests to `main` branch

**Jobs**:
```yaml
jobs:
  test-backend:
    - Checkout code
    - Setup .NET 8
    - Restore dependencies
    - Run unit tests (backend/tests/unit)
    - Run integration tests (backend/tests/integration)
    - Run contract tests (backend/tests/contract)
    
  test-frontend:
    - Checkout code
    - Setup Node 20
    - Install dependencies
    - Build frontend
    - Run type checking (svelte-check)
    
  test-e2e:
    needs: [test-backend, test-frontend]
    - Checkout code
    - Setup .NET 8 and Node 20
    - Install frontend dependencies
    - Install Playwright browsers (cached)
    - Start services with docker-compose
    - Run Playwright E2E tests
    - Upload test results/screenshots on failure
```

**Path Filtering**: 
- `test-backend` only runs if `backend/**` or shared files change
- `test-frontend` only runs if `frontend/**` or shared files change
- `test-e2e` runs if either backend or frontend changes

**Success Criteria**: All jobs must pass for PR to be mergeable

### 2. Build Workflow (`build.yml`) ✅ Complete

**Trigger**: GitHub release creation (target must be `main` branch)

**Jobs**:
```yaml
jobs:
  build-images:
    if: github.event.release.target_commitish == 'main'
    - Checkout code
    - Setup Docker Buildx
    - Login to Docker Hub
    - Extract metadata for backend
    - Extract metadata for frontend
    - Build and push backend image
      - Tag: thefnordling/ghcr-browser-backend:latest
      - Tag: thefnordling/ghcr-browser-backend:<release-tag>
      - Tag: thefnordling/ghcr-browser-backend:sha-<commit-sha>
    - Build and push frontend image
      - Tag: thefnordling/ghcr-browser-frontend:latest
      - Tag: thefnordling/ghcr-browser-frontend:<release-tag>
      - Tag: thefnordling/ghcr-browser-frontend:sha-<commit-sha>
```

**Image Building**:
- Both backend and frontend images always built together on every release
- Ensures synchronized versioning across services
- No change detection needed since releases are manual

**Image Tagging Strategy**:
- `latest`: Always points to most recent release
- `<release-tag>`: Semantic version from GitHub release (e.g., `0.0.0.1-alpha-1`)
- `sha-<commit-sha>`: Immutable reference to specific commit

### 3. Deploy Workflow (`deploy.yml`)

**Trigger**: 
- Manual workflow dispatch (with environment and version inputs)
- OR automatic on git tag matching `v*.*.*`

**Jobs**:
```yaml
jobs:
  deploy:
    - Checkout code
    - Pull specified image versions from Docker Hub
    - Deploy to target environment (staging/production)
    - Run smoke tests
    - Notify on success/failure
```

**Inputs**:
- `environment`: staging | production
- `backend_version`: Image tag to deploy (default: latest)
- `frontend_version`: Image tag to deploy (default: latest)

**Deployment Strategy**:
- Pull images from Docker Hub (don't rebuild)
- Update docker-compose or K8s manifests with specified versions
- Run health checks/smoke tests post-deployment

## Container Versioning

### Automated Versioning via Renovate Merges
Both containers receive the same version tag, auto-incremented on Renovate merges:
- Backend and frontend always deployed together with matching versions
- Simpler mental model and clearer release management
- Single git tag/release triggers both container builds
- Version is automatically determined from latest tag

**Release Process**:
1. Renovate PR with `auto-release` label merges to main
2. Auto-release workflow detects merged PR via label
3. Latest tag fetched (e.g., `v1.2.3`) and patch incremented (→ `v1.2.4`)
4. New tag and GitHub release created automatically
5. Both containers automatically built and tagged with release version
6. Images available as:
   - `thefnordling/ghcr-browser-backend:1.2.4`
   - `thefnordling/ghcr-browser-frontend:1.2.4`
   - Both also tagged as `:latest` and `:sha-<commit>`

**Manual Releases**: Developers can create releases manually via GitHub UI for non-dependency updates

**Decision**: Use **automated patch versioning for dependencies**, manual versioning for features

## Optimization Strategies

### 1. Selective Execution
- Path-based filtering to skip unchanged applications
- Saves CI minutes and speeds up feedback

### 2. Caching
- Docker layer caching with GitHub Actions cache
- NuGet package caching for .NET
- npm package caching for Node
- Playwright browser binary caching

### 3. Parallel Execution
- Backend and frontend jobs run in parallel
- Only E2E tests wait for both

### 4. Fast Failure
- Tests run before builds
- Unit tests before integration tests
- Lightweight checks before expensive builds

## Security

### Secrets Management
- GitHub Secrets for:
  - `DOCKERHUB_USERNAME`: Docker Hub username (thefnordling)
  - `DOCKERHUB_TOKEN`: Docker Hub access token
  - `DEPLOY_KEY`: SSH/API key for deployment target
- Never log secrets or expose in build output
- Use minimal permission scopes

### Image Scanning
- Optional: Add container security scanning (Trivy, Snyk)
- Run in build workflow before push
- Fail on critical vulnerabilities

## Branch Protection

### Main Branch Rules ✅ Implemented
- Require `test-backend`, `test-frontend`, `test-e2e` workflows to pass before merge
- Require branches to be up to date before merge (ensures tests run against merged state)
- Require pull requests (no direct pushes to main)
- Require 1 approval for PRs (optional, if team grows)
- Require linear history (prevents merge commits, use squash or rebase)

## Future Enhancements

### Phase 2 Improvements
- Add performance testing job
- Add container vulnerability scanning
- Implement deployment rollback automation
- Add automated release notes generation
- Implement canary deployments
- Add staging environment preview deployments for PRs

### Monitoring Integration
- Post-deployment health checks
- Integration with monitoring/alerting systems
- Deployment event tracking

## Implementation Checklist

- [x] Complete test suite (20 tests total: 6 unit + 3 integration + 4 contract + 7 E2E)
  - [x] Backend unit tests (ValidationTests.cs)
  - [x] Backend integration tests (InvalidFormatTests, NotFoundTests, SuccessTests)
  - [x] Backend contract tests (HealthTests, TagsContractTests, ErrorContractTests)
  - [x] Frontend E2E tests (health.spec.ts, tags.spec.ts, user-interactions.spec.ts)
  - [x] Manual validation via Playwright MCP
- [x] Create `.github/workflows/test.yml`
- [x] Create `.github/workflows/build.yml`
- [x] Configure GitHub Secrets (DOCKERHUB_USERNAME, DOCKERHUB_TOKEN)
- [x] Configure self-hosted runner with Docker access
- [x] Set up branch protection rules
- [ ] Create `.github/workflows/deploy.yml`
- [ ] Document release process
- [ ] Document deployment process
- [ ] Test full CI/CD pipeline end-to-end
- [ ] Validate production deployment

## References

- GitHub Actions Documentation: https://docs.github.com/en/actions
- Docker Hub: https://hub.docker.com/u/thefnordling
- Docker Buildx: https://docs.docker.com/buildx/working-with-buildx/
