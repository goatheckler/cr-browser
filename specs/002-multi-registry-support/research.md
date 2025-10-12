# Research: Multi-Registry Container Tag Browser

**Feature**: 002-multi-registry-support  
**Date**: 2025-10-12  
**Status**: Complete

## Overview

This document consolidates research findings for implementing multi-registry support in ghcr-browser. The primary goal is to determine how to extend the existing GHCR-only implementation to support Docker Hub, Quay.io, Google Container Registry, and other OCI-compliant registries.

---

## 1. OCI Distribution Specification Deep Dive

### Decision
Use the OCI Distribution Specification v1.0+ as the foundation for all registry clients. All target registries implement this standard, making tag listing portable across registries.

### Rationale
- **Standardization**: Docker Hub, Quay, GCR, GHCR, Harbor, ACR, and ECR all implement the OCI Distribution Spec
- **Existing Compliance**: Current `GhcrClient` already uses OCI-standard endpoints (`/v2/{repository}/tags/list`)
- **Minimal Variation**: Differences lie primarily in authentication, not tag listing logic
- **Future-Proof**: New registries adopting OCI spec work automatically with base implementation

### Key Findings

**Standard Tag Listing Endpoint**:
```
GET /v2/{repository}/tags/list
Optional query params: n={pageSize}, last={lastTag}
```

**Standard Response Format**:
```json
{
  "name": "owner/image",
  "tags": ["v1.0", "v2.0", "latest"]
}
```

**Pagination**:
- Link headers: `Link: </v2/{repo}/tags/list?n=100&last=v2.0>; rel="next"`
- Fallback: If `tags.length == pageSize`, assume more pages exist

**Error Responses**:
- 401 Unauthorized: Authentication required
- 404 Not Found: Repository doesn't exist
- 429 Too Many Requests: Rate limited
- 5xx: Registry transient errors

### Alternatives Considered
- **Registry-Specific APIs**: Rejected - each registry has custom APIs but all support OCI standard
- **Docker Registry HTTP API V2**: Rejected - superseded by OCI Distribution Spec

### Implementation Notes
- Extract common OCI logic into `OciRegistryClientBase`
- Reuse existing tag parsing, pagination, and error handling from `GhcrClient.cs:59-91`
- Registry-specific clients override only authentication and base URL configuration

---

## 2. Registry Authentication Patterns

### Decision
Implement per-registry token acquisition with registry-specific token endpoints and parameters. Use abstract method in base class for token acquisition, allowing each registry to customize.

### Rationale
- All registries use Bearer token authentication but with different token endpoints
- GHCR pattern: `GET {baseUrl}/token?scope=repository:{repo}:pull&service=ghcr.io`
- Pattern is extensible to other registries with minimal customization
- Anonymous access attempted first, token acquired only on 401 (existing pattern works well)

### Registry-Specific Auth Patterns

#### GitHub Container Registry (GHCR)
**Current Implementation** (GhcrClient.cs:93-107):
```
Token URL: https://ghcr.io/token
Parameters: scope=repository:{repository}:pull&service=ghcr.io
Response: { "token": "..." }
```
**Status**: ✅ Already implemented and working

#### Docker Hub
**Token URL**: `https://auth.docker.io/token`
**Parameters**: 
- `service=registry.docker.io`
- `scope=repository:{repository}:pull`
- For official images: repository = `library/{image}`

**Example**:
```
GET https://auth.docker.io/token?service=registry.docker.io&scope=repository:library/nginx:pull
Response: { "token": "...", "access_token": "...", "expires_in": 300 }
```

**Special Cases**:
- Official images: `library/nginx` not just `nginx`
- User images: `{username}/{image}` as expected
- Base URL: `https://registry-1.docker.io` (not docker.io)

#### Quay.io
**Token URL**: `https://quay.io/v2/auth`
**Parameters**: 
- `service=quay.io`
- `scope=repository:{repository}:pull`

**Example**:
```
GET https://quay.io/v2/auth?service=quay.io&scope=repository:prometheus/prometheus:pull
Response: { "token": "..." }
```

**Notes**: Very similar to GHCR, minimal customization needed

#### Google Container Registry (GCR)
**Token URL**: `https://gcr.io/v2/token`
**Parameters**: 
- `service=gcr.io`
- `scope=repository:{repository}:pull`

**Anonymous Access**: GCR often allows anonymous access for public repos
**Example**:
```
GET https://gcr.io/v2/token?service=gcr.io&scope=repository:distroless/base:pull
Response: { "token": "..." }
```

