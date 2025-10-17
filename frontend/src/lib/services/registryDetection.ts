import type { RegistryDetectionService } from '$lib/types/browse';

export const registryDetectionService: RegistryDetectionService = {
	async detectRegistry(url: string) {
		const apiUrl = new URL('/api/registries/detect', window.location.origin);
		const response = await fetch(apiUrl.toString(), {
			method: 'POST',
			headers: {
				'Content-Type': 'application/json'
			},
			body: JSON.stringify({ url })
		});

		if (!response.ok) {
			const errorText = await response.text();
			throw new Error(`Registry detection failed: ${errorText}`);
		}

		return await response.json();
	}
};
