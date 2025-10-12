# Multi-Registry Support - Quickstart Testing Guide

## Overview

This guide provides step-by-step instructions for manually testing the multi-registry support feature after implementation. It covers testing each supported registry type, error scenarios, and backward compatibility verification.

## Prerequisites

- Backend API running on `http://localhost:5000` (or configured port)
- Frontend running on `http://localhost:5173` (or configured port)
- Internet connection for accessing public registries
- curl or similar HTTP client for API testing

## Test Scenarios

### 1. GitHub Container Registry (GHCR) - Existing Functionality

**Test Case 1.1: Fetch tags using legacy endpoint**

```bash
# Should continue working exactly as before
curl -X GET "http://localhost:5000/api/images/microsoft/dotnet/tags?page=1&pageSize=10"
```

**Expected Response:**
```json
{
  "tags": [
    {"name": "8.0", "digest": "sha256:...", "lastModified": "2024-..."},
    {"name": "8.0-alpine", "digest": "sha256:...", "lastModified": "2024-..."}
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 10,
    "hasNextPage": true
  }
}
```

**Test Case 1.2: Fetch tags using new endpoint with GHCR**

```bash
# New explicit endpoint
curl -X GET "http://localhost:5000/api/registries/ghcr/microsoft/dotnet/tags?page=1&pageSize=10"
```

**Expected Response:** Same as 1.1 (backward compatibility verified)

**Frontend Test:**
- Navigate to `http://localhost:5173/`
- Default should be GHCR (or no registry selector if backward compatible)
- Enter `microsoft/dotnet` and search
- Verify tags display in ag-grid table

---

### 2. Docker Hub

**Test Case 2.1: Official library image**

```bash
# Docker Hub uses 'library/' prefix for official images
curl -X GET "http://localhost:5000/api/registries/dockerhub/library/nginx/tags?page=1&pageSize=10"
```

**Expected Response:**
```json
{
  "tags": [
    {"name": "latest", "digest": "sha256:...", "lastModified": "2024-..."},
    {"name": "alpine", "digest": "sha256:...", "lastModified": "2024-..."},
    {"name": "1.25", "digest": "sha256:...", "lastModified": "2024-..."}
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 10,
    "hasNextPage": true
  }
}
```

**Test Case 2.2: User repository**

```bash
# User-owned repository
curl -X GET "http://localhost:5000/api/registries/dockerhub/bitnami/nginx/tags?page=1&pageSize=10"
```

**Expected Response:** Similar structure with bitnami/nginx tags

**Test Case 2.3: Frontend - Official image shorthand**

- Navigate to `http://localhost:5173/?registry=dockerhub`
- Enter `nginx` (without `library/` prefix)
- Backend should automatically expand to `library/nginx`
- Verify tags display correctly

**Test Case 2.4: Frontend - User repository**

- Navigate to `http://localhost:5173/?registry=dockerhub`
- Enter `bitnami/nginx`
- Verify tags display correctly

---

### 3. Quay.io

**Test Case 3.1: Public repository**

```bash
# Quay.io public image
curl -X GET "http://localhost:5000/api/registries/quay/prometheus/prometheus/tags?page=1&pageSize=10"
```

**Expected Response:**
```json
{
  "tags": [
    {"name": "latest", "digest": "sha256:...", "lastModified": "2024-..."},
    {"name": "v2.45.0", "digest": "sha256:...", "lastModified": "2024-..."}
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 10,
    "hasNextPage": true
  }
}
```

**Frontend Test:**
- Navigate to `http://localhost:5173/?registry=quay`
- Enter `prometheus/prometheus`
- Verify tags display correctly

---

### 4. Google Container Registry (GCR)

**Test Case 4.1: Public GCR image**

```bash
# GCR public image (Kubernetes example)
curl -X GET "http://localhost:5000/api/registries/gcr/google-containers/pause/tags?page=1&pageSize=10"
```

**Expected Response:**
```json
{
  "tags": [
    {"name": "3.9", "digest": "sha256:...", "lastModified": "2024-..."},
    {"name": "latest", "digest": "sha256:...", "lastModified": "2024-..."}
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 10,
    "hasNextPage": false
  }
}
```

**Frontend Test:**
- Navigate to `http://localhost:5173/?registry=gcr`
- Enter `google-containers/pause`
- Verify tags display correctly

---

## Error Scenario Testing

### 5. Invalid Format Errors

**Test Case 5.1: Missing repository name**

```bash
curl -X GET "http://localhost:5000/api/registries/ghcr/microsoft/tags"
```

**Expected Response:** `400 Bad Request`
```json
{
  "error": {
    "code": "InvalidFormat",
    "message": "Repository path must be in format 'owner/image'",
    "details": "Path 'microsoft' is invalid"
  }
}
```

**Test Case 5.2: Invalid registry type**

```bash
curl -X GET "http://localhost:5000/api/registries/invalid-registry/owner/image/tags"
```

**Expected Response:** `400 Bad Request`
```json
{
  "error": {
    "code": "UnsupportedRegistry",
    "message": "Registry type 'invalid-registry' is not supported",
    "details": "Supported registries: ghcr, dockerhub, quay, gcr"
  }
}
```

