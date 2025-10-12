
# Implementation Plan: Multi-Registry Container Tag Browser

**Branch**: `002-multi-registry-support` | **Date**: 2025-10-12 | **Spec**: /specs/002-multi-registry-support/spec.md
**Input**: Feature specification from `/specs/002-multi-registry-support/spec.md`

## Execution Flow (/plan command scope)
```
1. Load feature spec from Input path
   → If not found: ERROR "No feature spec at {path}"
2. Fill Technical Context (scan for NEEDS CLARIFICATION)
   → Detect Project Type from context (web=frontend+backend, mobile=app+api)
   → Set Structure Decision based on project type
3. Fill the Constitution Check section based on the content of the constitution document.
4. Evaluate Constitution Check section below
   → If violations exist: Document in Complexity Tracking
   → If no justification possible: ERROR "Simplify approach first"
   → Update Progress Tracking: Initial Constitution Check
5. Execute Phase 0 → research.md
   → If NEEDS CLARIFICATION remain: ERROR "Resolve unknowns"
6. Execute Phase 1 → contracts, data-model.md, quickstart.md, agent-specific template file (e.g., `CLAUDE.md` for Claude Code, `.github/copilot-instructions.md` for GitHub Copilot, `GEMINI.md` for Gemini CLI, `QWEN.md` for Qwen Code or `AGENTS.md` for opencode).
7. Re-evaluate Constitution Check section
   → If new violations: Refactor design, return to Phase 1
   → Update Progress Tracking: Post-Design Constitution Check
8. Plan Phase 2 → Describe task generation approach (DO NOT create tasks.md)
9. STOP - Ready for /tasks command
```

**IMPORTANT**: The /plan command STOPS at step 8. Phases 2-4 are executed by other commands:
- Phase 2: /tasks command creates tasks.md
- Phase 3-4: Implementation execution (manual or via tools)

## Summary

Expand the existing GHCR-only tag browser to support multiple container registries (Docker Hub, Quay.io, GCR, and others) by leveraging the OCI Distribution Specification standard. The current GhcrClient already uses OCI-compliant endpoints, making multi-registry support feasible through abstraction of registry-specific authentication and base URLs. Users will select a registry type before lookup, with GHCR as the default to maintain backward compatibility.

## Technical Context

**Language/Version**: Backend C# (.NET 8), Frontend SvelteKit (Node 20)  
**Primary Dependencies**: HTTP client (System.Net.Http), JSON serialization (System.Text.Json), SvelteKit, Tailwind CSS, ag-grid (community), clipboard API  
**Storage**: None (in-memory cache only)  
**Testing**: .NET unit/integration/contract tests (xUnit), frontend component tests, E2E tests (Playwright)  
**Target Platform**: Linux containers (backend & frontend), browser clients (modern evergreen)  
**Project Type**: web (frontend + backend)  
**Performance Goals**: Lookup ≤2s for ≤100 tags (95th percentile) across all registries  
**Constraints**: Maintain backward compatibility; max 500 tags surfaced; memory for cache <64MB; public repositories only (no custom auth)  
**Scale/Scope**: Extend single feature vertical to 3+ registries initially; incremental registry additions over time

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Current constitution is placeholder-only without concrete principles. However, the design approach aligns with anticipated constitutional values:

**Alignment**:
- Test-First: All new registry clients require contract tests before implementation
- Simplicity: Reuse existing OCI-compliant logic from GhcrClient; avoid over-engineering
- Observability: Add registry_type to structured logging for request tracing
- Backward Compatibility: Zero breaking changes; existing tests must pass unchanged

**Potential Concerns**:
- Adding multiple registry implementations increases code surface area
- Mitigation: Use abstract base class (OciRegistryClientBase) to share common logic
- Justification: User value (multi-registry support) outweighs marginal complexity increase

**Assumptions**:
- Constitution will prioritize backward compatibility and incremental feature growth
- Test coverage requirements will mandate integration tests per registry
- No violations detected; feature extends existing architecture without fundamental redesign

