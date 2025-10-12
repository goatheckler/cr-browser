# cr-browser Development Guidelines

Auto-generated from all feature plans. Last updated: 2025-10-12

## Active Technologies
- Backend C# (.NET 8), Frontend SvelteKit (Node 20) + HTTP client (System.Net.Http), JSON serialization (System.Text.Json), SvelteKit, Tailwind CSS, ag-grid (community), clipboard API
- Multi-registry support (GHCR, Docker Hub, Quay, GCR) + OCI Distribution Specification

## Project Structure
```
backend/
frontend/
tests/
```

## Commands
# Add commands for Backend C# (.NET 8), Frontend SvelteKit (Node 20)

## Code Style
Backend C# (.NET 8), Frontend SvelteKit (Node 20): Follow standard conventions

## Critical: Sudo Command Restrictions
**NEVER run commands that require sudo or elevated permissions.**

Before executing ANY command, verify it does not require sudo access. Commands requiring sudo break the human/opencode interaction because stdio pass-through does not function correctly with elevated permissions.

Problematic commands include:
- `sudo <anything>`
- `npx playwright install --with-deps` (installs system dependencies requiring sudo)
- Docker commands if user requires sudo for Docker
- System package managers (apt, yum, etc.)
- Any command that prompts for password/elevation

Safe alternatives:
- Use `npx playwright install` without `--with-deps` (browser-only, no system deps)
- Assume browsers are already installed
- Build and compile code without running tests that require system setup
- Use existing scripts/tools that don't require elevation

## Recent Changes
- 002-multi-registry-support: Added multi-registry support (Docker Hub, Quay.io, GCR) with OCI Distribution Specification compliance
- 001: Added Backend C# (.NET 8), Frontend SvelteKit (Node 20) + HTTP client (System.Net.Http), JSON serialization (System.Text.Json), SvelteKit, Tailwind CSS, ag-grid (community), clipboard API

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