**Frontend Test:**
- Manually navigate to `http://localhost:5173/?registry=fakeregistry`
- Enter `owner/image` and search
- Verify error message displays in UI

---

### 6. Not Found Errors

**Test Case 6.1: Non-existent repository**

```bash
curl -X GET "http://localhost:5000/api/registries/ghcr/nonexistent-owner-12345/nonexistent-image-67890/tags"
```

**Expected Response:** `404 Not Found`
```json
{
  "error": {
    "code": "NotFound",
    "message": "Repository 'nonexistent-owner-12345/nonexistent-image-67890' not found in registry 'ghcr'",
    "details": "The repository may be private or does not exist"
  }
}
```

**Frontend Test:**
- Select any registry
- Enter `fake-owner-xyz/fake-image-abc`
- Verify friendly error message in UI

---

### 7. Rate Limiting

**Test Case 7.1: Docker Hub rate limit (anonymous)**

```bash
# Make 100+ rapid requests to trigger Docker Hub rate limit
for i in {1..150}; do
  curl -X GET "http://localhost:5000/api/registries/dockerhub/library/nginx/tags?page=1&pageSize=1"
done
```

**Expected Response (after ~100 requests):** `429 Too Many Requests`
```json
{
  "error": {
    "code": "RateLimited",
    "message": "Rate limit exceeded for registry 'dockerhub'",
    "details": "Retry after 60 seconds. Consider authenticating for higher limits."
  }
}
```

---

### 8. Pagination Testing

**Test Case 8.1: Navigate through pages**

```bash
# Page 1
curl -X GET "http://localhost:5000/api/registries/ghcr/microsoft/dotnet/tags?page=1&pageSize=5"

# Verify hasNextPage: true

# Page 2
curl -X GET "http://localhost:5000/api/registries/ghcr/microsoft/dotnet/tags?page=2&pageSize=5"

# Verify different tags returned
```

**Frontend Test:**
- Select GHCR, enter `microsoft/dotnet`
- Use ag-grid pagination controls to navigate pages
- Verify:
  - Tags change per page
  - Page numbers update
  - "Next" button disabled on last page

---

### 9. URL State Management

**Test Case 9.1: Bookmarkable URLs**

```bash
# Navigate to this URL directly
http://localhost:5173/?registry=dockerhub&owner=library&image=nginx&page=2
```

**Expected Behavior:**
- Registry selector shows "Docker Hub"
- Input field shows "library/nginx"
- ag-grid displays page 2 of nginx tags
- All state restored from URL

**Test Case 9.2: URL updates on interaction**

- Start at `http://localhost:5173/`
- Select "Quay.io" from dropdown
- Enter `prometheus/prometheus`
- Click search
- Click "Next Page"

**Expected URL updates:**
```
http://localhost:5173/
→ http://localhost:5173/?registry=quay
→ http://localhost:5173/?registry=quay&owner=prometheus&image=prometheus
→ http://localhost:5173/?registry=quay&owner=prometheus&image=prometheus&page=1
→ http://localhost:5173/?registry=quay&owner=prometheus&image=prometheus&page=2
```

---

### 10. Clipboard Copy Functionality

**Test Case 10.1: Copy tag reference**

- Search for any image (e.g., GHCR `microsoft/dotnet`)
- Click copy icon next to a tag (e.g., `8.0`)

**Expected Clipboard Content:**
```
ghcr.io/microsoft/dotnet:8.0
```

**Test Case 10.2: Copy digest**

- Click copy icon next to digest value

**Expected Clipboard Content:**
```
sha256:abc123...
```

**Test Case 10.3: Registry-specific formats**

| Registry   | Image                  | Tag      | Expected Clipboard                        |
|------------|------------------------|----------|-------------------------------------------|
| GHCR       | microsoft/dotnet       | 8.0      | `ghcr.io/microsoft/dotnet:8.0`           |
| Docker Hub | library/nginx          | alpine   | `docker.io/library/nginx:alpine`         |
| Docker Hub | nginx (shorthand)      | alpine   | `docker.io/library/nginx:alpine`         |
| Quay       | prometheus/prometheus  | v2.45.0  | `quay.io/prometheus/prometheus:v2.45.0`  |
| GCR        | google-containers/pause| 3.9      | `gcr.io/google-containers/pause:3.9`     |

---

### 11. Backward Compatibility Verification

**Critical Test: Existing Frontend Must Work Unchanged**

**Test Case 11.1: Deploy new backend, keep old frontend**

1. Start backend with multi-registry support
2. Use old frontend (without registry selector)
3. Navigate to `http://localhost:5173/`
4. Enter `microsoft/dotnet` (as before)
5. Click search

**Expected:**
- Legacy endpoint `/api/images/microsoft/dotnet/tags` called
- Tags returned successfully (defaults to GHCR)
- Zero breaking changes

**Test Case 11.2: Existing E2E tests pass**

```bash
# Run existing E2E tests without modification
npm run test:e2e

# All tests in frontend/tests/e2e/*.spec.ts should pass
```

