# Feature Specification: Custom Registry UI Enhancement

**Feature Branch**: `005-custom-registry-ui-enhancement`  
**Created**: 2025-10-16  
**Status**: Draft  
**Parent Feature**: 004-redpanda-custom-registry  
**Input**: Improve custom registry UX by moving validation to main UI and displaying registry URLs for all registry types.

## Overview

Enhance the custom registry user experience by surfacing the registry URL and validation workflow directly in the main application UI, rather than hiding it behind the Browse Images modal. This makes the custom registry validation state visible and prevents users from attempting operations before validation is complete.

**Primary Value**: 
- **Visibility**: Users can always see which registry URL they're working with
- **Clarity**: Validation state is clear before attempting browse/search operations
- **Consistency**: All registries (built-in and custom) show their URLs
- **Efficiency**: Fewer clicks to validate custom registries

**Scope**: 
- Display registry URL for all registry types in main UI
- Add URL validation button next to registry URL input
- Conditionally enable/disable browse and search based on custom registry validation state
- Reuse existing CustomRegistryInput modal for validation
- Reset validation state when custom registry URL changes

**Out of Scope for Initial Release**:
- Saving/remembering custom registry URLs between sessions
- Auto-validation on URL entry
- Advanced registry URL templates or suggestions

## User Scenarios & Testing

### Scenario 1: Working with Built-in Registry (GHCR)
**Actor**: Developer browsing GitHub packages  
**Goal**: Search for container images on GHCR

**Steps**:
1. User opens the application
2. User sees GHCR selected by default
3. User sees registry URL field displaying "ghcr.io" (readonly)
4. Check button is disabled/hidden
5. User enters owner "microsoft"
6. User enters image "vscode"
7. User clicks Search button
8. System displays tags for microsoft/vscode

**Expected Outcome**: Normal workflow unchanged, but registry URL is visible for transparency

**Test Coverage**: `tests/e2e/registry-url-display-builtin.spec.ts`
- Verify URL field shows correct value for each built-in registry
- Verify URL field is readonly
- Verify check button is disabled
- Verify browse/search buttons are enabled

### Scenario 2: First-Time Custom Registry Validation
**Actor**: DevOps engineer using Redpanda registry  
**Goal**: Validate and browse a custom registry

**Steps**:
1. User opens the application
2. User selects "Custom Registry" from dropdown
3. User sees registry URL field is empty and editable
4. Check button is enabled
5. Browse Images button is disabled
6. Search button is disabled
7. Owner input is disabled
8. Image input is disabled
9. User enters URL "docker.redpanda.com"
10. Check button remains enabled
11. User clicks Check button (✓)
12. System displays validation modal with CustomRegistryInput component
13. User clicks "Detect" in modal
14. System validates registry successfully
15. System shows "✓ OCI Registry Detected" with registry info
16. User clicks "Use This Registry"
17. Modal closes
18. Registry URL field shows "https://docker.redpanda.com" (readonly)
19. Check button becomes disabled
20. Browse Images button becomes enabled
21. Search button becomes enabled
22. Owner input becomes enabled
23. Image input becomes enabled
24. User enters owner "redpandadata"
25. User clicks Browse Images
26. System displays list of images

**Expected Outcome**: User must validate custom registry before using browse/search functionality

**Test Coverage**: `tests/e2e/custom-registry-validation-flow.spec.ts`
- Verify initial disabled state when custom registry selected
- Verify check button enables when URL entered
- Verify modal opens on check button click
- Verify successful validation enables all controls
- Verify URL field becomes readonly after validation
- Verify check button disables after validation

### Scenario 3: Changing Custom Registry URL After Validation
**Actor**: Developer switching between custom registries  
**Goal**: Validate a different custom registry URL

