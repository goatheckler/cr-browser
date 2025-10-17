# Quickstart: Custom Registry UI Enhancement

**Feature**: 005-custom-registry-ui-enhancement  
**Last Updated**: 2025-10-16

## What This Feature Does

Moves custom registry validation from a hidden modal workflow to the main UI, making the registry URL and validation state visible at all times. Users can see which registry they're working with and must explicitly validate custom registries before using browse/search features.

## Quick Demo

### Before (Current State)
1. Select "Custom Registry" → controls look normal
2. Click "Browse Images" → modal appears asking for URL
3. User has no idea validation is needed until they click browse

### After (This Feature)
1. Select "Custom Registry" → URL field empty, check button enabled, browse/search disabled
2. Enter URL "docker.redpanda.com" → check button stays enabled
3. Click check button (✓) → validation modal appears
4. Validate successfully → URL becomes readonly, all controls enable
5. Edit URL → validation resets, controls disable again

## Key Files

- **Main Implementation**: `frontend/src/routes/+page.svelte` (~80% of changes)
- **Reused Component**: `frontend/src/lib/components/CustomRegistryInput.svelte` (no changes)
- **Test Files**: `frontend/tests/e2e/` (5 new test files)

## Key Concepts

### State Variables
```typescript
customRegistryValidated: boolean    // tracks if custom URL passed detection
showDetectionModal: boolean         // controls modal visibility
customRegistryUrl: string | undefined // stores the validated URL
```

### State Flow
```
Custom Selected → URL Empty → Controls Disabled
                 ↓
           Enter URL → Check Button Enabled
                 ↓
         Click Check → Modal Opens
                 ↓
    Validate Success → Controls Enabled, URL Readonly
                 ↓
            Edit URL → Validation Resets, Controls Disabled
```

### Conditional Logic
```typescript
// Controls are disabled when:
registry === 'custom' && !customRegistryValidated

// URL field is readonly when:
registry !== 'custom' || (registry === 'custom' && customRegistryValidated)
```

## Running the Implementation

### 1. Make Code Changes
```bash
cd /home/fnord/code/ghcr-browser
# Edit frontend/src/routes/+page.svelte following plan.md
```

### 2. Start Dev Server
```bash
cd frontend
npm run dev
```

### 3. Manual Testing
```bash
# Open http://localhost:5173
# Test each scenario in spec.md
```

### 4. Run E2E Tests
```bash
cd frontend
npx playwright test tests/e2e/registry-url-display-builtin.spec.ts
npx playwright test tests/e2e/custom-registry-validation-flow.spec.ts
npx playwright test tests/e2e/custom-registry-url-change.spec.ts
npx playwright test tests/e2e/custom-registry-validation-failure.spec.ts
npx playwright test tests/e2e/registry-type-switching.spec.ts
```

### 5. Run All Tests
```bash
cd frontend
npm test  # Runs all tests including new ones
```

## Common Issues & Solutions

### Issue: Check button not enabling
**Solution**: Verify `customRegistryUrl` has a value and `customRegistryValidated` is false

### Issue: Modal not opening
**Solution**: Check `showDetectionModal` state and modal conditional rendering

### Issue: Controls not disabling
**Solution**: Verify `disabled={registry === 'custom' && !customRegistryValidated}` on all relevant elements

### Issue: URL field not readonly after validation
**Solution**: Check readonly condition: `registry !== 'custom' || customRegistryValidated`

### Issue: Owner/image not clearing on URL change
**Solution**: Verify `handleRegistryUrlChange` sets `owner = ''` and `image = ''`

## Testing Checklist (Quick)

### Built-in Registries
- [ ] GHCR shows "ghcr.io" (readonly)
- [ ] Docker Hub shows "docker.io" (readonly)
- [ ] Quay shows "quay.io" (readonly)
- [ ] GCR shows "gcr.io" (readonly)
- [ ] Check button hidden for all
- [ ] All controls enabled for all

### Custom Registry - Initial State
- [ ] URL field empty and editable
- [ ] Check button disabled (no URL)
- [ ] Browse button disabled
- [ ] Search button disabled
- [ ] Owner input disabled
- [ ] Image input disabled

### Custom Registry - After URL Entry
- [ ] Enter URL → check button enables
- [ ] Other controls stay disabled

### Custom Registry - After Validation
- [ ] Click check → modal opens
- [ ] Validate → modal closes
- [ ] URL field readonly with normalized URL
- [ ] Check button disabled
- [ ] All other controls enabled

### Custom Registry - After URL Edit
- [ ] Edit URL → check button enables
- [ ] All controls disable
- [ ] Owner/image fields cleared

## Code Snippets for Quick Reference

### Helper Functions Location
Add after line 91 in +page.svelte (after `getRegistryHost` function)

### UI Components Location
- URL input: after line 260 (after RegistrySelector)
- Check button: immediately after URL input
- Modal: after line 297 (after BrowseImagesDialog)

### Imports Location
Add after line 8 in +page.svelte:
```typescript
import CustomRegistryInput from '$lib/components/CustomRegistryInput.svelte';
import { registryDetectionService } from '$lib/services/registryDetection';
```

## Timeline Reference

- **State Management**: 30 minutes
- **UI Components**: 45 minutes
- **Testing (Create)**: 1 hour
- **Testing (Run)**: 1 hour
- **Documentation**: 15 minutes
- **Total**: ~3.5 hours

## Next Steps After Implementation

1. Manual test all scenarios
2. Run all E2E tests
3. Check for console errors
4. Verify visual consistency
5. Update CHANGELOG.md
6. Update AGENTS.md
7. Mark all tasks complete in tasks.md

## Related Documentation

- Full spec: `specs/005-custom-registry-ui-enhancement/spec.md`
- Implementation plan: `specs/005-custom-registry-ui-enhancement/plan.md`
- Task tracking: `specs/005-custom-registry-ui-enhancement/tasks.md`
- Parent feature: `specs/004-redpanda-custom-registry/`
