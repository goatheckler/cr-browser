<script lang="ts">
	import type { ImageListing } from '$lib/types/browse';

	export let images: ImageListing[];
	export let onSelect: (image: ImageListing) => void;

	function formatDate(date: Date | null): string {
		if (!date) return 'N/A';
		return new Date(date).toLocaleDateString();
	}

	function formatNumber(num: number | undefined): string {
		if (num === undefined) return 'N/A';
		return num.toLocaleString();
	}
</script>

<div class="overflow-x-auto" data-testid="image-list">
	<table class="min-w-full divide-y divide-gray-700">
		<thead class="bg-gray-700">
			<tr>
				<th class="px-4 py-3 text-left text-xs font-medium text-gray-300 uppercase tracking-wider">
					Image Name
				</th>
				<th class="px-4 py-3 text-left text-xs font-medium text-gray-300 uppercase tracking-wider">
					Description
				</th>
				<th class="px-4 py-3 text-left text-xs font-medium text-gray-300 uppercase tracking-wider">
					Updated
				</th>
				<th class="px-4 py-3 text-left text-xs font-medium text-gray-300 uppercase tracking-wider">
					Stars
				</th>
				<th class="px-4 py-3 text-left text-xs font-medium text-gray-300 uppercase tracking-wider">
					Pulls
				</th>
				<th class="px-4 py-3 text-left text-xs font-medium text-gray-300 uppercase tracking-wider">
					Actions
				</th>
			</tr>
		</thead>
		<tbody class="bg-gray-800 divide-y divide-gray-700">
			{#each images as image}
				<tr class="hover:bg-gray-700">
					<td class="px-4 py-3 whitespace-nowrap">
						<div class="text-sm font-medium text-white">{image.imageName}</div>
						{#if image.metadata.htmlUrl}
							<a
								href={image.metadata.htmlUrl}
								target="_blank"
								rel="noopener noreferrer"
								class="text-xs text-blue-400 hover:underline"
							>
								View on {image.registryType}
							</a>
						{/if}
					</td>
					<td class="px-4 py-3">
						<div class="text-sm text-gray-300 max-w-xs truncate">
							{image.metadata.description || 'No description'}
						</div>
					</td>
					<td class="px-4 py-3 whitespace-nowrap text-sm text-gray-400">
						{formatDate(image.lastUpdated)}
					</td>
					<td class="px-4 py-3 whitespace-nowrap text-sm text-gray-400">
						{formatNumber(image.metadata.starCount)}
					</td>
					<td class="px-4 py-3 whitespace-nowrap text-sm text-gray-400">
						{formatNumber(image.metadata.pullCount)}
					</td>
					<td class="px-4 py-3 whitespace-nowrap text-sm">
						<button
							onclick={() => onSelect(image)}
							class="text-blue-400 hover:text-blue-300 font-medium"
						>
							Select
						</button>
					</td>
				</tr>
			{/each}
		</tbody>
	</table>
</div>
