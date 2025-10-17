# Tasks: Custom Registry UI Enhancement

**Feature**: 005-custom-registry-ui-enhancement  
**Created**: 2025-10-16  
**Status**: Ready to Start

## Task Checklist

### Phase 1: State Management ⏳
- [ ] 1.1: Add `customRegistryValidated` and `showDetectionModal` state variables
- [ ] 1.2: Add helper functions (getRegistryUrlDisplay, handleRegistryUrlChange, handleCheckRegistry, handleDetectionSuccess, handleDetectionCancel)
- [ ] 1.3: Update handleRegistryChange to reset validation and clear owner/image

### Phase 2: UI Components ⏳
- [ ] 2.1: Add imports for CustomRegistryInput and registryDetectionService
- [ ] 2.2: Add registry URL input field with conditional readonly
- [ ] 2.3: Add check button with SVG checkmark icon
- [ ] 2.4: Update owner input with conditional disable
- [ ] 2.5: Update Browse Images button with conditional disable
- [ ] 2.6: Update image input with conditional disable
- [ ] 2.7: Update Search button with conditional disable
- [ ] 2.8: Add detection modal markup

### Phase 3: Testing ⏳
- [ ] 3.1: Create `tests/e2e/registry-url-display-builtin.spec.ts`
- [ ] 3.2: Create `tests/e2e/custom-registry-validation-flow.spec.ts`
- [ ] 3.3: Create `tests/e2e/custom-registry-url-change.spec.ts`
- [ ] 3.4: Create `tests/e2e/custom-registry-validation-failure.spec.ts`
- [ ] 3.5: Create `tests/e2e/registry-type-switching.spec.ts`
- [ ] 3.6: Run existing tests - verify no regressions
- [ ] 3.7: Run new E2E tests - verify all pass

### Phase 4: Documentation ⏳
- [ ] 4.1: Update CHANGELOG.md with new features
- [ ] 4.2: Update AGENTS.md with recent changes

### Phase 5: Manual Testing ⏳
- [ ] Verify built-in registries show correct URLs (readonly)
- [ ] Verify custom registry starts with controls disabled
- [ ] Verify check button enables when URL entered
- [ ] Verify validation modal opens on check button click
- [ ] Verify successful validation enables all controls
- [ ] Verify failed validation shows error and keeps controls disabled
- [ ] Verify editing URL after validation resets state
- [ ] Verify owner/image fields cleared when URL changes
- [ ] Verify switching between registry types works correctly
- [ ] Verify visual styling is consistent
- [ ] Verify keyboard navigation works (Tab, Escape)
- [ ] Verify no console errors or warnings

## Progress Tracking

### Completed: 0/30 tasks (0%)
### Estimated Time Remaining: ~3.5 hours

## Notes

- All UI changes are in a single file: `frontend/src/routes/+page.svelte`
- Reuses existing `CustomRegistryInput.svelte` component (no changes)
- No backend changes required
- 5 new E2E test files with 25+ test cases
- Comprehensive coverage of all state transitions

## Blockers

None currently identified.

## Dependencies

- Existing CustomRegistryInput component working ✅
- Existing registryDetectionService working ✅
- Backend `/api/registries/detect` endpoint working ✅

## Success Criteria

- [ ] All existing tests pass
- [ ] All new tests pass
- [ ] Manual testing confirms smooth UX
- [ ] No console errors or warnings
- [ ] Code follows existing patterns and style
- [ ] Validation state transitions are immediate
- [ ] Disabled states are visually clear
