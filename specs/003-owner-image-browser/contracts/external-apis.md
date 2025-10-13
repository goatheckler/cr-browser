# External Registry APIs

**Feature**: 003-owner-image-browser  
**Purpose**: Document external APIs consumed by the frontend

---

## Overview

The Owner Image Browser feature consumes public APIs from four container registries. All API calls are made **directly from the browser** (frontend) with no backend proxy.

---

## 1. Docker Hub API

### List Repositories by Namespace

**Endpoint**: `GET https://hub.docker.com/v2/repositories/{namespace}/`

**Parameters**:
- `namespace` (path): Organization or user namespace (e.g., "library", "nginx")
- `page_size` (query, optional): Number of results per page (default: 25, max: 100)
- `page` (query, optional): Page number (1-based)

**Authentication**: None required for public repositories

**Request Example**:
```
GET https://hub.docker.com/v2/repositories/library/?page_size=25
```

**Response Example**:
```json
{
  "count": 178,
  "next": "https://hub.docker.com/v2/repositories/library/?page=2&page_size=25",
  "previous": null,
  "results": [
    {
      "name": "nginx",
      "namespace": "library",
      "repository_type": "image",
      "description": "Official build of Nginx.",
      "is_private": false,
      "star_count": 19234,
      "pull_count": 5678901234,
      "last_updated": "2025-10-11T12:34:56.789Z"
    }
  ]
}
```

**Response Fields** (relevant):
- `count`: Total number of repositories
- `next`: URL for next page (null if last page)
- `results[].name`: Repository name
- `results[].namespace`: Owner/namespace
- `results[].description`: Repository description
- `results[].star_count`: Number of stars
- `results[].pull_count`: Total pulls
- `results[].last_updated`: ISO 8601 timestamp

**Pagination**: URL-based (use `next` field)

**Rate Limits**: 
- Unauthenticated: ~100 requests per minute
- No explicit rate limit headers

**Error Responses**:
- `404`: Namespace not found
- `5xx`: Server errors

---

## 2. Quay.io API

### List Repositories by Namespace

**Endpoint**: `GET https://quay.io/api/v1/repository`

**Parameters**:
- `namespace` (query): Organization or user namespace
- `public` (query, optional): Filter for public repositories only (default: false)

**Authentication**: None required for public repositories

**Request Example**:
```
GET https://quay.io/api/v1/repository?namespace=coreos&public=true
```

**Response Example**:
```json
{
  "repositories": [
    {
      "namespace": "coreos",
      "name": "etcd",
      "description": "Distributed reliable key-value store",
      "is_public": true,
      "kind": "image",
      "state": "NORMAL",
      "last_modified": 1728654321
    }
  ]
}
```

**Response Fields** (relevant):
- `repositories[].namespace`: Owner/namespace
- `repositories[].name`: Repository name
- `repositories[].description`: Repository description
- `repositories[].is_public`: Visibility flag
- `repositories[].state`: Repository state (NORMAL, READ_ONLY, MIRROR)
- `repositories[].last_modified`: Unix timestamp (seconds)

**Pagination**: Not documented for namespace queries (likely returns all results)

**Rate Limits**: Not publicly documented

**Error Responses**:
- `400`: Invalid namespace
- `404`: Namespace not found

---

## 3. GitHub Packages API (GHCR)

### List User Packages

**Endpoint**: `GET https://api.github.com/users/{username}/packages`

**Parameters**:
- `username` (path): GitHub username
- `package_type` (query): Filter by package type (use `container`)
- `per_page` (query, optional): Results per page (max: 100)
- `page` (query, optional): Page number

**Authentication**: **REQUIRED** - GitHub Personal Access Token (PAT) with `read:packages` scope

**Headers**:
```
Authorization: Bearer ghp_xxxxxxxxxxxxxxxxxxxx
Accept: application/vnd.github+json
X-GitHub-Api-Version: 2022-11-28
```

**Request Example**:
```
GET https://api.github.com/users/octocat/packages?package_type=container
```

**Response Example**:
```json
[
  {
    "id": 123456,
    "name": "hello-world",
    "package_type": "container",
    "owner": {
      "login": "octocat",
      "id": 1,
      "type": "User"
    },
    "visibility": "public",
    "url": "https://api.github.com/users/octocat/packages/container/hello-world",
    "html_url": "https://github.com/users/octocat/packages/container/hello-world",
    "created_at": "2023-01-15T10:30:00Z",
    "updated_at": "2025-10-10T14:22:00Z"
  }
]
```

