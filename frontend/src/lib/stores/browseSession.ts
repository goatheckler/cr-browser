import { writable } from 'svelte/store';
import type { BrowseSession } from '$lib/types/browse';

export const browseSession = writable<BrowseSession | null>(null);
