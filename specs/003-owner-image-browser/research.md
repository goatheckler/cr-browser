# Feature 003: Owner Image Browser - Research Document

## Executive Summary

This document analyzes the feasibility of implementing a "Browse Images" feature that lists all container images published by an owner across the four supported registries (GHCR, Docker Hub, Quay.io, GCR).

**Key Finding**: Uniform implementation across all registries is **PARTIALLY FEASIBLE** with significant caveats per registry.

## Research Findings by Registry

### 1. GitHub Container Registry (GHCR)

**API Availability**: ✅ **YES** - GitHub Packages REST API  

**Endpoints**:
- Users: `GET https://api.github.com/users/{username}/packages?package_type=container`
- Organizations: `GET https://api.github.com/orgs/{org}/packages?package_type=container`

**Response Data**:
```json
{
  "id": 123456,
  "name": "package-name",
  "package_type": "container",
  "owner": { "login": "owner-name" },
  "visibility": "public",
  "url": "https://api.github.com/...",
  "html_url": "https://github.com/...",
  "created_at": "2023-01-01T00:00:00Z",
  "updated_at": "2023-01-01T00:00:00Z"
}
```

**Authentication**:
- **CRITICAL LIMITATION**: Requires GitHub Personal Access Token (PAT) with `read:packages` scope
- **Different from GHCR registry token** used for tag listing
- Unauthenticated: 60 requests/hour
- Authenticated: 5,000 requests/hour

**Implications**:
- ✅ Can list all images for an owner
- ❌ Requires separate GitHub OAuth/PAT authentication
- ❌ Not available via anonymous OCI registry access
- ⚠️ User must provide GitHub token separately from registry credentials

---

### 2. Docker Hub

**API Availability**: ✅ **YES** - Public Docker Hub API v2

**Endpoints**:
- Public endpoint: `GET https://hub.docker.com/v2/repositories/{namespace}/?page_size={n}`
- Pagination: `next` field in response for subsequent pages

**Response Data**:
```json
{
  "count": 178,
  "next": "https://hub.docker.com/v2/repositories/library/?page=2&page_size=10",
  "results": [
    {
      "name": "image-name",
      "namespace": "owner-name",
      "repository_type": "image",
      "description": "...",
      "is_private": false,
      "star_count": 1234,
      "pull_count": 567890,
      "last_updated": "2025-01-01T00:00:00Z"
    }
  ]
}
```

**Authentication**:
- ✅ **NO AUTH REQUIRED** for listing public repositories
- Unauthenticated access works for public namespaces
- Private repos would require Docker Hub token

**Implications**:
- ✅ Easy to implement for public repositories
- ✅ No additional authentication required
- ✅ Rich metadata available (stars, pulls, descriptions)
- ⚠️ Cannot list private repositories without auth

---

### 3. Quay.io

**API Availability**: ✅ **YES** - Quay.io REST API v1

**Endpoints**:
- List by namespace: `GET https://quay.io/api/v1/repository?namespace={namespace}&public=true`
- Documentation: https://docs.quay.io/api/

**Response Data**:
```json
{
  "repositories": [
    {
      "namespace": "coreos",
      "name": "etcd",
      "description": "Built releases of https://github.com/coreos/etcd",
      "is_public": true,
      "kind": "image",
      "state": "NORMAL",
      "quota_report": {
        "quota_bytes": 10154456314,
        "configured_quota": null
      }
    }
  ]
}
```

**Authentication**:
- ✅ **NO AUTH REQUIRED** for listing public repositories  
- OAuth 2 token with `repo:read` scope for private repos
- Public repositories accessible without authentication

**Implications**:
- ✅ Easy to implement for public repositories
- ✅ No additional authentication required for public repos
- ✅ Clean API with good metadata
- ⚠️ Requires OAuth 2 setup for private repositories

---

### 4. Google Container Registry (GCR)

