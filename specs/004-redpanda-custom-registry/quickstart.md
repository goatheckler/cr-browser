# Quick Start: Redpanda & Custom Registry Support

## What This Feature Does

Adds support for:
1. **Redpanda registry** as a built-in option (docker.redpanda.com)
2. **Custom OCI registries** by entering any registry URL

## For End Users

### Using Redpanda Registry

1. Open the application
2. Select "**Redpanda**" from the registry dropdown
3. Enter owner: `redpandadata`
4. Click "Browse"
5. Browse available images (redpanda, console, etc.)
6. Click "Select" on an image to view tags

### Using Custom Registry

1. Open the application
2. Select "**Custom Registry**" from dropdown
3. Enter registry URL: `docker.redpanda.com` (or any OCI registry)
4. Click "Detect Registry" (auto-probes for compatibility)
5. If compatible, enter owner/namespace
6. Browse images as normal

## For Developers

### Research Findings

**Redpanda Registry** (docker.redpanda.com):
- ✅ OCI Distribution v2 compliant
- ✅ Standard tags/list endpoint works
- ❌ No catalog endpoint (namespace-based)
- ✅ Public access (no auth required)
- ✅ HTTPS only

**Detection Method**:
```bash
# Probe for OCI compatibility
curl -I https://docker.redpanda.com/v2/
# Look for: docker-distribution-api-version: registry/2.0
```

### Quick Implementation Guide

#### Backend: Add New Registry

1. Add to enum in `Models.cs`:
   ```csharp
   public enum RegistryType {
       Ghcr, DockerHub, Quay, Gcr, Redpanda
   }
   ```

2. Create client extending `OciRegistryClientBase`:
   ```csharp
   public class RedpandaClient : OciRegistryClientBase {
       public override string BaseUrl => "https://docker.redpanda.com";
       // Implement required methods
   }
   ```

3. Register in `RegistryFactory.cs`

#### Frontend: Add Registry Option

1. Update type in `types/browse.ts`:
   ```typescript
   export type RegistryType = 'GHCR' | 'DockerHub' | 'Quay' | 'GCR' | 'Redpanda';
   ```

2. Add option in `RegistrySelector.svelte`:
   ```svelte
   <option value="redpanda">Redpanda</option>
   ```

### Testing Redpanda Registry

```bash
# Backend integration test
dotnet test --filter FullyQualifiedName~RedpandaClientTests

# Frontend E2E test
npm run test:e2e -- browse-images-redpanda.spec.ts
```

### Known Compatible Registries

| Registry | URL | Catalog | Auth |
|----------|-----|---------|------|
| Redpanda | docker.redpanda.com | ❌ | Public |
| GitLab | registry.gitlab.com | ❌ | Varies |
| Harbor | demo.goharbor.io | ✅* | Varies |

*Depends on configuration

## Common Issues

### Issue: "Registry not supported"
**Cause**: Registry doesn't implement OCI Distribution v2  
**Fix**: Verify with `curl -I https://registry.example.com/v2/`

### Issue: "Unable to list images"
**Cause**: Registry doesn't support catalog endpoint  
**Fix**: This is normal for namespace-based registries - browse specific owner/namespace

### Issue: "Authentication required"
**Cause**: Registry requires auth for browsing  
**Fix**: Will be supported in future iteration

## File Locations

```
specs/004-redpanda-custom-registry/
├── spec.md           # Full specification
├── plan.md           # Implementation plan
├── research.md       # Investigation findings
├── SUMMARY.md        # Executive summary
└── quickstart.md     # This file

backend/src/CrBrowser.Api/
├── RedpandaClient.cs           # New
├── CustomOciRegistryClient.cs  # New
└── RegistryDetectionService.cs # New

frontend/src/
├── lib/components/
│   └── CustomRegistryInput.svelte  # New
└── lib/services/
    └── customRegistryDetection.ts  # New
```

## Time Estimates

- **Phase 1** (Redpanda): 4-6 hours
- **Phase 2** (Custom Registry): 6-8 hours
- **Phase 3** (Polish): 2-3 hours
- **Total**: 12-17 hours

## Questions?

Check the full specification in `spec.md` or the detailed plan in `plan.md`.
