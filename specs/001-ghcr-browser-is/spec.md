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
1. Given a user enters a repository reference `owner/image` and triggers lookup, when the lookup completes, then a table lists all discovered tag names (unsorted or simple lexical sort acceptable) with no additional metadata.
2. Given an invalid repository format is submitted, when lookup runs, then an inline error explains the correct `owner/image` format.
3. Given a non-existent repository is submitted, when lookup runs, then an error states the repository was not found.
4. Given a repository has zero tags, when lookup completes, then the user sees a neutral empty state message.
5. Given the input field is focused, when the user presses Enter, then lookup triggers identically to pressing the action button.
6. Given tags are displayed, when the user presses the copy control on a row (only full reference `owner/image:tag`), then the clipboard receives that value and a short confirmation appears (≥1s).

### Edge Cases
- Leading/trailing whitespace trimmed before validation.
- Input including `ghcr.io/` prefix accepted and normalized out in display.
- Mixed-case owner/image normalized to lowercase.
- Empty or whitespace-only input does not trigger a request; user sees guidance.
- Large tag sets: all returned in a single response (no pagination) – practical limit is whatever the upstream call returns; no truncation messaging in MVP.

### Measurable Success Criteria
- 95% of successful lookups with ≤200 tags render tag list in <2 seconds.
- Copy action places value on clipboard in <150 ms locally.
- Error clarity: ≥90% of testers correctly identify how to correct invalid format vs not found.

## Requirements *(mandatory)*

### Functional Requirements (MVP)
- FR-001: Accept repository reference without a tag (`owner/image`).
- FR-002: Accept repository reference with a tag (`owner/image:tag`) and include that tag in results (no highlight requirement).
- FR-003: Retrieve and list tag names returned by upstream (single request, no pagination logic).
- FR-004: Validate format and show corrective error for invalid references.
- FR-005: Show neutral empty state when zero tags returned.
- FR-006: Normalize optional leading `ghcr.io/` prefix out of displayed reference.
- FR-007: Trim leading/trailing whitespace.
- FR-008: Allow triggering lookup via button and Enter key.
- FR-009: Provide dark themed UI with purple accent styling for primary action.
- FR-010: Show loading indicator while fetching.
- FR-011: Provide copy-to-clipboard for full reference (`owner/image:tag`) per row.
- FR-012: Present simple not-found error when repository missing.

(Transient errors, rate limit distinction, metadata enrichment, pagination, truncation, digest handling, tag highlight, accessibility refinements, performance targets outside basic loading state are deferred.)

### Key Entities (MVP)
- Image Reference: Parsed input (owner, image, optional tagString) used for lookup.
- Tag: Simple string label; server returns array of strings.
- Error: { code, message } where code ∈ { InvalidFormat, NotFound } (Transient errors collapsed into a generic retry message if surfaced at all).

---

## Future Enhancements (Deferred from MVP)
- Tag metadata enrichment: size, updated timestamp, relative age, digest truncation & copy, source / Dockerfile links.
- Pagination (pageSize 100) and truncation cap (500) with notice.
- Tag highlight when a specific tag is part of input.
- Distinct transient vs rate limit vs permanent error taxonomy.
- Partial metadata placeholder (em dash + tooltip) and metadata state management.
- Retry & backoff policy plus request coalescing & in-memory caching.
- Performance instrumentation (latency logging, metrics, client render timing overlay).
- Accessibility refinements: focus management on error, aria-live announcements for state changes, keyboard row navigation cues.
- Token-based higher rate limit support & redaction guarantees.
- Truncation notice wording and explicit counts.

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
