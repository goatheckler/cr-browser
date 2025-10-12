<script lang="ts">
  import { goto } from '$app/navigation';
  
  interface Props {
    registry?: string;
    onchange?: (registry: string) => void;
  }
  
  let { registry = $bindable('ghcr'), onchange }: Props = $props();
  
  function handleChange() {
    if (typeof window !== 'undefined') {
      const params = new URLSearchParams(window.location.search);
      params.set('registry', registry);
      goto(`?${params.toString()}`);
    }
    onchange?.(registry);
  }
</script>

<select 
  bind:value={registry} 
  onchange={handleChange}
  data-testid="registry-selector"
  class="px-2 py-1 bg-surface border border-surface focus:outline-none focus:ring-2 focus:ring-primary"
>
  <option value="ghcr">GitHub Container Registry</option>
  <option value="dockerhub">Docker Hub</option>
  <option value="quay">Quay.io</option>
  <option value="gcr">Google Container Registry</option>
</select>
