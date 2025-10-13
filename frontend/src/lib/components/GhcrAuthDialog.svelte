<script lang="ts">
	import { ghcrCredential } from '$lib/stores/ghcrCredential';
	import { createCredential, validateCredential, saveCredential, isTokenFormatValid } from '$lib/services/ghcrAuth';

	let { open = $bindable(false), onSuccess }: { open?: boolean; onSuccess: () => void } = $props();

	let tokenValue = $state('');
	let isValidating = $state(false);
	let validationError = $state('');

	async function handleSubmit() {
		validationError = '';

		if (!isTokenFormatValid(tokenValue)) {
			validationError = 'Invalid token format. GitHub PATs start with "ghp_" and are 40 characters total.';
			return;
		}

		isValidating = true;
		try {
			const credential = createCredential(tokenValue);
			const isValid = await validateCredential(credential);

			if (!isValid) {
				validationError = 'Token validation failed. Please check the token and ensure it has "read:packages" scope.';
				isValidating = false;
				return;
			}

			saveCredential(credential);
			ghcrCredential.set(credential);
			
			tokenValue = '';
			validationError = '';
			open = false;
			onSuccess();
		} catch (err) {
			validationError = err instanceof Error ? err.message : 'Failed to validate token';
		} finally {
			isValidating = false;
		}
	}

	function handleCancel() {
		tokenValue = '';
		validationError = '';
		open = false;
	}
</script>

{#if open}
	<div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
		<div class="bg-gray-800 rounded-lg shadow-xl w-full max-w-md p-6" role="dialog" aria-modal="true" aria-labelledby="ghcr-auth-title">
			<h2 id="ghcr-auth-title" class="text-xl font-semibold mb-4 text-white">GHCR Authentication Required</h2>
			
			<p class="text-sm text-gray-300 mb-4">
				GitHub Container Registry requires authentication. Please provide a GitHub Personal Access Token with <code class="bg-gray-700 px-1 rounded text-gray-200">read:packages</code> scope.
			</p>

			<div class="mb-4">
				<a
					href="https://github.com/settings/tokens/new?scopes=read:packages&description=CR%20Browser%20GHCR%20Access"
					target="_blank"
					rel="noopener noreferrer"
					class="text-sm text-blue-400 hover:underline"
				>
					Create a new token on GitHub →
				</a>
			</div>

			<form onsubmit={(e) => { e.preventDefault(); handleSubmit(); }}>
				<div class="mb-4">
					<label for="ghcr-token" class="block text-sm font-medium text-gray-200 mb-2">
						GitHub Personal Access Token
					</label>
					<input
						id="ghcr-token"
						type="password"
						bind:value={tokenValue}
						placeholder="ghp_..."
						required
						class="w-full px-3 py-2 border border-gray-600 rounded-md bg-gray-700 text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500"
					/>
				</div>

				{#if validationError}
					<div class="mb-4 p-3 bg-red-900 border border-red-700 rounded-md">
						<p class="text-sm text-red-200">{validationError}</p>
					</div>
				{/if}

				<div class="flex gap-3 justify-end">
					<button
						type="button"
						onclick={handleCancel}
						disabled={isValidating}
						class="px-4 py-2 text-gray-200 border border-gray-600 rounded-md hover:bg-gray-700 disabled:opacity-50"
					>
						Cancel
					</button>
					<button
						type="submit"
						disabled={isValidating || !tokenValue}
						class="px-4 py-2 bg-blue-500 text-white rounded-md hover:bg-blue-600 disabled:bg-gray-600 disabled:cursor-not-allowed"
					>
						{isValidating ? 'Validating...' : 'Continue'}
					</button>
				</div>
			</form>
		</div>
	</div>
{/if}
