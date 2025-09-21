# Data Model: GHCR Image Tag Browser (MVP)

## 0. Overview
Minimal structures required for current working implementation: parse user input, call upstream to list tags, return list of strings or an error. All data ephemeral per request; no caching or enrichment.

## 1. ImageReference
Represents parsed user input.
- Fields:
  - owner (lowercased)
  - image (lowercased)
  - tag (original casing | null)
  - raw (original input)
  - normalized (owner/image)
  - qualified (owner/image:tag if tag present else normalized)
- Validation:
  - Reject if missing '/', extra segments, or invalid characters (simple regex approximations).
  - Strip optional leading `ghcr.io/`.
  - Trim whitespace before parsing.

## 2. TagListResponse
- Fields:
  - tags: string[] (may be empty)
- Empty indicates no tags (neutral UI).

## 3. Error
- Fields:
  - code: InvalidFormat | NotFound
  - message: user-readable guidance

## 4. Parsing Logic (Summary)
1. Trim input.
2. Remove leading `ghcr.io/` if present.
3. Split on ':' (first occurrence) to separate potential tag.
4. Split left part on '/': expect exactly 2 segments.
5. Validate owner and image with conservative `[a-z0-9][a-z0-9-]{0,38}` and `[a-z0-9][a-z0-9-_]{0,127}`.
6. If invalid → Error(code=InvalidFormat).

## 5. Upstream Call
Single registry HTTP call to list tags; response mapped to tags array. (No pagination or enrichment in MVP.)

## 6. Copy Behavior
Frontend derives copy value `qualified` for each tag (concatenate normalized + ':' + tag). No digest support in MVP.

## 7. Deferred Model Elements
PaginationSet, Tag (with metadata), TruncationNotice, LookupResult timing, caching keys, retry policies, enrichment states—all intentionally omitted (see Future Enhancements in spec).
