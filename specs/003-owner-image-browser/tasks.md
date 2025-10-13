# Tasks: Owner Image Browser

**Input**: Design documents from `/specs/003-owner-image-browser/`
**Prerequisites**: plan.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

## Execution Flow (main)
```
1. ✅ Load plan.md from feature directory
2. ✅ Load design documents (data-model.md, contracts/, quickstart.md)
3. ✅ Generate tasks by category
4. ✅ Apply task rules (TDD, parallel marking)
5. ✅ Number tasks sequentially
6. ✅ Generate dependency graph
7. ✅ Validate task completeness
8. Ready for execution
```

## Format: `[ID] [P?] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- Include exact file paths in descriptions

## Path Conventions
**Web app structure** (per plan.md):
- Frontend: `frontend/src/`
- Backend: `backend/src/` (no changes required)
- E2E Tests: `frontend/tests/e2e/`

---

## Phase 3.1: Setup & Type Definitions

- [ ] **T001** [P] Create TypeScript types file at `frontend/src/lib/types/browse.ts` with ImageListing, BrowseSession, PaginationState, RegistryCredential interfaces from contracts/frontend-services.ts

- [ ] **T002** [P] Create constants file at `frontend/src/lib/constants/browse.ts` with error codes, storage keys, and registry-specific configuration

---

## Phase 3.2: Tests First (TDD) ⚠️ MUST COMPLETE BEFORE 3.3

**CRITICAL: These tests MUST be written and MUST FAIL before ANY implementation**

### E2E Tests (Playwright)

- [ ] **T003** [P] E2E test for browsing Docker Hub images at `frontend/tests/e2e/browse-images-dockerhub.spec.ts` - test browsing "library" namespace, verify image list appears, verify filter works

- [ ] **T004** [P] E2E test for browsing Quay.io images at `frontend/tests/e2e/browse-images-quay.spec.ts` - test browsing "coreos" namespace, verify image list appears

- [ ] **T005** [P] E2E test for GHCR authentication flow at `frontend/tests/e2e/browse-images-ghcr-auth.spec.ts` - verify PAT prompt appears, test valid/invalid token handling, verify token storage

- [ ] **T006** [P] E2E test for GHCR image listing at `frontend/tests/e2e/browse-images-ghcr-listing.spec.ts` - test browsing with authenticated session, verify package list

- [ ] **T007** [P] E2E test for GCR project ID UX at `frontend/tests/e2e/browse-images-gcr-ux.spec.ts` - verify field label changes to "Project ID", verify help text appears

- [ ] **T008** [P] E2E test for image selection at `frontend/tests/e2e/browse-images-selection.spec.ts` - select image from list, verify main form populates, verify tags load automatically

- [ ] **T009** [P] E2E test for pagination at `frontend/tests/e2e/browse-images-pagination.spec.ts` - browse Docker Hub "library" (100+ images), scroll to bottom, verify next page loads

- [ ] **T010** [P] E2E test for error handling at `frontend/tests/e2e/browse-images-errors.spec.ts` - test unknown owner, network failure, invalid GHCR token scenarios

---

## Phase 3.3: Service Layer Implementation (ONLY after tests are failing)

### Core Services

- [ ] **T011** [P] Implement GHCR authentication service at `frontend/src/lib/services/ghcrAuth.ts` - implement GhcrAuthService interface: createCredential, validateCredential, saveCredential, loadCredential, clearCredential, isTokenFormatValid

- [ ] **T012** [P] Implement Docker Hub browser service at `frontend/src/lib/services/dockerHubBrowser.ts` - implement DockerHubBrowserService interface: listRepositories with pagination support

- [ ] **T013** [P] Implement Quay.io browser service at `frontend/src/lib/services/quayBrowser.ts` - implement QuayBrowserService interface: listRepositories

- [ ] **T014** [P] Implement GHCR browser service at `frontend/src/lib/services/ghcrBrowser.ts` - implement GhcrBrowserService interface: listPackages (user and org)

