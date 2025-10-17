import type { ImageListing } from '$lib/types/browse';

export class CatalogNotSupportedError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'CatalogNotSupportedError';
  }
}

export async function listRepositories(
  registryUrl: string,
  namespace: string,
  pageSize: number = 25,
  nextPageUrl?: string
): Promise<{
  repositories: ImageListing[];
  totalCount: number;
  nextPageUrl: string | null;
}> {
  const url = new URL(`/api/registries/custom/${namespace}/images`, window.location.origin);
  url.searchParams.set('pageSize', pageSize.toString());
  url.searchParams.set('customRegistryUrl', registryUrl);
  if (nextPageUrl) {
    url.searchParams.set('nextPageUrl', nextPageUrl);
  }

  const response = await fetch(url.toString());

  if (!response.ok) {
    if (response.status === 501) {
      const errorData = await response.json().catch(() => ({}));
      if (errorData.code === 'CatalogNotSupported') {
        throw new CatalogNotSupportedError(errorData.message || 'Registry does not support browsing');
      }
    }
    if (response.status === 404) {
      throw new Error(`Namespace "${namespace}" not found on ${registryUrl}`);
    }
    const errorData = await response.json().catch(() => ({}));
    throw new Error(errorData.message || `API error: ${response.status} ${response.statusText}`);
  }

  const data = await response.json();

  const repositories: ImageListing[] = data.images.map((img: any) => ({
    owner: img.owner,
    imageName: img.imageName,
    registryType: img.registryType,
    lastUpdated: img.lastUpdated ? new Date(img.lastUpdated) : null,
    createdAt: img.createdAt ? new Date(img.createdAt) : null,
    metadata: img.metadata || {}
  }));

  return {
    repositories,
    totalCount: data.totalCount || 0,
    nextPageUrl: data.nextPageUrl || null
  };
}