## Project Structure

### Documentation (this feature)
```
specs/002-multi-registry-support/
├── plan.md              # This file (/plan command output)
├── research.md          # Phase 0 output (/plan command)
├── data-model.md        # Phase 1 output (/plan command)
├── quickstart.md        # Phase 1 output (/plan command)
├── contracts/           # Phase 1 output (/plan command)
│   └── openapi.yaml     # Updated API contract with registry parameter
└── tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)
```
# Option 2: Web application (when "frontend" + "backend" detected)
backend/
├── src/
│   ├── GhcrBrowser.Api/
│   │   ├── Clients/                    # NEW: Registry client abstractions
│   │   │   ├── IContainerRegistryClient.cs
│   │   │   ├── OciRegistryClientBase.cs
│   │   │   ├── GhcrClient.cs           # REFACTORED: Inherit from base
│   │   │   ├── DockerHubClient.cs      # NEW
│   │   │   ├── QuayClient.cs           # NEW
│   │   │   └── GcrClient.cs            # NEW (optional initial phase)
│   │   ├── Factories/                  # NEW: Registry factory
│   │   │   └── RegistryFactory.cs
│   │   ├── Models.cs                   # UPDATED: Add RegistryType enum
│   │   ├── Program.cs                  # UPDATED: DI registration
│   │   └── ...
└── tests/
    ├── contract/                        # UPDATED: New registry parameter tests
    ├── integration/                     # UPDATED: Multi-registry tests
    └── unit/                            # NEW: Registry factory tests

frontend/
├── src/
│   ├── routes/
│   │   ├── +page.svelte                # UPDATED: Registry selector UI
│   │   └── +page.ts                    # UPDATED: API calls with registry param
│   └── lib/
│       └── components/
│           └── RegistrySelector.svelte # NEW: Registry dropdown/selector
└── tests/
    └── e2e/                             # UPDATED: Multi-registry E2E tests
```

**Structure Decision**: Option 2 (web application: backend + frontend) - extends existing structure

## Phase 0: Outline & Research

### Research Tasks

1. **OCI Distribution Specification Deep Dive**:
   - Confirm tag listing endpoint standardization across registries
   - Document variations in authentication flows (token endpoints, parameters)
   - Identify registry-specific quirks (Docker Hub `library/` prefix, GCR anonymous access)

2. **Registry Authentication Patterns**:
   - Research Docker Hub token acquisition (`https://auth.docker.io/token`)
   - Research Quay.io authentication (`https://quay.io/v2/auth`)
   - Research GCR anonymous/authenticated access patterns
   - Document ECR (AWS SigV4) and ACR (Azure AD) requirements for future phases

3. **Repository Path Conventions**:
   - Document Docker Hub official image naming (`library/{image}` vs `{owner}/{image}`)
   - Document registry-specific validation rules
   - Research copy-to-clipboard format requirements per registry

4. **Backward Compatibility Strategies**:
   - Evaluate URL routing patterns (query param vs path param for registry)
   - Research ASP.NET Core middleware for redirect/compatibility layer
   - Assess impact on existing bookmarks and API consumers

5. **Frontend State Management**:
   - Research SvelteKit patterns for URL query parameter preservation
   - Evaluate accessibility requirements for registry selector component
   - Document keyboard navigation patterns for dropdown/radio group

### Consolidation Format (research.md)

For each research task:
- **Decision**: [What approach was chosen]
- **Rationale**: [Why this approach best fits requirements]
- **Alternatives Considered**: [What else was evaluated and why rejected]
- **Implementation Notes**: [Key details for Phase 1 design]

**Output**: research.md with all registry-specific decisions documented

## Phase 1: Design & Contracts
*Prerequisites: research.md complete*

### 1. Extract Entities → data-model.md

