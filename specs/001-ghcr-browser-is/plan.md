
# Implementation Plan: GHCR Image Tag & Metadata Browser

**Branch**: `001-ghcr-browser-is` | **Date**: 2025-09-21 | **Spec**: /specs/001-ghcr-browser-is/spec.md
**Input**: Feature specification from `/specs/001-ghcr-browser-is/spec.md`

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

**IMPORTANT**: The /plan command STOPS at step 7. Phases 2-4 are executed by other commands:
- Phase 2: /tasks command creates tasks.md
- Phase 3-4: Implementation execution (manual or via tools)

## Summary
Provide a minimal web interface to list tag names for a GHCR image reference and allow copying fully qualified tag references. Excludes metadata enrichment, pagination, truncation handling, digest functionality, rate limit distinction, caching, retries, and advanced accessibility beyond basic keyboard submission and copy confirmation. Future enhancements tracked separately.

## Technical Context
**Language/Version**: Backend C# (.NET 8), Frontend SvelteKit (Node 20)
**Primary Dependencies**: HTTP client (System.Net.Http), JSON serialization (System.Text.Json), SvelteKit, Tailwind CSS, ag-grid (community), clipboard API
**Storage**: None (in-memory cache only)
**Testing**: .NET unit/integration tests (xUnit or MSTest TBD), contract tests via OpenAPI schema diff, frontend component tests (later phase)
**Target Platform**: Linux containers (backend & frontend), browser clients (modern evergreen)
**Project Type**: web (frontend + backend)
**Performance Goals**: Lookup ≤2s for ≤100 tags (95th percentile), additional page render <400ms (90th percentile)
**Constraints**: Max 500 tags surfaced; memory for cache <64MB; single registry (GHCR) read-only
**Scale/Scope**: Single feature vertical; initial user base low; future enhancements may add vulnerability scanning

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Current constitution placeholders lack concrete principles; however, feature plan aligns with drafted governance additions (code quality, test-first, observability, performance). No violations detected because no conflicting architectural complexity introduced. Risk: pending formal ratification of constitution details—track as assumption.

Assumptions:
- Test-first mandate will require contract tests before implementation.
- Observability will need structured logging fields (request id, owner, repo, counts) added early.
- Simplicity principle honored (single backend service, no DB).

No violations to record.

## Project Structure

### Documentation (this feature)
```
specs/[###-feature]/
├── plan.md              # This file (/plan command output)
├── research.md          # Phase 0 output (/plan command)
├── data-model.md        # Phase 1 output (/plan command)
├── quickstart.md        # Phase 1 output (/plan command)
├── contracts/           # Phase 1 output (/plan command)
└── tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)
```
# Option 1: Single project (DEFAULT)
src/
├── models/
├── services/
├── cli/
└── lib/

tests/
├── contract/
├── integration/
└── unit/

# Option 2: Web application (when "frontend" + "backend" detected)
backend/
├── src/
│   ├── models/
│   ├── services/
│   └── api/
└── tests/

frontend/
├── src/
│   ├── components/
│   ├── pages/
│   └── services/
└── tests/

# Option 3: Mobile + API (when "iOS/Android" detected)
api/
└── [same as backend above]