**API Availability**: ⚠️ **COMPLEX** - Multiple API approaches

**Option A: OCI Distribution Spec `_catalog` Endpoint**:
- Endpoint: `GET https://gcr.io/v2/_catalog`
- **Problem**: Lists ALL repositories in registry, not filtered by owner
- **Problem**: Requires authentication
- ❌ **NOT SUITABLE** - Cannot filter by owner/namespace

**Option B: Google Artifact Registry REST API**:
- Endpoint: `GET https://artifactregistry.googleapis.com/v1/projects/{project}/locations/{location}/repositories/{repo}/dockerImages`
- **Problem**: Requires knowing the GCP project ID
- **Problem**: GCR uses project-based organization, not username-based
- **Problem**: Requires Google Cloud authentication (service account, OAuth)

**Option C: Google Cloud Storage (GCS) approach**:
- GCR stores images in GCS buckets
- Buckets named: `gcr.io/{project-id}`, `us.gcr.io/{project-id}`, etc.
- Could potentially list bucket contents
- ❌ **NOT RECOMMENDED** - Circumvents intended API

**Authentication**:
- Requires Google Cloud credentials (service account JSON or OAuth)
- More complex than other registries
- Project-scoped, not user/org-scoped

**Implications**:
- ❌ **MAJOR LIMITATION**: GCR uses project-based namespacing, not username/org
- ❌ Cannot easily map "owner" to GCP project without additional context
- ❌ Would require users to input GCP project ID instead of owner name
- ⚠️ Fundamentally different architecture from other 3 registries

---

## Architectural Considerations

### Data Model Differences

| Registry | Owner Type | Namespace Model | Auth Model |
|----------|-----------|-----------------|------------|
| **GHCR** | GitHub user/org | `ghcr.io/owner/image` | GitHub PAT (read:packages) |
| **Docker Hub** | Docker namespace | `docker.io/owner/image` | None (public) / Docker token |
| **Quay.io** | Quay namespace | `quay.io/owner/image` | None (public) / OAuth 2 |
| **GCR** | GCP Project | `gcr.io/project/image` | GCP Service Account / OAuth |

### Common Challenges

1. **Authentication Fragmentation**:
   - GHCR needs GitHub token (not registry token)
   - GCR needs GCP credentials  
   - Docker Hub & Quay work without auth for public repos

2. **Owner Identification**:
   - GHCR/Docker/Quay: Direct owner/namespace concept
   - GCR: Project-based, requires project ID

3. **API Rate Limits**:
   - GHCR: 60/hr unauthenticated, 5000/hr authenticated
   - Docker Hub: Unknown (likely permissive for public API)
   - Quay: Unknown (likely permissive)
   - GCR: Standard GCP quotas

---

## Implementation Feasibility Analysis

### Scenario 1: Public Repositories Only (Recommended)

**Feasible Registries**: Docker Hub, Quay.io, GHCR (with token), GCR (❌)

**Implementation**:
- ✅ Docker Hub: Direct API call, no auth
- ✅ Quay.io: Direct API call, no auth  
- ⚠️ GHCR: Requires GitHub PAT input from user
- ❌ GCR: Not feasible without project ID

**User Experience**:
```
[Browse Images] button
→ For Docker/Quay: Works immediately
→ For GHCR: Prompts "Enter GitHub Token to browse packages"
→ For GCR: Shows error "GCR requires project ID, not owner name"
```

### Scenario 2: Include Private Repositories

**Feasible Registries**: Docker Hub (with token), Quay (with OAuth), GHCR (with PAT), GCR (❌)

**Implementation Complexity**: HIGH
- Each registry needs different auth flow
- Token storage/management required
- Security implications of storing multiple tokens

### Scenario 3: GCR Alternative - Use Project ID

**Feasible**: ✅ YES (but changes UX paradigm)

**Implementation**:
- Change UX to ask for "Project ID" instead of "Owner" for GCR
- Use Artifact Registry API with project scope
- Requires different form fields per registry type

