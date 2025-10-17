# Implementation Plan: Custom Registry Support

**Feature**: 004-redpanda-custom-registry  
**Created**: 2025-10-16  
**Updated**: 2025-10-16 - Simplified to custom registry only

## Implementation Strategy

Single-phase implementation that adds a "Custom Registry" option allowing users to specify any OCI-compliant registry URL. This eliminates the need for adding vendor-specific clients for each registry.

### Key Technical Decisions

1. **Registry Detection Approach**: Probe `/v2/` endpoint for OCI Distribution API header
2. **Custom Registry Client**: Create generic `CustomOciRegistryClient` that works with any OCI v2 registry
3. **URL Validation**: Use URI parsing with whitelist of allowed schemes (http, https)
4. **Session Storage**: Store custom registry URL in component state (no persistence initially)

## Backend Tasks

### Task 1: Add Custom registry type
**File**: `backend/src/CrBrowser.Api/Models.cs`
- Add `Custom` to `RegistryType` enum (line 13)

### Task 2: Create registry detection service
**File**: `backend/src/CrBrowser.Api/RegistryDetectionService.cs` (new)
- Interface: `IRegistryDetectionService`
- Method: `Task<RegistryDetectionResult> DetectRegistryAsync(string baseUrl, CancellationToken ct)`
  - Validate and normalize URL
  - Probe `/v2/` endpoint
  - Check for `Docker-Distribution-Api-Version` header
  - Check for standard response format
  - Return detection result with capabilities
- Method: `bool ValidateUrl(string url, out string? normalizedUrl)` - validate and normalize URL
- Register as singleton in DI