**Steps**:
1. User has previously validated "docker.redpanda.com"
2. Registry URL shows "https://docker.redpanda.com" (readonly)
3. Browse/Search buttons are enabled
4. Owner/Image inputs are enabled
5. User clicks in Registry URL field and edits it to "registry.example.com"
6. System detects URL change
7. Check button becomes enabled
8. Browse Images button becomes disabled
9. Search button becomes disabled
10. Owner input becomes disabled
11. Image input becomes disabled
12. Owner and Image fields are cleared
13. User clicks Check button
14. System displays validation modal
15. User validates new registry
16. System re-enables all controls with new registry

**Expected Outcome**: Changing URL invalidates previous validation and requires re-validation

**Test Coverage**: `tests/e2e/custom-registry-url-change.spec.ts`
- Verify editing URL after validation resets validation state
- Verify all controls become disabled again
- Verify owner/image fields are cleared
- Verify check button re-enables
- Verify user must re-validate before using browse/search

### Scenario 4: Custom Registry Validation Failure
**Actor**: User entering incorrect registry URL  
**Goal**: System provides clear feedback when validation fails

**Steps**:
1. User selects "Custom Registry"
2. User enters URL "not-a-registry.invalid"
3. User clicks Check button
4. System displays validation modal
5. User clicks "Detect" in modal
6. System attempts validation
7. System displays error: "Registry detection failed: [error details]"
8. User clicks "Cancel"
9. Modal closes
10. Registry URL field still shows user's input (editable)
11. Check button remains enabled
12. Browse/Search remain disabled
13. User can correct URL and try again

**Expected Outcome**: Failed validation doesn't lock user out, they can retry with corrected URL

**Test Coverage**: `tests/e2e/custom-registry-validation-failure.spec.ts`
- Verify validation failure shows error in modal
- Verify controls remain disabled after failed validation
- Verify user can retry validation
- Verify check button remains enabled for retry

### Scenario 5: Switching Between Registry Types
**Actor**: Developer working with multiple registries  
**Goal**: Switch between built-in and custom registries

**Steps**:
1. User has validated custom registry "docker.redpanda.com"
2. Owner field shows "redpandadata"
3. Image field shows "redpanda"
4. User switches registry dropdown from "Custom" to "Docker Hub"
5. Registry URL field changes to "docker.io" (readonly)
6. Check button becomes disabled/hidden
7. Owner/Image fields retain their values
8. Browse/Search buttons remain enabled
9. User switches back to "Custom Registry"
10. Registry URL field becomes empty and editable
11. Check button becomes enabled
12. Browse/Search buttons become disabled
13. Owner/Image fields become disabled
14. Previous custom URL is not remembered

**Expected Outcome**: Switching to custom registry always requires fresh validation

**Test Coverage**: `tests/e2e/registry-type-switching.spec.ts`
- Verify switching from custom to built-in clears validation
- Verify switching from built-in to custom requires new validation
- Verify URL field updates correctly for each registry type
- Verify check button state matches registry type

## Functional Requirements

### Registry URL Display
1. **FR-1**: System SHALL display a registry URL input field next to the registry type dropdown
2. **FR-2**: For built-in registries (GHCR, Docker Hub, Quay, GCR), URL field SHALL display the registry's URL in readonly mode
3. **FR-3**: For custom registry, URL field SHALL be editable and initially empty
4. **FR-4**: URL field SHALL use consistent styling with other input fields

### Custom Registry Validation UI
5. **FR-5**: System SHALL display a check button (✓ icon) next to the registry URL field when custom registry is selected
6. **FR-6**: Check button SHALL be enabled when custom registry is selected and URL field has a value
7. **FR-7**: Check button SHALL be disabled after successful validation
8. **FR-8**: Check button SHALL be hidden or disabled when built-in registry is selected
9. **FR-9**: Clicking check button SHALL open the custom registry validation modal

### Validation Modal
10. **FR-10**: Validation modal SHALL reuse the existing CustomRegistryInput component
11. **FR-11**: Modal SHALL display "Validate Custom Registry" as the title
12. **FR-12**: Successful validation SHALL close modal and enable browse/search controls
13. **FR-13**: Cancelled validation SHALL close modal without enabling controls
14. **FR-14**: Failed validation SHALL show error in modal and keep controls disabled

