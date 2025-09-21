# Quickstart: GHCR Image Tag Browser (MVP)

## Purpose
Run backend & frontend and validate minimal tag listing user journey (no metadata, pagination, truncation, or rate limit distinction).

## Prerequisites
- .NET 8 SDK
- Node.js 20 + pnpm (or npm)
- (Optional) GitHub personal access token if you later test private images (not required for MVP public browsing)

## Steps
1. Clone repository & switch to feature branch:
   ```bash
   git clone <repo-url> ghcr-browser && cd ghcr-browser
   git checkout 001-ghcr-browser-is
   ```
2. Build backend:
   ```bash
   dotnet build GhcrBrowser.sln
   ```
3. Run backend (dev):
   ```bash
   dotnet run --project backend/src/GhcrBrowser.Api/GhcrBrowser.Api.csproj
   # Serves: http://localhost:5080/api (health, tags endpoint)
   ```
4. Run frontend (if not already running):
   ```bash
   cd frontend
   pnpm install
   pnpm dev
   # Serves: http://localhost:5173
   ```
5. Open frontend and enter an image reference:
   - Example: `library/alpine` or `ghcr.io/library/alpine`
   - Press Enter or click primary button to trigger lookup.
6. Observe results list:
   - All tag names returned (single combined set; may take multiple upstream calls internally but no pagination UI).
   - Loading indicator visible during fetch.
7. Test invalid format:
   - Input: `badformat` → shows format guidance error.
8. Test not found:
   - Use improbable repo `someuser-does-not-exist/imagething` → not-found message.
9. Test empty state (if you know an image with zero tags) → neutral message.
10. Test tag-qualified input:
   - Input: `library/alpine:latest` → list still shows tags; no highlight required.
11. Copy action:
   - Click copy for a tag; verify clipboard contains `owner/image:tag`; confirmation message visible ~1s.
12. Accessibility basics:
   - Tab to input, Enter triggers lookup.
   - Tab to copy button, Space/Enter activates copy.

## Success Checks
- Tag list appears <2s for typical public images (<200 tags).
- Copy confirmation appears and disappears after ≥1 second.
- Error messages clearly distinguish invalid format vs not found.

## Cleanup
- Ctrl+C backend & frontend processes when finished.

## Deferred (Not in MVP)
Metadata (size, age, digest), pagination, truncation notice, rate-limit distinction, retry/backoff, advanced a11y focus management.
