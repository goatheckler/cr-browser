# Implementation Plan: Custom Registry UI Enhancement

**Feature**: 005-custom-registry-ui-enhancement  
**Created**: 2025-10-16  
**Status**: Ready for Implementation

## Overview

This plan details the implementation steps for moving custom registry validation from a hidden modal workflow to a visible, integrated main UI experience.

## Architecture Decisions

### State Management Strategy
- **Decision**: Use local Svelte 5 runes ($state) in +page.svelte
- **Rationale**: Feature is UI-centric, doesn't need global state
- **Impact**: Minimal - keeps state management simple and co-located

### Component Reuse Strategy
- **Decision**: Reuse existing CustomRegistryInput.svelte component unchanged
- **Rationale**: Component is well-tested and works perfectly
- **Impact**: Zero - no changes to detection logic needed

### Validation State Lifecycle
- **Decision**: Clear owner/image fields when custom URL changes
- **Rationale**: Prevents user confusion about which registry they're querying
- **Impact**: Positive - forces intentional workflow, prevents errors

### URL Field Behavior
- **Decision**: Make URL field readonly after successful validation
- **Rationale**: Prevents accidental changes that would invalidate state
- **Impact**: User must explicitly edit URL (which triggers reset) to change

## Implementation Tasks

### Phase 1: State Management (30 mins)

#### Task 1.1: Add New State Variables
**File**: `frontend/src/routes/+page.svelte`
**Lines**: After line 18 (after `customRegistryUrl` declaration)
**Changes**:
```typescript
let customRegistryValidated = $state(false);
let showDetectionModal = $state(false);
```

#### Task 1.2: Add Helper Functions
**File**: `frontend/src/routes/+page.svelte`
**Lines**: After line 91 (after `getRegistryHost` function)
**Changes**:
```typescript
function getRegistryUrlDisplay(): string {
  if (registry === 'custom') {
    return customRegistryUrl || '';
  }
  return getRegistryHost(registry);
}

function handleRegistryUrlChange(e: Event) {
  const target = e.target as HTMLInputElement;
  customRegistryUrl = target.value;
  customRegistryValidated = false;
  owner = '';
  image = '';
}

function handleCheckRegistry() {
  showDetectionModal = true;
}

function handleDetectionSuccess(data: { url: string; normalizedUrl: string }) {
  customRegistryUrl = data.normalizedUrl;
  customRegistryValidated = true;
  showDetectionModal = false;
}

function handleDetectionCancel() {
  showDetectionModal = false;
}
```

#### Task 1.3: Update handleRegistryChange
**File**: `frontend/src/routes/+page.svelte`
**Lines**: Replace lines 167-171
**Old**:
```typescript
function handleRegistryChange() {
  if (owner && image && searched) {
    fetchTags();
  }
}
```
**New**:
```typescript
function handleRegistryChange() {
  customRegistryValidated = false;
  customRegistryUrl = undefined;
  if (registry === 'custom') {
    owner = '';
    image = '';
  }
  if (owner && image && searched && registry !== 'custom') {
    fetchTags();
  }
}
```

### Phase 2: UI Components (45 mins)

#### Task 2.1: Add Imports
**File**: `frontend/src/routes/+page.svelte`
**Lines**: After line 8 (imports section)
**Changes**:
```typescript
import CustomRegistryInput from '$lib/components/CustomRegistryInput.svelte';
import { registryDetectionService } from '$lib/services/registryDetection';
```

#### Task 2.2: Add Registry URL Input Field
**File**: `frontend/src/routes/+page.svelte`
**Lines**: After line 260 (after RegistrySelector)
**Insert**:
```svelte
<label for="registry-url-input" class="text-sm text-gray-300">URL:</label>
<input 
  id="registry-url-input" 
  value={getRegistryUrlDisplay()} 
  oninput={handleRegistryUrlChange}
  readonly={registry !== 'custom' || customRegistryValidated}
  placeholder={registry === 'custom' && !customRegistryValidated ? 'Enter registry URL' : ''}
  class="px-2 py-1 bg-surface border border-surface focus:outline-none focus:ring-2 focus:ring-primary {registry !== 'custom' || customRegistryValidated ? 'opacity-60' : ''}"
  data-testid="registry-url-input"
/>
```

