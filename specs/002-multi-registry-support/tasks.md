# Multi-Registry Support - Implementation Tasks

**Feature**: 002-multi-registry-support  
**Generated**: 2025-10-12  
**Total Tasks**: 42

## Task Execution Order

Tasks are ordered by dependency. Markers:
- `[P]` = Can be executed in parallel with other `[P]` tasks
- `[SEQ]` = Must be executed sequentially after previous task(s)
- `[TEST]` = Test task (run before implementation)

---

## Phase 1: Backend Foundation & Refactoring (Tasks 1-12)

### Task 1: [P] [TEST] Create contract test for RegistryType enum
**File**: `backend/tests/contract/RegistryTypeContractTests.cs`
**Description**: Write contract test validating RegistryType enum values match OpenAPI spec
**Acceptance**:
- Test validates enum has values: `ghcr`, `dockerhub`, `quay`, `gcr`
- Test fails initially (enum doesn't exist yet)
- Test validates lowercase string serialization

**Implementation Notes**:
```csharp
// Validate enum serializes as lowercase strings
// Validate all 4 registry types are defined
// Use System.Text.Json serialization attributes
```

---

### Task 2: [P] Define RegistryType enum
**File**: `backend/src/GhcrBrowser.Api/Models.cs`
**Description**: Create RegistryType enum with JSON string serialization
**Acceptance**:
- Enum has 4 values: Ghcr, DockerHub, Quay, Gcr
- Uses `[JsonConverter(typeof(JsonStringEnumConverter))]` for lowercase serialization
- Task 1 contract test passes

**Implementation**:
```csharp
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RegistryType
{
    Ghcr,
    DockerHub,
    Quay,
    Gcr
}
```

---

### Task 3: [P] [TEST] Create unit tests for RegistryConfiguration model
**File**: `backend/tests/unit/RegistryConfigurationTests.cs`
**Description**: Write unit tests for RegistryConfiguration validation
**Acceptance**:
- Test validates BaseUrl is required
- Test validates BaseUrl is valid URI
- Test validates AuthUrl defaults correctly per registry
- Tests fail initially (model doesn't exist)

---

### Task 4: [P] Create RegistryConfiguration model
**File**: `backend/src/GhcrBrowser.Api/Models.cs`
**Description**: Add RegistryConfiguration record with validation
**Acceptance**:
- Record has properties: RegistryType, BaseUrl, AuthUrl (nullable)
- BaseUrl validates as valid URI
- Task 3 unit tests pass

**Implementation**:
```csharp
public record RegistryConfiguration(
    RegistryType Type,
    string BaseUrl,
    string? AuthUrl = null
);
```

---

### Task 5: [P] [TEST] Create unit tests for IContainerRegistryClient interface design
**File**: `backend/tests/unit/IContainerRegistryClientTests.cs`
**Description**: Write tests validating interface contract using mock implementations
**Acceptance**:
- Test validates GetTagsAsync method signature
- Test validates method returns TagsResponse
- Test validates pagination parameters
- Tests use mock implementation

---

### Task 6: [P] Define IContainerRegistryClient interface
**File**: `backend/src/GhcrBrowser.Api/IContainerRegistryClient.cs` (new file)
**Description**: Create interface defining registry client contract
**Acceptance**:
- Interface has `Task<TagsResponse> GetTagsAsync(string owner, string image, int page, int pageSize)`
- Interface has `RegistryType Type { get; }`
- Task 5 tests pass with mock implementation

**Implementation**:
```csharp
public interface IContainerRegistryClient
{
    RegistryType Type { get; }
    Task<TagsResponse> GetTagsAsync(string owner, string image, int page, int pageSize);
}
```

---

### Task 7: [SEQ] Extract OciRegistryClientBase abstract class from GhcrClient
**File**: `backend/src/GhcrBrowser.Api/OciRegistryClientBase.cs` (new file)
**Description**: Create abstract base class with shared OCI Distribution Spec logic
**Acceptance**:
- Extract token acquisition logic (abstract `GetAuthTokenAsync`)
- Extract tag fetching logic (concrete `GetTagsAsync` using OCI endpoints)
- Extract pagination logic
- Does NOT break existing GhcrClient (next task refactors it)

**Implementation Notes**:
```csharp
public abstract class OciRegistryClientBase : IContainerRegistryClient
{
    protected abstract Task<string> GetAuthTokenAsync(string owner, string image);
    protected abstract string FormatRepositoryPath(string owner, string image);
    
    public async Task<TagsResponse> GetTagsAsync(string owner, string image, int page, int pageSize)
    {
        var token = await GetAuthTokenAsync(owner, image);
        var repo = FormatRepositoryPath(owner, image);
        // Shared OCI logic: GET /v2/{repo}/tags/list
    }
}
```

---

### Task 8: [SEQ] Refactor GhcrClient to inherit from OciRegistryClientBase
**File**: `backend/src/GhcrBrowser.Api/GhcrClient.cs`
**Description**: Refactor existing GhcrClient to use base class
**Acceptance**:
- GhcrClient inherits from OciRegistryClientBase
- Implements abstract methods: `GetAuthTokenAsync`, `FormatRepositoryPath`
- **CRITICAL**: All existing integration tests pass unchanged
- No behavior changes (backward compatibility)

**Implementation**:
```csharp
public class GhcrClient : OciRegistryClientBase
{
    public override RegistryType Type => RegistryType.Ghcr;
    
    protected override async Task<string> GetAuthTokenAsync(string owner, string image)
    {
        // Existing GHCR token logic
    }
    
    protected override string FormatRepositoryPath(string owner, string image)
    {
        return $"{owner}/{image}"; // GHCR standard format
    }
}
```

**Validation**: Run existing tests: `cd backend/tests/integration && dotnet test`

---

### Task 9: [P] [TEST] Create unit tests for RegistryFactory
**File**: `backend/tests/unit/RegistryFactoryTests.cs`
**Description**: Write tests for factory pattern implementation
**Acceptance**:
- Test validates factory creates GhcrClient for RegistryType.Ghcr
- Test validates factory creates DockerHubClient for RegistryType.DockerHub
- Test validates factory creates QuayClient for RegistryType.Quay
- Test validates factory creates GcrClient for RegistryType.Gcr
- Test validates factory throws for unsupported registry
- Tests fail initially (factory doesn't exist)

---

### Task 10: [SEQ] Create IRegistryFactory interface
**File**: `backend/src/GhcrBrowser.Api/IRegistryFactory.cs` (new file)
**Description**: Define factory interface for creating registry clients
**Acceptance**:
- Interface has `IContainerRegistryClient CreateClient(RegistryType type)`

**Implementation**:
```csharp
public interface IRegistryFactory
{
    IContainerRegistryClient CreateClient(RegistryType type);
}
```

---

### Task 11: [SEQ] Implement RegistryFactory
**File**: `backend/src/GhcrBrowser.Api/RegistryFactory.cs` (new file)
**Description**: Implement factory for creating registry clients
**Acceptance**:
- Factory creates correct client instance per RegistryType
- Factory uses DI to inject HttpClient and configuration
- Task 9 unit tests pass

**Implementation**:
```csharp
public class RegistryFactory : IRegistryFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    
    public IContainerRegistryClient CreateClient(RegistryType type)
    {
        return type switch
        {
            RegistryType.Ghcr => new GhcrClient(_httpClientFactory.CreateClient(), _configuration),
            RegistryType.DockerHub => new DockerHubClient(_httpClientFactory.CreateClient(), _configuration),
            RegistryType.Quay => new QuayClient(_httpClientFactory.CreateClient(), _configuration),
            RegistryType.Gcr => new GcrClient(_httpClientFactory.CreateClient(), _configuration),
            _ => throw new ArgumentException($"Unsupported registry type: {type}")
        };
    }
}
```

---

### Task 12: [SEQ] Register RegistryFactory in DI container
**File**: `backend/src/GhcrBrowser.Api/Program.cs`
**Description**: Register factory and configuration in dependency injection
**Acceptance**:
- Factory registered as singleton
- HttpClient registered with appropriate policies
- Configuration bound from appsettings.json

**Implementation**:
```csharp
builder.Services.AddSingleton<IRegistryFactory, RegistryFactory>();
builder.Services.AddHttpClient(); // If not already registered
```

---

## Phase 2: Registry Implementations (Tasks 13-24)

### Task 13: [P] [TEST] Create integration test for DockerHubClient
**File**: `backend/tests/integration/DockerHubClientTests.cs`
**Description**: Write integration test using real Docker Hub API
**Acceptance**:
- Test fetches tags for `library/nginx` (official image)
- Test fetches tags for `bitnami/nginx` (user image)
- Test validates tag structure (name, digest, lastModified)
- Test validates pagination
- Tests fail initially (client doesn't exist)

**Test Data**:
```csharp
// Use public images: library/nginx, library/alpine, bitnami/nginx
```

---

### Task 14: [SEQ] Implement DockerHubClient
**File**: `backend/src/GhcrBrowser.Api/DockerHubClient.cs` (new file)
**Description**: Implement Docker Hub registry client
**Acceptance**:
- Inherits from OciRegistryClientBase
- Implements GetAuthTokenAsync (Docker Hub anonymous token: `https://auth.docker.io/token?service=registry.docker.io&scope=repository:{repo}:pull`)
- Implements FormatRepositoryPath (handles `library/` prefix for official images)
- Task 13 integration tests pass

**Implementation Notes**:
```csharp
protected override string FormatRepositoryPath(string owner, string image)
{
    // If owner is missing or is single-part (e.g., "nginx"), prepend "library/"
    if (!image.Contains("/") && string.IsNullOrEmpty(owner))
    {
        return $"library/{image}";
    }
    return $"{owner}/{image}";
}

protected override async Task<string> GetAuthTokenAsync(string owner, string image)
{
    var repo = FormatRepositoryPath(owner, image);
    var authUrl = $"https://auth.docker.io/token?service=registry.docker.io&scope=repository:{repo}:pull";
    // Fetch anonymous token
}
```

---

### Task 15: [P] [TEST] Create integration test for QuayClient
**File**: `backend/tests/integration/QuayClientTests.cs`
**Description**: Write integration test using real Quay.io API
**Acceptance**:
- Test fetches tags for `prometheus/prometheus`
- Test fetches tags for `coreos/etcd`
- Test validates tag structure
- Test validates pagination
- Tests fail initially (client doesn't exist)

**Test Data**:
```csharp
// Use public images: prometheus/prometheus, coreos/etcd
```

---

### Task 16: [SEQ] Implement QuayClient
**File**: `backend/src/GhcrBrowser.Api/QuayClient.cs` (new file)
**Description**: Implement Quay.io registry client
**Acceptance**:
- Inherits from OciRegistryClientBase
- Implements GetAuthTokenAsync (Quay anonymous: `https://quay.io/v2/auth?service=quay.io&scope=repository:{repo}:pull`)
- Implements FormatRepositoryPath (standard `owner/image`)
- Task 15 integration tests pass

---

### Task 17: [P] [TEST] Create integration test for GcrClient
**File**: `backend/tests/integration/GcrClientTests.cs`
**Description**: Write integration test using real GCR API
**Acceptance**:
- Test fetches tags for `google-containers/pause`
- Test validates tag structure
- Test validates pagination
- Tests fail initially (client doesn't exist)

**Test Data**:
```csharp
// Use public images: google-containers/pause
```

---

### Task 18: [SEQ] Implement GcrClient
**File**: `backend/src/GhcrBrowser.Api/GcrClient.cs` (new file)
**Description**: Implement Google Container Registry client
**Acceptance**:
- Inherits from OciRegistryClientBase
- Implements GetAuthTokenAsync (GCR uses different auth endpoint)
- Implements FormatRepositoryPath (standard format)
- Task 17 integration tests pass

---

### Task 19: [P] [TEST] Create unit tests for registry path formatting
**File**: `backend/tests/unit/RegistryPathFormattingTests.cs`
**Description**: Write tests for registry-specific path formatting logic
**Acceptance**:
- Test Docker Hub official image: `nginx` → `library/nginx`
- Test Docker Hub user image: `bitnami/nginx` → `bitnami/nginx`
- Test GHCR image: `microsoft/dotnet` → `microsoft/dotnet`
- Test validates all registry types

---

### Task 20: [SEQ] Add configuration section for registries
**File**: `backend/src/GhcrBrowser.Api/appsettings.json`
**Description**: Add registry configuration section
**Acceptance**:
- Configuration has Registries section with BaseUrl per registry
- Allows environment variable overrides

**Implementation**:
```json
{
  "Registries": {
    "Ghcr": {
      "BaseUrl": "https://ghcr.io",
      "AuthUrl": "https://ghcr.io/token"
    },
    "DockerHub": {
      "BaseUrl": "https://registry-1.docker.io",
      "AuthUrl": "https://auth.docker.io/token"
    },
    "Quay": {
      "BaseUrl": "https://quay.io",
      "AuthUrl": "https://quay.io/v2/auth"
    },
    "Gcr": {
      "BaseUrl": "https://gcr.io",
      "AuthUrl": "https://gcr.io/v2/token"
    }
  }
}
```

---

### Task 21: [SEQ] Update RegistryFactory to use configuration
**File**: `backend/src/GhcrBrowser.Api/RegistryFactory.cs`
**Description**: Inject configuration into registry clients
**Acceptance**:
- Factory reads BaseUrl and AuthUrl from configuration
- Factory passes configuration to client constructors
- Environment variables override appsettings.json values

---

### Task 22: [P] [TEST] Create validation tests for RegistryRequest model
**File**: `backend/tests/unit/RegistryRequestValidationTests.cs`
**Description**: Write tests for request validation logic
**Acceptance**:
- Test validates registryType is required
- Test validates owner/image format (no special chars)
- Test validates page >= 1
- Test validates pageSize in range [1, 100]
- Test validates unsupported registry type returns error

---

### Task 23: [SEQ] Create RegistryRequest model with validation
**File**: `backend/src/GhcrBrowser.Api/Models.cs`
**Description**: Add request model with validation attributes
**Acceptance**:
- Model has: RegistryType, Owner, Image, Page, PageSize
- Uses DataAnnotations for validation
- Task 22 tests pass

**Implementation**:
```csharp
public record RegistryRequest(
    [Required] RegistryType RegistryType,
    [Required] [RegularExpression(@"^[a-z0-9-_]+$")] string Owner,
    [Required] [RegularExpression(@"^[a-z0-9-_/.]+$")] string Image,
    [Range(1, int.MaxValue)] int Page = 1,
    [Range(1, 100)] int PageSize = 10
);
```

---

### Task 24: [P] [TEST] Create validation tests for error response models
**File**: `backend/tests/unit/ErrorResponseValidationTests.cs`
**Description**: Write tests for error response structure
**Acceptance**:
- Test validates ErrorResponse has code, message, details
- Test validates error codes: InvalidFormat, NotFound, UnsupportedRegistry, RateLimited, UpstreamError
- Tests validate JSON serialization matches OpenAPI spec

---

## Phase 3: API Updates (Tasks 25-32)

### Task 25: [P] [TEST] Create contract tests for new registry endpoint
**File**: `backend/tests/contract/RegistryEndpointContractTests.cs`
**Description**: Write contract tests for `/api/registries/{registryType}/{owner}/{image}/tags`
**Acceptance**:
- Test validates endpoint accepts registryType path parameter
- Test validates endpoint accepts query parameters (page, pageSize)
- Test validates response matches OpenAPI schema
- Tests fail initially (endpoint doesn't exist)

---

### Task 26: [SEQ] Add new registry endpoint to API
**File**: `backend/src/GhcrBrowser.Api/Program.cs`
**Description**: Add new endpoint with registry parameter
**Acceptance**:
- Endpoint: `GET /api/registries/{registryType}/{owner}/{image}/tags`
- Uses RegistryFactory to create appropriate client
- Returns TagsResponse
- Task 25 contract tests pass

**Implementation**:
```csharp
app.MapGet("/api/registries/{registryType}/{owner}/{image}/tags", 
    async (string registryType, string owner, string image, int page, int pageSize, IRegistryFactory factory) =>
{
    if (!Enum.TryParse<RegistryType>(registryType, true, out var type))
    {
        return Results.BadRequest(new ErrorResponse("UnsupportedRegistry", $"Registry '{registryType}' not supported"));
    }
    
    var client = factory.CreateClient(type);
    var tags = await client.GetTagsAsync(owner, image, page, pageSize);
    return Results.Ok(tags);
});
```

---

### Task 27: [P] [TEST] Create contract tests for legacy endpoint backward compatibility
**File**: `backend/tests/contract/LegacyEndpointContractTests.cs`
**Description**: Write tests validating legacy endpoint still works
**Acceptance**:
- Test validates `/api/images/{owner}/{image}/tags` still works
- Test validates response is identical to GHCR-specific call
- Test validates existing integration tests pass unchanged

---

### Task 28: [SEQ] Update legacy endpoint to default to GHCR
**File**: `backend/src/GhcrBrowser.Api/Program.cs`
**Description**: Keep legacy endpoint, route to GHCR client
**Acceptance**:
- Endpoint: `GET /api/images/{owner}/{image}/tags` (existing)
- Internally calls new endpoint with `registryType = "ghcr"`
- Task 27 contract tests pass
- **CRITICAL**: All existing integration/E2E tests pass unchanged

**Implementation**:
```csharp
app.MapGet("/api/images/{owner}/{image}/tags", 
    async (string owner, string image, int page, int pageSize, IRegistryFactory factory) =>
{
    // Default to GHCR for backward compatibility
    var client = factory.CreateClient(RegistryType.Ghcr);
    var tags = await client.GetTagsAsync(owner, image, page, pageSize);
    return Results.Ok(tags);
});
```

---

### Task 29: [P] [TEST] Create error handling tests
**File**: `backend/tests/contract/ErrorHandlingContractTests.cs`
**Description**: Write tests for error scenarios
**Acceptance**:
- Test NotFound: Non-existent repository returns 404
- Test InvalidFormat: Invalid owner/image format returns 400
- Test UnsupportedRegistry: Invalid registry type returns 400
- Test RateLimited: Rate limit returns 429 (simulate with mock)
- Test UpstreamError: Upstream failure returns 502

---

### Task 30: [SEQ] Implement error handling middleware
**File**: `backend/src/GhcrBrowser.Api/ErrorHandlingMiddleware.cs` (new file)
**Description**: Add global error handling for registry operations
**Acceptance**:
- Catches HttpRequestException → maps to appropriate error code
- Catches validation errors → returns InvalidFormat
- Catches 404 from registry → returns NotFound
- Catches 429 from registry → returns RateLimited
- Task 29 tests pass

---

### Task 31: [SEQ] Add error handling to endpoints
**File**: `backend/src/GhcrBrowser.Api/Program.cs`
**Description**: Wrap endpoint logic with try-catch for error responses
**Acceptance**:
- Endpoints return ErrorResponse on failure
- HTTP status codes match error types (400, 404, 429, 502)
- Error details include helpful messages

---

### Task 32: [P] [TEST] Create performance tests
**File**: `backend/tests/integration/PerformanceTests.cs`
**Description**: Write tests validating ≤2 second response time
**Acceptance**:
- Test measures response time for each registry
- Test validates all registries respond in ≤2 seconds
- Test uses real public images

---

## Phase 4: Frontend Updates (Tasks 33-38)

### Task 33: [P] [TEST] Create E2E test for registry selector component
**File**: `frontend/tests/e2e/registry-selector.spec.ts`
**Description**: Write Playwright test for registry dropdown
**Acceptance**:
- Test validates dropdown shows all 4 registries
- Test validates selecting registry updates URL param
- Test validates selecting registry triggers new search
- Test fails initially (component doesn't exist)

---

### Task 34: [SEQ] Create RegistrySelector Svelte component
**File**: `frontend/src/routes/RegistrySelector.svelte` (new file)
**Description**: Create dropdown component for registry selection
**Acceptance**:
- Component renders dropdown with 4 options: GHCR, Docker Hub, Quay, GCR
- Component binds to URL search param `?registry=`
- Component emits event on change
- Task 33 E2E test passes

**Implementation**:
```svelte
<script lang="ts">
  import { goto } from '$app/navigation';
  import { page } from '$app/stores';
  
  let selectedRegistry = $page.url.searchParams.get('registry') || 'ghcr';
  
  function handleChange() {
    const params = new URLSearchParams($page.url.searchParams);
    params.set('registry', selectedRegistry);
    goto(`?${params.toString()}`);
  }
</script>

<select bind:value={selectedRegistry} on:change={handleChange}>
  <option value="ghcr">GitHub Container Registry</option>
  <option value="dockerhub">Docker Hub</option>
  <option value="quay">Quay.io</option>
  <option value="gcr">Google Container Registry</option>
</select>
```

---

### Task 35: [SEQ] Integrate RegistrySelector into main page
**File**: `frontend/src/routes/+page.svelte`
**Description**: Add registry selector to search UI
**Acceptance**:
- RegistrySelector component added above search input
- Registry parameter passed to API calls
- URL updates with registry selection
- Existing E2E tests still pass (default to GHCR)

---

### Task 36: [SEQ] Update API client to support registry parameter
**File**: `frontend/src/routes/+page.ts` (or API client file)
**Description**: Update fetch calls to use new registry endpoint
**Acceptance**:
- API calls use `/api/registries/{registry}/{owner}/{image}/tags`
- Registry read from URL search params
- Falls back to legacy endpoint if registry not specified (backward compat)

**Implementation**:
```typescript
const registry = url.searchParams.get('registry') || 'ghcr';
const apiUrl = `/api/registries/${registry}/${owner}/${image}/tags?page=${page}&pageSize=${pageSize}`;
```

---

### Task 37: [SEQ] Update clipboard copy to use registry-specific format
**File**: `frontend/src/routes/+page.svelte`
**Description**: Update copy button to format references per registry
**Acceptance**:
- GHCR: `ghcr.io/{owner}/{image}:{tag}`
- Docker Hub: `docker.io/{owner}/{image}:{tag}` (or `{owner}/{image}:{tag}`)
- Quay: `quay.io/{owner}/{image}:{tag}`
- GCR: `gcr.io/{owner}/{image}:{tag}`

**Implementation**:
```typescript
function formatImageReference(registry: string, owner: string, image: string, tag: string): string {
  const registryHosts = {
    ghcr: 'ghcr.io',
    dockerhub: 'docker.io',
    quay: 'quay.io',
    gcr: 'gcr.io'
  };
  const host = registryHosts[registry] || 'ghcr.io';
  return `${host}/${owner}/${image}:${tag}`;
}
```

---

### Task 38: [P] [TEST] Create E2E test for URL state management
**File**: `frontend/tests/e2e/url-state.spec.ts`
**Description**: Write test validating URL params persist state
**Acceptance**:
- Test navigates to `?registry=dockerhub&owner=library&image=nginx`
- Test validates all state restored (registry, owner, image)
- Test validates clicking "Next Page" updates URL with page number
- Test validates bookmarking URL restores exact state

---

## Phase 5: Testing & Validation (Tasks 39-42)

### Task 39: [SEQ] Run all existing tests to verify backward compatibility
**File**: N/A (test execution)
**Description**: Execute all existing test suites
**Acceptance**:
- All unit tests pass: `dotnet test backend/tests/unit`
- All integration tests pass: `dotnet test backend/tests/integration`
- All contract tests pass: `dotnet test backend/tests/contract`
- All E2E tests pass: `npm run test:e2e` (in frontend/)
- **Zero test modifications required** (proves backward compatibility)

**Validation Commands**:
```bash
cd backend/tests/unit && dotnet test
cd backend/tests/integration && dotnet test
cd backend/tests/contract && dotnet test
cd frontend && npm run test:e2e
```

---

### Task 40: [P] [TEST] Create comprehensive E2E test for multi-registry user flow
**File**: `frontend/tests/e2e/multi-registry-flow.spec.ts`
**Description**: Write end-to-end test covering all registries
**Acceptance**:
- Test searches GHCR: `microsoft/dotnet`
- Test switches to Docker Hub, searches `library/nginx`
- Test switches to Quay, searches `prometheus/prometheus`
- Test validates clipboard copy per registry
- Test validates pagination works per registry
- Test validates error handling (NotFound scenario)

---

### Task 41: [SEQ] Execute manual quickstart testing scenarios
**File**: `specs/002-multi-registry-support/quickstart.md`
**Description**: Manually execute all 14 test scenarios from quickstart
**Acceptance**:
- All scenarios in quickstart.md pass
- Completion checklist fully checked
- Performance criteria met (≤2s response time)

**Reference**: See `quickstart.md` for detailed test cases 1.1-14

---

### Task 42: [SEQ] Update OpenAPI spec documentation
**File**: `specs/002-multi-registry-support/contracts/openapi.yaml`
**Description**: Ensure OpenAPI spec is accurate post-implementation
**Acceptance**:
- All endpoints documented match implementation
- All error codes documented match implementation
- Examples validated against real responses
- Contract tests validate against OpenAPI spec

---

## Completion Criteria

**All tasks complete when**:
- ✅ All 42 tasks marked complete
- ✅ All automated tests pass (unit, integration, contract, E2E)
- ✅ All manual quickstart scenarios pass
- ✅ Performance criteria met (≤2s response time across all registries)
- ✅ Backward compatibility verified (zero breaking changes)
- ✅ OpenAPI spec matches implementation

**Ready for production deployment**: All functional requirements (FR-001 to FR-022) satisfied

---

## Task Dependencies Diagram

```
Phase 1 Foundation:
[1,2,3,4,5,6] → [7] → [8] → [9,10] → [11] → [12]
     (Parallel)   ↓     ↓      ↓        ↓       ↓
                Extract Base  Refactor Factory  DI

Phase 2 Registry Implementations:
[13,15,17,19] → [14,16,18] → [20] → [21] → [22] → [23,24]
   (Parallel)    Implement    Config  Update  Validation

Phase 3 API Updates:
[25,27,29] → [26,28] → [30] → [31] → [32]
  (Parallel)  Endpoints  Error  Integrate  Perf

Phase 4 Frontend:
[33] → [34] → [35] → [36] → [37] → [38]
 Test  Component  Integrate  API  Clipboard  URL

Phase 5 Validation:
[39] → [40,41,42]
Regression → Final Validation
```

---

**Next Step**: Begin execution with Task 1 (Create contract test for RegistryType enum)
