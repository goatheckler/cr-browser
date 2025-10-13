
# Implementation Plan: Owner Image Browser

**Branch**: `003-owner-image-browser` | **Date**: 2025-10-12 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/003-owner-image-browser/spec.md`

## Execution Flow (/plan command scope)
```
1. ✅ Load feature spec from Input path
2. ✅ Fill Technical Context (scan for NEEDS CLARIFICATION)
3. ✅ Fill the Constitution Check section
4. ✅ Evaluate Constitution Check section
5. ✅ Phase 0 → research.md (already completed)
6. ✅ Execute Phase 1 → contracts, data-model.md, quickstart.md, AGENTS.md
7. ✅ Re-evaluate Constitution Check section
8. ✅ Plan Phase 2 → Describe task generation approach
9. ✅ STOP - Ready for /tasks command
```

**IMPORTANT**: The /plan command STOPS at step 9. Phases 2-4 are executed by other commands:
- Phase 2: /tasks command creates tasks.md
- Phase 3-4: Implementation execution (manual or via tools)

## Summary

This feature adds the ability to browse all container images published by a specific owner/organization across all four supported registries (GHCR, Docker Hub, Quay.io, GCR). Users can click a "Browse Images" button to see a list of available images and select one to automatically populate the main tag viewer form.

**Technical Approach** (from research):
- **Docker Hub/Quay**: Frontend makes direct CORS-friendly API calls (no auth for public repos)
- **GHCR**: Frontend prompts for GitHub PAT, stores in localStorage, makes GitHub API calls
- **GCR**: Change UX to request "Project ID" instead of "Owner"
- All logic implemented in frontend (SvelteKit); no backend changes required

## Technical Context
**Language/Version**: Frontend: TypeScript (SvelteKit, Node 20), Backend: C# (.NET 8)  
**Primary Dependencies**: SvelteKit, Tailwind CSS, ag-grid (community), existing System.Net.Http backend  
**Storage**: Browser localStorage for GHCR GitHub PAT (client-side only)  
**Testing**: Frontend: Playwright (e2e tests), Backend: xUnit (contract/integration/unit tests)  
**Target Platform**: Web application (browser-based UI, cross-platform backend)  
**Project Type**: web (frontend + backend structure)  
**Performance Goals**: <500ms for API responses, responsive UI during pagination  
**Constraints**: HTTPS-only for token storage, no backend storage of GitHub PAT, registry API rate limits  
**Scale/Scope**: 4 registries, ~50-1000 images per owner query, pagination support required

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Analysis**: The constitution file contains only template placeholders. No project-specific constitutional principles have been defined for this codebase.

**Conclusion**: ✅ **PASS** - No constitutional violations possible when no constitution exists.

**Recommendation**: Consider establishing project constitution in future iterations to guide architectural decisions around:
- Test-first development requirements
- Security principles (e.g., never-backend-token-storage rule)
- API contract standards
- Multi-registry extensibility patterns

## Project Structure

### Documentation (this feature)
```
specs/003-owner-image-browser/
├── plan.md              # This file (/plan command output)
├── research.md          # ✅ Phase 0 complete
├── data-model.md        # ⏳ Phase 1 output
├── quickstart.md        # ⏳ Phase 1 output
├── contracts/           # ⏳ Phase 1 output (if needed)
└── tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)
```
backend/
├── src/
│   └── CrBrowser.Api/
│       ├── Models.cs
│       ├── Program.cs
│       ├── IContainerRegistryClient.cs
│       ├── RegistryFactory.cs
│       └── [DockerHub/Ghcr/Quay/Gcr]Client.cs
└── tests/
    ├── contract/
    ├── integration/
    └── unit/

frontend/
├── src/
│   ├── routes/
│   │   ├── +page.svelte              # Main UI
│   │   ├── +page.ts                  # Data loading
│   │   ├── +layout.svelte
│   │   └── RegistrySelector.svelte   # Existing component
│   └── app.html
└── tests/
    └── e2e/
        ├── health.spec.ts
        ├── registry-selector.spec.ts
        ├── tags.spec.ts
        └── user-interactions.spec.ts
```

**Structure Decision**: Option 2 (Web application) - backend/ + frontend/ detected

## Phase 0: Outline & Research

### Research Status: ✅ **COMPLETE**

Research conducted in `research.md` addressed the following unknowns:

1. **Registry API Availability** for listing images by owner:
   - ✅ GHCR: GitHub Packages API (requires GitHub PAT with `read:packages`)
   - ✅ Docker Hub: Public API v2 (no auth for public repos)
   - ✅ Quay.io: Public API v1 (no auth for public repos)
   - ✅ GCR: Project-based model (requires project ID, not username)

2. **Authentication Requirements**:
   - GHCR: Separate GitHub PAT needed (different from registry token)
   - Docker Hub/Quay: No auth for public repositories
   - GCR: Service account or gcloud auth (out of scope for MVP)

3. **Implementation Options Evaluated**:
   - Option 1: Docker Hub + Quay only (rejected - incomplete feature)
   - Option 2: Mock/placeholder for GHCR/GCR (rejected - poor UX)
   - **Option 3: All 4 registries with registry-specific UX** ← **SELECTED**

4. **Frontend vs Backend Decision**:
   - **Decision**: Frontend implementation only
   - **Rationale**: All registry APIs support CORS, GitHub PAT should never reach backend
   - **Implication**: No new backend endpoints needed

**Output**: `research.md` with comprehensive registry API analysis and implementation strategy

## Phase 1: Design & Contracts

### 1. Data Model (`data-model.md`)

**Entities to Extract from Spec**:
- Image Listing (owner, name, last_updated, metadata, registry_type)
- Browse Session (registry, owner/project_id, auth_state, pagination, filters)
- Registry Credential (registry_type, token, scope, validity, storage_location)

