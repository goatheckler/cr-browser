# Data Model: Multi-Registry Container Tag Browser

**Feature**: 002-multi-registry-support  
**Date**: 2025-10-12  
**Status**: Draft

## Overview

This document defines the data entities, their relationships, validation rules, and state transitions for the multi-registry support feature. The model extends the existing GHCR-only implementation to support multiple container registries while maintaining backward compatibility.

---

## Core Entities

### 1. RegistryType (Enum)

**Purpose**: Identifier for supported container registries

**Definition**:
```csharp
public enum RegistryType
{
    Ghcr,       // GitHub Container Registry
    DockerHub,  // Docker Hub (registry-1.docker.io)
    Quay,       // Quay.io
    Gcr,        // Google Container Registry
    Ecr,        // Amazon Elastic Container Registry (future)
    Acr,        // Azure Container Registry (future)
    Harbor,     // Harbor self-hosted (future)
    Artifactory // JFrog Artifactory (future)
}
```

**Validation Rules**:
- Must be one of the defined enum values
- Case-insensitive string parsing supported: `"dockerhub"` → `RegistryType.DockerHub`
- Unknown values → 400 Bad Request with clear error message

**Serialization**:
- JSON: lowercase string (`"ghcr"`, `"dockerhub"`, `"quay"`, `"gcr"`)
- URL path: lowercase string (`/api/registries/dockerhub/...`)

**Default Value**: `RegistryType.Ghcr` (for backward compatibility)

---

### 2. RegistryConfiguration

**Purpose**: Configuration settings for a specific container registry

**Fields**:

| Field | Type | Required | Description | Example |
|-------|------|----------|-------------|---------|
| `BaseUrl` | string | Yes | Registry API base URL | `"https://ghcr.io"` |
| `TokenEndpoint` | string | Yes | OAuth2/token endpoint path | `"/token"` |
| `ServiceName` | string | Yes | Service identifier for token requests | `"ghcr.io"` |
| `Enabled` | bool | No | Whether registry is available | `true` |
| `DisplayName` | string | No | Human-readable name | `"GitHub Container Registry"` |
| `PathPrefix` | string? | No | Optional path prefix (e.g., `library/` for Docker Hub) | `null` |

**Validation Rules**:
- `BaseUrl` must be valid HTTPS URL
- `TokenEndpoint` must start with `/` or be absolute URL
- `ServiceName` must not be empty
- `Enabled` defaults to `true`

**Example**:
```json
{
  "baseUrl": "https://registry-1.docker.io",
  "tokenEndpoint": "https://auth.docker.io/token",
  "serviceName": "registry.docker.io",
  "enabled": true,
  "displayName": "Docker Hub"
}
```

**Relationships**:
- One RegistryConfiguration per RegistryType
- Loaded from `appsettings.json` at startup
- Injected into registry clients via dependency injection

---

### 3. RegistryRequest

**Purpose**: Input parameters for a tag listing request

**Fields**:

| Field | Type | Required | Description | Validation |
|-------|------|----------|-------------|------------|
| `RegistryType` | RegistryType | Yes | Target registry | Must be supported/enabled |
| `Owner` | string | Yes | Repository owner/org | 1-255 chars, alphanumeric + `-_` |
| `Image` | string | Yes | Image/repository name | 1-255 chars, alphanumeric + `-_./` |
| `PageSize` | int | No | Results per page | 1-500, default 100 |
| `Last` | string? | No | Pagination cursor | Optional, provided by previous response |

**Validation Rules**:
- `Owner`: 
  - Required (except Docker Hub official images where it may be `_` or empty)
  - Length: 1-255 characters
  - Pattern: `^[a-zA-Z0-9][a-zA-Z0-9_-]*$`
  - Lowercase conversion applied
  
- `Image`:
  - Required
  - Length: 1-255 characters
  - Pattern: `^[a-zA-Z0-9][a-zA-Z0-9_.-/]*$`
  - Lowercase conversion applied

- `PageSize`:
  - Range: 1-500
  - Default: 100
  - Caps to 500 even if larger value requested

- `Last`:
  - Optional
  - Format varies by registry (opaque cursor)
  - Not validated for format (passed through to registry)

