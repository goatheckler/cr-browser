# Tasks: GHCR Image Tag Browser (MVP)

Scope: Minimal tag listing only. Excludes metadata enrichment, pagination, truncation, input normalization, digest formatting, rate‑limit distinction, retries, caching.

## Conventions
`[ID] Description (Mapped FRs)`

## Implementation Tasks
- [x] T01 Align OpenAPI to MVP (FR-001..FR-009)
- [x] T02 Implement tags endpoint returning `{ tags: [] }` (FR-001, FR-002)
- [x] T03 Implement error responses: InvalidFormat, NotFound, TransientUpstream (FR-003, FR-009)
- [x] T04 Frontend: input form + search button (FR-001, FR-005)
- [x] T05 Frontend: tag grid display with ag-grid (FR-002)
- [x] T06 Frontend: copy-to-clipboard with confirmation (FR-008)
- [x] T07 Frontend: Enter key triggers search (FR-005)
- [x] T08 Frontend: loading indicator (FR-007)
- [x] T09 Frontend: error display (FR-003, FR-009)
- [x] T10 Frontend: empty state display (FR-004)
- [x] T11 Frontend: dark theme + purple accents (FR-006)

## Testing Tasks
- [x] T12 Backend: Add validation unit tests (6 tests)
- [x] T13 Backend: Add integration tests (3 tests)
- [x] T14 Backend: Add contract tests (4 tests)
- [x] T15 Frontend: Add E2E tests (7 tests)
- [ ] T16 Update CI/CD: Create test.yml workflow
- [ ] T17 Update CI/CD: Create build.yml workflow
- [ ] T18 Update CI/CD: Create deploy.yml workflow

## Validation Checklist
- [x] Three error codes exposed: InvalidFormat, NotFound, TransientUpstream
- [x] No pagination or metadata fields in response
- [x] Copy action works for real public images
- [x] Dark theme with purple accents applied
- [x] Complete test coverage for all acceptance scenarios (20 tests total)
- [x] Manual validation complete via Playwright MCP
- [ ] CI/CD workflows functional

## Deferred (Tracked in spec Future Enhancements)
Input normalization (whitespace, prefix, case), enrichment, pagination, truncation notice, highlight tag, digest, retry/backoff, caching, rate limit distinction, advanced a11y, performance metrics, token support.
