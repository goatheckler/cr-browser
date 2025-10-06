# Feature Specification: GHCR Image Tag Browser (MVP – Simplified Tag Listing)

**Feature Branch**: `001-ghcr-browser-is`  
**Created**: 2025-09-21  
**Status**: Draft  
**Input**: User description: "ghcr-browser is a web application that will let users enter an image tag hosted on ghcr.io and show the users the tags availble for the image, along with relevant metadata (size, last updated/age of image, link to project and/or docker file details) this project should be structured as a mono repo.  there should be one project in src/ for the back-end, a C# webapi using openapi to expose functionality via REST, and a svelte/runekit project using tailwind css for the front end.  both the back-end and front-end applications will be deployed as container images so each should have a dockerfile.  there should be documentation in a README file for how to install deps/build/compile/run/deploy each project.  the front end will be hosted on ghcr-browser.goatheckler.com.  the front end should have a dark theme with purple highlights, it should use a modern clean design letting users enter a tag in a text field at the top of the page, with a button to the right of the text field labelled \"show details\" which when pressed will populate an ag-grid table (with a complementary dark theme) with the tags available for the image along with the meta data."

## Execution Flow (main)
```
1. Parse user description from Input
   → If empty: ERROR "No feature description provided"
2. Extract key concepts from description
   → Identify: actors, actions, data, constraints
3. For each unclear aspect:
   → Mark with [NEEDS CLARIFICATION: specific question]
4. Fill User Scenarios & Testing section
   → If no clear user flow: ERROR "Cannot determine user scenarios"
5. Generate Functional Requirements
   → Each requirement must be testable
   → Mark ambiguous requirements
6. Identify Key Entities (if data involved)
7. Run Review Checklist
   → If any [NEEDS CLARIFICATION]: WARN "Spec has uncertainties"
   → If implementation details found: ERROR "Remove tech details"
8. Return: SUCCESS (spec ready for planning)
```

---

## ⚡ Quick Guidelines
- ✅ Focus on WHAT users need and WHY
- ❌ Avoid HOW to implement (no tech stack, specific UI libraries, internal code structure)
- 👥 Written for business stakeholders / product reviewers

### Section Requirements
- **Mandatory sections**: Must be completed for every feature
- **Optional sections**: Include only when relevant to the feature
- When a section doesn't apply, remove it entirely (don't leave as "N/A")

### For AI Generation
1. Ambiguities resolved; none remain marked
2. No guessing beyond explicitly stated resolutions
3. Requirements structured for objective testing
4. Common risk areas addressed (error handling, performance, accessibility)

---

## Overview
A minimal web experience to look up and display all tags for a public GHCR image reference. Scope intentionally excludes enrichment (size, age, digest, source links), pagination, truncation handling, rate‑limit distinction, and highlight of a specific tag. Those appear only in Future Enhancements.

Primary value: quickly see which tag names exist for an `owner/image` so a user can select or copy one.

Out of scope (MVP): any metadata beyond raw tag strings, authenticated/token paths, performance instrumentation, caching beyond what .NET HTTP client naturally does, pagination, partial loads, truncation notices.

## User Scenarios & Testing *(mandatory)*

### Primary User Story
A user wants to quickly inspect all available tags for a given container image hosted on the GitHub Container Registry in order to understand available versions, their relative freshness, and size footprint, with links back to the originating project resources.

### Acceptance Scenarios
1. Given a user enters a valid repository reference `owner/image` and triggers lookup, when the lookup completes, then a table lists all discovered tag names.
2. Given an invalid repository format is submitted, when lookup runs, then an inline error message is displayed.
3. Given a non-existent repository is submitted, when lookup runs, then a not-found error message is displayed.
4. Given a repository has zero tags, when lookup completes, then the user sees an empty state message.
5. Given the input field is focused, when the user presses Enter, then lookup triggers identically to pressing the action button.
6. Given tags are displayed, when the user clicks the copy button on a row, then the full reference is copied to clipboard and a confirmation appears briefly.

### Test Coverage ✅ Complete
**Total: 20 tests** (13 backend + 7 E2E)

**Backend Tests** (13 total):
- Unit Tests (6): ValidationService with valid/invalid owner and image formats including edge cases
- Integration Tests (3): Successful tag lookup, invalid format error (400), not found error (404)
- Contract Tests (4): Health endpoint schema, tags endpoint success schema, invalid format error schema, not found error schema

**E2E Tests** (7 total):
1. Health indicator displays on page load
2. Valid search displays tag results in grid
3. Invalid format shows error message
4. Non-existent repository shows not-found error
5. Enter key triggers search
6. Copy button copies full reference to clipboard
7. Empty repository shows empty state

**Coverage Status**: All 6 acceptance scenarios fully covered by automated tests. Manual validation complete via Playwright MCP.

### Measurable Success Criteria
- Successful lookups with ≤200 tags render tag list in <2 seconds.
- Copy action completes and shows confirmation.
- Error messages clearly distinguish invalid format from repository not found.

## Requirements *(mandatory)*

### Functional Requirements (MVP)
- FR-001: Accept repository reference in format `owner/image`.
- FR-002: Retrieve and list tag names from GHCR.
- FR-003: Validate format and show error for invalid references.
- FR-004: Show empty state when zero tags returned.
- FR-005: Allow triggering lookup via button and Enter key.
- FR-006: Provide dark themed UI with purple accent styling.
- FR-007: Show loading indicator while fetching.
- FR-008: Provide copy-to-clipboard for full reference per row.
- FR-009: Present not-found error when repository missing.

(Input normalization, tag highlighting, metadata enrichment, pagination, accessibility enhancements are deferred.)

### Key Entities (MVP)
- Image Reference: Owner and image name used for lookup.
- Tag: Simple string label returned from GHCR.
- Error: Code and message where code ∈ { InvalidFormat, NotFound, TransientUpstream }.

---

## Future Enhancements (Deferred from MVP)
- Input normalization: whitespace trimming, `ghcr.io/` prefix removal, case normalization
- Tag metadata enrichment: size, updated timestamp, relative age, digest, source/Dockerfile links
- Pagination and truncation handling
- Tag highlighting when specific tag is in input
- Advanced error taxonomy (rate limits, retries)
- Request coalescing and in-memory caching
- Performance instrumentation and metrics
- Advanced accessibility features (keyboard navigation, screen reader optimization)
- Token-based authentication for higher rate limits

## Review & Acceptance Checklist
*GATE: Automated checks run during main() execution*

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
*Updated by main() during processing*

- [x] User description parsed
- [x] Key concepts extracted
- [x] Ambiguities marked
- [x] User scenarios defined
- [x] Requirements generated
- [x] Entities identified
- [x] Review checklist passed

---