**Registry-Specific Transformations**:

**Docker Hub**:
```csharp
// Official images: owner="_" or empty → "library"
if (string.IsNullOrEmpty(request.Owner) || request.Owner == "_")
{
    request.Owner = "library";
}
```

**Example**:
```json
{
  "registryType": "dockerhub",
  "owner": "library",
  "image": "nginx",
  "pageSize": 50,
  "last": null
}
```

---

### 4. RegistryResponse

**Purpose**: Result of a tag listing request

**Fields**:

| Field | Type | Description |
|-------|------|-------------|
| `Tags` | `IReadOnlyList<string>` | List of tag names |
| `NotFound` | bool | Repository doesn't exist |
| `Retryable` | bool | Transient error, can retry |
| `HasMore` | bool | More pages available |

**State Combinations**:

| Scenario | Tags | NotFound | Retryable | HasMore |
|----------|------|----------|-----------|---------|
| Success with tags | `["v1", "v2"]` | false | false | true/false |
| Success, empty repo | `[]` | false | false | false |
| Repository not found | `[]` | true | false | false |
| Rate limited (429) | `[]` | false | true | false |
| Server error (5xx) | `[]` | false | true | false |
| Auth required (401) | Internal retry → success or not found |

**Validation Rules**:
- Tags must be non-null (empty list allowed)
- Each tag string: 1-128 characters
- Tag pattern: `^[a-zA-Z0-9][a-zA-Z0-9_.-]*$`

**Example**:
```json
{
  "tags": ["latest", "v1.0.0", "v1.0.1", "main"],
  "notFound": false,
  "retryable": false,
  "hasMore": true
}
```

---

### 5. TagReference

**Purpose**: Fully qualified container image reference for copy-to-clipboard

**Fields**:

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `Registry` | string | Registry hostname | `"ghcr.io"` |
| `Owner` | string | Repository owner | `"microsoft"` |
| `Image` | string | Image name | `"dotnet-samples"` |
| `Tag` | string | Specific tag | `"latest"` |
| `FullReference` | string | Complete reference | `"ghcr.io/microsoft/dotnet-samples:latest"` |

**Formatting Rules**:

**GHCR**:
```
ghcr.io/{owner}/{image}:{tag}
Example: ghcr.io/microsoft/dotnet-samples:latest
```

**Docker Hub**:
```
docker.io/{owner}/{image}:{tag}
Example: docker.io/library/nginx:latest
Alternative (official images): {image}:{tag}
Decision: Always include docker.io prefix for clarity
```

**Quay.io**:
```
quay.io/{owner}/{image}:{tag}
Example: quay.io/prometheus/prometheus:v2.45.0
```

**GCR**:
```
gcr.io/{project}/{image}:{tag}
Example: gcr.io/distroless/base:latest
```

**Validation Rules**:
- All components required
- Owner may be empty for Docker Hub (converted to `library`)
- Tag cannot be empty (default to `latest` if omitted in UI)

**Example**:
```json
{
  "registry": "dockerhub",
  "owner": "library",
  "image": "nginx",
  "tag": "1.25.3",
  "fullReference": "docker.io/library/nginx:1.25.3"
}
```

---

## Interfaces & Contracts

### IContainerRegistryClient

**Purpose**: Abstract interface for all registry client implementations

**Methods**:

```csharp
public interface IContainerRegistryClient
{
    /// <summary>
    /// Lists tags for a repository with pagination support
    /// </summary>
    Task<RegistryResponse> ListTagsPageAsync(
        string owner, 
        string image, 
        int pageSize, 
        string? last, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets the registry type identifier
    /// </summary>
    RegistryType RegistryType { get; }
    
    /// <summary>
    /// Gets the registry's base URL
    /// </summary>
    string BaseUrl { get; }
    
    /// <summary>
    /// Formats a full image reference for copy-to-clipboard
    /// </summary>
    string FormatFullReference(string owner, string image, string tag);
}
```

**Implementation Requirements**:
- All methods must be thread-safe
- `ListTagsPageAsync` must handle authentication automatically
- Cancellation token must be honored
- Errors must be mapped to `RegistryResponse` states (NotFound, Retryable)

---

### IRegistryFactory