**Core Entities**:
- **RegistryType** (enum): Ghcr, DockerHub, Quay, Gcr, Ecr, Acr, Harbor, Artifactory
- **RegistryConfiguration**: BaseUrl, TokenEndpoint, ServiceName, PathPrefix (e.g., `library/` for Docker Hub)
- **RegistryRequest**: RegistryType, Owner, Image, PageSize, Last (pagination token)
- **RegistryResponse**: Tags (list), NotFound (bool), Retryable (bool), HasMore (bool)

**Validation Rules**:
- RegistryType must be supported (from configured set)
- Owner/Image format varies by registry (e.g., Docker Hub allows `library/` omission)
- Default RegistryType = Ghcr when not specified

**State Transitions**: N/A (stateless API calls)

### 2. Generate API Contracts → contracts/openapi.yaml

**New/Updated Endpoints**:
```yaml
/api/registries/{registryType}/{owner}/{image}/tags:
  get:
    parameters:
      - name: registryType
        in: path
        required: true
        schema:
          type: string
          enum: [ghcr, dockerhub, quay, gcr]
      - name: owner
        in: path
        required: true
      - name: image
        in: path
        required: true
      - name: pageSize
        in: query
        schema:
          type: integer
          default: 100
      - name: last
        in: query
        schema:
          type: string
    responses:
      200:
        description: Tag list retrieved
        content:
          application/json:
            schema:
              type: object
              properties:
                tags:
                  type: array
                  items:
                    type: string
                hasMore:
                  type: boolean
      400:
        description: Invalid request (unsupported registry or invalid format)
      404:
        description: Repository not found

# Backward compatibility endpoint (redirects to /api/registries/ghcr/...)
/api/images/{owner}/{image}/tags:
  get:
    deprecated: true
    description: Legacy endpoint - defaults to GHCR
```

### 3. Generate Contract Tests

**New Test Files**:
- `RegistryParameterTests.cs`: Validate registry parameter handling (supported/unsupported)
- `MultiRegistrySchemaTests.cs`: Verify response schema consistency across registries
- `BackwardCompatibilityTests.cs`: Ensure legacy endpoint redirects to GHCR

**Test Requirements**:
- All tests must fail initially (no implementation)
- Schema validation against updated OpenAPI spec
- Verify registry-specific error responses

### 4. Extract Test Scenarios → quickstart.md

**Integration Test Scenarios** (from acceptance scenarios):
1. Docker Hub lookup: `dockerhub/library/nginx` → returns nginx tags
2. Quay lookup: `quay/prometheus/prometheus` → returns Prometheus tags
3. GHCR lookup: `ghcr/microsoft/dotnet-samples` → preserves existing behavior
4. Legacy URL: `/api/images/microsoft/dotnet-samples/tags` → defaults to GHCR
5. Unsupported registry: Returns 400 with clear error message
6. Registry-specific not-found: Proper 404 handling per registry

**E2E Test Scenarios**:
1. Select Docker Hub → enter `library/nginx` → verify tags displayed
2. Select Quay → copy tag → verify `quay.io/prometheus/prometheus:tag` in clipboard
3. Navigate with keyboard → select registry → trigger search
4. Bookmark URL with registry param → reload → verify registry preserved

### 5. Update AGENTS.md

Run update script:
```bash
.specify/scripts/bash/update-agent-context.sh opencode
```

**Expected Updates**:
- Add multi-registry support to Active Technologies
- Add RegistryType enum, IContainerRegistryClient interface to Recent Changes
- Preserve existing manual additions between markers
- Keep under 150 lines

**Output**: data-model.md, /contracts/openapi.yaml, contract tests (failing), quickstart.md, AGENTS.md updated

## Phase 2: Task Planning Approach
*This section describes what the /tasks command will do - DO NOT execute during /plan*

### Task Generation Strategy

**From Phase 1 Design Docs**:
- Each contract endpoint → contract test task [P]
- Each entity (RegistryType, RegistryConfiguration) → model task [P]
- Each registry client (DockerHub, Quay, GCR) → implementation task
- Each acceptance scenario → integration test task
- Each E2E scenario → E2E test task