#### Task 2.3: Add Check Button
**File**: `frontend/src/routes/+page.svelte`
**Lines**: After Task 2.2 insertion
**Insert**:
```svelte
{#if registry === 'custom'}
  <button 
    onclick={handleCheckRegistry}
    disabled={!customRegistryUrl || customRegistryValidated}
    class="px-2 py-1 bg-surface hover:bg-surface/80 rounded border border-primary disabled:opacity-30 disabled:cursor-not-allowed"
    title="Validate registry"
    data-testid="check-registry-button"
  >
    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path>
    </svg>
  </button>
{/if}
```

#### Task 2.4: Update Owner Input
**File**: `frontend/src/routes/+page.svelte`
**Lines**: Replace line 265
**Old**:
```svelte
<input id="owner-input" placeholder={registry === 'gcr' ? 'project-id' : 'owner'} bind:value={owner} class="px-2 py-1 bg-surface border border-surface focus:outline-none focus:ring-2 focus:ring-primary" onkeydown={onKey} />
```
**New**:
```svelte
<input 
  id="owner-input" 
  placeholder={registry === 'gcr' ? 'project-id' : 'owner'} 
  bind:value={owner} 
  disabled={registry === 'custom' && !customRegistryValidated}
  class="px-2 py-1 bg-surface border border-surface focus:outline-none focus:ring-2 focus:ring-primary disabled:opacity-30 disabled:cursor-not-allowed" 
  onkeydown={onKey} 
/>
```

#### Task 2.5: Update Browse Images Button
**File**: `frontend/src/routes/+page.svelte`
**Lines**: Replace line 266
**Old**:
```svelte
<button onclick={() => showBrowseDialog = true} class="px-3 py-1 bg-surface hover:bg-surface/80 rounded border border-primary">Browse Images</button>
```
**New**:
```svelte
<button 
  onclick={() => showBrowseDialog = true} 
  disabled={registry === 'custom' && !customRegistryValidated}
  class="px-3 py-1 bg-surface hover:bg-surface/80 rounded border border-primary disabled:opacity-30 disabled:cursor-not-allowed"
>
  Browse Images
</button>
```

#### Task 2.6: Update Image Input
**File**: `frontend/src/routes/+page.svelte`
**Lines**: Replace line 268
**Old**:
```svelte
<input id="image-input" placeholder="image" bind:value={image} class="px-2 py-1 bg-surface border border-surface focus:outline-none focus:ring-2 focus:ring-primary" onkeydown={onKey} />
```
**New**:
```svelte
<input 
  id="image-input" 
  placeholder="image" 
  bind:value={image} 
  disabled={registry === 'custom' && !customRegistryValidated}
  class="px-2 py-1 bg-surface border border-surface focus:outline-none focus:ring-2 focus:ring-primary disabled:opacity-30 disabled:cursor-not-allowed" 
  onkeydown={onKey} 
/>
```

#### Task 2.7: Update Search Button
**File**: `frontend/src/routes/+page.svelte`
**Lines**: Replace line 269
**Old**:
```svelte
<button onclick={submit} class="px-3 py-1 bg-primary hover:bg-primary/80 rounded disabled:opacity-50" disabled={loadingTags}>Search</button>
```
**New**:
```svelte
<button 
  onclick={submit} 
  disabled={loadingTags || (registry === 'custom' && !customRegistryValidated)}
  class="px-3 py-1 bg-primary hover:bg-primary/80 rounded disabled:opacity-50"
>
  Search
</button>
```