**Purpose**: Creates registry client instances based on registry type

**Methods**:

```csharp
public interface IRegistryFactory
{
    /// <summary>
    /// Creates a registry client for the specified type
    /// </summary>
    /// <exception cref="NotSupportedException">Registry type not supported/enabled</exception>
    IContainerRegistryClient CreateClient(RegistryType registryType);
    
    /// <summary>
    /// Gets all supported registry types
    /// </summary>
    IEnumerable<RegistryType> GetSupportedRegistries();
    
    /// <summary>
    /// Checks if a registry type is supported
    /// </summary>
    bool IsSupported(RegistryType registryType);
}
```

**Behavior**:
- `CreateClient`: Throws `NotSupportedException` if registry disabled/unknown
- `GetSupportedRegistries`: Returns only enabled registries from configuration
- `IsSupported`: Checks both enum validity and `Enabled` configuration flag

---

## Validation Rules Summary

### Repository Reference Validation

**Owner**:
- Pattern: `^[a-zA-Z0-9][a-zA-Z0-9_-]*$`
- Length: 1-255 characters
- Case: Converted to lowercase
- Special: Docker Hub allows `_` or empty → converted to `library`

**Image**:
- Pattern: `^[a-zA-Z0-9][a-zA-Z0-9_.-/]*$`
- Length: 1-255 characters
- Case: Converted to lowercase
- Allows `/` for nested repositories (e.g., `project/subproject/image`)

**Tag**:
- Pattern: `^[a-zA-Z0-9][a-zA-Z0-9_.-]*$`
- Length: 1-128 characters
- Case: Preserved (tags are case-sensitive)
- Common values: `latest`, `main`, semantic versions (`v1.2.3`)

### Error Validation

**HTTP Status Mapping**:
- 400 Bad Request → Invalid format (validation failure)
- 401 Unauthorized → Trigger token acquisition, retry
- 404 Not Found → `NotFound = true`
- 429 Too Many Requests → `Retryable = true`
- 5xx Server Error → `Retryable = true`

---

## State Transitions

### Authentication Flow

```
[Start] → Send Request (no auth)
    ↓
[Receive 401] → Acquire Token from registry
    ↓
[Token Acquired] → Retry Request with Bearer token
    ↓
[200 OK] → Parse Response → Return Tags
    ↓
[401 Again / Token Failed] → Return NotFound (treat as private/non-existent)
```

**States**:
1. `Unauthenticated`: Initial request without token
2. `TokenAcquisition`: Fetching anonymous/public token
3. `Authenticated`: Retry with Bearer token
4. `Success`: Tags retrieved
5. `NotFound`: Repository doesn't exist or is private
6. `Error`: Transient or permanent failure

### Pagination State

```
[Initial Request] → last = null
    ↓
[Receive Response] → Check Link header or page size
    ↓
[Has Link rel="next"] → hasMore = true, extract last cursor
[No Link, tags.count == pageSize] → hasMore = true (heuristic)
[No Link, tags.count < pageSize] → hasMore = false
    ↓
[Next Request] → last = cursor from previous response
```

---

## Registry-Specific Variations

### GHCR (GitHub Container Registry)

**Repository Path**: `{owner}/{image}`
**Token Endpoint**: `https://ghcr.io/token?scope=repository:{repo}:pull&service=ghcr.io`
**Special Rules**: None
**Example**: `microsoft/dotnet-samples`

### Docker Hub

**Repository Path**: 
- User images: `{owner}/{image}`
- Official images: `library/{image}`

**Token Endpoint**: `https://auth.docker.io/token?service=registry.docker.io&scope=repository:{repo}:pull`

**Special Rules**:
- Owner `_` or empty → converted to `library`
- Base URL: `https://registry-1.docker.io` (not `docker.io`)

**Example**: `library/nginx`, `nginx/nginx`

### Quay.io

**Repository Path**: `{owner}/{image}`
**Token Endpoint**: `https://quay.io/v2/auth?service=quay.io&scope=repository:{repo}:pull`
**Special Rules**: None (standard OCI)
**Example**: `prometheus/prometheus`

### Google Container Registry (GCR)

