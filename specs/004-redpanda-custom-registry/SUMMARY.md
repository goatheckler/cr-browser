# Feature 004: Custom Registry Support - Summary

## Quick Overview

**Goal**: Enable users to browse any OCI-compliant registry (including Redpanda's docker.redpanda.com) by entering a custom URL.

**User Value**: 
- Work with vendor registries (like Redpanda, GitLab, Harbor) without waiting for code updates
- Browse self-hosted or enterprise registries
- Test new registries before requesting built-in support

## What's Being Added

### Custom Registry Support
- New option "Custom Registry" in dropdown
- Text input for registry URL (e.g., "docker.redpanda.com", "registry.example.com")
- Automatic detection of OCI Distribution API v2 compatibility
- Same browsing experience as built-in registries

### Registry Auto-Detection
- Probes `/v2/` endpoint to verify OCI compatibility
- Detects registry capabilities (catalog support, auth requirements)
- Shows clear error messages for unsupported registries
- 5-second timeout with progress indication

## Technical Approach

### Custom Registry Detection
1. User enters URL (e.g., "docker.redpanda.com")
2. System validates URL format
3. System probes `/v2/` endpoint
4. System checks for `docker-distribution-api-version` header
5. System reports success or failure with details

### Architecture
- **Backend**: New `CustomOciRegistryClient` class with detection logic
- **Frontend**: Updated dropdown, new URL input component, detection service
- **Detection**: New backend endpoint `/api/registries/detect`

## Implementation (Single Phase)

**Total Estimate**: 11-16 hours

### Backend (6-8 hours)
- Add `Custom` to `RegistryType` enum
- Create `RegistryDetectionService` for URL validation/probing
- Create `CustomOciRegistryClient` (generic OCI v2 client)
- Add `/api/registries/detect` endpoint
- Update `RegistryFactory` to handle custom registries
- Unit tests for detection and validation

### Frontend (3-5 hours)
- Add "Custom Registry" dropdown option
- Create URL input component with validation
- Add detection status display
- Update browse flow to pass customRegistryUrl
- Update services to handle custom registries

### Testing (2-3 hours)
- Integration tests for docker.redpanda.com
- E2E tests for custom registry workflow
- E2E tests for detection failures
- Test with multiple registries (Redpanda, GitLab, etc.)

## Key Files Changed

### Backend (C#)
- `Models.cs` - Add Custom to enum
- `CustomOciRegistryClient.cs` (new) - Generic OCI client
- `RegistryDetectionService.cs` (new) - Detection logic
- `Program.cs` - New detection endpoint
- `RegistryFactory.cs` - Handle custom registry type

### Frontend (TypeScript/Svelte)
- `types/browse.ts` - Update RegistryType
- `RegistrySelector.svelte` - Add custom option
- `CustomRegistryInput.svelte` (new) - URL input component
- `services/customRegistryDetection.ts` (new) - Detection service
- `+page.svelte` - Custom registry workflow

### Tests
- New unit tests for detection and validation
- New integration tests for docker.redpanda.com
- New E2E tests for custom registry workflow

## Success Criteria

- [x] Research completed - Redpanda registry verified compatible
- [ ] Can enter docker.redpanda.com as custom URL and browse images
- [ ] Can enter other OCI v2 registry URLs and browse images
- [ ] Detection works for OCI v2 registries
- [ ] Clear errors for invalid/unsupported registries
- [ ] All existing tests still pass
- [ ] Documentation updated with examples

## Risk Assessment: LOW

- No database changes
- No breaking changes to existing functionality
- Pure additive feature
- Easy rollback (remove dropdown options)
- Well-defined OCI standard to follow

## Next Steps

1. ✅ Review and approve this specification
2. Create feature branch: `004-redpanda-custom-registry`
3. Begin implementation (backend components)
4. Implement frontend components
5. Add tests for custom registry workflow
6. Validate with docker.redpanda.com and other registries
7. Documentation updates

## Questions?

- Want to add registries as built-in options? (Redpanda, GitLab, Harbor, JFrog)
- Want to persist custom URLs for reuse?
- Want to support importing from Docker config.json?
- Want to display registry health/status indicators?

These can be follow-up features after this MVP.


---

## Investigation Summary

### Redpanda Registry Verification

**Registry URL**: https://docker.redpanda.com

#### Compatibility Tests Performed:
1. ✅ **OCI v2 API Check**: Confirmed `docker-distribution-api-version: registry/2.0` header
2. ❌ **Catalog Endpoint**: Not supported (returns 404) - standard for namespace-based registries
3. ✅ **Tags Listing**: Successfully listed tags for `redpandadata/redpanda`
4. ✅ **Manifest Pull**: Successfully retrieved manifest for `latest` tag
5. ✅ **Multiple Repos**: Confirmed `redpandadata/console` also works

#### Redpanda Registry Characteristics:
- **Type**: Standard OCI Distribution v2
- **Access**: Public (no authentication required for basic operations)
- **Architecture**: Namespace-based (similar to GHCR, Docker Hub)
- **HTTPS**: Yes (secure by default)
- **Known Namespaces**: `redpandadata`
- **Known Images**: `redpanda`, `console`, `connect`, `redpanda-operator`

### Recommended Implementation Path

Based on investigation, Redpanda registry should be implemented exactly like GCR:
- No catalog support (direct namespace/image access only)
- Standard OCI tag listing
- Public access model
- Namespace required for browsing

This validates our approach and ensures Redpanda will work seamlessly with the existing architecture.