**Task Categories**:
1. **Foundation** (P = parallel-safe):
   - Define IContainerRegistryClient interface [P]
   - Define RegistryType enum [P]
   - Create RegistryConfiguration model [P]
   - Extract OciRegistryClientBase from GhcrClient
   - Refactor GhcrClient to inherit from base
   - Create RegistryFactory [P]

2. **Registry Implementations** (per registry):
   - Implement DockerHubClient + integration test
   - Implement QuayClient + integration test
   - Implement GcrClient + integration test (optional initial phase)

3. **API Updates**:
   - Add registry parameter to API endpoint
   - Implement backward compatibility middleware
   - Update validation service for registry-specific rules

4. **Frontend Updates**:
   - Create RegistrySelector component
   - Update page to include registry selector
   - Update API client to pass registry parameter
   - Update copy button for registry-specific formatting

5. **Testing**:
   - Write contract tests for registry parameter
   - Write integration tests per registry
   - Update E2E tests for multi-registry scenarios
   - Verify backward compatibility (all existing tests pass)

6. **Documentation**:
   - Update README with supported registries
   - Update quickstart with registry selection examples
   - Update OpenAPI spec with registry parameter

### Ordering Strategy

**TDD Order**: Tests before implementation
1. Contract tests → fail
2. Unit tests → fail
3. Implementation → tests pass
4. Integration tests → verify end-to-end

**Dependency Order**:
1. Models & interfaces (RegistryType, IContainerRegistryClient)
2. Base abstractions (OciRegistryClientBase)
3. GhcrClient refactoring (ensure existing tests pass)
4. Registry factory
5. New registry clients (DockerHub, Quay, GCR)
6. API endpoint updates
7. Frontend components
8. E2E tests

**Parallel Execution Markers**:
- [P] = Can be executed in parallel (no dependencies)
- Sequential tasks clearly ordered by dependency

**Estimated Output**: 35-40 numbered, ordered tasks in tasks.md

### Exit Criteria Per Task Type

- **Contract**: Schema validates against OpenAPI; test fails with NotImplemented
- **Unit**: Pure logic coverage ≥ critical path; all edge cases enumerated
- **Integration**: End-to-end registry call succeeds with real public image
- **Refactoring**: All existing tests pass unchanged
- **E2E**: User flow completes successfully; accessibility verified
- **Documentation**: Instructions reproduce quickstart scenarios

**IMPORTANT**: This phase is executed by the /tasks command, NOT by /plan

## Implementation Flow (High-Level)

### Phase 1: Backend Refactoring (Foundation)
1. Define abstraction layer (IContainerRegistryClient, RegistryType enum)
2. Extract OciRegistryClientBase from GhcrClient (shared OCI logic)
3. Refactor GhcrClient to inherit from base
4. Create RegistryFactory for client instantiation
5. **Exit Criteria**: All existing GHCR tests pass unchanged

### Phase 2: Registry Implementations
6. Implement DockerHubClient with token acquisition
7. Implement QuayClient with OCI-standard auth
8. Implement GcrClient for Google Container Registry
9. Integration test each client with known public image
10. **Exit Criteria**: 3+ registries supported with passing integration tests

### Phase 3: API Updates
11. Update API endpoint to accept registry parameter
12. Implement backward compatibility for legacy endpoint
13. Update validation service for registry-specific rules
14. **Exit Criteria**: Contract tests pass; legacy endpoint redirects correctly

### Phase 4: Frontend Updates
15. Create RegistrySelector component (dropdown/radio group)
16. Update main page to include registry selector
17. Update API calls to pass selected registry
18. Update copy button for registry-specific formatting
19. **Exit Criteria**: UI tests pass; keyboard navigation works

### Phase 5: Testing & Documentation
20. Write contract tests for new registry parameter
21. Write integration tests covering all registries
22. Update E2E tests for multi-registry scenarios
23. Verify all existing tests pass (backward compatibility)
24. Update README, quickstart, OpenAPI docs
25. **Exit Criteria**: All tests green; documentation complete

