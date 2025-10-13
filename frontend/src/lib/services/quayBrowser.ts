import type { ImageListing } from '$lib/types/browse';

export async function listRepositories(
  namespace: string,
  publicOnly: boolean = true
): Promise<{
  repositories: ImageListing[];
}> {
  const url = new URL(`/api/registries/quay/${namespace}/images`, window.location.origin);
  url.searchParams.set('pageSize', '100');

  const response = await fetch(url.toString());

  if (!response.ok) {
    if (response.status === 404) {
      throw new Error(`Namespace "${namespace}" not found on Quay.io`);
    }
    if (response.status === 400) {
      throw new Error(`Invalid namespace: "${namespace}"`);
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
    repositories
  };
}
