<script lang="ts">
	import { browseSession } from '$lib/stores/browseSession';
	import { ghcrCredential } from '$lib/stores/ghcrCredential';
	import { loadImages, loadNextPage, searchImages, getFilteredImages } from '$lib/services/registryBrowser';
	import { DEFAULT_PAGE_SIZE } from '$lib/constants/browse';
	import type { RegistryType, ImageListing } from '$lib/types/browse';
	import ImageListTable from './ImageListTable.svelte';
	import GhcrAuthDialog from './GhcrAuthDialog.svelte';

	export let open = false;
	export let registryType: RegistryType;
	export let ownerOrProjectId: string;
	export let onImageSelected: (image: ImageListing) => void;

	let showGhcrAuth = false;
	let filterText = '';
	let isLoadingMore = false;
	let localOwner = ownerOrProjectId || '';

	$: if (open) {
		localOwner = ownerOrProjectId || '';
	}

	$: filteredImages = $browseSession ? getFilteredImages($browseSession) : [];

	async function handleBrowse() {
		if (!localOwner) return;

		if (registryType === 'GHCR' && !$ghcrCredential) {
			showGhcrAuth = true;
			return;
		}

		browseSession.set({
			sessionId: crypto.randomUUID(),
			registryType,
			ownerOrProjectId: localOwner,
			authState: { type: 'unauthenticated' },
			images: [],
			totalCount: null,
			pagination: {
				currentPage: 1,
				pageSize: 25,
				hasMore: false,
				nextPageUrl: null,
				cursor: null
			},
			filterText: '',
			selectedImage: null,
			status: 'loading',
			error: null
		});

		try {
			const session = await loadImages(
				registryType,
				localOwner,
				registryType === 'GHCR' ? $ghcrCredential! : undefined
			);
			browseSession.set(session);
		} catch (err) {
			console.error('Failed to load images:', err);
		}
	}

	function handleGhcrAuthSuccess() {
		showGhcrAuth = false;
		handleBrowse();
	}

	function handleFilterChange(event: Event) {
		const target = event.target as HTMLInputElement;
		filterText = target.value;
		if ($browseSession) {
			const updated = searchImages($browseSession, filterText);
			browseSession.set(updated);
		}
	}

	async function handleLoadMore() {
		if (!$browseSession || isLoadingMore || !$browseSession.pagination.hasMore) return;

		isLoadingMore = true;
		try {
			const updated = await loadNextPage($browseSession);
			browseSession.set(updated);
		} catch (err) {
			console.error('Failed to load more images:', err);
		} finally {
			isLoadingMore = false;
		}
	}

	function handleImageSelect(image: ImageListing) {
		onImageSelected(image);
		open = false;
	}

	function handleClose() {
		open = false;
		browseSession.set(null);
		filterText = '';
	}
</script>

{#if open}
	<div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50" role="presentation" onclick={handleClose} onkeydown={(e) => e.key === 'Escape' && handleClose()}>
		<div class="bg-gray-800 rounded-lg shadow-xl w-full max-w-4xl max-h-[80vh] flex flex-col" role="dialog" aria-modal="true" onclick={(e) => e.stopPropagation()}>
			<div class="px-6 py-4 border-b border-gray-700 flex justify-between items-center">
				<h2 class="text-xl font-semibold text-white">Browse Images - {registryType}</h2>
			<button
				onclick={handleClose}
				class="text-purple-500 hover:text-purple-400 text-2xl leading-none bg-transparent border-0 p-0"
				aria-label="Close dialog"
			>
				×
			</button>
			</div>

			<div class="px-6 py-4 border-b border-gray-700 space-y-3">
				<div class="flex gap-2">
					<input
						type="text"
						placeholder={registryType === 'GCR' ? 'Project ID (e.g., google-containers)' : 'Owner/Namespace (e.g., library)'}
						bind:value={localOwner}
						class="flex-1 px-3 py-2 border border-gray-600 rounded-md bg-gray-700 text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500"
					/>
					<button
						onclick={handleBrowse}
						disabled={!localOwner || $browseSession?.status === 'loading'}
						class="px-4 py-2 bg-blue-500 text-white rounded-md hover:bg-blue-600 disabled:bg-gray-600 disabled:cursor-not-allowed"
					>
						{$browseSession?.status === 'loading' ? 'Loading...' : 'Browse'}
					</button>
				</div>
				
				{#if $browseSession?.status === 'loaded'}
					<input
						type="text"
						placeholder="Filter images..."
						value={filterText}
						oninput={handleFilterChange}
						class="w-full px-3 py-2 border border-gray-600 rounded-md bg-gray-700 text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500"
					/>
				{/if}
			</div>

			<div class="flex-1 overflow-auto px-6 py-4">
				{#if $browseSession?.status === 'loading'}
					<div class="flex items-center justify-center py-12">
						<div class="text-gray-400">Loading images...</div>
					</div>
				{:else if $browseSession?.status === 'error'}
					<div class="flex items-center justify-center py-12">
						<div class="text-red-400">
							Error: {$browseSession.error?.message || 'Failed to load images'}
						</div>
					</div>
				{:else if filteredImages.length === 0 && $browseSession?.status === 'loaded'}
					<div class="flex items-center justify-center py-12">
						<div class="text-gray-400">No images found</div>
					</div>
				{:else if filteredImages.length > 0}
					<ImageListTable images={filteredImages} onSelect={handleImageSelect} />
					
					{#if $browseSession?.pagination.hasMore}
						<div class="mt-4 text-center">
							<button
								onclick={handleLoadMore}
								disabled={isLoadingMore}
								class="px-4 py-2 bg-blue-500 text-white rounded-md hover:bg-blue-600 disabled:bg-gray-600 disabled:cursor-not-allowed"
							>
								{isLoadingMore ? 'Loading...' : 'Load More'}
							</button>
						</div>
					{/if}
				{/if}
			</div>
		</div>
	</div>
{/if}

<GhcrAuthDialog bind:open={showGhcrAuth} onSuccess={handleGhcrAuthSuccess} />