#### Task 2.8: Add Detection Modal
**File**: `frontend/src/routes/+page.svelte`
**Lines**: After line 297 (after BrowseImagesDialog)
**Insert**:
```svelte

{#if showDetectionModal}
  <div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50" role="presentation" onclick={handleDetectionCancel} onkeydown={(e) => e.key === 'Escape' && handleDetectionCancel()}>
    <div class="bg-gray-800 rounded-lg shadow-xl w-full max-w-2xl p-6" role="dialog" aria-modal="true" tabindex="-1" onclick={(e) => e.stopPropagation()} onkeydown={(e) => e.key === 'Escape' && handleDetectionCancel()}>
      <h2 class="text-xl font-semibold text-white mb-4">Validate Custom Registry</h2>
      <CustomRegistryInput 
        detectionService={registryDetectionService}
        onSubmit={handleDetectionSuccess}
        onCancel={handleDetectionCancel}
      />
    </div>
  </div>
{/if}
```

### Phase 3: Testing (2 hours)

#### Task 3.1: Create Built-in Registry URL Display Tests
**File**: `frontend/tests/e2e/registry-url-display-builtin.spec.ts` (new)
**Content**:
```typescript
import { test, expect } from '@playwright/test';

test.describe('Registry URL Display - Built-in Registries', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('GHCR displays correct URL in readonly field', async ({ page }) => {
    await page.selectOption('[data-testid="registry-selector"]', 'ghcr');
    const urlInput = page.locator('#registry-url-input');
    await expect(urlInput).toHaveValue('ghcr.io');
    await expect(urlInput).toHaveAttribute('readonly');
    const checkButton = page.locator('[data-testid="check-registry-button"]');
    await expect(checkButton).not.toBeVisible();
  });

  test('Docker Hub displays correct URL in readonly field', async ({ page }) => {
    await page.selectOption('[data-testid="registry-selector"]', 'dockerhub');
    const urlInput = page.locator('#registry-url-input');
    await expect(urlInput).toHaveValue('docker.io');
    await expect(urlInput).toHaveAttribute('readonly');
    const checkButton = page.locator('[data-testid="check-registry-button"]');
    await expect(checkButton).not.toBeVisible();
  });

  test('Quay displays correct URL in readonly field', async ({ page }) => {
    await page.selectOption('[data-testid="registry-selector"]', 'quay');
    const urlInput = page.locator('#registry-url-input');
    await expect(urlInput).toHaveValue('quay.io');
    await expect(urlInput).toHaveAttribute('readonly');
    const checkButton = page.locator('[data-testid="check-registry-button"]');
    await expect(checkButton).not.toBeVisible();
  });

  test('GCR displays correct URL in readonly field', async ({ page }) => {
    await page.selectOption('[data-testid="registry-selector"]', 'gcr');
    const urlInput = page.locator('#registry-url-input');
    await expect(urlInput).toHaveValue('gcr.io');
    await expect(urlInput).toHaveAttribute('readonly');
    const checkButton = page.locator('[data-testid="check-registry-button"]');
    await expect(checkButton).not.toBeVisible();
  });

  test('built-in registries have all controls enabled', async ({ page }) => {
    const registries = ['ghcr', 'dockerhub', 'quay', 'gcr'];
    
    for (const registry of registries) {
      await page.selectOption('[data-testid="registry-selector"]', registry);
      
      const browseButton = page.locator('button', { hasText: 'Browse Images' });
      await expect(browseButton).toBeEnabled();
      
      const searchButton = page.locator('button', { hasText: 'Search' });
      await expect(searchButton).toBeEnabled();
      
      const ownerInput = page.locator('#owner-input');
      await expect(ownerInput).toBeEnabled();
      
      const imageInput = page.locator('#image-input');
      await expect(imageInput).toBeEnabled();
    }
  });
});
```