**User Experience**:
```
Registry: GCR
Owner/Project: [Input changes to "GCP Project ID"]
[Browse Images] → Lists images in that project
```

---

## Recommended Implementation Approach

### Phase 1: Docker Hub + Quay.io (Simple, No Auth)

**Scope**: 
- Implement "Browse Images" for Docker Hub and Quay.io
- Public repositories only
- No additional authentication required

**Benefits**:
- Works out of the box
- No security concerns
- Simple implementation

**Limitations**:
- Only 2 of 4 registries supported initially

### Phase 2: GHCR with GitHub Token

**Scope**:
- Add GHCR support
- Require user to provide GitHub PAT
- Store token securely (browser localStorage or session)

**Benefits**:
- Enables GHCR image browsing
- User has full control over token

**Challenges**:
- UX: Extra step to input token
- Security: Token storage considerations
- Education: Users must understand GitHub PAT != GHCR registry token

### Phase 3: GCR with Project-Based UX (Future)

**Scope**:
- Add GCR support with different UX
- Accept "Project ID" instead of "Owner"
- Explain GCR's project-based model

**Challenges**:
- Different mental model from other registries
- Requires GCP authentication setup
- More complex user education

---

## Data Returned by Each Registry

### Common Fields Available:
- ✅ Image/Repository name
- ✅ Owner/Namespace
- ✅ Last updated timestamp
- ✅ Public/Private visibility

### Registry-Specific Fields:

**Docker Hub**:
- Star count
- Pull count  
- Description

**Quay.io**:
- Repository state
- Storage quota usage

**GHCR**:
- GitHub package URL
- Created/Updated timestamps

**GCR**:
- Artifact Registry metadata
- Image digest information

---

## Security Considerations

1. **Token Storage**:
   - GitHub PAT has broad permissions beyond just packages
   - Must be stored securely (HTTPS only, secure cookie/localStorage)
   - Should support token revocation

2. **Rate Limiting**:
   - Implement client-side caching
   - Respect API rate limits
   - Show appropriate error messages

3. **CORS/Proxy**:
   - Some APIs may require backend proxy
   - Direct browser requests may fail due to CORS

---

## Recommended Specification for Feature 003

### Minimal Viable Product (MVP)

**Scope**: Docker Hub + Quay.io only

**Rationale**:
- No authentication complexity
- Uniform UX across both registries
- Immediate value for most common public registries
- Establishes pattern for future expansion

**UI Behavior**:
1. User enters `owner` in existing field
2. Clicks "Browse Images" button
3. For GHCR/GCR: Show tooltip "Coming soon - requires additional setup"
4. For Docker Hub/Quay: Show modal/page with image list
5. Display: Owner/Image name, Last Updated
6. Click image → populates main form → fetches tags

### Future Enhancement

**Phase 2**: Add GHCR with token input flow  
**Phase 3**: Add GCR with project-based UX

---

## Open Questions

1. **UI/UX**:
   - Modal dialog vs. dedicated page for browsing?
   - How to display 100+ images (pagination)?
   - Inline search/filter on image list?

2. **Caching**:
   - Cache image lists client-side?
   - Expiration strategy?

3. **Error Handling**:
   - Owner not found?
   - API unavailable?
   - Rate limit exceeded?

4. **GHCR Token Flow**:
   - One-time token input?
   - Remember token (security implications)?
   - Link to GitHub PAT creation docs?

---

## Conclusion

**✅ FEASIBLE** for Docker Hub and Quay.io with no authentication  
**⚠️ FEASIBLE** for GHCR with additional GitHub PAT input  
**❌ NOT FEASIBLE** for GCR without changing owner → project ID paradigm

**RECOMMENDATION**: Implement MVP with Docker Hub + Quay.io support, clearly indicating GHCR/GCR support coming in future phases.
