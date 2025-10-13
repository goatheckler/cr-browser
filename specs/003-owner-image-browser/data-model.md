# Data Model: Owner Image Browser

**Feature**: 003-owner-image-browser  
**Created**: 2025-10-12  
**Source**: [spec.md](./spec.md)

---

## Entity Definitions

### 1. ImageListing

Represents a container image discovered in a registry's catalog during a browse operation.

**TypeScript Type**:
```typescript
interface ImageListing {
  // Core identity
  owner: string;                    // Namespace/organization/user
  imageName: string;                // Repository/image name
  registryType: RegistryType;       // Which registry this belongs to
  
  // Temporal
  lastUpdated: Date | null;         // When image was last modified
  createdAt: Date | null;           // When image was first published (if available)
  
  // Registry-specific metadata
  metadata: ImageMetadata;
}

type RegistryType = 'GHCR' | 'DockerHub' | 'Quay' | 'GCR';

interface ImageMetadata {
  // Docker Hub specific
  description?: string;
  starCount?: number;
  pullCount?: number;
  
  // Quay.io specific
  isPublic?: boolean;
  repositoryState?: 'NORMAL' | 'READ_ONLY' | 'MIRROR';
  
  // GHCR specific
  packageId?: number;
  visibility?: 'public' | 'private' | 'internal';
  htmlUrl?: string;
  
  // GCR specific (future)
  projectId?: string;
}
```

**Validation Rules**:
- `owner` must be non-empty string
- `imageName` must be non-empty string
- `registryType` must be one of the enum values
- `lastUpdated` should be valid ISO 8601 date when present
- For GHCR: `metadata.packageId` required
- For Docker Hub: `metadata.pullCount` should be non-negative
- For Quay: `metadata.isPublic` required

**State**: Immutable - represents point-in-time snapshot from registry API

---

### 2. BrowseSession

Represents an active image browsing session with state management for UI and pagination.

**TypeScript Type**:
```typescript
interface BrowseSession {
  // Session identity
  sessionId: string;                // Unique session identifier (UUID)
  
  // Configuration
  registryType: RegistryType;       // Which registry being browsed
  ownerOrProjectId: string;         // Owner name or GCR project ID
  
  // Authentication state (GHCR only)
  authState: AuthState;
  
  // Results state
  images: ImageListing[];           // Current page of results
  totalCount: number | null;        // Total available (if known)
  
  // Pagination state
  pagination: PaginationState;
  
  // UI state
  filterText: string;               // Client-side search filter
  selectedImage: ImageListing | null; // Currently selected image
  
  // Status
  status: 'idle' | 'loading' | 'loaded' | 'error';
  error: ErrorInfo | null;
}

interface PaginationState {
  currentPage: number;              // 1-based page number
  pageSize: number;                 // Items per page
  hasMore: boolean;                 // Whether more pages exist
  nextPageUrl: string | null;       // Docker Hub next URL
  cursor: string | null;            // Cursor-based pagination token
}

interface ErrorInfo {
  code: string;                     // Error code (NETWORK_ERROR, AUTH_FAILED, etc.)
  message: string;                  // Human-readable message
  retryable: boolean;               // Whether operation can be retried
  rateLimitReset: Date | null;      // When rate limit resets (if applicable)
}

type AuthState = 
  | { type: 'unauthenticated' }
  | { type: 'authenticated'; credential: RegistryCredential }
  | { type: 'invalid'; reason: string };
```

**Validation Rules**:
- `sessionId` must be valid UUID
- `registryType` must be valid enum value
- `ownerOrProjectId` must be non-empty string
- For GCR: `ownerOrProjectId` must match project ID format (lowercase, alphanumeric + hyphens)
- `pagination.currentPage` must be >= 1
- `pagination.pageSize` must be > 0
- If `status === 'error'`, `error` must be non-null
- If `registryType === 'GHCR'` and `status === 'loading'|'loaded'`, `authState.type` must be 'authenticated'

**State Transitions**:
```
[Initial] -> idle
idle -> loading (user clicks Browse Images)
loading -> loaded (API success)
loading -> error (API failure)
loaded -> loading (pagination, retry)
error -> loading (retry)

For GHCR:
authState.unauthenticated -> authenticated (user provides PAT)
authState.authenticated -> invalid (token expires/revoked)
authState.invalid -> authenticated (user provides new PAT)
```

---

### 3. RegistryCredential

Represents authentication information for registries requiring credentials for browse operations.

**TypeScript Type**:
```typescript
interface RegistryCredential {
  // Identity
  registryType: 'GHCR';             // Currently only GHCR requires browse credentials
  
  // Token data
  tokenValue: string;               // The actual token (e.g., GitHub PAT)
  tokenPrefix: string;              // Expected prefix (e.g., 'ghp_' for GitHub PAT)
  
  // Scope & validity
  requiredScopes: string[];         // ['read:packages'] for GHCR
  isValid: boolean;                 // Validation state
  validatedAt: Date | null;         // Last successful validation
  expiresAt: Date | null;           // Token expiration (if known)
  
  // Storage
  storageLocation: 'localStorage' | 'sessionStorage';
  storageKey: string;               // Key used in browser storage
}
```