**Notes**: Similar pattern to GHCR/Quay

#### Azure Container Registry (ACR) - Future Phase
**Auth**: Azure AD OAuth2 or username/password
**Complexity**: Higher - requires Azure SDK integration
**Recommendation**: Defer to Phase 2 or 3

#### Amazon ECR - Future Phase
**Auth**: AWS SigV4 signing
**Complexity**: Higher - requires AWS SDK integration
**Recommendation**: Defer to Phase 2 or 3

### Alternatives Considered
- **Unified Token Service**: Rejected - each registry has its own token endpoint
- **Pre-authenticated Requests**: Rejected - wastes tokens on public repos, increases latency
- **Persistent Token Cache**: Considered for future - current stateless approach simpler

### Implementation Notes

**Base Class Pattern**:
```csharp
public abstract class OciRegistryClientBase : IContainerRegistryClient
{
    protected abstract Task<string?> AcquireTokenAsync(string repository, CancellationToken ct);
    protected abstract string GetTokenServiceName();
    protected abstract Uri GetTokenEndpoint();
}
```

**Per-Registry Override**:
```csharp
public class DockerHubClient : OciRegistryClientBase
{
    protected override string GetTokenServiceName() => "registry.docker.io";
    protected override Uri GetTokenEndpoint() => new Uri("https://auth.docker.io/token");
    
    // Optional: Override repository formatting
    protected override string FormatRepositoryPath(string owner, string image)
    {
        // Handle "library/" prefix for official images
        if (IsOfficialImage(owner))
            return $"library/{image}";
        return $"{owner}/{image}";
    }
}
```

---

## 3. Repository Path Conventions

### Decision
Implement per-registry repository path formatting with base class providing default behavior and registry-specific overrides for special cases.

### Rationale
- Most registries use `{owner}/{image}` format
- Docker Hub has special case: official images use `library/{image}`
- GCR may include project ID: `{project}/{image}`
- Abstraction allows registry-specific logic without polluting common code

### Registry-Specific Path Formats

| Registry | Format | Example | Notes |
|----------|--------|---------|-------|
| GHCR | `{owner}/{image}` | `microsoft/dotnet-samples` | Standard OCI format |
| Docker Hub (user) | `{owner}/{image}` | `nginx/nginx` | Standard format |
| Docker Hub (official) | `library/{image}` | `library/nginx` | Special prefix required |
| Quay.io | `{owner}/{image}` | `prometheus/prometheus` | Standard format |
| GCR | `{project}/{image}` | `distroless/base` | Project acts as owner |
| ACR | `{registry}/{image}` | May omit owner for single-tenant | Varies by config |

### Copy-to-Clipboard Formats

Users expect fully qualified image references when copying:

| Registry | Copy Format | Example |
|----------|-------------|---------|
| GHCR | `ghcr.io/{owner}/{image}:{tag}` | `ghcr.io/microsoft/dotnet-samples:latest` |
| Docker Hub | `docker.io/{path}:{tag}` or just `{path}:{tag}` | `docker.io/library/nginx:latest` or `nginx:latest` |
| Quay | `quay.io/{owner}/{image}:{tag}` | `quay.io/prometheus/prometheus:v2.45.0` |
| GCR | `gcr.io/{project}/{image}:{tag}` | `gcr.io/distroless/base:latest` |

**Decision**: Always include registry prefix for clarity (even for Docker Hub)

### Alternatives Considered
- **User Configurable Format**: Rejected - adds UI complexity, most users expect standard format
- **Omit Docker Hub Prefix**: Considered (common convention) but rejected for consistency
- **Smart Detection**: Rejected - explicit registry selection clearer than parsing URLs

### Implementation Notes

**Base Class**:
```csharp
public abstract class OciRegistryClientBase
{
    protected virtual string FormatRepositoryPath(string owner, string image)
    {
        return $"{owner}/{image}".ToLowerInvariant();
    }
    
    public abstract string FormatFullReference(string owner, string image, string tag);
}
```

**Docker Hub Override**:
```csharp
protected override string FormatRepositoryPath(string owner, string image)
{
    // Docker Hub official images have special prefix
    if (string.IsNullOrEmpty(owner) || owner.Equals("_", StringComparison.OrdinalIgnoreCase))
        return $"library/{image}".ToLowerInvariant();
    
    return $"{owner}/{image}".ToLowerInvariant();
}

public override string FormatFullReference(string owner, string image, string tag)
{
    var path = FormatRepositoryPath(owner, image);
    return $"docker.io/{path}:{tag}";
}
```