- [ ] **T015** [P] Implement GCR browser service at `frontend/src/lib/services/gcrBrowser.ts` - implement GcrBrowserService interface: validateProjectId (minimal implementation for MVP)

- [ ] **T016** Implement main registry browser service at `frontend/src/lib/services/registryBrowser.ts` - implement RegistryBrowserService interface: loadImages, loadNextPage, searchImages (orchestrates registry-specific services)

---

## Phase 3.4: Svelte Stores & State Management

- [ ] **T017** [P] Create browse session store at `frontend/src/lib/stores/browseSession.ts` - writable store for BrowseSession state

- [ ] **T018** [P] Create GHCR credential store at `frontend/src/lib/stores/ghcrCredential.ts` - writable store for RegistryCredential, load from localStorage on init

---

## Phase 3.5: UI Components

- [ ] **T019** Create browse images dialog component at `frontend/src/lib/components/BrowseImagesDialog.svelte` - modal dialog with image list table, filter input, pagination controls, loading states, error display

- [ ] **T020** Create GHCR authentication dialog at `frontend/src/lib/components/GhcrAuthDialog.svelte` - PAT input form, validation, help text with link to GitHub token creation

- [ ] **T021** Create image list table component at `frontend/src/lib/components/ImageListTable.svelte` - displays ImageListing array, supports selection, filtering, pagination, registry-specific metadata columns

- [ ] **T022** Update main page at `frontend/src/routes/+page.svelte` - add "Browse Images" button, wire up to BrowseImagesDialog, handle registry-specific field labels (GCR "Project ID"), handle image selection → form population

- [ ] **T023** Update RegistrySelector component at `frontend/src/routes/RegistrySelector.svelte` - add logic to change "Owner" label to "Project ID" when GCR selected, add help text for GCR

---

## Phase 3.6: Integration & Polish

- [ ] **T024** Add error boundary and retry logic to all service calls in `frontend/src/lib/services/registryBrowser.ts` - wrap API calls with try/catch, implement exponential backoff for retries

- [ ] **T025** Add HTTPS enforcement check at `frontend/src/lib/services/ghcrAuth.ts` - verify window.location.protocol === 'https:' before storing tokens (allow localhost)

- [ ] **T026** Add loading indicators and transitions to `frontend/src/lib/components/BrowseImagesDialog.svelte` - skeleton loaders, smooth pagination, scroll position management

- [ ] **T027** Add accessibility features to all new components - ARIA labels, keyboard navigation (Enter to select, Esc to close), focus management

- [ ] **T028** Verify quickstart.md scenarios manually - run through all 8 scenarios from quickstart.md, verify behavior matches specification

- [ ] **T029** Run all E2E tests and verify they pass - execute `npm run test:e2e` in frontend/, fix any failures

- [ ] **T030** Run frontend linting and type checking - execute `npm run lint` and `npm run check` in frontend/, fix all errors

---

## Dependencies

**Critical Path**:
```
T001, T002 (Setup)
  ↓
T003-T010 (E2E Tests - must fail)
  ↓
T011-T015 (Service Layer - parallel)
  ↓
T016 (Main Service - depends on T011-T015)
  ↓
T017, T018 (Stores - parallel, depends on T001)
  ↓
T019-T023 (UI Components - sequential, depend on T016-T018)
  ↓
T024-T027 (Polish - parallel)
  ↓
T028-T030 (Validation - sequential)
```

**Detailed Dependencies**:
- T003-T010 depend on T001 (types must exist for tests to reference)
- T011-T016 depend on T001, T002 (types and constants)
- T017-T018 depend on T001 (types)
- T019 depends on T016, T017 (main service + stores)
- T020 depends on T018 (GHCR credential store)
- T021 depends on T001, T017 (types + browse session store)
- T022 depends on T019, T020, T021 (all dialog/table components)
- T023 depends on existing RegistrySelector.svelte
- T024-T027 depend on T011-T023 (all implementation complete)
- T028-T030 depend on T024-T027 (all features complete)

