# Feature 003: Owner Image Browser - Executive Summary

## What You Requested
Add a "Browse Images" button that shows all images published by an owner across all 4 supported registries (GHCR, Docker Hub, Quay, GCR), with simplified columns showing only Owner/Image.

## Research Completion Status: ✅ COMPLETE

All 4 registries have been thoroughly researched. Detailed findings in `research.md`.

---

## TL;DR - What's Possible

| Registry | Can Browse Owner's Images? | Auth Required? | Implementation Complexity |
|----------|---------------------------|----------------|---------------------------|
| **Docker Hub** | ✅ YES | ❌ No (public repos) | 🟢 EASY |
| **Quay.io** | ✅ YES | ❌ No (public repos) | 🟢 EASY |
| **GHCR** | ✅ YES | ⚠️ Yes (GitHub PAT) | 🟡 MEDIUM |
| **GCR** | ❌ NO* | ⚠️ Yes (GCP creds) | 🔴 HARD |

*GCR uses project-based namespacing, not owner-based. Would need "project ID" instead of "owner".

---

## Key Findings

### ✅ Docker Hub (EASY - No Auth Needed)
- **API**: `GET https://hub.docker.com/v2/repositories/{owner}/`
- **Works**: Immediately, no authentication
- **Returns**: Image names, descriptions, stars, pull counts, last updated
- **Tested**: ✅ Works perfectly (see research.md)

### ✅ Quay.io (EASY - No Auth Needed)
- **API**: `GET https://quay.io/api/v1/repository?namespace={owner}&public=true`
- **Works**: Immediately, no authentication
- **Returns**: Image names, descriptions, state, quota info
- **Tested**: ✅ Works perfectly (see research.md)

### ⚠️ GHCR (MEDIUM - Requires GitHub Token)
- **API**: `GET https://api.github.com/users/{owner}/packages?package_type=container`
- **Problem**: Requires GitHub Personal Access Token (PAT) with `read:packages` scope
- **Not the same as**: GHCR registry token you already use for tags
- **Rate limits**: 60/hr without token, 5000/hr with token
- **Implementation**: User must input GitHub PAT separately

### ❌ GCR (HARD - Fundamentally Different)
- **Problem**: GCR uses GCP **project IDs**, not owner usernames
- **No API**: To list images by "owner" - concept doesn't exist
- **Would require**: Changing UX to ask for "Project ID" instead of "Owner"
- **Recommendation**: Skip for MVP, revisit in future

---

## Recommended Implementation Path

### 🎯 Option 1: MVP - Docker Hub + Quay Only (RECOMMENDED)

**What to Build**:
- "Browse Images" button works for Docker Hub & Quay
- For GHCR/GCR: Show tooltip "Coming soon" or disable button
- Simple, clean, works immediately

**Pros**:
- ✅ No authentication complexity
- ✅ Uniform UX across both registries  
- ✅ Delivers immediate value
- ✅ Can ship quickly

**Cons**:
- ❌ Doesn't work for GHCR (yet)
- ❌ Doesn't work for GCR (likely never with current model)

**Effort**: ~1-2 days implementation

---

### 🔄 Option 2: Docker Hub + Quay + GHCR (with token)

**What to Build**:
- Same as Option 1, PLUS:
- For GHCR: Prompt user for GitHub PAT
- Store token securely (localStorage/session)
- Link to docs on creating GitHub PAT

**Pros**:
- ✅ Covers 3 of 4 registries
- ✅ Reusable token pattern

**Cons**:
- ❌ Extra complexity for GHCR users
- ❌ Security considerations (token storage)
- ❌ User education needed (PAT ≠ registry token)

**Effort**: ~2-3 days implementation

---

### 🌈 Option 3: All 4 Registries (Different UX for GCR)

**What to Build**:
- Docker Hub + Quay: Works as-is
- GHCR: Token input flow
- GCR: Change "Owner" field label to "Owner/Project" and handle differently

**Pros**:
- ✅ Full coverage

**Cons**:
- ❌ Inconsistent UX (GCR behaves differently)
- ❌ Confusing for users (why does GCR need "project"?)
- ❌ Most complexity

**Effort**: ~3-5 days implementation

---

## My Recommendation

**Start with Option 1 (MVP)**:
1. Implement Docker Hub + Quay support first
2. Get it working, get user feedback
3. Phase 2: Add GHCR with token flow
4. Phase 3: Evaluate GCR based on user demand

**Rationale**:
- Docker Hub & Quay are most popular public registries
- No auth = simpler, more secure, faster to ship
- Can always add GHCR/GCR later
- 80/20 rule: Cover 80% of use cases with 20% of effort

---

## Next Steps (If You Approve)

1. **You decide**: Which option above?
2. **I'll create**: Full feature spec using `.specify` framework:
   - `spec.md` - Full specification
   - `plan.md` - Implementation plan
   - `data-model.md` - Data structures
   - `tasks.md` - Task breakdown
   - `contracts/openapi.yaml` - API contracts

3. **Then implement**: Following the spec

---

## Questions for You

1. **Which option** do you want to pursue? (1, 2, or 3)
2. **GHCR users**: Are they a priority? (affects Option 1 vs 2)
3. **GCR users**: Do they even need this feature? (GCR users likely know their project IDs)
4. **UI preference**: Modal dialog vs. dedicated page for image list?
5. **Scope confirmation**: Public repos only, or plan for private repo support?

Let me know your decision and I'll proceed with creating the full specification!
