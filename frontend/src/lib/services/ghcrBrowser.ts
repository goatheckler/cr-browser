import type { ImageListing, RegistryCredential } from '$lib/types/browse';

export async function listPackages(
  ownerType: 'user' | 'org',
  owner: string,
  credential: RegistryCredential
): Promise<{
  packages: ImageListing[];
}> {
  const url = new URL(`/api/registries/ghcr/${owner}/images`, window.location.origin);
  url.searchParams.set('pageSize', '100');

  const response = await fetch(url.toString(), {
    headers: {
      'Authorization': `Bearer ${credential.tokenValue}`
    }
  });

  if (!response.ok) {
    if (response.status === 401) {
      throw new Error('Authentication failed. Please check your GitHub token.');
    }
    if (response.status === 403) {
      throw new Error('Token lacks required scopes. Ensure token has "read:packages" scope.');
    }
    if (response.status === 404) {
      throw new Error(`User/organization "${owner}" not found`);
    }
    throw new Error(`API error: ${response.status} ${response.statusText}`);
  }

  const data = await response.json();

  const packages: ImageListing[] = data.images.map((img: any) => ({
    owner: img.owner,
    imageName: img.imageName,
    registryType: img.registryType,
    lastUpdated: img.lastUpdated ? new Date(img.lastUpdated) : null,
    createdAt: img.createdAt ? new Date(img.createdAt) : null,
    metadata: img.metadata || {}
  }));

  return {
    packages
  };
}