---

## 4. Backward Compatibility Strategies

### Decision
Use ASP.NET Core endpoint routing to support both legacy and new endpoints simultaneously. Default registry to GHCR when not specified.

### Rationale
- Zero breaking changes for existing users
- Existing bookmarks continue to work
- URL query parameters preserve registry selection for new usage
- Clean migration path without forced upgrade

### Approach: Dual Endpoint Pattern

**Legacy Endpoint** (deprecated but functional):
```
GET /api/images/{owner}/{image}/tags
→ Internally routes to GHCR registry
→ Response format unchanged
```

**New Endpoint**:
```
GET /api/registries/{registryType}/{owner}/{image}/tags
→ Explicit registry selection
→ Response format identical to legacy
```

**Alternative Considered - Query Parameter**:
```
GET /api/images/{owner}/{image}/tags?registry=dockerhub
```
Rejected: Less RESTful, harder to route, but kept as fallback option

### URL Preservation Strategy

**SvelteKit Approach**:
- Store selected registry in URL search params: `?registry=dockerhub`
- Read registry from URL on page load
- Default to `ghcr` if parameter absent
- Update URL when registry selection changes (using `goto` with `replaceState`)

**Example URLs**:
```
# Legacy (no registry param) → defaults to GHCR
https://ghcr-browser.goatheckler.com/?owner=microsoft&image=dotnet-samples

# Explicit registry selection
https://ghcr-browser.goatheckler.com/?registry=dockerhub&owner=library&image=nginx
```

### Migration Timeline

**Phase 1** (Current): Dual support
- Both endpoints live
- Legacy endpoint marked deprecated in OpenAPI
- No forced migration

**Phase 2** (Future - 6+ months):
- Add deprecation warning in UI for legacy endpoint users
- Documentation updated with new endpoint

**Phase 3** (Future - 12+ months):
- Evaluate usage metrics
- Consider retiring legacy endpoint if adoption is complete

### Implementation Notes

**Program.cs Routing**:
```csharp
// New endpoint
app.MapGet("/api/registries/{registryType}/{owner}/{image}/tags", 
    async (string registryType, string owner, string image, IRegistryFactory factory) => {
        var client = factory.CreateClient(registryType);
        return await client.ListTagsPageAsync(owner, image, 100, null);
    });

// Legacy endpoint (backward compatibility)
app.MapGet("/api/images/{owner}/{image}/tags",
    async (string owner, string image, IRegistryFactory factory) => {
        var client = factory.CreateClient("ghcr"); // Default to GHCR
        return await client.ListTagsPageAsync(owner, image, 100, null);
    });
```

**Frontend (+page.ts)**:
```typescript
export async function load({ url }) {
    const registry = url.searchParams.get('registry') ?? 'ghcr';
    const owner = url.searchParams.get('owner');
    const image = url.searchParams.get('image');
    
    return { registry, owner, image };
}
```

---

## 5. Frontend State Management

### Decision
Use SvelteKit's native URL search params with `$page.url.searchParams` for registry selection state. Update URL on registry change using `goto()` with `replaceState: true`.

### Rationale
- **URL as Source of Truth**: Bookmarkable, shareable, back-button friendly
- **SvelteKit Native**: No additional state management library needed
- **SSR Compatible**: Works with SvelteKit's server-side rendering
- **Simple**: Minimal code, leverages framework capabilities

### State Flow

1. **Initial Load**: Read `?registry=` from URL, default to `ghcr`
2. **User Selection**: Update URL search params → triggers reactive update
3. **API Call**: Use registry from URL params in fetch request
4. **Bookmark/Share**: URL contains complete state

### Component Architecture

**RegistrySelector.svelte**:
- Dropdown or radio button group
- Bound to `selectedRegistry` prop
- Emits `change` event on selection
- ARIA labeled for accessibility

**+page.svelte** (Main Page):
- Consumes registry from `$page.url.searchParams`
- Passes to RegistrySelector as prop
- On registry change: update URL → triggers load function

### Accessibility Requirements

**Keyboard Navigation**:
- Tab to selector → Enter/Space to open dropdown
- Arrow keys to navigate options
- Enter/Space to select
- Escape to close without changing

**Screen Reader Support**:
```html
<select 
  aria-label="Select container registry"
  aria-describedby="registry-help"
>
  <option value="ghcr">GitHub Container Registry</option>
  <option value="dockerhub">Docker Hub</option>
  <option value="quay">Quay.io</option>
</select>
<span id="registry-help" class="sr-only">
  Choose which container registry to search
</span>
```