### Task 3: Create CustomOciRegistryClient
**File**: `backend/src/CrBrowser.Api/CustomOciRegistryClient.cs` (new)
- Extend `OciRegistryClientBase`
- Accept `baseUrl` and `HttpClient` in constructor
- Override `BaseUrl` property to return provided URL
- Override `RegistryType` to return `RegistryType.Custom`
- Implement `FormatFullReference` to return `{baseUrl}/{owner}/{image}:{tag}`
- Implement `ListImagesAsync` - return NotSupported error (most registries don't support catalog)
- Reuse inherited `ListTagsPageAsync` and `AcquireTokenAsync` logic

### Task 4: Update RegistryFactory
**File**: `backend/src/CrBrowser.Api/RegistryFactory.cs`
- Add method: `IContainerRegistryClient CreateCustomClient(string baseUrl)`
- Create HttpClient with custom base address
- Return new CustomOciRegistryClient instance

### Task 5: Add registry detection endpoint
**File**: `backend/src/CrBrowser.Api/Program.cs`
- New endpoint: `POST /api/registries/detect`
- Request body: `{ "url": "registry.example.com" }`
- Response: 
  ```json
  {
    "supported": true,
    "normalizedUrl": "https://registry.example.com",
    "apiVersion": "registry/2.0",
    "capabilities": {
      "catalog": false,
      "tagsList": true
    }
  }
  ```
- Error response for unsupported/unreachable registries

### Task 6: Update tags endpoint to accept custom registry
**File**: `backend/src/CrBrowser.Api/Program.cs`
- Modify `/api/registries/{registryType}/{owner}/{image}/tags` endpoint (line 130)
- Accept optional `customRegistryUrl` query parameter
- When `registryType == "custom"`:
  - Require `customRegistryUrl` parameter
  - Validate URL using detection service
  - Use `factory.CreateCustomClient(customRegistryUrl)`
- Return 400 if custom selected but no URL provided

### Task 7: Update images endpoint to accept custom registry
**File**: `backend/src/CrBrowser.Api/Program.cs`
- Modify `/api/registries/{registryType}/{owner}/images` endpoint (line 87)
- Accept optional `customRegistryUrl` query parameter
- Handle custom registry type similar to Task 6
- Note: Most custom registries won't support this (no catalog), return appropriate error

## Frontend Tasks

### Task 8: Update RegistryType definition
**File**: `frontend/src/lib/types/browse.ts`
- Update line 1: Add `'Custom'` to type union
  ```typescript
  export type RegistryType = 'GHCR' | 'DockerHub' | 'Quay' | 'GCR' | 'Custom';
  ```

### Task 9: Create custom registry input component
**File**: `frontend/src/lib/components/CustomRegistryInput.svelte` (new)
- Props: `{ url: string bindable, error: string | null, detecting: boolean }`
- Text input for registry URL with label "Registry URL"
- Placeholder: "docker.redpanda.com or registry.example.com:5000"
- Format hint: "Enter an OCI-compliant container registry URL"
- Show validation error if present
- Show detection status indicator (detecting/success/error)
- Auto-prepend https:// if no scheme provided

### Task 10: Create custom registry detection service
**File**: `frontend/src/lib/services/customRegistryDetection.ts` (new)
- Function: `detectRegistry(url: string): Promise<RegistryDetectionResult>`
- Call `POST /api/registries/detect` endpoint
- Return detection result or throw error
- Handle network errors gracefully

### Task 11: Update registry selector
**File**: `frontend/src/routes/RegistrySelector.svelte`
- Add option: `<option value="custom">Custom Registry</option>` (after line 30)

### Task 12: Update main page with custom registry flow
**File**: `frontend/src/routes/+page.svelte`
- Add state: `customRegistryUrl: string = ''`
- Add state: `detectionStatus: 'idle' | 'detecting' | 'success' | 'error' = 'idle'`
- Add state: `detectionError: string | null = null`
- Show `CustomRegistryInput` component when `selectedRegistry === 'custom'`
- Add "Detect Registry" button (or auto-detect on input blur)
- Call detection service when URL entered
- Update detection status based on result
- Pass `customRegistryUrl` to browse dialog when opening
- Disable browse button until detection succeeds

### Task 13: Update browse images dialog
**File**: `frontend/src/lib/components/BrowseImagesDialog.svelte`
- Add prop: `customRegistryUrl?: string`
- Update API calls to include `customRegistryUrl` query param when registryType is Custom
- Display custom registry URL in dialog header when provided
  - Example: "Browse Images - Custom Registry (docker.redpanda.com)"

### Task 14: Create browser service for custom registry
**File**: `frontend/src/lib/services/customRegistryBrowser.ts` (new)
- Or extend existing services to handle custom registry type
- Pass customRegistryUrl as query parameter to backend endpoints

## Testing Tasks

### Task 15: Unit tests for detection service
**File**: `backend/tests/unit/RegistryDetectionTests.cs` (new)
- Test URL validation (valid/invalid formats)
- Test URL normalization (adding https://, handling ports)
- Test detection with mocked HTTP responses
  - Test successful OCI v2 detection
  - Test non-OCI registry (no header)
  - Test unreachable registry (timeout/connection error)

### Task 16: Unit tests for CustomOciRegistryClient
**File**: `backend/tests/unit/CustomRegistryClientTests.cs` (new)
- Test ListTagsPageAsync with mocked responses
- Test FormatFullReference output
- Test error handling

### Task 17: Integration tests for custom registry
**File**: `backend/tests/integration/CustomRegistryTests.cs` (new)
- Test detection endpoint with real Redpanda registry
- Test tags endpoint with Redpanda registry
  - `GET /api/registries/custom/redpandadata/redpanda/tags?customRegistryUrl=https://docker.redpanda.com`
- Test error handling for invalid URL
- Test error handling for non-OCI registry

### Task 18: E2E tests for custom registry workflow
**File**: `frontend/tests/e2e/browse-custom-registry.spec.ts` (new)
- Test selecting "Custom Registry" from dropdown
- Test entering valid URL (docker.redpanda.com)
- Test detection success flow
- Test browsing redpandadata/redpanda tags
- Test entering invalid URL and seeing error
- Test browsing with custom registry

## Polish & Documentation

### Task 19: Improve error messages
- Add user-friendly error messages:
  - "Unable to connect to registry. Check URL and network connection."
  - "This registry doesn't support the OCI Distribution API v2."
  - "Invalid URL format. Expected: hostname[:port][/path]"
  - "Connection timeout. Registry may be unreachable."

### Task 20: Add loading states
- Show detection progress indicator
- Disable browse button during detection
- Show inline status: "Detecting registry..." / "✓ Compatible" / "✗ Not supported"

### Task 21: Add security warnings
- Display warning icon for HTTP (non-HTTPS) registries
- Tooltip: "This registry uses unencrypted HTTP. Data may be visible to others."

### Task 22: Update documentation
- Update README with custom registry usage section
- Add examples:
  - Redpanda: `docker.redpanda.com` → `redpandadata/redpanda`
  - GitLab: `registry.gitlab.com` → `gitlab-org/gitlab-runner`
- Document detection process
- List known compatible registries

## Implementation Order

1. **Backend Detection**: Tasks 1-2 (detection service)
2. **Backend Client**: Task 3 (CustomOciRegistryClient)
3. **Backend Endpoints**: Tasks 4-7 (factory, detection endpoint, update existing endpoints)
4. **Backend Tests**: Tasks 15-17 (unit + integration tests)
5. **Frontend Types**: Task 8 (update RegistryType)
6. **Frontend Services**: Tasks 9-10 (input component, detection service)
7. **Frontend UI**: Tasks 11-13 (selector, main page, dialog)
8. **Frontend Service**: Task 14 (browser service)
9. **E2E Tests**: Task 18 (full workflow tests)
10. **Polish**: Tasks 19-22 (errors, loading, docs)

## Estimated Effort

- **Backend (Tasks 1-7)**: 3-4 hours
  - Detection service: 1.5 hours
  - Custom client: 1 hour
  - Endpoint updates: 1 hour
  
- **Backend Testing (Tasks 15-17)**: 2-3 hours
  - Unit tests: 1 hour
  - Integration tests: 1.5 hours
  
- **Frontend (Tasks 8-14)**: 3-4 hours
  - Components: 1.5 hours
  - Services: 1 hour
  - Integration: 1.5 hours
  
- **E2E Testing (Task 18)**: 1-2 hours
  
- **Polish & Docs (Tasks 19-22)**: 2-3 hours

**Total**: 11-16 hours

## Success Criteria

- [ ] Can select "Custom Registry" from dropdown
- [ ] Can enter Redpanda URL (docker.redpanda.com) and browse images
- [ ] Detection correctly identifies OCI v2 registries
- [ ] Detection fails gracefully for non-OCI registries
- [ ] Can browse redpandadata/redpanda tags via custom registry
- [ ] Clear error messages for invalid registries
- [ ] All existing tests pass
- [ ] New tests cover custom registry scenarios
- [ ] Documentation includes examples

## Risks & Mitigation

1. **Risk**: Custom registries may have varying auth requirements
   - **Mitigation**: Start with public registries, document auth limitations

2. **Risk**: Detection may be slow for some registries
   - **Mitigation**: Add 5s timeout, show progress indicator

3. **Risk**: URL validation may be too strict or too loose
   - **Mitigation**: Test with variety of registry URLs, iterate on validation

4. **Risk**: Users may expect catalog support for all registries
   - **Mitigation**: Clear messaging that direct namespace/image access is required

## Rollback Plan

If issues arise:
1. Feature can be hidden by removing "Custom Registry" dropdown option
2. Backend endpoints can be disabled via configuration
3. No database or persistent state changes
4. Safe to revert commits atomically

## Dependencies

- No external library changes needed
- Uses existing OCI client patterns
- Reuses existing HttpClient infrastructure
- Compatible with existing test frameworks
