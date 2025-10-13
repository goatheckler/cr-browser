import { writable } from 'svelte/store';
import type { RegistryCredential } from '$lib/types/browse';
import { loadCredential } from '$lib/services/ghcrAuth';

function createGhcrCredentialStore() {
	const initialCredential = loadCredential();
	const { subscribe, set, update } = writable<RegistryCredential | null>(initialCredential);

	return {
		subscribe,
		set,
		update,
		clear: () => set(null)
	};
}

export const ghcrCredential = createGhcrCredentialStore();
