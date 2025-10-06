# CI/CD Strategy for ghcr-browser

**Last Updated**: 2025-10-06  
**Status**: Test Suite Complete - Ready for Workflow Implementation

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
- Built-in container registry (ghcr.io) integration
- Simple YAML configuration
- Good monorepo support with path filtering
- Marketplace of pre-built actions

## Workflow Strategy

### Multiple Workflows Approach
We will implement **three separate workflows** rather than one monolithic workflow:

1. **`test.yml`** - Testing (runs on all PRs and pushes)
2. **`build.yml`** - Build & Push Images (runs on main branch)
3. **`deploy.yml`** - Deployment (manual trigger or tag-based)

**Rationale**:
- Different trigger conditions (test on every push, build only on main, deploy manually)
- Faster feedback loop (tests fail fast without building)
- Clear separation of concerns and failure points
- Different permission boundaries (test=none, build=registry write, deploy=production credentials)
- Reusability (deploy to staging vs production with same workflow)

## Workflow Details

### 1. Test Workflow (`test.yml`)

**Trigger**: Every push, every pull request

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
    - Run unit tests (when implemented)
    - Run linting/type checking
    
  test-e2e:
    needs: [test-backend, test-frontend]
    - Checkout code
    - Start services with docker-compose
    - Run Playwright E2E tests
    - Upload test results/screenshots on failure
```

**Path Filtering**: 
- `test-backend` only runs if `backend/**` or shared files change
- `test-frontend` only runs if `frontend/**` or shared files change
- `test-e2e` runs if either backend or frontend changes

**Success Criteria**: All jobs must pass for PR to be mergeable

### 2. Build Workflow (`build.yml`)

**Trigger**: Push to `main` branch only

**Jobs**:
```yaml
jobs:
  detect-changes:
    - Determine which applications changed since last build
    
  build-backend:
    needs: detect-changes
    if: needs.detect-changes.outputs.backend == 'true'
    - Checkout code
    - Setup Docker Buildx
    - Login to ghcr.io
    - Build backend image
    - Tag: ghcr.io/[owner]/ghcr-browser-backend:latest
    - Tag: ghcr.io/[owner]/ghcr-browser-backend:sha-<commit-sha>
    - Tag: ghcr.io/[owner]/ghcr-browser-backend:<version> (if tagged)
    - Push all tags
    
  build-frontend:
    needs: detect-changes
    if: needs.detect-changes.outputs.frontend == 'true'
    - Checkout code
    - Setup Docker Buildx
    - Login to ghcr.io
    - Build frontend image
    - Tag: ghcr.io/[owner]/ghcr-browser-frontend:latest
    - Tag: ghcr.io/[owner]/ghcr-browser-frontend:sha-<commit-sha>
    - Tag: ghcr.io/[owner]/ghcr-browser-frontend:<version> (if tagged)
    - Push all tags
```

**Selective Building**:
- Only build containers for changed applications
- Use `git diff` or GitHub's `paths` filter to detect changes
- Skip builds when only docs/tests change

**Image Tagging Strategy**:
- `latest`: Always points to most recent main branch build
- `sha-<commit-sha>`: Immutable reference to specific commit
- `<version>`: Semantic version tag (e.g., `v1.2.3`) when git tagged

### 3. Deploy Workflow (`deploy.yml`)

**Trigger**: 
- Manual workflow dispatch (with environment and version inputs)
- OR automatic on git tag matching `v*.*.*`

**Jobs**:
```yaml
jobs:
  deploy:
    - Checkout code
    - Pull specified image versions from ghcr.io
    - Deploy to target environment (staging/production)
    - Run smoke tests
    - Notify on success/failure
```

**Inputs**:
- `environment`: staging | production
- `backend_version`: Image tag to deploy (default: latest)
- `frontend_version`: Image tag to deploy (default: latest)

**Deployment Strategy**:
- Pull images from ghcr.io (don't rebuild)
- Update docker-compose or K8s manifests with specified versions
- Run health checks/smoke tests post-deployment

## Container Versioning

### Independent Versioning
Each container (backend/frontend) can have independent version numbers:
- Allows deploying backend v1.2.3 with frontend v1.2.4
- Supports independent deployment cadences
- Tracked in separate files: `backend/VERSION` and `frontend/VERSION` (optional)

### Shared Versioning (Alternative)
Single version for entire release:
- Both containers get same version tag
- Simpler mental model
- Single git tag triggers both deployments

**Decision**: Start with **independent versioning** for maximum flexibility

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
  - `GHCR_TOKEN`: GitHub token with package write permissions
  - `DEPLOY_KEY`: SSH/API key for deployment target
- Never log secrets or expose in build output
- Use minimal permission scopes

### Image Scanning
- Optional: Add container security scanning (Trivy, Snyk)
- Run in build workflow before push
- Fail on critical vulnerabilities

## Branch Protection

### Main Branch Rules
- Require `test` workflow to pass before merge
- Require 1 approval for PRs (if team grows)
- No direct pushes to main
- Require branches to be up to date before merge

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
- [ ] Create `.github/workflows/test.yml`
- [ ] Create `.github/workflows/build.yml`
- [ ] Create `.github/workflows/deploy.yml`
- [ ] Configure GitHub Secrets
- [ ] Set up branch protection rules
- [ ] Document deployment process
- [ ] Test full CI/CD pipeline on feature branch
- [ ] Merge to main and validate

## References

- GitHub Actions Documentation: https://docs.github.com/en/actions
- GitHub Container Registry: https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry
- Docker Buildx: https://docs.docker.com/buildx/working-with-buildx/