#### Task 3.2: Create Custom Registry Validation Flow Tests
**File**: `frontend/tests/e2e/custom-registry-validation-flow.spec.ts` (new)
**Content**:
```typescript
import { test, expect } from '@playwright/test';

test.describe('Custom Registry Validation Flow', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.selectOption('[data-testid="registry-selector"]', 'custom');
  });

  test('custom registry starts with all controls disabled', async ({ page }) => {
    const urlInput = page.locator('#registry-url-input');
    await expect(urlInput).toHaveValue('');
    await expect(urlInput).not.toHaveAttribute('readonly');
    
    const checkButton = page.locator('[data-testid="check-registry-button"]');
    await expect(checkButton).toBeVisible();
    await expect(checkButton).toBeDisabled();
    
    const browseButton = page.locator('button', { hasText: 'Browse Images' });
    await expect(browseButton).toBeDisabled();
    
    const searchButton = page.locator('button', { hasText: 'Search' });
    await expect(searchButton).toBeDisabled();
    
    const ownerInput = page.locator('#owner-input');
    await expect(ownerInput).toBeDisabled();
    
    const imageInput = page.locator('#image-input');
    await expect(imageInput).toBeDisabled();
  });

  test('entering URL enables check button', async ({ page }) => {
    const urlInput = page.locator('#registry-url-input');
    await urlInput.fill('docker.redpanda.com');
    
    const checkButton = page.locator('[data-testid="check-registry-button"]');
    await expect(checkButton).toBeEnabled();
    
    const browseButton = page.locator('button', { hasText: 'Browse Images' });
    await expect(browseButton).toBeDisabled();
    
    const searchButton = page.locator('button', { hasText: 'Search' });
    await expect(searchButton).toBeDisabled();
  });

  test('successful validation enables all controls', async ({ page }) => {
    const urlInput = page.locator('#registry-url-input');
    await urlInput.fill('docker.redpanda.com');
    
    const checkButton = page.locator('[data-testid="check-registry-button"]');
    await checkButton.click();
    
    await expect(page.locator('role=dialog')).toBeVisible();
    await expect(page.locator('h2', { hasText: 'Validate Custom Registry' })).toBeVisible();
    
    const detectButton = page.locator('button.detect-btn');
    await detectButton.click();
    
    await expect(page.locator('.success-title')).toBeVisible();
    await expect(page.locator('.success-title')).toContainText('OCI Registry Detected');
    
    const useButton = page.locator('button.submit-btn');
    await useButton.click();
    
    await expect(page.locator('role=dialog')).not.toBeVisible();
    
    await expect(urlInput).toHaveValue(/https:\/\/docker\.redpanda\.com/);
    await expect(urlInput).toHaveAttribute('readonly');
    
    await expect(checkButton).toBeDisabled();
    
    const browseButton = page.locator('button', { hasText: 'Browse Images' });
    await expect(browseButton).toBeEnabled();
    
    const searchButton = page.locator('button', { hasText: 'Search' });
    await expect(searchButton).toBeEnabled();
    
    const ownerInput = page.locator('#owner-input');
    await expect(ownerInput).toBeEnabled();
    
    const imageInput = page.locator('#image-input');
    await expect(imageInput).toBeEnabled();
  });

  test('can cancel validation', async ({ page }) => {
    const urlInput = page.locator('#registry-url-input');
    await urlInput.fill('docker.redpanda.com');
    
    const checkButton = page.locator('[data-testid="check-registry-button"]');
    await checkButton.click();
    
    await expect(page.locator('role=dialog')).toBeVisible();
    
    const cancelButton = page.locator('button.cancel-btn');
    await cancelButton.click();
    
    await expect(page.locator('role=dialog')).not.toBeVisible();
    
    await expect(checkButton).toBeEnabled();
    
    const browseButton = page.locator('button', { hasText: 'Browse Images' });
    await expect(browseButton).toBeDisabled();
  });
});
```