**Expected:** All existing tests pass with zero changes to test code

---

## Performance Testing

### 12. Response Time Verification

**Test Case 12.1: Response time under 2 seconds**

```bash
# Test each registry with timing
time curl -X GET "http://localhost:5000/api/registries/ghcr/microsoft/dotnet/tags?page=1&pageSize=10"
time curl -X GET "http://localhost:5000/api/registries/dockerhub/library/nginx/tags?page=1&pageSize=10"
time curl -X GET "http://localhost:5000/api/registries/quay/prometheus/prometheus/tags?page=1&pageSize=10"
time curl -X GET "http://localhost:5000/api/registries/gcr/google-containers/pause/tags?page=1&pageSize=10"
```

**Expected:** All requests complete in ≤2 seconds (success criteria)

---

## Configuration Testing

### 13. Registry Configuration Override

**Test Case 13.1: Custom Docker Hub URL**

Edit `appsettings.Development.json`:
```json
{
  "Registries": {
    "DockerHub": {
      "BaseUrl": "https://custom-docker-mirror.example.com"
    }
  }
}
```

Restart backend, then:
```bash
curl -X GET "http://localhost:5000/api/registries/dockerhub/library/nginx/tags?page=1&pageSize=10"
```

**Expected:** Request goes to custom mirror URL (verify in logs)

**Test Case 13.2: Environment variable override**

```bash
export Registries__Quay__BaseUrl=https://custom-quay.example.com
dotnet run

curl -X GET "http://localhost:5000/api/registries/quay/prometheus/prometheus/tags?page=1&pageSize=10"
```

**Expected:** Request goes to custom Quay URL

---

## Browser Compatibility Testing

### 14. Cross-Browser Frontend Tests

**Test manually in:**
- Chrome/Chromium
- Firefox
- Safari (if available)
- Edge

**For each browser, verify:**
- Registry selector dropdown works
- Image search works
- ag-grid renders correctly
- Pagination works
- Clipboard copy works
- URL state management works

---

## Automated Test Execution Summary

After manual quickstart testing, verify automated tests:

```bash
# Unit tests
cd backend/tests/unit
dotnet test

# Integration tests
cd backend/tests/integration
dotnet test

# Contract tests
cd backend/tests/contract
dotnet test

# E2E tests
cd frontend
npm run test:e2e
```

**Success Criteria:**
- All unit tests pass (validation, factory logic)
- All integration tests pass (real registry calls)
- All contract tests pass (OpenAPI spec compliance)
- All E2E tests pass (including existing unchanged tests)

---

## Quick Reference: Test Images

Use these known-good public images for testing:

| Registry   | Owner/Org          | Image          | Notes                          |
|------------|--------------------|----------------|--------------------------------|
| GHCR       | microsoft          | dotnet         | Official .NET images           |
| GHCR       | actions            | runner         | GitHub Actions runner          |
| Docker Hub | library            | nginx          | Official nginx (use shorthand) |
| Docker Hub | library            | alpine         | Official Alpine Linux          |
| Docker Hub | bitnami            | nginx          | User repository                |
| Quay       | prometheus         | prometheus     | Prometheus monitoring          |
| Quay       | coreos             | etcd           | etcd key-value store           |
| GCR        | google-containers  | pause          | Kubernetes pause container     |

---

## Troubleshooting

**Issue: "NotFound" error for known public image**

- **Cause:** Image path format incorrect for registry
- **Fix:** Check registry-specific path conventions:
  - Docker Hub: Official images need `library/` prefix
  - Others: Standard `owner/image` format

**Issue: Rate limit errors on Docker Hub**

- **Cause:** Anonymous Docker Hub limits ~100 requests/6 hours
- **Fix:** Add Docker Hub authentication in configuration (future enhancement)

**Issue: Frontend doesn't update URL on registry change**

- **Cause:** URL state management not wired correctly
- **Fix:** Verify `goto()` calls in `+page.svelte` update search params

**Issue: Clipboard copy not working**

- **Cause:** Browser security requires HTTPS for clipboard API
- **Fix:** Test on `localhost` (always allowed) or use HTTPS in production

---

## Completion Checklist

Mark each test scenario as completed:

- [ ] Test Case 1.1: GHCR legacy endpoint
- [ ] Test Case 1.2: GHCR new endpoint
- [ ] Test Case 2.1-2.4: Docker Hub (official + user repos)
- [ ] Test Case 3.1: Quay.io
- [ ] Test Case 4.1: GCR
- [ ] Test Case 5.1-5.2: Invalid format errors
- [ ] Test Case 6.1: Not found errors
- [ ] Test Case 7.1: Rate limiting
- [ ] Test Case 8.1: Pagination
- [ ] Test Case 9.1-9.2: URL state management
- [ ] Test Case 10.1-10.3: Clipboard copy
- [ ] Test Case 11.1-11.2: Backward compatibility
- [ ] Test Case 12.1: Performance (≤2s)
- [ ] Test Case 13.1-13.2: Configuration override
- [ ] Test Case 14: Cross-browser testing
- [ ] All automated tests pass

**When all items checked: Feature is ready for production deployment.**
