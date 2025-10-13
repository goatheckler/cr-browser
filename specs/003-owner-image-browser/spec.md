# Feature Specification: Owner Image Browser

**Feature Branch**: `003-owner-image-browser`  
**Created**: 2025-10-12  
**Status**: Draft  
**Input**: User request to add "Browse Images" button that shows all images published by an owner across all supported registries (GHCR, Docker Hub, Quay, GCR) with simplified columns showing only Owner/Image.

## Execution Flow (main)
✅ All steps completed successfully

---

## ⚡ Quick Guidelines
- ✅ Focus on WHAT users need and WHY
- ❌ Avoid HOW to implement (no tech stack, APIs, code structure)
- 👥 Written for business stakeholders, not developers

---

## User Scenarios & Testing

### Primary User Story
As a container registry user, I want to browse all images published by a specific owner/organization across different registries, so that I can discover available images without knowing their exact names beforehand.

### Acceptance Scenarios

1. **Given** I am viewing the registry browser with "Docker Hub" selected and "library" as owner, **When** I click the "Browse Images" button, **Then** I see a list of all public images published by the "library" namespace

2. **Given** I am viewing the registry browser with "Quay.io" selected and "coreos" as owner, **When** I click the "Browse Images" button, **Then** I see a list of all public images published by the "coreos" namespace

3. **Given** I am viewing the registry browser with "GHCR" selected and a valid owner name, **When** I click the "Browse Images" button, **Then** the system prompts me to provide a GitHub Personal Access Token

4. **Given** I have provided a valid GitHub PAT for GHCR, **When** I browse images, **Then** I see all container packages published by that GitHub user/organization

5. **Given** I am viewing the registry browser with "GCR" selected, **When** I enter a value in the owner field, **Then** the system indicates this should be a GCP Project ID, not a username

6. **Given** I am viewing a list of images, **When** I click on a specific image, **Then** the main form is populated with that owner/image combination and the tag list is automatically fetched

7. **Given** I am viewing a list of 100+ images, **When** I scroll to the bottom, **Then** the next page of results is automatically loaded (for registries with pagination)

8. **Given** I have browsed images and selected one, **When** I close the browse dialog, **Then** the selected image information persists in the main form

### Edge Cases
- What happens when an owner has no published images? → Display "No images found for this owner"
- How does the system handle network errors during image browsing? → Show error message with retry option
- What happens when a GitHub PAT is invalid or expired for GHCR? → Clear error message directing user to generate new token
- How does the system handle registries with different authentication requirements? → Registry-specific prompts and help text
- What happens when a GCR user enters a username instead of project ID? → Show helpful error explaining GCR's project-based model
- How does the system handle rate limiting from registry APIs? → Display rate limit message with countdown/retry guidance

## Requirements

### Functional Requirements

#### Core Browsing Functionality
- **FR-001**: System MUST provide a "Browse Images" action that allows users to discover images without knowing exact image names
- **FR-002**: System MUST display a list of images showing Owner and Image Name columns
- **FR-003**: System MUST allow users to select an image from the browse list to populate the main tag viewer form
- **FR-004**: System MUST automatically fetch tags when an image is selected from the browse list

#### Registry-Specific Requirements

##### Docker Hub
- **FR-005**: System MUST support browsing public images on Docker Hub without requiring authentication
- **FR-006**: System MUST display Docker Hub-specific metadata (description, star count, pull count) when available

##### Quay.io
- **FR-007**: System MUST support browsing public images on Quay.io without requiring authentication
- **FR-008**: System MUST display Quay.io-specific metadata (description, repository state) when available

##### GitHub Container Registry (GHCR)
- **FR-009**: System MUST prompt users to provide a GitHub Personal Access Token when browsing GHCR images
- **FR-010**: System MUST securely store the GitHub PAT for the duration of the browsing session
- **FR-011**: System MUST clearly communicate that the GitHub PAT requires "read:packages" scope
- **FR-012**: System MUST differentiate between GHCR registry authentication (for tags) and GitHub API authentication (for package listing)
- **FR-013**: System MUST provide help text or link to GitHub PAT creation documentation

##### Google Container Registry (GCR)
- **FR-014**: System MUST indicate to users that GCR requires a GCP Project ID instead of a username/owner
- **FR-015**: System MUST adjust the input field label from "Owner" to "Project ID" when GCR is selected
- **FR-016**: System MUST provide help text explaining GCR's project-based organization model

#### User Experience
- **FR-017**: System MUST provide visual feedback when the browse operation is in progress (loading indicator)
- **FR-018**: System MUST handle pagination for registries that return large numbers of images
- **FR-019**: System MUST display appropriate error messages when an owner/project has no images
- **FR-020**: System MUST allow users to search/filter within the browsed image list
- **FR-021**: System MUST display the last updated timestamp for each image when available
- **FR-022**: System MUST provide a way to close/cancel the browse dialog without selecting an image

#### Error Handling
- **FR-023**: System MUST display clear error messages when network requests fail
- **FR-024**: System MUST handle and display registry-specific error responses (404 for unknown owner, 401 for auth failures)
- **FR-025**: System MUST handle and display API rate limiting messages with guidance
- **FR-026**: System MUST validate GitHub PAT format before making API requests
- **FR-027**: System MUST clear invalid/expired tokens and re-prompt the user

#### Security & Privacy
- **FR-028**: System MUST only store GitHub PAT in browser session/local storage with HTTPS enforcement
- **FR-029**: System MUST allow users to clear/revoke stored GitHub PAT
- **FR-030**: System MUST not transmit GitHub PAT to the backend server
- **FR-031**: System MUST not log or persist tokens in any permanent storage

### Key Entities

- **Image Listing**: Represents a container image in a registry's catalog
  - Owner/Namespace: The organization or user that published the image
  - Image Name: The name of the container image
  - Last Updated: Timestamp when the image was last modified
  - Metadata: Registry-specific information (description, stats, visibility)
  - Registry Type: Which registry this image belongs to (Docker Hub, Quay, GHCR, GCR)

- **Browse Session**: Represents an active image browsing session
  - Selected Registry: Which registry is being browsed
  - Owner/Project ID: The identifier being queried
  - Authentication State: Whether user has provided required credentials (for GHCR)
  - Pagination State: Current page/cursor for multi-page results
  - Filter State: Any active search/filter criteria

- **Registry Credential**: Represents authentication information for a specific registry
  - Registry Type: GHCR (only registry requiring browse credentials currently)
  - Token Value: The GitHub PAT
  - Token Scope: Expected to include "read:packages"
  - Expiration/Validity: Whether token is still valid
  - Storage Location: Browser localStorage/sessionStorage

---

## Review & Acceptance Checklist

### Content Quality
- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

### Requirement Completeness
- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous  
- [x] Success criteria are measurable
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

---

## Execution Status

- [x] User description parsed
- [x] Key concepts extracted
- [x] Ambiguities marked (all resolved via research)
- [x] User scenarios defined
- [x] Requirements generated
- [x] Entities identified
- [x] Review checklist passed

---