### Phase 6: Validation
26. Performance testing (≤2s response time per registry)
27. Accessibility audit (keyboard navigation, ARIA labels)
28. Backward compatibility verification (existing bookmarks work)
29. **Exit Criteria**: Meets all FR success criteria

## FR Traceability Matrix

Mapping functional requirements to implementation components:

- **FR-001** (Registry selection): RegistrySelector.svelte, API endpoint parameter
- **FR-002** (3+ registries): DockerHubClient, QuayClient, GcrClient implementations
- **FR-003** (Accessible selector): RegistrySelector with ARIA labels, keyboard nav
- **FR-004** (URL preservation): SvelteKit query param handling
- **FR-005** (GHCR compatibility): Existing GhcrClient refactored, tests unchanged
- **FR-006** (Default to GHCR): API endpoint logic, backward compat middleware
- **FR-007** (Legacy URL support): Redirect middleware in Program.cs
- **FR-008** (Registry-specific paths): Per-client repository formatting logic
- **FR-009** (Registry-specific copy format): Copy button logic with registry prefix
- **FR-010** (Registry validation): Validation service with registry-specific rules
- **FR-011** (Unsupported registry error): API endpoint validation, 400 response
- **FR-012** (Error distinction): Per-registry error mapping
- **FR-013** (Registry-specific errors): Client-specific error messages
- **FR-014** (≤2s performance): Integration tests with timeout assertions
- **FR-015** (Rate limiting): Per-registry retry policies
- **FR-016** (Retry support): Frontend retry button, backend retry logic
- **FR-017** (Keyboard accessible): RegistrySelector Tab navigation
- **FR-018** (ARIA labels): RegistrySelector accessibility attributes
- **FR-019** (Selection indication): Visual active state in selector
- **FR-020** (Selection persistence): URL query param preservation
- **FR-021** (Extensibility): IContainerRegistryClient abstraction
- **FR-022** (Configuration): appsettings.json registry config, env vars

## Phase 3+: Future Implementation
*These phases are beyond the scope of the /plan command*

**Phase 3**: Task execution (/tasks command creates tasks.md)  
**Phase 4**: Implementation (execute tasks.md following constitutional principles)  
**Phase 5**: Validation (run tests, execute quickstart.md, performance validation)

## Complexity Tracking
*Fill ONLY if Constitution Check has violations that must be justified*

No violations detected. Feature extends existing architecture using proven patterns (abstraction, factory, inheritance). Complexity increase is justified by user value and market need for multi-registry support.

## Progress Tracking
*This checklist is updated during execution flow*

**Phase Status**:
- [x] Phase 0: Research complete (/plan command) - 2025-10-12
- [x] Phase 1: Design complete (/plan command) - 2025-10-12
- [x] Phase 2: Task planning complete (/plan command - describe approach only) - 2025-10-12
- [x] Phase 3: Tasks generated (tasks.md created) - 2025-10-12
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation passed

**Gate Status**:
- [x] Initial Constitution Check: PASS (placeholder constitution, aligned with anticipated principles)
- [x] Post-Design Constitution Check: PASS (Phase 1 completed successfully)
- [x] All NEEDS CLARIFICATION resolved (research.md completed with all clarifications)
- [x] Complexity deviations documented (none - straightforward OCI abstraction)

**Deliverables Completed**:
- [x] spec.md (212 lines, 22 functional requirements)
- [x] research.md (686 lines, all registries researched)
- [x] data-model.md (507 lines, 5 entities + 2 interfaces)
- [x] contracts/openapi.yaml (335 lines, dual endpoints)
- [x] quickstart.md (540 lines, manual testing guide with 14 scenarios)
- [x] AGENTS.md updated with multi-registry technologies
- [x] tasks.md (42 numbered tasks, fully ordered with dependencies)

---
*Based on Constitution placeholder - See `/memory/constitution.md`*
