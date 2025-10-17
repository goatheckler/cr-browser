# Feature Specification: Custom Registry Support with Auto-Detection

**Feature Branch**: `004-redpanda-custom-registry`  
**Created**: 2025-10-16  
**Status**: Draft  
**Input**: Enable users to browse any OCI-compliant registry (including Redpanda's docker.redpanda.com) by entering a custom registry URL with automatic registry type detection.

## Overview

Enable users to browse container images from any OCI-compliant registry by providing a custom registry URL. The application should automatically detect the registry type and capabilities, providing the same browsing experience as built-in registries.

**Primary Value**: Users working with vendor-specific or private OCI-compliant registries can use the same convenient browsing interface without requiring code changes to add new registries. This is particularly valuable for:
- Vendor-specific registries (Redpanda, GitLab, custom Harbor installations)
- Self-hosted registries
- Testing new registries before adding built-in support

**Scope**: 
- Add "Custom Registry" option with URL input
- Implement registry type detection via OCI Distribution API probing
- Support both named tags browsing and image listing for custom registries
- Generic OCI client that works with any compliant registry

**Out of Scope for Initial Release**:
- Custom authentication schemes beyond standard Bearer tokens
- Non-OCI compliant registries
- Registry capability negotiation beyond basic detection
- Saving/managing multiple custom registry URLs

## User Scenarios & Testing

### Scenario 1: Browse Redpanda Images (Custom Registry)
**Actor**: DevOps engineer working with Redpanda  
**Goal**: Browse available Redpanda container images

1. User opens the application
2. User selects "Custom Registry" from dropdown
3. User enters registry URL: "docker.redpanda.com"
4. User enters owner/organization: "redpandadata"
5. User clicks Browse button
6. System detects OCI v2 compatibility
7. System displays list of available images (redpanda, console, etc.)
8. User selects an image to view tags
9. System displays all available tags with metadata

**Expected Outcome**: User sees Redpanda images in consistent format with other registries

### Scenario 2: Browse Different Custom Registry
**Actor**: Developer using a vendor's private registry  
**Goal**: Browse images from an OCI-compliant registry not in the built-in list

1. User opens the application
2. User selects "Custom Registry" from dropdown
3. User enters registry URL: "registry.example.com"
4. User enters owner/namespace: "myorg"
5. User clicks Browse button
6. System detects registry type/capabilities
7. System displays list of available images
8. User selects an image to view tags

**Expected Outcome**: User can browse custom registry without requiring app modification

### Scenario 3: Invalid Custom Registry URL
**Actor**: User entering incorrect registry URL  
**Goal**: System provides helpful error message

1. User selects "Custom Registry"
2. User enters invalid URL: "not-a-registry.com"
3. User attempts to browse
4. System attempts connection and detection
5. System displays clear error: "Unable to connect to registry or registry does not support OCI Distribution API"

**Expected Outcome**: Clear, actionable error message

## Functional Requirements

### Registry Support
1. **FR-1**: System SHALL include "Custom Registry" as a registry option
2. **FR-2**: System SHALL accept custom registry URLs in the format: `hostname[:port][/path]`
3. **FR-3**: System SHALL validate custom registry URLs before attempting connection
4. **FR-4**: System SHALL support both HTTP and HTTPS for custom registries (with HTTPS preference)

### Registry Detection
5. **FR-5**: System SHALL probe custom registries using OCI Distribution Specification endpoints
6. **FR-6**: System SHALL detect if a registry supports `/v2/` base endpoint (OCI/Docker Registry v2)
7. **FR-7**: System SHALL detect registry capabilities by testing standard endpoints (catalog, tags/list)
8. **FR-8**: System SHALL determine appropriate authentication method based on 401 response headers
9. **FR-9**: System SHALL cache registry detection results for the session duration

### User Experience
10. **FR-10**: Custom registry URL input SHALL appear when "Custom Registry" is selected
11. **FR-11**: System SHALL provide URL format hints and examples (e.g., "docker.redpanda.com", "registry.example.com:5000")
12. **FR-12**: System SHALL display detection progress during registry probing
13. **FR-13**: System SHALL preserve user's custom registry URL during browsing session
14. **FR-14**: System SHALL display registry connection errors with actionable guidance

### Data & API
15. **FR-15**: Custom registries SHALL be treated as generic OCI registries
16. **FR-16**: System SHALL support unauthenticated browsing for public custom registries
17. **FR-17**: System SHALL support Bearer token authentication for authenticated custom registries

## Key Entities

### Registry Configuration
- Registry Type: enum (GHCR, DockerHub, Quay, GCR, Custom)
- Base URL: string (e.g., "https://docker.redpanda.com")
- Auth URL: optional string
- Detection Method: Auto-detected
- Capabilities: list of supported features

### Custom Registry Input
- User-provided URL: string
- Normalized URL: string (with scheme and validation)
- Detection Status: enum (Pending, Success, Failed)
- Error Message: optional string
- Detected Capabilities: registry capabilities object

### Registry Capabilities
- Supports Catalog API: boolean
- Supports Tags Pagination: boolean
- Auth Method: enum (None, Bearer, Basic)
- API Version: string (e.g., "registry/2.0")

## User Interface Requirements

### Registry Selector Enhancement
- Add "Custom Registry" option at bottom of list
- When "Custom Registry" selected:
  - Show URL input field with placeholder: "Enter registry URL (e.g., docker.redpanda.com, registry.example.com)"
  - Show format hint: "Supports OCI-compliant registries"
  - Enable Browse button only when URL is non-empty

### Custom Registry Detection UI
- Display inline status during detection:
  - "Connecting to registry..."
  - "Detecting capabilities..."
  - "Registry detected: OCI Distribution v2" (success)
  - "Unable to connect to registry" (failure)
- Display detected registry information (optional):
  - API version
  - Supported features

## Acceptance Criteria

1. ✅ User can select "Custom Registry" option
2. ✅ User can enter docker.redpanda.com and browse redpandadata/redpanda tags
3. ✅ User can enter any OCI-compliant registry URL and browse images
4. ✅ System automatically detects OCI Distribution API v2 registries
5. ✅ System displays appropriate errors for invalid/unreachable registries
6. ✅ Custom registry URLs are validated before connection attempts
7. ✅ All existing registry tests continue to pass
8. ✅ E2E tests cover custom registry scenarios with Redpanda and other registries

## Non-Functional Requirements

### Performance
- Registry detection SHOULD complete within 5 seconds
- Failed detection SHOULD not block UI for more than 10 seconds
- Detection results SHOULD be cached for session duration

### Security
- Custom registry URLs MUST be validated to prevent injection attacks
- HTTPS MUST be preferred over HTTP for custom registries
- HTTP connections SHOULD display security warning to user

### Compatibility
- MUST maintain backward compatibility with existing registries
- SHOULD work with any OCI Distribution Specification v2 compliant registry
- SHOULD gracefully degrade for registries with limited capabilities

## Open Questions & Assumptions

### Assumptions
1. Most custom registries will be OCI Distribution v2 compliant
2. Users are responsible for network access to custom registries
3. Basic Bearer token auth is sufficient for authenticated custom registries

### Questions for Discussion
1. Should we save previously used custom registry URLs for quick access?
2. Should we display a list of "suggested" registries (JFrog, Harbor, GitLab, etc.) with URL templates?
3. How should we handle registries behind VPNs or requiring special network configuration?
4. Should we support importing registry configurations from Docker config.json?

## Dependencies & Integration Points

### Backend Changes
- Add `Custom` to `RegistryType` enum
- Create `CustomOciRegistryClient` with detection logic
- Add registry detection service/utility
- Update `RegistryFactory` to handle custom registries
- Add `/api/registries/detect` endpoint for validation

### Frontend Changes
- Update `RegistryType` type definition
- Add "Custom Registry" to dropdown
- Add custom URL input component
- Add detection status display
- Update registry browser services to handle custom registries
- Add URL validation logic

### Testing
- Unit tests for registry detection logic
- Unit tests for URL validation
- Integration tests for custom registry with docker.redpanda.com
- E2E tests for custom registry workflow
- E2E tests for detection failure scenarios

## Success Metrics

- Users can successfully browse docker.redpanda.com as a custom registry
- Users can successfully browse at least 3 different custom registries in testing
- Registry detection success rate > 95% for OCI-compliant registries
- No increase in error rates for existing registries
- Page load time increase < 500ms for custom registry detection

---

## Review Checklist
- [ ] All requirements are user-focused (what/why, not how)
- [ ] No implementation details or technology choices
- [ ] Scenarios are testable and complete
- [ ] Out of scope items clearly listed
- [ ] All [NEEDS CLARIFICATION] items addressed
- [ ] Success metrics defined
- [ ] Dependencies identified
