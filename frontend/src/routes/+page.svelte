<script lang="ts">
  // Using vanilla ag-grid (no svelte wrapper)
  import 'ag-grid-community/styles/ag-grid.css';
  import 'ag-grid-community/styles/ag-theme-alpine.css';
  import { onMount, onDestroy, tick } from 'svelte';
  import type { GridApi, GridOptions, ColDef } from 'ag-grid-community';
  import { Grid } from 'ag-grid-community';
  import RegistrySelector from './RegistrySelector.svelte';
  import BrowseImagesDialog from '$lib/components/BrowseImagesDialog.svelte';
  import type { ImageListing } from '$lib/types/browse';
  import { ghcrCredential } from '$lib/stores/ghcrCredential';
  import { clearCredential } from '$lib/services/ghcrAuth';
  import { browseSession } from '$lib/stores/browseSession';
  import { registryDetectionService } from '$lib/services/registryDetection';
  
  let owner = $state('');
  let image = $state('');
  let registry = $state('ghcr');
  let customRegistryUrl = $state<string | undefined>(undefined);
  let customRegistryValidated = $state(false);
  let detectionResult = $state<{
    normalizedUrl: string;
    apiVersion: string | null;
    error?: string;
  } | null>(null);
  let showDetectionResult = $state(false);
  let detectingRegistry = $state(false);
  let tags = $state<string[]>([]);
  let copied = $state<string | null>(null);
  let copyTimer = $state<ReturnType<typeof setTimeout> | null>(null);
  let showBrowseDialog = $state(false);
  let health = $state<{ status: string; uptimeSeconds: number } | null>(null);
  let loadingHealth = $state(true);
  let loadingTags = $state(false);
  let error = $state<string | null>(null);
  let searched = $state(false);
  // aria-live feedback
  let announce = $state<string | null>(null);
  interface Row { tag: string; full: string; }
  let rowData = $state<Row[]>([]);
  let gridApi = $state<GridApi | null>(null);
  let gridDiv = $state<HTMLDivElement | null>(null);
  let pageContainer = $state<HTMLDivElement | null>(null);
  let gridCreated = $state(false);
  let gridHeightPx = $state(400);

  const columnDefs: ColDef[] = [
    { headerName: 'Tag', field: 'tag', sortable: true, filter: true, resizable: true, sort: 'desc', sortIndex: 0 },
    { headerName: 'Full Reference', field: 'full', flex: 1, cellRenderer: CopyRenderer }
  ];

  function CopyRenderer(params: { value: string }) {
    const span = document.createElement('span');
    span.className = 'fullref-cell';
    const btn = document.createElement('button');
    btn.className = 'copy-btn';
    btn.type = 'button';
    btn.textContent = params.value;
    btn.addEventListener('click', () => copy(params.value));
    span.appendChild(btn);
    return span;
  }

  async function copy(text: string) {
    try {
      await navigator.clipboard.writeText(text);
      copied = text;
      announce = 'Copied to clipboard';
      if (copyTimer) clearTimeout(copyTimer);
      copyTimer = setTimeout(() => { copied = null; announce = null; }, 2000);
    } catch (e) {
      console.error('Copy failed', e);
    }
  }
 
  async function fetchHealth() {
    loadingHealth = true;
    try {
      const res = await fetch('/api/health');
      if (!res.ok) throw new Error('Health fetch failed');
      health = await res.json();
    } catch (e: unknown) {
      error = e instanceof Error ? e.message : 'Unknown error';
    } finally {
      loadingHealth = false;
    }
  }

  function getRegistryHost(registryType: string): string {
    if (registryType === 'custom' && customRegistryUrl) {
      return customRegistryUrl.replace(/^https?:\/\//, '');
    }
    const registryHosts: Record<string, string> = {
      ghcr: 'ghcr.io',
      dockerhub: 'docker.io',
      quay: 'quay.io',
      gcr: 'gcr.io'
    };
    return registryHosts[registryType] || 'ghcr.io';
  }

  function getRegistryUrlDisplay(): string {
    if (registry === 'custom') {
      return customRegistryUrl || '';
    }
    return getRegistryHost(registry);
  }

  function handleRegistryUrlChange(e: Event) {
    const input = e.target as HTMLInputElement;
    customRegistryUrl = input.value;
    customRegistryValidated = false;
    owner = '';
    image = '';
  }

  async function handleCheckRegistry() {
    if (!customRegistryUrl?.trim()) {
      return;
    }
    
    detectingRegistry = true;
    detectionResult = null;
    
    try {
      const result = await registryDetectionService.detectRegistry(customRegistryUrl);
      
      if (result.supported && result.normalizedUrl) {
        detectionResult = {
          normalizedUrl: result.normalizedUrl,
          apiVersion: result.apiVersion
        };
        customRegistryUrl = result.normalizedUrl;
        customRegistryValidated = true;
      } else {
        detectionResult = {
          normalizedUrl: customRegistryUrl,
          apiVersion: null,
          error: result.errorMessage || 'Registry not supported'
        };
        customRegistryValidated = false;
      }
      showDetectionResult = true;
    } catch (err) {
      detectionResult = {
        normalizedUrl: customRegistryUrl,
        apiVersion: null,
        error: err instanceof Error ? err.message : 'Failed to detect registry'
      };
      customRegistryValidated = false;
      showDetectionResult = true;
    } finally {
      detectingRegistry = false;
    }
  }

  function closeDetectionResult() {
    showDetectionResult = false;
  }

  async function fetchTags() {
    error = null;
    loadingTags = true;
    tags = [];
    rowData = [];
    copied = null;
    gridApi?.showLoadingOverlay();
    try {
      if (!owner || !image) {
        throw new Error('Owner and image required');
      }
      const url = new URL(`/api/registries/${registry}/${owner}/${image}/tags`, window.location.origin);
      if (customRegistryUrl) {
        url.searchParams.set('customRegistryUrl', customRegistryUrl);
      }
      const res = await fetch(url);
      if (!res.ok) {
        const body = await res.json().catch(() => ({}));
        throw new Error(body.message || 'Tags fetch failed');
      }
      const data = await res.json();
      tags = data.tags || [];
      const host = getRegistryHost(registry);
      rowData = tags.map((t: string) => ({ tag: t, full: `${host}/${owner}/${image}:${t}` }));
    } catch (e: unknown) {
      error = e instanceof Error ? e.message : 'Unknown error';
    } finally {
      loadingTags = false;
    }
  }

  const submit = async () => {
    searched = true;
    await fetchTags();
    await tick();
    computeGridHeight();
  };
  const onKey = (e: KeyboardEvent) => {
    if (e.key === 'Enter') submit();
  };

  function computeGridHeight() {
    if (!gridDiv) return;
    const rect = gridDiv.getBoundingClientRect();
    // Derive bottom padding from the page container's computed style (fallback 24px for p-6)
    let bottomPadding = 24;
    if (pageContainer) {
      const style = getComputedStyle(pageContainer);
      const parsed = parseFloat(style.paddingBottom || '0');
      if (!Number.isNaN(parsed) && parsed >= 0) bottomPadding = parsed;
    }
    const available = window.innerHeight - rect.top - bottomPadding;
    if (available > 150) gridHeightPx = available;
  }

  onMount(() => {
    // Read registry from URL
    if (typeof window !== 'undefined') {
      const params = new URLSearchParams(window.location.search);
      const urlRegistry = params.get('registry');
      if (urlRegistry) {
        registry = urlRegistry;
      }
      const urlOwner = params.get('owner');
      const urlImage = params.get('image');
      if (urlOwner) owner = urlOwner;
      if (urlImage) image = urlImage;
    }
    
    fetchHealth();
    maybeCreateGrid();
    computeGridHeight();
    window.addEventListener('resize', computeGridHeight);
  });

  function handleRegistryChange() {
    customRegistryValidated = false;
    if (registry === 'custom') {
      customRegistryUrl = '';
      owner = '';
      image = '';
    }
    if (owner && image && searched) {
      fetchTags();
    }
  }

  onDestroy(() => {
    window.removeEventListener('resize', computeGridHeight);
  });

  $effect(() => {
    if (gridCreated && gridApi) {
      gridApi.setRowData(rowData);
      if (loadingTags) {
        gridApi.showLoadingOverlay();
      } else if (rowData.length === 0) {
        gridApi.showNoRowsOverlay();
      } else {
        gridApi.hideOverlay();
      }
      try { gridApi.sizeColumnsToFit(); } catch {}
    }
  });

  function maybeCreateGrid() {
    if (!gridCreated && gridDiv) {
      const gridOptions: GridOptions<Row> = {
        columnDefs,
        rowData,
        suppressCellFocus: true,
        rowHeight: 36,
        overlayLoadingTemplate: '<div class="ag-overlay-loading-center text-sm">Loading tags...</div>',
        overlayNoRowsTemplate: '<div class="ag-overlay-no-rows-center text-sm opacity-70">No tags</div>',
        onGridReady: (e: { api: GridApi }) => { 
          gridApi = e.api; 
          computeGridHeight();
          if (loadingTags) e.api.showLoadingOverlay(); else e.api.showNoRowsOverlay();
        }
      };
      new Grid(gridDiv, gridOptions);
      gridCreated = true;
    }
  }

  async function handleImageSelected(selectedImage: ImageListing) {
    owner = selectedImage.owner;
    image = selectedImage.imageName;
    registry = selectedImage.registryType.toLowerCase();
    customRegistryUrl = $browseSession?.customRegistryUrl;
    customRegistryValidated = registry === 'custom' && !!customRegistryUrl;
    showBrowseDialog = false;
    searched = true;
    maybeCreateGrid();
    await tick();
    await fetchTags();
  }

  function getRegistryType(): 'GHCR' | 'DockerHub' | 'Quay' | 'GCR' | 'Custom' {
    const map: Record<string, 'GHCR' | 'DockerHub' | 'Quay' | 'GCR' | 'Custom'> = {
      ghcr: 'GHCR',
      dockerhub: 'DockerHub',
      quay: 'Quay',
      gcr: 'GCR',
      custom: 'Custom'
    };
    return map[registry] || 'GHCR';
  }

  function handleClearToken() {
    clearCredential();
    ghcrCredential.clear();
  }

</script>

<div class="min-h-screen bg-background text-white p-6 space-y-6" bind:this={pageContainer}>
  <div class="flex items-start justify-between gap-4 flex-wrap w-full">
    <h1 class="text-2xl font-bold flex items-center gap-2">
      <img src="/favicon.svg" alt="" class="w-8 h-8" />
      CR Browser
    </h1>
    <div class="text-sm text-right">
      {#if loadingHealth}
        <div>Checking health...</div>
      {:else if health}
        <div class="text-green-400">API healthy (uptime {health.uptimeSeconds}s)</div>
      {:else}
        <div class="text-red-400">Health unavailable</div>
      {/if}
    </div>
  </div>

  <section class="space-y-2">
    <div class="flex gap-2 items-center flex-wrap">
      <RegistrySelector bind:registry onchange={handleRegistryChange} />
      {#if registry === 'ghcr' && $ghcrCredential}
        <button onclick={handleClearToken} class="px-3 py-1 bg-red-600 hover:bg-red-700 rounded text-sm">Clear Token</button>
      {/if}
      <label for="registry-url-input" class="text-sm text-gray-300">Registry URL:</label>
      <input 
        id="registry-url-input" 
        value={getRegistryUrlDisplay()} 
        oninput={handleRegistryUrlChange}
        readonly={registry !== 'custom'}
        placeholder="docker.example.com"
        class="px-2 py-1 bg-surface border border-surface focus:outline-none focus:ring-2 focus:ring-primary {registry !== 'custom' ? 'opacity-60' : ''}" 
      />
      <button 
        onclick={handleCheckRegistry} 
        disabled={registry !== 'custom' || customRegistryValidated || !customRegistryUrl || detectingRegistry}
        data-testid="check-registry-button"
        class="px-3 py-1 bg-surface hover:bg-surface/80 rounded border border-primary disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-1"
        title="Validate custom registry"
      >
        <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
        </svg>
        {detectingRegistry ? 'Checking...' : 'Check'}
      </button>
      <label for="owner-input" class="text-sm text-gray-300">{registry === 'gcr' ? 'Project ID' : 'Owner'}:</label>
      <input 
        id="owner-input" 
        placeholder={registry === 'gcr' ? 'project-id' : 'owner'} 
        bind:value={owner} 
        disabled={registry === 'custom' && !customRegistryValidated}
        class="px-2 py-1 bg-surface border border-surface focus:outline-none focus:ring-2 focus:ring-primary disabled:opacity-50 disabled:cursor-not-allowed" 
        onkeydown={onKey} 
      />
      <button 
        onclick={() => showBrowseDialog = true} 
        disabled={registry === 'custom' && !customRegistryValidated}
        class="px-3 py-1 bg-surface hover:bg-surface/80 rounded border border-primary disabled:opacity-50 disabled:cursor-not-allowed"
      >
        Browse Images
      </button>
      <label for="image-input" class="text-sm text-gray-300">Image:</label>
      <input 
        id="image-input" 
        placeholder="image" 
        bind:value={image} 
        disabled={registry === 'custom' && !customRegistryValidated}
        class="px-2 py-1 bg-surface border border-surface focus:outline-none focus:ring-2 focus:ring-primary disabled:opacity-50 disabled:cursor-not-allowed" 
        onkeydown={onKey} 
      />
      <button 
        onclick={submit} 
        disabled={loadingTags || (registry === 'custom' && !customRegistryValidated)}
        class="px-3 py-1 bg-primary hover:bg-primary/80 rounded disabled:opacity-50"
      >
        Search
      </button>
    </div>
    {#if registry === 'gcr'}
      <div class="text-sm text-gray-400">Project ID format: lowercase alphanumeric with hyphens (e.g., google-containers)</div>
    {/if}
    {#if error}
      <div class="text-red-400 text-sm">{error}</div>
    {/if}
  </section>

  <section class="space-y-2">
    <div role="status">{announce}</div>
    {#if !error && searched && !loadingTags && tags.length > 0}
      <div class="status-line">Found {tags.length} tag{tags.length === 1 ? '' : 's'}</div>
    {/if}
    <div class="ag-theme-alpine ag-theme-alpine-dark border border-surface rounded" bind:this={gridDiv} style="height: {gridHeightPx}px; width: 100%; position:relative;">
      {#if !gridCreated}
        <div class="absolute inset-0 flex items-center justify-center text-sm opacity-60">Initializing grid...</div>
      {/if}
    </div>
  </section>
</div>

<BrowseImagesDialog 
  bind:open={showBrowseDialog} 
  registryType={getRegistryType()} 
  ownerOrProjectId={owner}
  initialCustomRegistryUrl={customRegistryValidated ? customRegistryUrl : undefined}
  onImageSelected={handleImageSelected} 
/>

<svelte:window onkeydown={(e) => { if (e.key === 'Escape' && showDetectionResult) closeDetectionResult(); }} />

{#if showDetectionResult && detectionResult}
  <div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50" role="presentation" onclick={closeDetectionResult}>
    <div class="bg-gray-800 rounded-lg shadow-xl w-full max-w-2xl p-6" role="dialog" aria-modal="true" tabindex="-1" onclick={(e) => e.stopPropagation()}>
      {#if detectionResult.error}
        <div class="space-y-4">
          <h2 class="text-xl font-semibold text-red-400">✗ Registry Not Detected</h2>
          <div class="bg-gray-900 rounded p-4 space-y-2">
            <div><strong class="text-gray-300">URL:</strong> <span class="text-white">{detectionResult.normalizedUrl}</span></div>
            <div><strong class="text-gray-300">Error:</strong> <span class="text-red-300">{detectionResult.error}</span></div>
          </div>
          <div class="flex justify-end">
            <button onclick={closeDetectionResult} class="px-4 py-2 bg-gray-700 hover:bg-gray-600 rounded text-white">
              Close
            </button>
          </div>
        </div>
      {:else}
        <div class="space-y-4">
          <h2 class="text-xl font-semibold text-green-400">✓ OCI Registry Detected</h2>
          <div class="bg-gray-900 rounded p-4 space-y-2">
            <div><strong class="text-gray-300">URL:</strong> <span class="text-white">{detectionResult.normalizedUrl}</span></div>
            {#if detectionResult.apiVersion}
              <div><strong class="text-gray-300">API Version:</strong> <span class="text-white">{detectionResult.apiVersion}</span></div>
            {/if}
          </div>
          <div class="flex justify-end">
            <button onclick={closeDetectionResult} class="px-4 py-2 bg-gray-700 hover:bg-gray-600 rounded text-white">
              Close
            </button>
          </div>
        </div>
      {/if}
    </div>
  </div>
{/if}