### Conditional Enabling/Disabling
15. **FR-15**: When custom registry is selected and NOT validated:
    - Browse Images button SHALL be disabled
    - Search button SHALL be disabled
    - Owner input SHALL be disabled
    - Image input SHALL be disabled
16. **FR-16**: When custom registry is selected and validated:
    - All controls SHALL be enabled
    - URL field SHALL become readonly
    - Check button SHALL become disabled
17. **FR-17**: When built-in registry is selected:
    - All controls SHALL function normally (enabled)
    - URL field SHALL be readonly
    - Check button SHALL be hidden/disabled

### Validation State Management
18. **FR-18**: Editing custom registry URL after validation SHALL reset validation state
19. **FR-19**: Resetting validation state SHALL:
    - Re-enable check button
    - Disable browse/search buttons
    - Disable owner/image inputs
    - Clear owner and image field values
20. **FR-20**: Switching from custom registry to built-in registry SHALL clear custom validation state
21. **FR-21**: Switching from built-in registry to custom registry SHALL require new validation

### Visual Feedback
22. **FR-22**: Disabled controls SHALL have reduced opacity (30% opacity)
23. **FR-23**: Disabled controls SHALL show not-allowed cursor on hover
24. **FR-24**: Check button SHALL use inline SVG checkmark icon
25. **FR-25**: Readonly URL fields SHALL have reduced opacity (60% opacity) to indicate non-editable state

## Key Entities

### UI State (in +page.svelte)
```typescript
{
  registry: 'ghcr' | 'dockerhub' | 'quay' | 'gcr' | 'custom',
  customRegistryUrl: string | undefined,
  customRegistryValidated: boolean,
  showDetectionModal: boolean,
  owner: string,
  image: string,
  // ... existing state
}
```

### Registry URL Display Logic
```typescript
function getRegistryUrlDisplay(): string {
  if (registry === 'custom') {
    return customRegistryUrl || '';
  }
  return getRegistryHost(registry); // 'ghcr.io', 'docker.io', etc.
}
```

### Validation State Transitions
```
Initial (Custom Selected)
  ↓
  customRegistryValidated: false
  check button: enabled
  browse/search: disabled
  owner/image: disabled
  ↓ (user clicks check button)
Modal Shown
  ↓ (user validates successfully)
Validated State
  customRegistryValidated: true
  check button: disabled
  browse/search: enabled
  owner/image: enabled
  URL field: readonly
  ↓ (user edits URL)
Back to Initial State
  customRegistryValidated: false
  check button: enabled
  browse/search: disabled
  owner/image: disabled
  owner/image values: cleared
```

## User Interface Requirements

### Layout Changes
Current layout (line 258-270 in +page.svelte):
```
[Registry Dropdown] [Clear Token] [Owner Label] [Owner Input] [Browse] [Image Label] [Image Input] [Search]
```

New layout:
```
[Registry Dropdown] [URL Label] [Registry URL Input] [Check Button] [Clear Token]
[Owner Label] [Owner Input] [Browse Images] [Image Label] [Image Input] [Search]
```

### Component Specifications

#### Registry URL Input Field
- Type: `<input type="text">`
- ID: `registry-url-input`
- Label: "URL:"
- Placeholder (custom only): "Enter registry URL"
- Readonly: when `registry !== 'custom'` OR when `registry === 'custom' && customRegistryValidated`
- Value: result of `getRegistryUrlDisplay()`
- On input: `handleRegistryUrlChange()` (only fires when editable)
- Styling: 
  - Base: `px-2 py-1 bg-surface border border-surface focus:outline-none focus:ring-2 focus:ring-primary`
  - Readonly: add `opacity-60`
  - Disabled: add `opacity-30 cursor-not-allowed`