#### Task 3.3: Create URL Change Tests
**File**: `frontend/tests/e2e/custom-registry-url-change.spec.ts` (new)
**Content**:
```typescript
import { test, expect } from '@playwright/test';

test.describe('Custom Registry URL Change', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.selectOption('[data-testid="registry-selector"]', 'custom');
  });

  async function validateRegistry(page: any) {
    const urlInput = page.locator('#registry-url-input');
    await urlInput.fill('docker.redpanda.com');
    
    const checkButton = page.locator('[data-testid="check-registry-button"]');
    await checkButton.click();
    
    const detectButton = page.locator('button.detect-btn');
    await detectButton.click();
    
    await expect(page.locator('.success-title')).toBeVisible();
    
    const useButton = page.locator('button.submit-btn');
    await useButton.click();
    
    await expect(page.locator('role=dialog')).not.toBeVisible();
  }

  test('editing URL after validation resets state', async ({ page }) => {
    await validateRegistry(page);
    
    const ownerInput = page.locator('#owner-input');
    await ownerInput.fill('testowner');
    
    const imageInput = page.locator('#image-input');
    await imageInput.fill('testimage');
    
    const urlInput = page.locator('#registry-url-input');
    await urlInput.click();
    await urlInput.fill('registry.example.com');
    
    const checkButton = page.locator('[data-testid="check-registry-button"]');
    await expect(checkButton).toBeEnabled();
    
    const browseButton = page.locator('button', { hasText: 'Browse Images' });
    await expect(browseButton).toBeDisabled();
    
    const searchButton = page.locator('button', { hasText: 'Search' });
    await expect(searchButton).toBeDisabled();
    
    await expect(ownerInput).toBeDisabled();
    await expect(ownerInput).toHaveValue('');
    
    await expect(imageInput).toBeDisabled();
    await expect(imageInput).toHaveValue('');
  });

  test('can re-validate after URL change', async ({ page }) => {
    await validateRegistry(page);
    
    const urlInput = page.locator('#registry-url-input');
    await urlInput.click();
    await urlInput.fill('docker.redpanda.com');
    
    const checkButton = page.locator('[data-testid="check-registry-button"]');
    await expect(checkButton).toBeEnabled();
    
    await checkButton.click();
    
    const detectButton = page.locator('button.detect-btn');
    await detectButton.click();
    
    await expect(page.locator('.success-title')).toBeVisible();
    
    const useButton = page.locator('button.submit-btn');
    await useButton.click();
    
    const browseButton = page.locator('button', { hasText: 'Browse Images' });
    await expect(browseButton).toBeEnabled();
    
    const searchButton = page.locator('button', { hasText: 'Search' });
    await expect(searchButton).toBeEnabled();
  });
});
```

#### Task 3.4: Create Validation Failure Tests
**File**: `frontend/tests/e2e/custom-registry-validation-failure.spec.ts` (new)
**Content**:
```typescript
import { test, expect } from '@playwright/test';

test.describe('Custom Registry Validation Failure', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.selectOption('[data-testid="registry-selector"]', 'custom');
  });

  test('validation failure shows error in modal', async ({ page }) => {
    const urlInput = page.locator('#registry-url-input');
    await urlInput.fill('not-a-registry.invalid');
    
    const checkButton = page.locator('[data-testid="check-registry-button"]');
    await checkButton.click();
    
    await expect(page.locator('role=dialog')).toBeVisible();
    
    const detectButton = page.locator('button.detect-btn');
    await detectButton.click();
    
    await expect(page.locator('.error-message')).toBeVisible();
    
    const useButton = page.locator('button.submit-btn');
    await expect(useButton).toBeDisabled();
  });

  test('can cancel after validation failure', async ({ page }) => {
    const urlInput = page.locator('#registry-url-input');
    await urlInput.fill('not-a-registry.invalid');
    
    const checkButton = page.locator('[data-testid="check-registry-button"]');
    await checkButton.click();
    
    const detectButton = page.locator('button.detect-btn');
    await detectButton.click();
    
    await expect(page.locator('.error-message')).toBeVisible();
    
    const cancelButton = page.locator('button.cancel-btn');
    await cancelButton.click();
    
    await expect(page.locator('role=dialog')).not.toBeVisible();
    
    await expect(urlInput).not.toHaveAttribute('readonly');
    await expect(checkButton).toBeEnabled();
    
    const browseButton = page.locator('button', { hasText: 'Browse Images' });
    await expect(browseButton).toBeDisabled();
  });

  test('can retry after validation failure', async ({ page }) => {
    const urlInput = page.locator('#registry-url-input');
    await urlInput.fill('not-a-registry.invalid');
    
    const checkButton = page.locator('[data-testid="check-registry-button"]');
    await checkButton.click();
    
    const detectButton = page.locator('button.detect-btn');
    await detectButton.click();
    
    await expect(page.locator('.error-message')).toBeVisible();
    
    const cancelButton = page.locator('button.cancel-btn');
    await cancelButton.click();
    
    await urlInput.fill('docker.redpanda.com');
    await checkButton.click();
    
    const detectButton2 = page.locator('button.detect-btn');
    await detectButton2.click();
    
    await expect(page.locator('.success-title')).toBeVisible();
    
    const useButton = page.locator('button.submit-btn');
    await useButton.click();
    
    const browseButton = page.locator('button', { hasText: 'Browse Images' });
    await expect(browseButton).toBeEnabled();
  });
});
```

