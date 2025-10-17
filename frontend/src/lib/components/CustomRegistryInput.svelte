<script lang="ts">
  import type { RegistryDetectionService } from '$lib/types/browse';
  
  interface Props {
    detectionService: RegistryDetectionService;
    initialUrl?: string;
    onSubmit: (data: { url: string; normalizedUrl: string }) => void;
    onCancel: () => void;
  }
  
  let { detectionService, initialUrl = '', onSubmit, onCancel }: Props = $props();
  
  let registryUrl = $state(initialUrl);
  let detecting = $state(false);
  let error: string | null = $state(null);
  let detectionResult: {
    normalizedUrl: string;
    apiVersion: string | null;
  } | null = $state(null);
  
  async function handleDetect() {
    if (!registryUrl.trim()) {
      error = 'Please enter a registry URL';
      return;
    }
    
    detecting = true;
    error = null;
    detectionResult = null;
    
    try {
      const result = await detectionService.detectRegistry(registryUrl);
      
      if (result.supported && result.normalizedUrl) {
        detectionResult = {
          normalizedUrl: result.normalizedUrl,
          apiVersion: result.apiVersion
        };
      } else {
        error = result.errorMessage || 'Registry not supported';
      }
    } catch (err) {
      error = err instanceof Error ? err.message : 'Failed to detect registry';
    } finally {
      detecting = false;
    }
  }
  
  function handleSubmit() {
    if (detectionResult) {
      onSubmit({
        url: registryUrl,
        normalizedUrl: detectionResult.normalizedUrl
      });
    }
  }
  
  function handleCancelClick() {
    onCancel();
  }
</script>

<div class="custom-registry-input">
  <div class="input-section">
    <label for="registry-url">Registry URL</label>
    <div class="input-group">
      <input
        id="registry-url"
        type="text"
        bind:value={registryUrl}
        placeholder="docker.redpanda.com"
        disabled={detecting}
        onkeydown={(e) => e.key === 'Enter' && handleDetect()}
      />
      <button
        onclick={handleDetect}
        disabled={detecting || !registryUrl.trim()}
        class="detect-btn"
      >
        {detecting ? 'Detecting...' : 'Detect'}
      </button>
    </div>
    
    {#if error}
      <div class="error-message">{error}</div>
    {/if}
    
    {#if detectionResult}
      <div class="success-message">
        <div class="success-title">✓ OCI Registry Detected</div>
        <div class="registry-info">
          <div><strong>URL:</strong> {detectionResult.normalizedUrl}</div>
          {#if detectionResult.apiVersion}
            <div><strong>API Version:</strong> {detectionResult.apiVersion}</div>
          {/if}
        </div>
      </div>
    {/if}
  </div>
  
  <div class="actions">
    <button onclick={handleCancelClick} class="cancel-btn">
      Cancel
    </button>
    <button
      onclick={handleSubmit}
      disabled={!detectionResult}
      class="submit-btn"
    >
      Use This Registry
    </button>
  </div>
</div>

<style>
  .custom-registry-input {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
  }
  
  .input-section {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }
  
  label {
    font-weight: 500;
    font-size: 0.875rem;
  }
  
  .input-group {
    display: flex;
    gap: 0.5rem;
  }
  
  input {
    flex: 1;
    padding: 0.5rem;
    border: 1px solid #d1d5db;
    border-radius: 0.375rem;
    font-size: 0.875rem;
  }
  
  input:disabled {
    background-color: #f3f4f6;
    cursor: not-allowed;
  }
  
  button {
    padding: 0.5rem 1rem;
    border-radius: 0.375rem;
    font-size: 0.875rem;
    font-weight: 500;
    cursor: pointer;
    transition: all 0.2s;
  }
  
  button:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
  
  .detect-btn {
    background-color: #3b82f6;
    color: white;
    border: none;
  }
  
  .detect-btn:hover:not(:disabled) {
    background-color: #2563eb;
  }
  
  .error-message {
    padding: 0.75rem;
    background-color: #fef2f2;
    border: 1px solid #fecaca;
    border-radius: 0.375rem;
    color: #dc2626;
    font-size: 0.875rem;
  }
  
  .success-message {
    padding: 0.75rem;
    background-color: #f0fdf4;
    border: 1px solid #bbf7d0;
    border-radius: 0.375rem;
  }
  
  .success-title {
    color: #16a34a;
    font-weight: 500;
    margin-bottom: 0.5rem;
  }
  
  .registry-info {
    font-size: 0.875rem;
    color: #374151;
  }
  
  .registry-info > div {
    margin-top: 0.25rem;
  }
  
  .actions {
    display: flex;
    justify-content: flex-end;
    gap: 0.75rem;
    padding-top: 0.5rem;
    border-top: 1px solid #e5e7eb;
  }
  
  .cancel-btn {
    background-color: white;
    color: #374151;
    border: 1px solid #d1d5db;
  }
  
  .cancel-btn:hover {
    background-color: #f9fafb;
  }
  
  .submit-btn {
    background-color: #10b981;
    color: white;
    border: none;
  }
  
  .submit-btn:hover:not(:disabled) {
    background-color: #059669;
  }
</style>
