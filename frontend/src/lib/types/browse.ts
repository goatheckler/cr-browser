export type RegistryType = 'GHCR' | 'DockerHub' | 'Quay' | 'GCR';

export interface ImageListing {
  owner: string;
  imageName: string;
  registryType: RegistryType;
  lastUpdated: Date | null;
  createdAt: Date | null;
  metadata: ImageMetadata;
}

export interface ImageMetadata {
  description?: string;
  starCount?: number;
  pullCount?: number;
  isPublic?: boolean;
  repositoryState?: 'NORMAL' | 'READ_ONLY' | 'MIRROR';
  packageId?: number;
  visibility?: 'public' | 'private' | 'internal';
  htmlUrl?: string;
  projectId?: string;
}

export interface BrowseSession {
  sessionId: string;
  registryType: RegistryType;
  ownerOrProjectId: string;
  authState: AuthState;
  images: ImageListing[];
  totalCount: number | null;
  pagination: PaginationState;
  filterText: string;
  selectedImage: ImageListing | null;
  status: 'idle' | 'loading' | 'loaded' | 'error';
  error: ErrorInfo | null;
}

export interface PaginationState {
  currentPage: number;
  pageSize: number;
  hasMore: boolean;
  nextPageUrl: string | null;
  cursor: string | null;
}

export interface ErrorInfo {
  code: string;
  message: string;
  retryable: boolean;
  rateLimitReset: Date | null;
}

export type AuthState =
  | { type: 'unauthenticated' }
  | { type: 'authenticated'; credential: RegistryCredential }
  | { type: 'invalid'; reason: string };

export interface RegistryCredential {
  registryType: 'GHCR';
  tokenValue: string;
  tokenPrefix: string;
  requiredScopes: string[];
  isValid: boolean;
  validatedAt: Date | null;
  expiresAt: Date | null;
  storageLocation: 'localStorage' | 'sessionStorage';
  storageKey: string;
}

export interface RegistryBrowserService {
  loadImages(
    registryType: RegistryType,
    ownerOrProjectId: string,
    credential?: RegistryCredential,
    pageSize?: number
  ): Promise<BrowseSession>;

  loadNextPage(session: BrowseSession): Promise<BrowseSession>;

  searchImages(session: BrowseSession, filterText: string): BrowseSession;
}

export interface DockerHubBrowserService {
  listRepositories(
    namespace: string,
    pageSize?: number,
    nextPageUrl?: string
  ): Promise<{
    repositories: ImageListing[];
    totalCount: number;
    nextPageUrl: string | null;
  }>;
}

export interface QuayBrowserService {
  listRepositories(
    namespace: string,
    publicOnly?: boolean
  ): Promise<{
    repositories: ImageListing[];
  }>;
}

export interface GhcrBrowserService {
  listPackages(
    ownerType: 'user' | 'org',
    owner: string,
    credential: RegistryCredential
  ): Promise<{
    packages: ImageListing[];
  }>;
}

export interface GcrBrowserService {
  validateProjectId(projectId: string): boolean;
}

export interface GhcrAuthService {
  createCredential(tokenValue: string): RegistryCredential;

  validateCredential(credential: RegistryCredential): Promise<boolean>;

  saveCredential(credential: RegistryCredential): void;

  loadCredential(): RegistryCredential | null;

  clearCredential(): void;

  isTokenFormatValid(tokenValue: string): boolean;
}