#### Check Button
- Type: `<button>`
- Visible: only when `registry === 'custom'`
- Disabled: when `!customRegistryUrl || customRegistryValidated`
- Title: "Validate registry"
- Icon: Inline SVG checkmark (✓)
- On click: `handleCheckRegistry()`
- Styling:
  - Base: `px-2 py-1 bg-surface hover:bg-surface/80 rounded border border-primary`
  - Disabled: add `disabled:opacity-30 disabled:cursor-not-allowed`

#### Browse Images Button (modified)
- Existing button with added disabled condition
- Disabled: when `(registry === 'custom' && !customRegistryValidated) || loadingTags`
- Styling: add `disabled:opacity-30 disabled:cursor-not-allowed`

#### Search Button (modified)
- Existing button with added disabled condition
- Disabled: when `loadingTags || (registry === 'custom' && !customRegistryValidated)`
- Styling: already has `disabled:opacity-50`

#### Owner Input (modified)
- Existing input with added disabled condition
- Disabled: when `registry === 'custom' && !customRegistryValidated`
- Styling: add `disabled:opacity-30 disabled:cursor-not-allowed`

#### Image Input (modified)
- Existing input with added disabled condition
- Disabled: when `registry === 'custom' && !customRegistryValidated`
- Styling: add `disabled:opacity-30 disabled:cursor-not-allowed`

### Validation Modal
- Positioned: fixed overlay with centered content
- Background: semi-transparent black backdrop
- Content: `CustomRegistryInput` component
- Title: "Validate Custom Registry"
- Z-index: 50 (same as Browse dialog)
- Keyboard: Escape key closes modal

## Acceptance Criteria

1. ✅ Built-in registries display their URLs in readonly field
2. ✅ Check button is hidden/disabled for built-in registries
3. ✅ Custom registry shows empty editable URL field initially
4. ✅ Check button is enabled when custom URL is entered
5. ✅ Browse/Search/Owner/Image are disabled until custom registry is validated
6. ✅ Check button opens validation modal with CustomRegistryInput component
7. ✅ Successful validation enables all controls and disables check button
8. ✅ URL field becomes readonly after successful validation
9. ✅ Editing custom URL after validation resets state and disables controls
10. ✅ Owner and image fields are cleared when URL changes
11. ✅ Switching between registry types maintains correct enabled/disabled states
12. ✅ All existing e2e tests continue to pass
13. ✅ New e2e tests cover all validation workflows

## Non-Functional Requirements

### Usability
- Disabled controls MUST be visually distinguishable (opacity, cursor)
- Validation state transitions MUST be immediate (no lag)
- Modal MUST be keyboard accessible (Escape to close)
- Tab order MUST flow logically through controls

### Accessibility
- All inputs MUST have associated labels
- Disabled states MUST be communicated to screen readers
- Check button MUST have aria-label or title
- Modal MUST trap focus and be dismissible

### Performance
- State updates MUST not cause unnecessary re-renders
- Clearing owner/image fields MUST be instantaneous
- Modal open/close MUST be smooth (no jank)

## Testing Requirements

### E2E Test Files to Create

#### 1. `tests/e2e/registry-url-display-builtin.spec.ts`
```typescript
test('displays correct URLs for built-in registries', async ({ page }) => {
  // Test GHCR shows ghcr.io
  // Test Docker Hub shows docker.io
  // Test Quay shows quay.io
  // Test GCR shows gcr.io
  // Verify URL field is readonly for all
  // Verify check button is disabled/hidden for all
});

test('built-in registries have all controls enabled', async ({ page }) => {
  // For each built-in registry
  // Verify Browse Images button is enabled
  // Verify Search button is enabled
  // Verify Owner input is enabled
  // Verify Image input is enabled
});
```

