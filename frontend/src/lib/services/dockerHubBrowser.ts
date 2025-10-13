import type { ImageListing } from '$lib/types/browse';

export async function listRepositories(
  namespace: string,
  pageSize: number = 25,
  nextPageUrl?: string
): Promise<{
  repositories: ImageListing[];
  totalCount: number;
  nextPageUrl: string | null;
}> {
  const url = new URL(`/api/registries/dockerhub/${namespace}/images`, window.location.origin);
  url.searchParams.set('pageSize', pageSize.toString());
  if (nextPageUrl) {
    url.searchParams.set('nextPageUrl', nextPageUrl);
  }

  const response = await fetch(url.toString());

  if (!response.ok) {
    if (response.status === 404) {
      throw new Error(`Namespace "${namespace}" not found`);
    }
    throw new Error(`API error: ${response.status} ${response.statusText}`);
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
