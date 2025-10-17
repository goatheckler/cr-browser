<script lang="ts">
	import { browseSession } from '$lib/stores/browseSession';
	import { ghcrCredential } from '$lib/stores/ghcrCredential';
	import { loadImages, loadNextPage, searchImages, getFilteredImages } from '$lib/services/registryBrowser';
	import { DEFAULT_PAGE_SIZE } from '$lib/constants/browse';
	import type { RegistryType, ImageListing } from '$lib/types/browse';
	import ImageListTable from './ImageListTable.svelte';
	import GhcrAuthDialog from './GhcrAuthDialog.svelte';
	import CustomRegistryInput from './CustomRegistryInput.svelte';
	import { registryDetectionService } from '$lib/services/registryDetection';

	let { open = $bindable(false), registryType, ownerOrProjectId, initialCustomRegistryUrl, onImageSelected }: { 
		open?: boolean; 
		registryType: RegistryType; 
		ownerOrProjectId: string;
		initialCustomRegistryUrl?: string;
		onImageSelected: (image: ImageListing) => void 
	} = $props();

	let showGhcrAuth = $state(false);
	let showCustomInput = $state(false);
	let customRegistryUrl = $state<string | undefined>(undefined);
	let filterText = $state('');
	let isLoadingMore = $state(false);
	let localOwner = $state(ownerOrProjectId || '');

	$effect(() => {
		if (open) {
			localOwner = ownerOrProjectId || '';
			showGhcrAuth = false;
			showCustomInput = false;
			
			if (registryType === 'GHCR' && !$ghcrCredential) {
				showGhcrAuth = true;
			}
			if (registryType === 'Custom') {
				if (initialCustomRegistryUrl) {
					customRegistryUrl = initialCustomRegistryUrl;
					showCustomInput = false;
				} else {
					showCustomInput = true;
				}
			}
		}
	});

	let filteredImages = $derived($browseSession ? getFilteredImages($browseSession) : []);

	async function handleBrowse() {
		if (!localOwner) return;

		if (registryType === 'GHCR' && !$ghcrCredential) {
			showGhcrAuth = true;
			return;
		}

		if (registryType === 'Custom' && !customRegistryUrl) {
			showCustomInput = true;
			return;
		}

		browseSession.set({
			sessionId: crypto.randomUUID(),
			registryType,
			ownerOrProjectId: localOwner,
			customRegistryUrl,
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
				registryType === 'GHCR' ? $ghcrCredential! : undefined,
				customRegistryUrl
			);
			browseSession.set(session);
		} catch (err) {
			console.error('Failed to load images:', err);
			browseSession.update((s) => s ? { 
				...s, 
				status: 'error', 
				error: { 
					code: 'LOAD_ERROR',
					message: err instanceof Error ? err.message : 'Unknown error',
					retryable: true,
					rateLimitReset: null
				} 
			} : null);
		}
	}

	function handleGhcrAuthSuccess() {
		showGhcrAuth = false;
		handleBrowse();
	}

	function handleCustomRegistrySubmit(data: { url: string; normalizedUrl: string }) {
		customRegistryUrl = data.normalizedUrl;
		showCustomInput = false;
		handleBrowse();
	}

	function handleCustomRegistryCancel() {
		showCustomInput = false;
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

	function handleScroll(event: Event) {
		const target = event.target as HTMLElement;
		const scrolledToBottom = target.scrollHeight - target.scrollTop <= target.clientHeight + 50;
		
		if (scrolledToBottom && $browseSession?.pagination.hasMore && !isLoadingMore) {
			handleLoadMore();
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
		<div class="bg-gray-800 rounded-lg shadow-xl w-full max-w-4xl max-h-[80vh] flex flex-col" role="dialog" aria-modal="true" tabindex="-1" onclick={(e) => e.stopPropagation()} onkeydown={(e) => e.key === 'Escape' && handleClose()}>
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
				{#if registryType === 'Custom' && customRegistryUrl}
					<div class="text-sm text-gray-300">
						<span class="font-medium">Registry:</span> {customRegistryUrl}
					</div>
				{/if}
				
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
						{$browseSession?.status === 'loading' ? 'Browsing...' : 'Browse'}
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

		<div class="flex-1 overflow-auto px-6 py-4" onscroll={handleScroll} data-testid="scrollable-container">
			{#if $browseSession?.status === 'loading'}
				<div class="flex items-center justify-center py-12" data-testid="loading-state">
					<div class="text-gray-400">Loading images...</div>
				</div>
			{:else if $browseSession?.status === 'error'}
				<div class="flex flex-col items-center justify-center py-12 gap-4">
					<div class="text-red-400 max-w-md text-center whitespace-pre-line" data-testid="error-message">
						{$browseSession.error?.message || 'Failed to load images'}
					</div>
					{#if $browseSession.error?.code === 'CATALOG_NOT_SUPPORTED'}
						<div class="text-gray-300 text-sm max-w-md text-center">
							You can still access tags by entering the image name directly in the main form.
						</div>
						<button
							onclick={handleClose}
							class="px-4 py-2 bg-purple-500 text-white rounded-md hover:bg-purple-600"
						>
							Close and Enter Image Name
						</button>
					{:else if $browseSession.error?.retryable}
						<button
							onclick={handleBrowse}
							class="px-4 py-2 bg-blue-500 text-white rounded-md hover:bg-blue-600"
						>
							Retry
						</button>
					{/if}
				</div>
			{:else if $browseSession?.status === 'loaded' && filteredImages.length > 0}
				<ImageListTable images={filteredImages} onSelect={handleImageSelect} />
				
				{#if isLoadingMore}
					<div class="mt-4 text-center text-gray-400" data-testid="loading-more-indicator">
						Loading More Images...
					</div>
				{:else if $browseSession?.pagination.hasMore}
					<div class="mt-4 text-center">
						<button
							onclick={handleLoadMore}
							disabled={isLoadingMore}
							class="px-4 py-2 bg-blue-500 text-white rounded-md hover:bg-blue-600 disabled:bg-gray-600 disabled:cursor-not-allowed"
						>
							{isLoadingMore ? 'Loading More...' : 'Load More'}
						</button>
					</div>
				{/if}
			{:else if $browseSession?.status === 'loaded' && filteredImages.length === 0}
				<div class="flex items-center justify-center py-12">
					<div class="text-gray-400">No images found</div>
				</div>
			{/if}
		</div>
		</div>
	</div>
{/if}

<GhcrAuthDialog bind:open={showGhcrAuth} onSuccess={handleGhcrAuthSuccess} />

{#if showCustomInput}
	<div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50" role="presentation">
		<div class="bg-gray-800 rounded-lg shadow-xl w-full max-w-2xl p-6" role="dialog" aria-modal="true" tabindex="-1" onclick={(e) => e.stopPropagation()} onkeydown={(e) => e.key === 'Escape' && handleCustomRegistryCancel()}>
			<h2 class="text-xl font-semibold text-white mb-4">Configure Custom Registry</h2>
			<CustomRegistryInput 
				detectionService={registryDetectionService}
				onSubmit={handleCustomRegistrySubmit}
				onCancel={handleCustomRegistryCancel}
			/>
		</div>
	</div>
{/if}