**Alternative Endpoint for Organizations**:
```
GET https://api.github.com/orgs/{org}/packages?package_type=container
```

**Response Fields** (relevant):
- `id`: Package ID
- `name`: Package name (image name)
- `owner.login`: Owner username/org name
- `visibility`: public, private, or internal
- `html_url`: Link to GitHub package page
- `created_at`: ISO 8601 timestamp
- `updated_at`: ISO 8601 timestamp

**Pagination**: Link header with `rel="next"`

**Rate Limits**:
- Unauthenticated: 60 requests/hour (per IP)
- Authenticated: 5,000 requests/hour (per PAT)
- Headers: `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset`

**Error Responses**:
- `401`: Invalid or missing authentication
- `403`: Token lacks required scopes or rate limited
- `404`: User/org not found

**Token Generation**:
- URL: https://github.com/settings/tokens/new
- Required scope: `read:packages`
- Token format: `ghp_` + 36 alphanumeric characters

---

## 4. Google Container Registry (GCR)

### Project-Based Model

**Important**: GCR uses a **project-based** organization model, not username/owner.

**Catalog Endpoint**: `GET https://gcr.io/v2/{project-id}/_catalog`

**Parameters**:
- `project-id` (path): GCP project ID (e.g., "google-containers")

**Authentication**: 
- Public projects: May work unauthenticated
- Private projects: Requires GCP service account or OAuth

**Response Example**:
```json
{
  "repositories": [
    "pause",
    "kube-apiserver",
    "etcd"
  ]
}
```

**MVP Decision**: 
- **Collect project ID in form** (instead of "owner")
- **Display help text** explaining GCR's model
- **Do not implement listing** in initial release (requires complex GCP auth)
- User can manually enter project ID and image name

**Future Enhancement**: Implement GCP OAuth flow for authenticated listing

---

## CORS Considerations

### Supported (Frontend can call directly)
- ✅ **Docker Hub**: CORS headers present for public API
- ✅ **Quay.io**: CORS headers present for public API
- ✅ **GitHub API**: CORS headers present

### Potentially Restricted
- ⚠️ **GCR**: CORS support varies by project configuration

---

## Error Handling Matrix

| Registry | 404 (Not Found) | 401/403 (Auth) | 429 (Rate Limit) | Network Error |
|----------|----------------|----------------|------------------|---------------|
| Docker Hub | Invalid namespace | N/A (public) | No specific header | Retry with backoff |
| Quay.io | Invalid namespace | N/A (public) | Unknown behavior | Retry with backoff |
| GHCR | User/org not found | Invalid/missing PAT | `X-RateLimit-Reset` header | Retry with backoff |
| GCR | Project not found | Invalid/missing auth | Unknown | Retry with backoff |

---

## Security Considerations

### GitHub PAT Handling
- **Storage**: Browser localStorage only (HTTPS context required)
- **Transmission**: Only to `api.github.com`, never to backend
- **Validation**: Test with `GET /user` endpoint before use
- **Expiration**: No programmatic expiration detection (user must refresh when invalid)
- **Scope**: Minimum required: `read:packages`

### Token Storage Keys
```typescript
const STORAGE_KEY = 'cr-browser:ghcr:pat';
```

### HTTPS Enforcement
Check before storing tokens:
```typescript
if (window.location.protocol !== 'https:' && window.location.hostname !== 'localhost') {
  throw new Error('Token storage requires HTTPS');
}
```

---

## Testing Endpoints

### Public Test Data

**Docker Hub**:
- Namespace: `library` (official images, 100+ repos)
- Namespace: `nginx` (single org, few repos)

**Quay.io**:
- Namespace: `coreos` (popular org with public repos)
- Namespace: `prometheus` (monitoring tools)

**GHCR**:
- User: Requires valid GitHub PAT (cannot test without auth)
- Org: `microsoft`, `docker`, `github` (popular orgs, may have public packages)

**GCR**:
- Project: `google-containers` (Kubernetes images)
- Project: `distroless` (Google distroless images)

---