ios/ or android/
└── [platform-specific structure]
```

**Structure Decision**: Option 2 (web application: backend + frontend)

## Phase 0: Outline & Research
1. **Extract unknowns from Technical Context** above:
   - For each NEEDS CLARIFICATION → research task
   - For each dependency → best practices task
   - For each integration → patterns task

2. **Generate and dispatch research agents**:
   ```
   For each unknown in Technical Context:
     Task: "Research {unknown} for {feature context}"
   For each technology choice:
     Task: "Find best practices for {tech} in {domain}"
   ```

3. **Consolidate findings** in `research.md` using format:
   - Decision: [what was chosen]
   - Rationale: [why chosen]
   - Alternatives considered: [what else evaluated]

**Output**: research.md with all NEEDS CLARIFICATION resolved (none outstanding).

## Phase 1: Design & Contracts
*Prerequisites: research.md complete*

1. **Extract entities from feature spec** → `data-model.md`:
   - Entity name, fields, relationships
   - Validation rules from requirements
   - State transitions if applicable

2. **Generate API contracts** from functional requirements:
   - For each user action → endpoint
   - Use standard REST/GraphQL patterns
   - Output OpenAPI/GraphQL schema to `/contracts/`

3. **Generate contract tests** from contracts:
   - One test file per endpoint
   - Assert request/response schemas
   - Tests must fail (no implementation yet)

4. **Extract test scenarios** from user stories:
   - Each story → integration test scenario
   - Quickstart test = story validation steps

5. **Update agent file incrementally** (O(1) operation):
   - Run `.specify/scripts/bash/update-agent-context.sh opencode`
     **IMPORTANT**: Execute it exactly as specified above. Do not add or remove any arguments.
   - If exists: Add only NEW tech from current plan
   - Preserve manual additions between markers
   - Update recent changes (keep last 3)
   - Keep under 150 lines for token efficiency
   - Output to repository root

**Output**: data-model.md, /contracts/*, failing tests, quickstart.md, agent-specific file

## Phase 2: Task Planning Approach
*This section describes what the /tasks command will do - DO NOT execute during /plan*

**Task Generation Strategy**:
- Load `.specify/templates/tasks-template.md` as base
- Generate tasks from Phase 1 design docs (contracts, data model, quickstart)
- Each contract → contract test task [P]
- Each entity → model creation task [P] 
- Each user story → integration test task
- Implementation tasks to make tests pass

**Ordering Strategy**:
- TDD order: Tests before implementation 
- Dependency order: Models before services before UI
- Mark [P] for parallel execution (independent files)

**Estimated Output**: 25-30 numbered, ordered tasks in tasks.md

**IMPORTANT**: This phase is executed by the /tasks command, NOT by /plan

## Implementation Flow (MVP Simplified)
Single thin vertical slice; no pagination, enrichment, truncation, retry logic, or advanced accessibility beyond Enter key + copy confirmation.

1. Backend minimal API endpoint `/api/images/{owner}/{image}/tags` returning `{ tags: string[] }`.
2. Validation service for repository reference (owner/image[:tag]) with trimming, `ghcr.io/` prefix removal.
3. GHCR client (existing) used only for iterative tag enumeration until upstream ends (no metadata calls).
4. Health endpoint `/api/health` retained for basic liveness.
5. Frontend page: input + button, Enter key triggers fetch, display unordered list (or basic table) of tag names, copy button per tag to write `owner/image:tag` to clipboard (1s confirmation text).
6. Error states: InvalidFormat (400) and NotFound (404) mapped to inline messages; empty list → neutral message.
7. Dark theme styling with purple accent applied to input, button, and list focus states.
8. README & quickstart updated to remove pagination, metadata, truncation, digest, rate limit references.
9. Remove deprecated formatting & enrichment code and associated tests.
10. Run tests & adjust to match reduced scope.

## (Removed legacy Execution Task Table – superseded by simplified MVP scope)
| Step | Goal | Key Artifacts (Planned Names) | Primary FRs | Test Layers | Exit Criteria |
|------|------|-------------------------------|-------------|-------------|---------------|
| 1 | Scaffold backend project | `backend/` solution, `Program.cs` | FR-001..003 | None (scaffold) | Project builds, empty health returns 200 |
| 2 | Serve OpenAPI spec | `openapi.yaml` copy served | FR-001..003, 023,025 | Contract | `/swagger` loads, spec hash logged |
| 3 | Contract tests failing | `tests/contract/ListTagsTests.cs` | FR-001..003,010..013,023,025,028 | Contract | Tests compile & fail with NotImplemented |
| 4 | Validation & normalization | `ValidationService.cs` | FR-010,014,015 | Unit | All format cases covered; invalid returns 400 in test harness |
| 5 | GHCR client abstraction | `IGhcrClient.cs`, `GhcrClient.cs` | FR-003..008,024 | Unit/Integration (mock) | Interface sealed; mockable; basic list call tested |
| 6 | Pagination + sorting | `PaginationService.cs` | FR-017,023,025 | Unit | Order descending; page boundaries enforced |
| 7 | Metadata enrichment | `TagEnrichmentService.cs` | FR-004..008,021,024,026 | Unit/Integration | Partial/Complete states verified |
| 8 | Coalescing concurrency | `InFlightRequestRegistry.cs` | FR-020,027 | Unit | Duplicate parallel requests share result |
| 9 | Truncation logic | `TruncationService.cs` | FR-003,025 | Unit | Over-cap returns cap & notice flag |
| 10 | Size & age utilities | `SizeFormatter.cs`, `AgeFormatter.cs` | FR-004,006 | Unit | Formatting spec tests pass |
| 11 | Digest truncation backend | `DigestFormatter.cs` | FR-024 | Unit | 12-char + ellipsis tests pass |
| 12 | Error mapping middleware | `ErrorMappingMiddleware.cs` | FR-010..013,028 | Integration | 400/404/429/503 mapped schema |
| 13 | Logging & observability | `LoggingMiddleware.cs` | FR (perf goals),029 | Integration | Logs contain fields; tokens absent |
| 14 | Retry & rate limiting | `RetryPolicy.cs` | FR-012,028 | Unit/Integration | 429 & 5xx retried per policy |
| 15 | Cache & TTL | `LookupCache.cs` | FR-027 | Unit | TTL eviction & memory guard tests |
| 16 | Health endpoint | `HealthController.cs` | (Support) | Contract | 200 JSON `{"status":"ok"}` |
| 17 | Frontend scaffold | `frontend/` SvelteKit | FR-018,019 | None | Dev server runs with base page |
| 18 | Theme & a11y tokens | `theme.css`, `tokens.ts` | FR-016,019 | UI unit | Contrast >=4.5:1 documented |
| 19 | API client service | `api/tagsClient.ts` | FR-001..003,023,025 | Unit | Fetch + highlight param forwarded |
| 20 | UI state model | `state/lookupStore.ts` | FR-013,020 | Unit | State transitions tested |
| 21 | Table integration | `components/TagsTable.svelte` | FR-003..008,021,024,026 | UI/Integration | Columns render placeholders |
| 22 | Highlight & scroll | (same table) | FR-009 | UI | Highlight row visible & scrolled |
| 23 | Copy & feedback | `components/CopyButton.svelte` | FR-022,024 | UI | Clipboard mock & 1s feedback tests |
| 24 | Pagination & truncation UI | `components/PaginationControls.svelte` | FR-023,025 | UI | Load-more stops at cap; notice renders |
| 25 | Error & empty states | `components/LookupStates.svelte` | FR-010..013,028 | UI | Distinct accessible banners |
| 26 | Keyboard & a11y validation | Audit script | FR-016,018,022 | A11y | Axe scan passes; tab order test |
| 27 | Performance instrumentation | `perf/metrics.ts` + backend timing | Success criteria | Integration | Log entries & client measure available |
| 28 | Docker & containerization | `backend/Dockerfile`, `frontend/Dockerfile` | Deploy need | None | Images build; health passes in container |
| 29 | README alignment | `README.md` updated | All FR refs | Doc | Instructions reproduce quickstart |
| 30 | Final validation | QA artifacts | All | Integration | All FRs traced & tests passing |

## Exit Criteria Definitions
- Unit: Pure logic coverage ≥ critical path, all edge cases enumerated.
- Contract: Schema validated against `openapi.yaml` with no undocumented fields.
- Integration: End-to-end call exercising multiple components with expected logs.
- UI: Component renders required props, accessibility roles, and passes snapshot or DOM assertions.
- A11y: Automated (axe) + keyboard traversal success without traps; aria-live announcements verified.
- Performance: Sample of 5 local runs meets success thresholds; logs contain latencyMs.
- Security/Redaction: No token substrings present in captured logs.

## (Removed legacy FR Traceability Matrix – not applicable to MVP FR set)
Mapping each FR to planned implementation components (file/class or layer). Additional detail may later live in tasks.md.
- FR-001, FR-002: Input parsing → Validation service (Step 4), API endpoint.
- FR-003: GHCR client + pagination/truncation (Steps 5,6,9).
- FR-004: Size utility + enrichment pipeline (Steps 7,10).
- FR-005, FR-006: Manifest timestamp + age utility (Steps 7,10).
- FR-007, FR-008: Label extraction in enrichment (Step 7).
- FR-009: Frontend highlight logic (Step 22).
- FR-010, FR-014, FR-015: Validation rules (Step 4) + error mapping (Step 12).
- FR-011: 404 mapping (Step 12).
- FR-012: Retry/backoff policy (Step 14).
- FR-013: Empty state detection (Steps 6,20,25).
- FR-016: Focus & interactive row styles (Step 18,26).
- FR-017: Sorting descending updated (Step 6 test & implementation).
- FR-018: Enter triggers search (Frontend scaffold + event binding Step 17/26).
- FR-019: Theme tokens & styling (Steps 17,18).
- FR-020: Loading state modeling (Step 20).
- FR-021: Placeholder logic (Step 7 + table rendering Step 21).
- FR-022: Copy controls + confirmation (Step 23).
- FR-023: Pagination (Steps 6,24).
- FR-024: Digest truncation backend + UI display (Steps 11,21,23).
- FR-025: Truncation notice (Steps 9,24).
- FR-026: Partial metadata retention (Step 7 rendering semantics).
- FR-027: Coalescing & cache (Steps 8,15).
- FR-028: Rate limit distinction & messaging (Steps 12,14,25).
- FR-029: Token redaction in logs & surfaces (Steps 13,27).

## Phase 3+: Future Implementation
*These phases are beyond the scope of the /plan command*

**Phase 3**: Task execution (/tasks command creates tasks.md)  
**Phase 4**: Implementation (execute tasks.md following constitutional principles)  
**Phase 5**: Validation (run tests, execute quickstart.md, performance validation)

## Complexity Tracking
*Fill ONLY if Constitution Check has violations that must be justified*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |


## Progress Tracking
*This checklist is updated during execution flow*

**Phase Status**:
- [x] Phase 0: Research complete (/plan command)
- [x] Phase 1: Design complete (/plan command)
- [ ] Phase 2: Task planning complete (/plan command - describe approach only)
- [ ] Phase 3: Tasks generated (/tasks command)
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation passed

**Gate Status**:
- [x] Initial Constitution Check: PASS
- [x] Post-Design Constitution Check: PASS
- [x] All NEEDS CLARIFICATION resolved
- [ ] Complexity deviations documented

---
*Based on Constitution v2.1.1 - See `/memory/constitution.md`*