**Relationships**:
- Browse Session → Multiple Image Listings
- Browse Session → Optional Registry Credential (for GHCR)
- Image Listing → Registry Type (enum)

**Validation Rules**:
- GitHub PAT format validation (ghp_*)
- Project ID format for GCR (alphanumeric + hyphens)
- Registry type must be one of [GHCR, Docker Hub, Quay, GCR]

**State Transitions**:
- Browse Session: Idle → Loading → Loaded/Error
- GHCR Auth: Unauthenticated → Token Prompt → Authenticated → Invalid/Expired

### 2. API Contracts

**Analysis**: This feature is **frontend-only**. No new backend API endpoints required.

**Frontend Service Contracts** (TypeScript interfaces):
- `RegistryBrowserService` interface for image listing operations
- `GhcrAuthService` interface for GitHub PAT management
- Type definitions for Image, BrowseSession, PaginationState

**External API Contracts** (consumed by frontend):
- Docker Hub: `GET https://hub.docker.com/v2/repositories/{namespace}/`
- Quay.io: `GET https://quay.io/api/v1/repository?namespace={namespace}`
- GHCR: `GET https://api.github.com/users/{username}/packages?package_type=container`
- GCR: Not implemented in MVP (project ID collection only)

**Contract Test Strategy**:
- No new backend contract tests (no new endpoints)
- Frontend integration tests will verify external API consumption
- E2E tests will validate end-to-end user flows

**Decision**: `/contracts/` directory will contain:
- `frontend-services.ts` - TypeScript interface definitions
- `external-apis.md` - Documentation of consumed external APIs

### 3. Test Scenarios

**From User Stories** (spec.md):

**E2E Test Scenarios** (Playwright):
1. `browse-images-dockerhub.spec.ts` - Browse Docker Hub "library" images
2. `browse-images-quay.spec.ts` - Browse Quay.io "coreos" images
3. `browse-images-ghcr-auth.spec.ts` - GHCR PAT prompt and authentication flow
4. `browse-images-ghcr-listing.spec.ts` - GHCR image listing with valid PAT
5. `browse-images-gcr-ux.spec.ts` - GCR project ID field label/help text
6. `browse-images-selection.spec.ts` - Click image → populate form → fetch tags
7. `browse-images-pagination.spec.ts` - Scroll pagination for 100+ images
8. `browse-images-persistence.spec.ts` - Selected image persists after dialog close

**Integration Test Scenarios**:
- Frontend service unit tests for each registry-specific API client
- GitHub PAT validation logic
- Pagination state management
- Error handling for network failures

**Quickstart Test** = Manual validation of all 8 scenarios above

### 4. Agent Context Update

**Update to `AGENTS.md`**:
- Add: Browser localStorage for client-side token storage
- Add: GitHub Packages API integration (frontend)
- Add: Docker Hub/Quay public API consumption (frontend)
- Update recent changes: Feature 003 - Owner Image Browser (frontend-only, multi-registry support)
- Preserve existing manual additions

**Execution**: Run `.specify/scripts/bash/update-agent-context.sh opencode`

**Output**: Updated `AGENTS.md` in repository root

## Phase 2: Task Planning Approach
*This section describes what the /tasks command will do - DO NOT execute during /plan*

**Task Generation Strategy**:

1. **Load base template**: `.specify/templates/tasks-template.md`

2. **Generate tasks from design docs**:
   - Each entity in data-model.md → TypeScript type definition task [P]
   - Each service contract → service implementation task [P]
   - Each E2E scenario → Playwright test task (serial, depends on UI)
   - Each registry integration → API client task [P]

3. **Implementation tasks**:
   - Create Browse Images dialog component
   - Implement registry-specific browser services (Docker Hub, Quay, GHCR, GCR)
   - Implement GitHub PAT authentication flow
   - Implement pagination handling
   - Implement image selection → form population
   - Add GCR-specific UX (project ID field label)
   - Wire up "Browse Images" button to existing RegistrySelector

4. **Testing tasks** (TDD order):
   - Write E2E tests first (failing)
   - Write service unit tests first (failing)
   - Implement to make tests pass
   - Validation: Run all tests, verify quickstart.md

**Ordering Strategy**:
- Phase A: Type definitions and contracts [P]
- Phase B: Service implementations [P] (depends on Phase A)
- Phase C: UI component implementation (depends on Phase B)
- Phase D: E2E test implementation (depends on Phase C)
- Phase E: Integration and validation

**Estimated Output**: 20-25 numbered tasks in tasks.md

**IMPORTANT**: This phase is executed by the /tasks command, NOT by /plan

## Phase 3+: Future Implementation
*These phases are beyond the scope of the /plan command*

**Phase 3**: Task execution (/tasks command creates tasks.md)  
**Phase 4**: Implementation (execute tasks.md following TDD approach)  
**Phase 5**: Validation (run e2e tests, verify quickstart.md flows, check rate limiting)

## Complexity Tracking
*Fill ONLY if Constitution Check has violations that must be justified*

N/A - No constitutional violations (no constitution defined)

## Progress Tracking
*This checklist is updated during execution flow*

**Phase Status**:
- [x] Phase 0: Research complete (/plan command)
- [x] Phase 1: Design complete (/plan command)
- [x] Phase 2: Task planning complete (/plan command - describe approach only)
- [ ] Phase 3: Tasks generated (/tasks command)
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation passed

**Gate Status**:
- [x] Initial Constitution Check: PASS
- [x] Post-Design Constitution Check: PASS
- [x] All NEEDS CLARIFICATION resolved
- [x] Complexity deviations documented (N/A)

---
*Based on Constitution v2.1.1 - See `/memory/constitution.md`*
