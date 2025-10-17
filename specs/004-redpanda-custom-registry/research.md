# Research: Redpanda Registry & Custom Registry Detection

**Date**: 2025-10-16  
**Feature**: 004-redpanda-custom-registry

## Redpanda Registry Investigation

### Registry Endpoint Testing

**Base URL**: `https://docker.redpanda.com`

#### Test 1: OCI Distribution API Version Check
```bash
curl -sI https://docker.redpanda.com/v2/
```
**Result**: 
- Status: 200 OK
- Header: `docker-distribution-api-version: registry/2.0`
- **Conclusion**: ✅ Redpanda registry is OCI Distribution v2 compliant

#### Test 2: Catalog Endpoint
```bash
curl -s https://docker.redpanda.com/v2/_catalog
```
**Result**: 
- Status: 404 Not Found
- **Conclusion**: ❌ Catalog endpoint not supported (common for namespaced registries)

#### Test 3: Tags List (Known Repository)
```bash
curl -s https://docker.redpanda.com/v2/redpandadata/redpanda/tags/list | jq .
```
**Result**: 
```json
{
  "name": "redpandadata/redpanda",
  "tags": ["latest", "v22.1.1", "v22.1.10", ...]
}
```
- **Conclusion**: ✅ Standard OCI tags/list endpoint works

#### Test 4: Manifest Pull
```bash
curl -s "https://docker.redpanda.com/v2/redpandadata/redpanda/manifests/latest" \
  -H "Accept: application/vnd.docker.distribution.manifest.v2+json"
```
**Result**: 
- Schema Version: 2
- Media Type: `application/vnd.docker.distribution.manifest.v2+json`
- **Conclusion**: ✅ Standard manifest endpoint works

#### Test 5: Another Repository
```bash
curl -s https://docker.redpanda.com/v2/redpandadata/console/tags/list | jq .
```
**Result**: 
```json
{
  "name": "redpandadata/console",
  "tags": ["latest", "v2.0.0", "v2.0.2", ...]
}
```
- **Conclusion**: ✅ Multiple repositories confirmed

### Redpanda Registry Characteristics

| Feature | Supported | Notes |
|---------|-----------|-------|
| OCI Distribution v2 API | ✅ Yes | Standard compliant |
| `/v2/` base endpoint | ✅ Yes | Returns `{}` with 200 |
| `/v2/_catalog` | ❌ No | Returns 404 |
| `/v2/{name}/tags/list` | ✅ Yes | Standard format |
| `/v2/{name}/manifests/{ref}` | ✅ Yes | Standard format |
| Authentication | ⚠️ Unknown | May require for private repos |
| HTTPS | ✅ Yes | Secure by default |

**Registry Type**: Standard OCI Distribution v2 (namespace-based, no catalog)

**Recommended Implementation**: Extend `OciRegistryClientBase` similar to how GCR is implemented (no catalog support, direct tag access)

## OCI Registry Detection Strategy

### Detection Algorithm

1. **URL Validation**
   - Parse URL to extract scheme, host, port, path
   - Validate scheme is http or https
   - Validate host is valid domain or IP
   - Default to https if no scheme provided

2. **OCI API Probe** (`/v2/` endpoint)
   ```
   GET {baseUrl}/v2/
   Expected: 
   - Status: 200 or 401 (401 indicates auth required but API exists)
   - Header: docker-distribution-api-version: registry/2.0
   - Body: {} or auth challenge
   ```

3. **Capability Detection**
   - Test catalog endpoint: `GET /v2/_catalog`
     - 200 → Catalog supported
     - 404 → Catalog not supported (namespace-based)
   - For namespace-based: Require owner/namespace input
   - For catalog-based: Can list all repositories

4. **Authentication Detection**
   - 401 response includes `Www-Authenticate` header
   - Parse header for auth method (Bearer, Basic)
   - Extract auth service URL if provided

### Detection Response Format

```json
{
  "supported": true,
  "apiVersion": "registry/2.0",
  "capabilities": {
    "catalog": false,
    "tagsList": true,
    "manifestPull": true
  },
  "authentication": {
    "required": false,
    "method": "bearer",
    "realm": "https://docker.redpanda.com/v2/token"
  },
  "registryType": "oci-v2"
}
```

### Known OCI-Compliant Registries

| Registry | Base URL | Catalog Support | Notes |
|----------|----------|-----------------|-------|
| Docker Hub | hub.docker.com | ❌ No | Namespace-based |
| GHCR | ghcr.io | ❌ No | Namespace-based |
| Quay.io | quay.io | ❌ No | Namespace-based |
| GCR | gcr.io | ❌ No | Project-based |
| Redpanda | docker.redpanda.com | ❌ No | Namespace-based |
| Harbor | registry.example.com | ✅ Yes* | Self-hosted, may vary |
| Artifactory | artifactory.example.com | ✅ Yes* | Self-hosted, may vary |
| GitLab | registry.gitlab.com | ❌ No | Namespace-based |