**Visual Indication**:
- Selected registry visually distinct (purple accent per design system)
- Focus state clearly visible (outline, background change)
- Active state during API call (disabled during fetch)

### Alternatives Considered

**Svelte Stores**: Rejected - URL params simpler, more RESTful
**React Query / TanStack Query**: Rejected - overkill for this use case
**Local Storage**: Rejected - doesn't support bookmarking, harder to test

### Implementation Notes

**+page.svelte**:
```svelte
<script lang="ts">
  import { page } from '$app/stores';
  import { goto } from '$app/navigation';
  import RegistrySelector from '$lib/components/RegistrySelector.svelte';
  
  $: registry = $page.url.searchParams.get('registry') ?? 'ghcr';
  
  function handleRegistryChange(newRegistry: string) {
    const url = new URL($page.url);
    url.searchParams.set('registry', newRegistry);
    goto(url, { replaceState: true, noScroll: true });
  }
</script>

<RegistrySelector value={registry} on:change={e => handleRegistryChange(e.detail)} />
```

**RegistrySelector.svelte**:
```svelte
<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  export let value: string;
  
  const dispatch = createEventDispatcher();
  const registries = [
    { id: 'ghcr', name: 'GitHub Container Registry', label: 'GHCR' },
    { id: 'dockerhub', name: 'Docker Hub', label: 'Docker Hub' },
    { id: 'quay', name: 'Quay.io', label: 'Quay' },
  ];
  
  function handleChange(event: Event) {
    const target = event.target as HTMLSelectElement;
    dispatch('change', target.value);
  }
</script>

<div class="registry-selector">
  <label for="registry" class="label">Registry</label>
  <select 
    id="registry" 
    {value} 
    on:change={handleChange}
    class="select"
    aria-label="Select container registry"
  >
    {#each registries as reg}
      <option value={reg.id}>{reg.label}</option>
    {/each}
  </select>
</div>
```

---

## 6. Testing Strategy

### Decision
Layered testing approach: Unit tests for registry clients, integration tests with real registries, contract tests for API schemas, E2E tests for user flows.

### Test Pyramid

**Unit Tests** (Fast, Isolated):
- Registry factory instantiation
- Repository path formatting per registry
- Token URL construction
- Error response parsing
- Base class shared logic

**Integration Tests** (Real API Calls):
- Each registry with known public image:
  - GHCR: `ghcr.io/microsoft/dotnet-samples`
  - Docker Hub: `library/nginx`
  - Quay: `quay.io/prometheus/prometheus`
  - GCR: `gcr.io/distroless/base`
- Verify actual tag retrieval
- Verify authentication flow (anonymous → 401 → token → success)
- Network timeout handling

**Contract Tests** (Schema Validation):
- OpenAPI spec compliance for new registry parameter
- Response format consistency across registries
- Error response schemas (400, 404, 429, 5xx)

**E2E Tests** (User Flows):
- Select registry → search → view tags
- Copy tag with registry-specific format
- Keyboard navigation through registry selector
- Backward compatibility (legacy URL still works)
- URL parameter persistence (bookmark → reload)

### Integration Test Considerations

**Challenge**: Integration tests hit real registries
**Risks**: 
- Rate limiting (429 errors)
- Network flakiness
- Registry downtime

**Mitigations**:
1. Use well-known, stable public images
2. Implement retry logic in tests (3 attempts with backoff)
3. Cache successful responses for subsequent test runs (optional)
4. Run integration tests separately from unit tests (different CI stage)
5. Mark integration tests as `[Trait("Category", "Integration")]`

**Example Integration Test**:
```csharp
[Fact]
[Trait("Category", "Integration")]
public async Task DockerHubClient_ListTags_NginxOfficial_ReturnsLatestTag()
{
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://registry-1.docker.io") };
    var client = new DockerHubClient(httpClient);
    
    // Act
    var result = await client.ListTagsPageAsync("library", "nginx", 10, null);
    
    // Assert
    Assert.False(result.NotFound);
    Assert.Contains("latest", result.Tags);
    Assert.True(result.Tags.Count > 0);
}
```

### Alternatives Considered
- **Mock All Registry Calls**: Rejected for integration tests - defeats purpose of verifying real API compatibility
- **Dedicated Test Registry**: Considered but rejected - adds infrastructure complexity
- **VCR/HTTP Recording**: Considered for caching - good future enhancement

