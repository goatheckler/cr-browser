<script lang="ts">
  // Using vanilla ag-grid (no svelte wrapper)
  import 'ag-grid-community/styles/ag-grid.css';
  import 'ag-grid-community/styles/ag-theme-alpine.css';
  import { onMount, onDestroy, tick } from 'svelte';
  import type { GridApi, GridOptions, ColDef } from 'ag-grid-community';
  import { Grid } from 'ag-grid-community';
  import RegistrySelector from './RegistrySelector.svelte';
  
  let owner = $state('');
  let image = $state('');
  let registry = $state('ghcr');
  let tags = $state<string[]>([]);
  let copied = $state<string | null>(null);
  let copyTimer = $state<ReturnType<typeof setTimeout> | null>(null);
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
    const registryHosts: Record<string, string> = {
      ghcr: 'ghcr.io',
      dockerhub: 'docker.io',
      quay: 'quay.io',
      gcr: 'gcr.io'
    };
    return registryHosts[registryType] || 'ghcr.io';
  }

  async function fetchTags() {
    error = null;
    loadingTags = true;
    tags = [];
    copied = null;
    gridApi?.showLoadingOverlay();
    try {
      if (!owner || !image) {
        throw new Error('Owner and image required');
      }
      const res = await fetch(`/api/registries/${registry}/${owner}/${image}/tags`);
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
      <input placeholder="owner" bind:value={owner} class="px-2 py-1 bg-surface border border-surface focus:outline-none focus:ring-2 focus:ring-primary" on:keydown={onKey} />
      <input placeholder="image" bind:value={image} class="px-2 py-1 bg-surface border border-surface focus:outline-none focus:ring-2 focus:ring-primary" on:keydown={onKey} />
      <button on:click={submit} class="px-3 py-1 bg-primary hover:bg-primary/80 rounded disabled:opacity-50" disabled={loadingTags}>Search</button>
    </div>
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