#### 2. `tests/e2e/custom-registry-validation-flow.spec.ts`
```typescript
test('custom registry starts with all controls disabled', async ({ page }) => {
  // Select Custom Registry
  // Verify URL field is empty and editable
  // Verify check button is disabled (no URL yet)
  // Verify Browse button is disabled
  // Verify Search button is disabled
  // Verify Owner input is disabled
  // Verify Image input is disabled
});

test('entering URL enables check button', async ({ page }) => {
  // Select Custom Registry
  // Enter URL "docker.redpanda.com"
  // Verify check button becomes enabled
  // Verify other controls remain disabled
});

test('successful validation enables all controls', async ({ page }) => {
  // Select Custom Registry
  // Enter URL "docker.redpanda.com"
  // Click check button
  // Verify modal opens
  // Click Detect in modal
  // Wait for success message
  // Click "Use This Registry"
  // Verify modal closes
  // Verify URL field is readonly with normalized URL
  // Verify check button is disabled
  // Verify Browse button is enabled
  // Verify Search button is enabled
  // Verify Owner input is enabled
  // Verify Image input is enabled
});

test('can browse images after validation', async ({ page }) => {
  // Complete validation flow
  // Enter owner "redpandadata"
  // Click Browse Images
  // Verify browse dialog opens
  // Verify images load successfully
});

test('can search tags after validation', async ({ page }) => {
  // Complete validation flow
  // Enter owner "redpandadata"
  // Enter image "redpanda"
  // Click Search
  // Verify tags load successfully
});
```

#### 3. `tests/e2e/custom-registry-url-change.spec.ts`
```typescript
test('editing URL after validation resets state', async ({ page }) => {
  // Complete validation flow
  // Enter owner "test"
  // Enter image "test"
  // Click in URL field
  // Change URL to "registry.example.com"
  // Verify check button becomes enabled
  // Verify Browse button becomes disabled
  // Verify Search button becomes disabled
  // Verify Owner input becomes disabled
  // Verify Image input becomes disabled
  // Verify owner field is cleared
  // Verify image field is cleared
});

test('can re-validate after URL change', async ({ page }) => {
  // Complete initial validation
  // Change URL
  // Click check button
  // Complete validation again
  // Verify all controls enabled again
});
```

#### 4. `tests/e2e/custom-registry-validation-failure.spec.ts`
```typescript
test('validation failure shows error in modal', async ({ page }) => {
  // Select Custom Registry
  // Enter invalid URL "not-a-registry.invalid"
  // Click check button
  // Modal opens
  // Click Detect
  // Verify error message appears
  // Verify "Use This Registry" button is disabled
});

test('can cancel after validation failure', async ({ page }) => {
  // Trigger validation failure
  // Click Cancel in modal
  // Verify modal closes
  // Verify URL field still editable
  // Verify check button still enabled
  // Verify other controls still disabled
});

test('can retry after validation failure', async ({ page }) => {
  // Trigger validation failure
  // Click Cancel
  // Correct the URL
  // Click check button again
  // Complete successful validation
  // Verify controls are enabled
});
```

#### 5. `tests/e2e/registry-type-switching.spec.ts`
```typescript
test('switching from custom to built-in clears validation', async ({ page }) => {
  // Complete custom registry validation
  // Switch to Docker Hub
  // Verify URL shows docker.io (readonly)
  // Verify check button is hidden/disabled
  // Verify all controls enabled
  // Switch back to Custom
  // Verify URL field is empty and editable
  // Verify validation state is reset
  // Verify controls are disabled
});

test('switching between built-in registries works normally', async ({ page }) => {
  // Select GHCR
  // Verify URL shows ghcr.io
  // Switch to Quay
  // Verify URL shows quay.io
  // Verify all controls remain enabled
  // Verify check button remains hidden/disabled
});

test('owner/image values persist when switching built-in registries', async ({ page }) => {
  // Select GHCR
  // Enter owner "microsoft"
  // Enter image "vscode"
  // Switch to Docker Hub
  // Verify owner still shows "microsoft"
  // Verify image still shows "vscode"
});

test('owner/image values cleared when switching to custom', async ({ page }) => {
  // Select GHCR
  // Enter owner "microsoft"
  // Enter image "vscode"
  // Switch to Custom Registry
  // Verify owner field is empty
  // Verify image field is empty
});
```

### Unit Test Additions