---

## Parallel Execution Batches

### Batch 1: Setup (can run together)
```
T001: Create types
T002: Create constants
```

### Batch 2: E2E Tests (can run together after Batch 1)
```
T003: Docker Hub E2E test
T004: Quay E2E test
T005: GHCR auth E2E test
T006: GHCR listing E2E test
T007: GCR UX E2E test
T008: Selection E2E test
T009: Pagination E2E test
T010: Error handling E2E test
```

### Batch 3: Registry Services (can run together after Batch 2)
```
T011: GHCR auth service
T012: Docker Hub browser service
T013: Quay browser service
T014: GHCR browser service
T015: GCR browser service
```

### Batch 4: Stores (can run together after T016)
```
T017: Browse session store
T018: GHCR credential store
```

### Batch 5: Polish (can run together after T023)
```
T024: Error handling
T025: HTTPS enforcement
T026: Loading indicators
T027: Accessibility
```

---

## Task Execution Notes

### TDD Workflow
1. Run Batch 1 (T001-T002)
2. Run Batch 2 (T003-T010) - **verify all tests FAIL**
3. Run Batch 3 (T011-T015) - tests should start passing
4. Run T016 - more tests pass
5. Run Batch 4 (T017-T018)
6. Run T019-T023 sequentially - all tests should pass by T023
7. Run Batch 5 (T024-T027) - refinements
8. Run T028-T030 - final validation

### Registry-Specific Considerations

**Docker Hub** (T012, T003):
- Public API: `https://hub.docker.com/v2/repositories/{namespace}/`
- Pagination via `next` URL field
- No authentication required
- Test with `library` namespace (178+ images)

**Quay.io** (T013, T004):
- Public API: `https://quay.io/api/v1/repository?namespace={namespace}&public=true`
- No pagination documented
- No authentication required
- Test with `coreos` namespace

**GHCR** (T011, T014, T005, T006):
- GitHub API: `https://api.github.com/users/{username}/packages?package_type=container`
- Requires GitHub PAT with `read:packages` scope
- Token format: `ghp_` prefix + 36 chars
- Storage: localStorage with key `cr-browser:ghcr:pat`
- Rate limit: 5000 req/hour (authenticated)

**GCR** (T015, T007):
- MVP: Project ID validation only (no listing implementation)
- Field label: "Project ID" instead of "Owner"
- Help text: "GCR uses GCP Project IDs instead of usernames. Example: google-containers"

### Security Checklist
- [ ] GitHub PAT stored only in localStorage (T011)
- [ ] HTTPS enforcement before storing tokens (T025)
- [ ] PAT never sent to backend (verified in T011, T014)
- [ ] Clear/revoke token functionality (T011)
- [ ] Token validation before use (T011)

### Performance Targets
- API responses: <500ms (verify in T028)
- Pagination smooth scrolling (verify in T026)
- Filter instant client-side (verify in T021)

---

## Validation Checklist
*GATE: Must verify before marking feature complete*

- [x] All contracts have corresponding implementations (T011-T016)
- [x] All entities have TypeScript types (T001)
- [x] All tests written before implementation (T003-T010 before T011-T023)
- [x] Parallel tasks truly independent (see Parallel Execution Batches)
- [x] Each task specifies exact file path
- [x] No task modifies same file as another [P] task
- [ ] All E2E tests pass (T029)
- [ ] Quickstart scenarios validated (T028)
- [ ] No linting/type errors (T030)

---

## Success Criteria

✅ **Feature complete when**:
1. All 30 tasks completed
2. All E2E tests passing (T029)
3. All quickstart scenarios work (T028)
4. No TypeScript/linting errors (T030)
5. GitHub PAT stored securely (client-side only)
6. Docker Hub + Quay work without authentication
7. GHCR authentication flow works reliably
8. GCR shows project ID field with help text
9. Image selection populates main form and loads tags
10. Pagination handles 100+ images smoothly

---
