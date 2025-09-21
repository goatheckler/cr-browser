# Tasks: GHCR Image Tag Browser (MVP)

Scope: Minimal tag listing only. Excludes metadata enrichment, pagination, truncation, digest formatting, rate‑limit distinction, retries, caching.

## Conventions
`[ID] Description (Mapped FRs)`

## Task List
- [ ] T01 Align OpenAPI to MVP (DONE in repo – verify) (FR-001..FR-005, FR-012)
- [ ] T02 Prune deprecated code & tests (formatters, enrichment, unused manifest logic references) (FR-001..FR-012)
- [ ] T03 Adjust or remove tests referencing removed features (digest, size, age) (FR-001..FR-012)
- [ ] T04 Ensure validation covers trimming, prefix removal, lowercase normalization (FR-004, FR-006, FR-007)
- [ ] T05 Implement / refine tags endpoint returning `{ tags: [] }` only (FR-001..FR-003, FR-005, FR-012)
- [ ] T06 Implement simple not-found vs invalid format error responses (FR-004, FR-012)
- [ ] T07 Frontend: minimal page (input + button + list + loading + errors + empty) (FR-001..FR-012)
- [ ] T08 Frontend: copy-to-clipboard per tag with 1s confirmation (FR-011)
- [ ] T09 Frontend: Enter key triggers lookup (FR-008)
- [ ] T10 Dark theme + purple accent styling basics (FR-009)
- [ ] T11 Update quickstart & README to reflect MVP + deferred list (docs)
- [ ] T12 Run end-to-end manual quickstart validation & record notes

## Validation Checklist
- [ ] Only two error codes exposed: InvalidFormat, NotFound
- [ ] No pagination or metadata fields in any response
- [ ] Copy action works for at least one real public image
- [ ] Dark theme applied; accessible focus visible
- [ ] README and quickstart free of enrichment/pagination wording

## Deferred (Tracked in spec Future Enhancements)
Enrichment, pagination, truncation notice, highlight tag, digest, retry/backoff, caching, rate limit distinction, advanced a11y, performance metrics, token redaction.
