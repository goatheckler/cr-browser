import type { BrowseSession, RegistryType, RegistryCredential, ImageListing } from '$lib/types/browse';
import { ERROR_CODES, ERROR_MESSAGES, DEFAULT_PAGE_SIZE } from '$lib/constants/browse';
import * as dockerHubBrowser from './dockerHubBrowser';
import * as quayBrowser from './quayBrowser';
import * as ghcrBrowser from './ghcrBrowser';
import * as gcrBrowser from './gcrBrowser';

export async function loadImages(
  registryType: RegistryType,
  ownerOrProjectId: string,
  credential?: RegistryCredential,
  pageSize: number = DEFAULT_PAGE_SIZE
): Promise<BrowseSession> {
  const sessionId = crypto.randomUUID();
  
  const session: BrowseSession = {
    sessionId,
    registryType,
    ownerOrProjectId,
    authState: credential 
      ? { type: 'authenticated', credential } 
      : { type: 'unauthenticated' },
    images: [],
    totalCount: null,
    pagination: {
      currentPage: 1,
      pageSize,
      hasMore: false,
      nextPageUrl: null,
      cursor: null
    },
    filterText: '',
    selectedImage: null,
    status: 'loading',
    error: null
  };

  try {
    let images: ImageListing[] = [];
    let totalCount: number | null = null;
    let nextPageUrl: string | null = null;

    switch (registryType) {
      case 'DockerHub': {
        const result = await dockerHubBrowser.listRepositories(ownerOrProjectId, pageSize);
        images = result.repositories;
        totalCount = result.totalCount;
        nextPageUrl = result.nextPageUrl;
        break;
      }

      case 'Quay': {
        const result = await quayBrowser.listRepositories(ownerOrProjectId, true);
        images = result.repositories;
        totalCount = images.length;
        break;
      }

      case 'GHCR': {
        if (!credential) {
          throw new Error('GHCR requires authentication');
        }
        const ownerType = ownerOrProjectId.includes('-') ? 'org' : 'user';
        const result = await ghcrBrowser.listPackages(ownerType, ownerOrProjectId, credential);
        images = result.packages;
        totalCount = images.length;
        break;
      }

      case 'GCR': {
        if (!gcrBrowser.validateProjectId(ownerOrProjectId)) {
          throw new Error('Invalid GCR project ID format');
        }
        images = [];
        totalCount = 0;
        break;
      }

      default:
        throw new Error(`Unsupported registry type: ${registryType}`);
    }

    return {
      ...session,
      images,
      totalCount,
      pagination: {
        ...session.pagination,
        hasMore: nextPageUrl !== null,
        nextPageUrl
      },
      status: 'loaded',
      error: null
    };

  } catch (error) {
    const errorMessage = error instanceof Error ? error.message : 'Unknown error';
    let errorCode: string = ERROR_CODES.UNKNOWN;
    let retryable = true;

    if (errorMessage.includes('not found') || errorMessage.includes('404')) {
      errorCode = ERROR_CODES.NOT_FOUND;
      retryable = false;
    } else if (errorMessage.includes('Authentication failed') || errorMessage.includes('401')) {
      errorCode = ERROR_CODES.AUTH_FAILED;
      retryable = false;
    } else if (errorMessage.includes('Rate limit')) {
      errorCode = ERROR_CODES.RATE_LIMITED;
      retryable = true;
    } else if (errorMessage.includes('network') || errorMessage.includes('fetch')) {
      errorCode = ERROR_CODES.NETWORK_ERROR;
      retryable = true;
    }

    return {
      ...session,
      status: 'error',
      error: {
        code: errorCode,
        message: errorMessage,
        retryable,
        rateLimitReset: null
      }
    };
  }
}

export async function loadNextPage(session: BrowseSession): Promise<BrowseSession> {
  if (!session.pagination.hasMore) {
    return session;
  }

  try {
    const updatedSession = { ...session, status: 'loading' as const };

    switch (session.registryType) {
      case 'DockerHub': {
        if (!session.pagination.nextPageUrl) {
          return session;
        }

        const result = await dockerHubBrowser.listRepositories(
          session.ownerOrProjectId,
          session.pagination.pageSize,
          session.pagination.nextPageUrl
        );

        return {
          ...updatedSession,
          images: [...session.images, ...result.repositories],
          pagination: {
            ...session.pagination,
            currentPage: session.pagination.currentPage + 1,
            hasMore: result.nextPageUrl !== null,
            nextPageUrl: result.nextPageUrl
          },
          status: 'loaded',
          error: null
        };
      }

      case 'Quay':
      case 'GHCR':
      case 'GCR':
        return session;

      default:
        return session;
    }

  } catch (error) {
    const errorMessage = error instanceof Error ? error.message : 'Unknown error';
    
    return {
      ...session,
      status: 'error',
      error: {
        code: ERROR_CODES.NETWORK_ERROR,
        message: errorMessage,
        retryable: true,
        rateLimitReset: null
      }
    };
  }
}

export function searchImages(session: BrowseSession, filterText: string): BrowseSession {
  return {
    ...session,
    filterText
  };
}

export function getFilteredImages(session: BrowseSession): ImageListing[] {
  if (!session.filterText) {
    return session.images;
  }

  const searchLower = session.filterText.toLowerCase();
  return session.images.filter(image => 
    image.imageName.toLowerCase().includes(searchLower) ||
    image.owner.toLowerCase().includes(searchLower) ||
    (image.metadata.description?.toLowerCase().includes(searchLower) ?? false)
  );
}