**Validation Rules**:
- `tokenValue` must be non-empty string
- For GHCR: `tokenValue` must start with 'ghp_' (GitHub Personal Access Token prefix)
- `tokenValue` minimum length: 40 characters (GitHub PAT format)
- `requiredScopes` must include 'read:packages' for GHCR
- `storageKey` must be namespaced (e.g., 'ghcr-browser:ghcr:pat')
- `isValid` must be false if `validatedAt` is null
- Cannot store with `storageLocation: 'localStorage'` unless HTTPS context

**Security Constraints**:
- MUST only be stored in browser storage (localStorage/sessionStorage)
- MUST NEVER be transmitted to backend server
- MUST NEVER be logged or persisted to disk
- MUST be cleared when marked invalid
- MUST enforce HTTPS-only storage (checked at runtime)

**Storage Format** (localStorage):
```json
{
  "tokenValue": "ghp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "validatedAt": "2025-10-12T10:30:00Z",
  "expiresAt": null
}
```

---

## Entity Relationships

```
BrowseSession
  ├── has one RegistryType (enum)
  ├── has many ImageListing (composition)
  ├── has one PaginationState (composition)
  ├── has one ErrorInfo (composition, optional)
  └── has zero-or-one RegistryCredential (association, GHCR only)

ImageListing
  ├── belongs to one RegistryType (enum)
  └── has one ImageMetadata (composition)

RegistryCredential
  └── belongs to one RegistryType (currently only GHCR)
```

---

## Domain Operations

### BrowseSession Operations

**Create Session**:
```typescript
function createBrowseSession(
  registryType: RegistryType,
  ownerOrProjectId: string,
  credential?: RegistryCredential
): BrowseSession
```

**Load Images**:
```typescript
async function loadImages(session: BrowseSession): Promise<BrowseSession>
// Returns updated session with images, pagination, status
```

**Load Next Page**:
```typescript
async function loadNextPage(session: BrowseSession): Promise<BrowseSession>
// Appends to session.images, updates pagination state
```

**Filter Images**:
```typescript
function filterImages(session: BrowseSession, filterText: string): BrowseSession
// Client-side filtering, updates session.filterText
```

**Select Image**:
```typescript
function selectImage(session: BrowseSession, image: ImageListing): BrowseSession
// Updates session.selectedImage
```

### RegistryCredential Operations

**Create Credential**:
```typescript
function createGhcrCredential(tokenValue: string): RegistryCredential
```

**Validate Token**:
```typescript
async function validateCredential(credential: RegistryCredential): Promise<boolean>
// Makes test API call to verify token works
```

**Save to Storage**:
```typescript
function saveCredential(credential: RegistryCredential): void
// Persists to localStorage/sessionStorage
```

**Load from Storage**:
```typescript
function loadCredential(registryType: 'GHCR'): RegistryCredential | null
// Retrieves from browser storage
```

**Clear Credential**:
```typescript
function clearCredential(registryType: 'GHCR'): void
// Removes from storage
```

---

## Implementation Notes

### Frontend State Management

**Svelte Stores Recommended**:
```typescript
// stores/browseSession.ts
import { writable } from 'svelte/store';

export const browseSession = writable<BrowseSession | null>(null);
export const ghcrCredential = writable<RegistryCredential | null>(null);
```

### Storage Keys Convention

```typescript
const STORAGE_KEYS = {
  GHCR_PAT: 'cr-browser:ghcr:pat',
  GHCR_PAT_VALIDATED_AT: 'cr-browser:ghcr:pat:validated',
} as const;
```

### Error Codes

```typescript
const ERROR_CODES = {
  NETWORK_ERROR: 'NETWORK_ERROR',
  AUTH_REQUIRED: 'AUTH_REQUIRED',
  AUTH_FAILED: 'AUTH_FAILED',
  INVALID_TOKEN: 'INVALID_TOKEN',
  RATE_LIMITED: 'RATE_LIMITED',
  NOT_FOUND: 'NOT_FOUND',
  UNKNOWN: 'UNKNOWN',
} as const;
```

---

## Data Flow Example

```
User clicks "Browse Images" (Docker Hub, owner: "library")
  ↓
Create BrowseSession {
  registryType: 'DockerHub',
  ownerOrProjectId: 'library',
  status: 'idle',
  authState: { type: 'unauthenticated' },
  images: [],
  pagination: { currentPage: 1, pageSize: 25, hasMore: false }
}
  ↓
loadImages(session)
  ↓
Fetch: GET https://hub.docker.com/v2/repositories/library/?page_size=25
  ↓
Parse response -> ImageListing[]
  ↓
Update session {
  status: 'loaded',
  images: [{ owner: 'library', imageName: 'nginx', ... }, ...],
  totalCount: 178,
  pagination: { currentPage: 1, hasMore: true, nextPageUrl: '...' }
}
  ↓
User scrolls to bottom
  ↓
loadNextPage(session)
  ↓
Fetch: GET {nextPageUrl}
  ↓
Append to session.images, update pagination
  ↓
User selects image "nginx"
  ↓
selectImage(session, nginxImage)
  ↓
Populate main form: owner='library', image='nginx'
  ↓
Trigger tag fetch for library/nginx
```

---

## Testing Considerations

### Unit Tests
- ImageListing validation rules
- BrowseSession state transitions
- RegistryCredential format validation
- PaginationState calculations

### Integration Tests
- loadImages() for each registry type
- Pagination handling (Docker Hub next URL)
- Error handling and retry logic
- GHCR token validation flow

### E2E Tests
- Full browse -> select -> populate flow
- GHCR authentication prompts
- GCR project ID field label
- Error states and recovery

---
