# Research: GHCR Image Tag & Metadata Browser

## Decisions & Rationale

### Registry Access Strategy
- **Decision**: Use GHCR HTTP API + GitHub Packages API for tag enumeration and manifest retrieval.
- **Rationale**: Provides authoritative tag list and metadata (timestamps, digests, layers) without needing third-party services.
- **Alternatives Considered**: Direct `docker` CLI invocation (adds runtime dependency, slower); OCI distribution generic endpoints only (lacks enriched metadata like updated time). Rejected for complexity or reduced metadata richness.

### Tag Limit & Pagination
- **Decision**: Hard cap 500 tags, page size 100.
- **Rationale**: Balances performance and utility; prevents excessive client payload and UI slowdown.
- **Alternatives**: Unlimited (risk: large payload latency); Smaller pages (50) increases interaction friction.

### Size Metric
- **Decision**: Display total compressed size (sum of layer sizes) base 1024 with decimal precision rules.
- **Rationale**: Common expectation; reproducible from manifest.
- **Alternatives**: Uncompressed size (not readily available); individual layer breakdown (excess detail v1).

### Source & Dockerfile Links
- **Decision**: Use `org.opencontainers.image.source` label only; no heuristic Dockerfile link v1.
- **Rationale**: Ensures accuracy; avoids broken links.
- **Alternatives**: Guess repository from image path; parse README—unreliable, deferred.

### Digest Presentation
- **Decision**: Truncate to first 12 chars with ellipsis; copy full via control.
- **Rationale**: Saves horizontal space; consistent with common tooling.
- **Alternatives**: Full digest inline (crowding); hash icon only (less clarity).

### Input Normalization
- **Decision**: Lowercase owner/repo, preserve tag case, strip `ghcr.io/` prefix, trim whitespace.
- **Rationale**: Consistency; reduces duplicate lookups.
- **Alternatives**: Preserve case entirely (risk: mismatched cache keys).

### Partial Metadata Handling
- **Decision**: Always render tag row; placeholder for missing fields.
- **Rationale**: User sees full set of tags even if enrichment fails.

### Duplicate In-Flight Requests
- **Decision**: Coalesce identical concurrent lookups.
- **Rationale**: Saves network & avoids flicker.

### Accessibility & Keyboard Support
- **Decision**: Enter triggers search; focus styles high contrast; tooltips accessible; copy buttons keyboard operable.
- **Rationale**: Baseline usability & compliance.

### Observability (Forward Plan)
- **Decision**: Structured logging (requestId, owner, repo, tagCount, cacheHit, latencyMs).
- **Rationale**: Enables performance verification vs. success criteria.

### Testing Approach
- **Decision**: Contract tests from OpenAPI before implementation; integration scenario for each acceptance scenario; unit tests for parsing & size aggregation.
- **Rationale**: Aligns with test-first constitutional principle.

## Open Questions (Resolved in Spec)
None outstanding—clarifications incorporated into functional requirements.

## Risks
- **Upstream Rate Limits**: May throttle frequent lookups; mitigation: optional token configuration & basic retry/backoff.
- **Large Manifest Latency**: Parallel fetch concurrency tuning required.
- **Label Absence**: Source link may often be unavailable—communicate gracefully.

## Future Considerations
- Vulnerability scanning integration.
- Tag diffing (size delta, created vs updated analysis).
- Multi-registry expansion (Docker Hub, Quay).
- Persistent caching layer if traffic grows.