#### Task 3.5: Create Registry Type Switching Tests
**File**: `frontend/tests/e2e/registry-type-switching.spec.ts` (new)
**Content**:
```typescript
import { test, expect } from '@playwright/test';

test.describe('Registry Type Switching', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  async function validateCustomRegistry(page: any) {
    await page.selectOption('[data-testid="registry-selector"]', 'custom');
    
    const urlInput = page.locator('#registry-url-input');
    await urlInput.fill('docker.redpanda.com');
    
    const checkButton = page.locator('[data-testid="check-registry-button"]');
    await checkButton.click();
    
    const detectButton = page.locator('button.detect-btn');
    await detectButton.click();
    
    await expect(page.locator('.success-title')).toBeVisible();
    
    const useButton = page.locator('button.submit-btn');
    await useButton.click();
  }

  test('switching from custom to built-in clears validation', async ({ page }) => {
    await validateCustomRegistry(page);
    
    await page.selectOption('[data-testid="registry-selector"]', 'dockerhub');
    
    const urlInput = page.locator('#registry-url-input');
    await expect(urlInput).toHaveValue('docker.io');
    await expect(urlInput).toHaveAttribute('readonly');
    
    const checkButton = page.locator('[data-testid="check-registry-button"]');
    await expect(checkButton).not.toBeVisible();
    
    const browseButton = page.locator('button', { hasText: 'Browse Images' });
    await expect(browseButton).toBeEnabled();
    
    await page.selectOption('[data-testid="registry-selector"]', 'custom');
    
    await expect(urlInput).toHaveValue('');
    await expect(urlInput).not.toHaveAttribute('readonly');
    
    await expect(checkButton).toBeVisible();
    await expect(browseButton).toBeDisabled();
  });

  test('switching between built-in registries works normally', async ({ page }) => {
    await page.selectOption('[data-testid="registry-selector"]', 'ghcr');
    
    const urlInput = page.locator('#registry-url-input');
    await expect(urlInput).toHaveValue('ghcr.io');
    
    await page.selectOption('[data-testid="registry-selector"]', 'quay');
    await expect(urlInput).toHaveValue('quay.io');
    
    const browseButton = page.locator('button', { hasText: 'Browse Images' });
    await expect(browseButton).toBeEnabled();
    
    const checkButton = page.locator('[data-testid="check-registry-button"]');
    await expect(checkButton).not.toBeVisible();
  });

  test('owner/image values persist when switching built-in registries', async ({ page }) => {
    await page.selectOption('[data-testid="registry-selector"]', 'ghcr');
    
    const ownerInput = page.locator('#owner-input');
    await ownerInput.fill('microsoft');
    
    const imageInput = page.locator('#image-input');
    await imageInput.fill('vscode');
    
    await page.selectOption('[data-testid="registry-selector"]', 'dockerhub');
    
    await expect(ownerInput).toHaveValue('microsoft');
    await expect(imageInput).toHaveValue('vscode');
  });

  test('owner/image values cleared when switching to custom', async ({ page }) => {
    await page.selectOption('[data-testid="registry-selector"]', 'ghcr');
    
    const ownerInput = page.locator('#owner-input');
    await ownerInput.fill('microsoft');
    
    const imageInput = page.locator('#image-input');
    await imageInput.fill('vscode');
    
    await page.selectOption('[data-testid="registry-selector"]', 'custom');
    
    await expect(ownerInput).toHaveValue('');
    await expect(imageInput).toHaveValue('');
  });
});
```