#### `frontend/src/routes/+page.spec.ts` (create if doesn't exist)
```typescript
describe('getRegistryUrlDisplay', () => {
  it('returns custom URL when set', () => {});
  it('returns empty string when custom not set', () => {});
  it('returns ghcr.io for GHCR', () => {});
  it('returns docker.io for Docker Hub', () => {});
  it('returns quay.io for Quay', () => {});
  it('returns gcr.io for GCR', () => {});
});

describe('handleRegistryUrlChange', () => {
  it('sets customRegistryUrl', () => {});
  it('sets customRegistryValidated to false', () => {});
});

describe('handleRegistryChange', () => {
  it('resets customRegistryValidated', () => {});
  it('clears customRegistryUrl', () => {});
  it('clears owner when switching to custom', () => {});
  it('clears image when switching to custom', () => {});
});
```

## Implementation Checklist

### Code Changes
- [ ] Add `customRegistryValidated` state variable
- [ ] Add `showDetectionModal` state variable
- [ ] Implement `getRegistryUrlDisplay()` function
- [ ] Implement `handleRegistryUrlChange()` function
- [ ] Implement `handleCheckRegistry()` function
- [ ] Implement `handleDetectionSuccess()` function
- [ ] Implement `handleDetectionCancel()` function
- [ ] Update `handleRegistryChange()` to reset validation and clear owner/image
- [ ] Add registry URL input field to UI
- [ ] Add check button to UI
- [ ] Add conditional disabled attributes to Browse button
- [ ] Add conditional disabled attributes to Search button
- [ ] Add conditional disabled attributes to Owner input
- [ ] Add conditional disabled attributes to Image input
- [ ] Add validation modal markup
- [ ] Import `CustomRegistryInput` component
- [ ] Import `registryDetectionService`

### Testing
- [ ] Create `tests/e2e/registry-url-display-builtin.spec.ts`
- [ ] Create `tests/e2e/custom-registry-validation-flow.spec.ts`
- [ ] Create `tests/e2e/custom-registry-url-change.spec.ts`
- [ ] Create `tests/e2e/custom-registry-validation-failure.spec.ts`
- [ ] Create `tests/e2e/registry-type-switching.spec.ts`
- [ ] Run all existing tests to ensure no regressions
- [ ] Run new tests to verify all scenarios pass

### Documentation
- [ ] Update README with new UI workflow screenshots/description
- [ ] Update CHANGELOG with UI enhancement entry
- [ ] Add inline code comments for state management logic

## Dependencies & Integration Points

### Component Dependencies
- Reuses: `CustomRegistryInput.svelte` (no changes)
- Reuses: `registryDetectionService` (no changes)
- Modifies: `+page.svelte` (main implementation)
- No changes needed: `RegistrySelector.svelte`, `BrowseImagesDialog.svelte`

### State Management
- Local component state only (Svelte 5 runes)
- No new stores required
- Existing `browseSession` store usage unchanged

### Backend API
- No backend changes required
- Uses existing `/api/registries/detect` endpoint

## Success Metrics

- All 5 new e2e test files pass with 100% success rate
- All existing e2e tests continue to pass
- Manual testing confirms smooth UX for all scenarios
- No console errors or warnings during usage
- Validation state transitions are immediate and clear

## Open Questions

### Resolved
- ✅ Should we clear owner/image when URL changes? **YES** - prevents confusion
- ✅ Should URL field be readonly after validation? **YES** - prevents accidental changes
- ✅ Should we reuse CustomRegistryInput component? **YES** - DRY principle

### For Future Consideration
- Should we remember last validated custom URL in localStorage?
- Should we add a "clear" button to reset custom URL?
- Should we show a visual indicator (green border) when validated?
- Should validation happen automatically after user stops typing?

---

## Review Checklist
- [x] All requirements are user-focused and testable
- [x] Scenarios cover all user workflows
- [x] Test requirements are comprehensive
- [x] UI specifications are detailed and implementable
- [x] Acceptance criteria are clear and measurable
- [x] No implementation details leak into requirements
- [x] Success metrics defined
- [x] Dependencies identified