---

## 7. Configuration Management

### Decision
Use `appsettings.json` for registry configurations with environment variable overrides for deployment flexibility.

### Rationale
- ASP.NET Core standard approach
- Easy to extend with new registries
- Environment-specific overrides (dev, staging, prod)
- Supports air-gapped / private registry mirrors

### Configuration Schema

**appsettings.json**:
```json
{
  "Registries": {
    "Ghcr": {
      "BaseUrl": "https://ghcr.io",
      "TokenEndpoint": "/token",
      "ServiceName": "ghcr.io",
      "Enabled": true
    },
    "DockerHub": {
      "BaseUrl": "https://registry-1.docker.io",
      "TokenEndpoint": "https://auth.docker.io/token",
      "ServiceName": "registry.docker.io",
      "Enabled": true
    },
    "Quay": {
      "BaseUrl": "https://quay.io",
      "TokenEndpoint": "/v2/auth",
      "ServiceName": "quay.io",
      "Enabled": true
    },
    "Gcr": {
      "BaseUrl": "https://gcr.io",
      "TokenEndpoint": "/v2/token",
      "ServiceName": "gcr.io",
      "Enabled": false
    }
  }
}
```

**Environment Variable Overrides**:
```bash
# Override Docker Hub base URL for private mirror
Registries__DockerHub__BaseUrl=https://docker-mirror.internal.com

# Disable GCR support
Registries__Gcr__Enabled=false
```

### Model Binding

**RegistryConfiguration.cs**:
```csharp
public class RegistryConfiguration
{
    public string BaseUrl { get; set; } = string.Empty;
    public string TokenEndpoint { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}

public class RegistriesConfiguration
{
    public RegistryConfiguration Ghcr { get; set; } = new();
    public RegistryConfiguration DockerHub { get; set; } = new();
    public RegistryConfiguration Quay { get; set; } = new();
    public RegistryConfiguration Gcr { get; set; } = new();
}
```

**Program.cs DI Registration**:
```csharp
builder.Services.Configure<RegistriesConfiguration>(
    builder.Configuration.GetSection("Registries"));

builder.Services.AddSingleton<IRegistryFactory, RegistryFactory>();
```

### Alternatives Considered
- **Hardcoded URLs**: Rejected - not flexible for private registries
- **Database Configuration**: Rejected - overkill for static registry definitions
- **Feature Flags**: Considered for `Enabled` - using simple bool instead

---

## Summary of Key Decisions

| Aspect | Decision | Rationale |
|--------|----------|-----------|
| **API Standard** | OCI Distribution Specification | All registries support it; existing code already compliant |
| **Authentication** | Per-registry token acquisition | Registry-specific endpoints but standard Bearer token pattern |
| **Path Formatting** | Registry-specific overrides | Docker Hub `library/` prefix requires special handling |
| **Backward Compat** | Dual endpoints, GHCR default | Zero breaking changes; smooth migration |
| **State Management** | URL search params | Bookmarkable, shareable, SvelteKit-native |
| **Testing** | Unit + Integration + Contract + E2E | Comprehensive coverage; integration tests with real registries |
| **Configuration** | appsettings.json + env vars | Standard .NET approach; flexible deployment |

---

## Risk Assessment

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Registry API changes | High | Low | OCI spec stable; monitor registry changelogs |
| Rate limiting varies | Medium | Medium | Document limits; implement per-registry retry policies |
| Docker Hub auth complexity | Medium | Low | Well-documented; existing implementations to reference |
| Integration test flakiness | Low | Medium | Retry logic; separate CI stage; known stable images |
| User confusion with selector | Medium | Low | Clear labels; default to GHCR; helpful error messages |

---

## Open Questions for Implementation

1. **UI Component Choice**: Dropdown vs Radio Group vs Tabs?
   - **Recommendation**: Dropdown for <5 registries; tabs if we expect 5+
   - **Decision Point**: During frontend implementation

2. **GCR in Initial Release?**
   - **Recommendation**: Include if time permits; auth pattern very similar to GHCR/Quay
   - **Decision Point**: During task prioritization

3. **Copy Button Placement**: Per-row or single global?
   - **Recommendation**: Keep existing per-row pattern for consistency
   - **Decision Point**: During frontend implementation

4. **Registry Icons/Logos?**
   - **Recommendation**: Nice-to-have; defer to future enhancement
   - **Decision Point**: Not critical for MVP

---

**Research Complete**: All technical unknowns resolved. Ready for Phase 1 (Design & Contracts).