#### Task 3.6: Run Existing Tests
**Command**: `cd frontend && npm test`
**Expected**: All existing tests pass without regressions

#### Task 3.7: Run New E2E Tests
**Command**: `cd frontend && npx playwright test tests/e2e/registry-url-display-builtin.spec.ts tests/e2e/custom-registry-validation-flow.spec.ts tests/e2e/custom-registry-url-change.spec.ts tests/e2e/custom-registry-validation-failure.spec.ts tests/e2e/registry-type-switching.spec.ts`
**Expected**: All new tests pass

### Phase 4: Documentation (15 mins)

#### Task 4.1: Update CHANGELOG
**File**: `CHANGELOG.md`
**Section**: Unreleased
**Content**:
```markdown
### Added
- Registry URL display field for all registry types (built-in and custom)
- Check button for custom registry validation in main UI
- Conditional enabling/disabling of controls based on custom registry validation state
- Validation modal for custom registries triggered from main UI

### Changed
- Custom registry validation now happens in main UI instead of browse modal
- Owner and image fields are cleared when custom registry URL changes
- Browse and search buttons disabled until custom registry is validated
```

#### Task 4.2: Update AGENTS.md
**File**: `AGENTS.md`
**Section**: Recent Changes
**Content**:
```markdown
- 005-custom-registry-ui-enhancement: Enhanced custom registry UX with visible URL validation workflow in main UI
```

## Testing Strategy

### Unit Tests
- None required (logic is simple state management)
- Could add tests for helper functions if component is extracted later

### E2E Tests
- **5 new test files** covering all user scenarios
- **25+ individual test cases** covering:
  - Built-in registry URL display
  - Custom registry initial state
  - Validation flow (success and failure)
  - URL change behavior
  - Registry type switching
  - Control enabling/disabling
  - Field clearing behavior

### Manual Testing Checklist
- [ ] All built-in registries show correct URLs
- [ ] Custom registry starts with controls disabled
- [ ] Check button behavior correct
- [ ] Validation modal opens and closes correctly
- [ ] Successful validation enables controls
- [ ] Failed validation shows error
- [ ] Changing URL resets state
- [ ] Switching registries works correctly
- [ ] Visual styling looks consistent
- [ ] Keyboard navigation works
- [ ] Screen reader announces state changes

## Rollout Plan

### Phase 1: Implementation (2 hours)
- Complete all code changes
- Add all test files

### Phase 2: Testing (1 hour)
- Run all existing tests
- Run all new tests
- Manual testing of edge cases

### Phase 3: Documentation (15 mins)
- Update CHANGELOG
- Update AGENTS.md

### Phase 4: Review (30 mins)
- Code review
- Test coverage review
- Visual design review

## Risk Assessment

### Low Risk
- **State management changes**: Simple boolean flags
- **Component reuse**: No changes to existing CustomRegistryInput
- **Backend API**: No changes needed

### Medium Risk
- **Conditional disable logic**: Must be tested thoroughly
- **Field clearing on URL change**: Could surprise users initially

### Mitigation
- Comprehensive E2E test coverage for all state transitions
- Clear visual feedback for disabled states
- Readonly URL field after validation prevents accidental changes

## Success Criteria

- [ ] All 5 new E2E test files pass
- [ ] All existing E2E tests pass
- [ ] Manual testing confirms smooth UX
- [ ] No console errors or warnings
- [ ] Validation state transitions are immediate
- [ ] Disabled states are visually clear
- [ ] Code follows existing patterns and style

## Timeline

- State Management: 30 minutes
- UI Components: 45 minutes
- Testing: 2 hours
- Documentation: 15 minutes
- **Total: ~3.5 hours**

## Dependencies

- No new npm packages
- No backend changes
- No database changes
- Reuses existing CustomRegistryInput component
- Reuses existing registryDetectionService

## Follow-up Tasks (Future)

- Add localStorage persistence for last custom URL
- Add "clear" button to reset custom URL
- Add visual indicator (green border) when validated
- Consider auto-validation after typing stops
- Add keyboard shortcuts (Enter to validate, etc.)