**Repository Path**: `{project}/{image}`
**Token Endpoint**: `https://gcr.io/v2/token?service=gcr.io&scope=repository:{repo}:pull`
**Special Rules**: Often allows anonymous access for public images
**Example**: `distroless/base`

---

## Relationships Diagram

```
RegistryRequest
    ├── RegistryType (enum)
    └── Owner + Image → Repository Path

RegistryConfiguration
    ├── BaseUrl
    ├── TokenEndpoint
    └── ServiceName

IRegistryFactory
    └── CreateClient(RegistryType)
            └── IContainerRegistryClient
                    ├── GhcrClient
                    ├── DockerHubClient
                    ├── QuayClient
                    └── GcrClient

IContainerRegistryClient.ListTagsPageAsync()
    └── Returns: RegistryResponse
            ├── Tags (list)
            ├── NotFound (bool)
            ├── Retryable (bool)
            └── HasMore (bool)

TagReference
    ├── Registry
    ├── Owner
    ├── Image
    ├── Tag
    └── FullReference (formatted)
```

---

## Database Schema

**N/A** - This feature is stateless. No database storage required.

All data is transient:
- Configuration loaded from `appsettings.json` at startup
- API requests are stateless
- Responses not cached (future enhancement may add in-memory cache)

---

## Migration from Existing Model

### Existing Model (GHCR-only)

```csharp
// Current GhcrClient interface
public interface IGhcrClient
{
    Task<(IReadOnlyList<string> Tags, bool NotFound, bool Retryable, bool HasMore)> 
        ListTagsPageAsync(string owner, string image, int pageSize, string? last, CancellationToken ct);
}
```

### New Model (Multi-registry)

```csharp
// New abstraction
public interface IContainerRegistryClient
{
    Task<RegistryResponse> ListTagsPageAsync(string owner, string image, int pageSize, string? last, CancellationToken ct);
    RegistryType RegistryType { get; }
    string BaseUrl { get; }
    string FormatFullReference(string owner, string image, string tag);
}

// RegistryResponse replaces tuple
public sealed record RegistryResponse(
    IReadOnlyList<string> Tags,
    bool NotFound,
    bool Retryable,
    bool HasMore
);
```

**Migration Strategy**:
1. Create `RegistryResponse` record type
2. Update `IGhcrClient` return type to use `RegistryResponse` (or keep tuple for backward compat)
3. Extract shared logic to `OciRegistryClientBase`
4. Make `GhcrClient` implement `IContainerRegistryClient`
5. Existing API endpoint continues working (no breaking changes)

---

## Extension Points

### Adding New Registry

**Required Steps**:
1. Add new value to `RegistryType` enum
2. Create configuration section in `appsettings.json`
3. Create new client class inheriting from `OciRegistryClientBase`
4. Override registry-specific methods:
   - `AcquireTokenAsync`
   - `FormatRepositoryPath` (if special formatting needed)
   - `FormatFullReference`
5. Update `RegistryFactory` to instantiate new client
6. Add integration tests with public image from new registry

**Example** (adding Harbor):
```csharp
public class HarborClient : OciRegistryClientBase
{
    public HarborClient(HttpClient http, RegistryConfiguration config)
        : base(http, config, RegistryType.Harbor) { }
    
    protected override async Task<string?> AcquireTokenAsync(string repository, CancellationToken ct)
    {
        // Harbor-specific token acquisition
        var tokenUrl = $"{_config.TokenEndpoint}?service={_config.ServiceName}&scope=repository:{repository}:pull";
        // ... implementation
    }
}
```

---

## Open Questions

1. **Tag Metadata**: Should `RegistryResponse` include metadata (size, age) or remain tags-only?
   - **Current Decision**: Tags-only for MVP (deferred metadata to future enhancement)

2. **Caching Strategy**: Should `RegistryResponse` be cached? TTL?
   - **Current Decision**: No caching in MVP (stateless API)

3. **Registry Aliases**: Should we support user-defined registry aliases?
   - **Current Decision**: No, use predefined `RegistryType` enum

4. **Partial Results**: If a registry returns partial tags (some metadata missing), how to handle?
   - **Current Decision**: Not applicable for MVP (tags are simple strings)

---

**Data Model Complete**: All entities, relationships, and validation rules defined. Ready for contract generation.