*Catalog support depends on configuration

## Technical Architecture Decisions

### Decision 1: Redpanda Client Implementation

**Options**:
1. Create RedpandaClient extending OciRegistryClientBase
2. Use generic CustomOciRegistryClient for Redpanda
3. Reuse existing client (e.g., GcrClient) with different config

**Chosen**: Option 1 - Dedicated RedpandaClient
**Rationale**: 
- Maintains consistency with existing architecture
- Allows Redpanda-specific customization if needed
- Clear separation of concerns
- Easy to test independently

### Decision 2: Custom Registry URL Storage

**Options**:
1. Store in localStorage (persistent)
2. Store in session state (ephemeral)
3. URL parameter (shareable)

**Chosen**: Option 2 - Session state for MVP
**Rationale**:
- Simpler implementation
- No privacy concerns about storing registry URLs
- Can add persistence later if requested
- URL parameter makes sense for sharing specific registry+image

### Decision 3: Detection Timeout

**Chosen**: 5 seconds
**Rationale**:
- Balance between user patience and slow networks
- Most registries respond within 1-2 seconds
- Prevents indefinite hanging

### Decision 4: HTTP Support for Custom Registries

**Chosen**: Support with warning
**Rationale**:
- Some self-hosted registries use HTTP (dev/testing)
- Display security warning to user
- Default to HTTPS when scheme omitted

## Example Registries for Testing

### Public OCI Registries (No Auth Required)
1. **Redpanda**: docker.redpanda.com
   - Owner: redpandadata
   - Images: redpanda, console

2. **GitLab**: registry.gitlab.com
   - Requires namespace
   - Example: registry.gitlab.com/gitlab-org/gitlab-runner

3. **Public Harbor Demo**: demo.goharbor.io
   - May have test projects available

### Testing Strategy
1. Start with Redpanda (known working, vendor-specific)
2. Test with GitLab registry (large public registry)
3. Test with invalid URLs (error handling)
4. Test with non-OCI endpoints (detection failure)

## API Compatibility Matrix

| Feature | GHCR | Docker Hub | Quay | GCR | Redpanda | Custom |
|---------|------|------------|------|-----|----------|--------|
| Image List API | ✅ | ✅ | ✅ | ✅ | ⚠️ Unknown | ⚠️ Varies |
| Tags List | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ Expected |
| OCI Standard | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ Expected |
| Pagination | ✅ | ✅ | ✅ | ✅ | ✅ Likely | ✅ Expected |

⚠️ = Needs investigation or may vary

## Security Considerations

1. **URL Injection**: Validate and sanitize custom URLs
2. **SSRF Prevention**: Consider blocking private IP ranges in production
3. **Certificate Validation**: Always validate SSL certificates for HTTPS
4. **Auth Token Handling**: Never log or expose auth tokens
5. **Rate Limiting**: Respect registry rate limits during detection

## Performance Considerations

1. **Detection Caching**: Cache detection results per session
2. **Parallel Requests**: Only probe essential endpoints
3. **Timeout Handling**: Fail fast with clear errors
4. **Progress Feedback**: Show detection status to user

## Implementation Notes

### Backend (C#)
```csharp
// Example RedpandaClient structure
public class RedpandaClient : OciRegistryClientBase
{
    public override RegistryType RegistryType => RegistryType.Redpanda;
    public override string BaseUrl => "https://docker.redpanda.com";
    
    // ListImagesAsync may return NotSupported error
    // since catalog endpoint doesn't work
}
```

### Frontend (TypeScript)
```typescript
// Example custom registry URL handling
interface CustomRegistryState {
  url: string;
  normalizedUrl: string;
  detectionStatus: 'idle' | 'detecting' | 'success' | 'error';
  capabilities: RegistryCapabilities | null;
  error: string | null;
}
```

## Open Questions

1. ✅ **Does Redpanda support image listing?** - No catalog endpoint, requires namespace
2. ✅ **Is Redpanda registry public?** - Yes, can access without auth
3. ⚠️ **Authentication method?** - Appears to be public, auth URL exists but not required
4. ❓ **Should we support HTTP registries?** - Yes with warning (decided)
5. ❓ **Max detection timeout?** - 5 seconds (decided)

## References

- [OCI Distribution Specification](https://github.com/opencontainers/distribution-spec/blob/main/spec.md)
- [Docker Registry HTTP API V2](https://docs.docker.com/registry/spec/api/)
- [Redpanda Documentation](https://docs.redpanda.com/)
