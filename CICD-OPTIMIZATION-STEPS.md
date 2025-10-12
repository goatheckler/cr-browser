# CI/CD Optimization Implementation Checklist

**Related Plan**: See `CICD-OPTIMIZATION-PLAN.md` for full context  
**Created**: 2025-10-12  
**Status**: Phase 4 Complete - Ready for Testing

## Overview
This checklist guides the step-by-step implementation of replacing string-based Renovate detection with label-based PR event triggering.

---

## Phase 1: Prepare and Validate Current State

- [x] **Step 1.1**: Review current auto-release workflow
  - **Action**: Read `.github/workflows/auto-release.yml`
  - **Purpose**: Understand current implementation before changes
  - **What to check**: 
    - Current trigger (should be `on: push`)
    - Renovate detection logic (grep for "Co-authored-by")
    - All conditional steps using `check_renovate` output

- [x] **Step 1.2**: Review current Renovate configuration
  - **Action**: Read `renovate.json`
  - **Purpose**: Understand existing Renovate settings
  - **What to check**:
    - Current labels (if any)
    - Auto-merge settings
    - PR creation strategy

- [x] **Step 1.3**: Document baseline behavior
  - **Action**: Review recent workflow runs (if accessible)
  - **Purpose**: Understand success/failure patterns
  - **What to note**: Any recent failures or edge cases

---

## Phase 2: Update Renovate Configuration

- [x] **Step 2.1**: Add labels to renovate.json
  - **Action**: Edit `renovate.json` to add label configuration
  - **What to add**:
    ```json
    "labels": ["dependencies", "auto-release"]
    ```
  - **Location**: Add at root level of JSON object
  - **Why two labels**:
    - `dependencies`: Clear categorization of PR type
    - `auto-release`: Explicit trigger for automation

- [x] **Step 2.2**: Verify renovate.json syntax
  - **Action**: Validate JSON syntax
  - **Command**: Use JSON linter or parser
  - **Expected**: No syntax errors, valid JSON

- [x] **Step 2.3**: Review complete renovate.json
  - **Action**: Read full file to ensure no conflicts
  - **What to check**: 
    - Labels don't conflict with other settings
    - Auto-merge still enabled
    - Existing package rules preserved

---

## Phase 3: Update Auto-Release Workflow

- [x] **Step 3.1**: Change workflow trigger
  - **Action**: Edit `.github/workflows/auto-release.yml`
  - **Change FROM**:
    ```yaml
    on:
      push:
        branches:
          - main
    ```
  - **Change TO**:
    ```yaml
    on:
      pull_request:
        types: [closed]
        branches:
          - main
    ```
  - **Why**: PR events provide better metadata than push events

- [x] **Step 3.2**: Add job-level condition
  - **Action**: Add `if` condition to `auto-release` job
  - **Add after** `runs-on: self-hosted`:
    ```yaml
    if: |
      github.event.pull_request.merged == true &&
      contains(github.event.pull_request.labels.*.name, 'auto-release')
    ```
  - **Why**: Only run when PR is merged AND has auto-release label
  - **Note**: This replaces the step-level check_renovate logic

- [x] **Step 3.3**: Remove Renovate detection step
  - **Action**: Delete the "Check if Renovate commit" step entirely
  - **Lines to remove**: Steps that check commit message for "Co-authored-by"
  - **Why**: No longer needed with PR label detection

- [x] **Step 3.4**: Remove conditional step guards
  - **Action**: Remove all `if: steps.check_renovate.outputs.is_renovate == 'true'` lines
  - **Where**: All subsequent steps in the workflow
  - **Why**: Job-level condition already filters execution
  - **Steps affected**:
    - Checkout step
    - Get latest version step
    - Create tag and release step
    - Close failed Renovate PRs step

- [x] **Step 3.5**: Verify workflow syntax
  - **Action**: Validate YAML syntax
  - **Tools**: GitHub CLI, YAML linter, or online validator
  - **Expected**: No syntax errors, valid GitHub Actions workflow

---

## Phase 4: Update Documentation

- [x] **Step 4.1**: Update README.md CI/CD section
  - **Action**: Edit `README.md` around line 110-178 (CI/CD Pipeline section)
  - **Changes needed**:
    - Update auto-release description to mention PR-based trigger
    - Update flowchart if it shows push-based flow
    - Change "Triggers on push to main by renovate[bot]" to "Triggers on PR merge with auto-release label"
  - **Why**: Keep user-facing docs accurate

- [x] **Step 4.2**: Update CICD.md workflow documentation
  - **Action**: Edit `CICD.md` around line 171-177 (Auto Release section)
  - **Changes needed**:
    - Update trigger description
    - Update detection mechanism explanation
    - Mention label-based approach
  - **Example update**:
    ```markdown
    **Trigger**: Pull request closed (merged) with `auto-release` label
    - Checks PR was actually merged (not just closed)
    - Validates `auto-release` label present
    ```

- [x] **Step 4.3**: Update AGENTS.md if needed
  - **Action**: Check if `AGENTS.md` references the old workflow
  - **Changes**: Update any workflow command examples
  - **Why**: Keep development guidelines current

---

## Phase 5: Testing and Validation

- [ ] **Step 5.1**: Commit changes to a feature branch
  - **Action**: Create branch and commit all changes
  - **Branch name suggestion**: `cicd/label-based-auto-release`
  - **Commit message**: Follow conventional commits
    ```
    feat(cicd): replace string-based Renovate detection with PR labels
    
    - Update renovate.json to add 'dependencies' and 'auto-release' labels
    - Change auto-release.yml trigger from push to pull_request[closed]
    - Replace commit message parsing with PR label checking
    - Update documentation in README.md and CICD.md
    
    This improves reliability by using structured PR metadata instead of
    fragile string parsing of commit messages.
    ```

- [ ] **Step 5.2**: Create pull request
  - **Action**: Push branch and open PR
  - **PR title**: `feat(cicd): replace string-based Renovate detection with PR labels`
  - **PR description**: Link to `CICD-OPTIMIZATION-PLAN.md`

- [ ] **Step 5.3**: Verify test workflow runs
  - **Action**: Check that test.yml workflow runs on the PR
  - **Expected**: All tests pass
  - **Why**: Ensure no unintended changes to test execution

- [ ] **Step 5.4**: Manually test label detection
  - **Action**: Add `auto-release` label to the PR
  - **Expected**: Auto-release workflow should NOT trigger (PR not merged yet)
  - **Why**: Verify job condition checks merged status

- [ ] **Step 5.5**: Merge the PR
  - **Action**: Merge using squash merge
  - **Expected**: Auto-release workflow SHOULD trigger because:
    - PR is merged
    - PR has `auto-release` label
  - **Verify**:
    - New version tag created (e.g., v1.1.9)
    - GitHub release created
    - Build workflow triggered

- [ ] **Step 5.6**: Wait for next Renovate PR
  - **Action**: Monitor for next Renovate-created PR
  - **Expected**: PR should have `dependencies` and `auto-release` labels
  - **Verify**: Check PR labels in GitHub UI
  - **Timeline**: May take up to 6 hours (Renovate runs every 6 hours)

- [ ] **Step 5.7**: Validate Renovate PR auto-release
  - **Action**: Let Renovate PR auto-merge (if minor/patch)
  - **Expected**: Auto-release workflow triggers and creates new version
  - **Verify**:
    - Workflow run appears under PR in GitHub UI
    - New tag/release created
    - Build workflow triggered

---

## Phase 6: Cleanup and Finalization

- [ ] **Step 6.1**: Monitor for edge cases
  - **Action**: Watch next 2-3 Renovate PRs for any issues
  - **What to watch**:
    - Labels applied correctly
    - Auto-release triggers reliably
    - No false positives (workflow running on wrong PRs)

- [ ] **Step 6.2**: Update CICD-OPTIMIZATION-PLAN.md
  - **Action**: Update status from "Planning Phase" to "Completed"
  - **Add**: Implementation date and outcome notes

- [ ] **Step 6.3**: Update this checklist
  - **Action**: Mark status as "Completed"
  - **Add**: Final validation date and notes

---

## Rollback Procedure (If Needed)

If issues occur during testing or after deployment:

1. **Immediate revert of auto-release.yml**:
   ```bash
   git revert <commit-hash-of-workflow-change>
   git push origin main
   ```

2. **Revert renovate.json**:
   ```bash
   git revert <commit-hash-of-renovate-change>
   git push origin main
   ```

3. **Manual release creation** (if needed):
   - Use GitHub UI to create release manually
   - Follow existing version numbering

4. **Document issue**:
   - Create GitHub issue describing the problem
   - Reference this checklist and the optimization plan

---

## Notes and Observations

### Implementation Notes
- [ ] Record any deviations from plan here
- [ ] Note any unexpected behaviors
- [ ] Document any additional changes needed

### Performance Observations
- [ ] Workflow execution time comparison (before/after)
- [ ] Reduction in unnecessary workflow runs
- [ ] Any improvements in reliability

### Future Improvements
- [ ] Consider adding more labels for different release types (major/minor/patch)
- [ ] Consider auto-closing superseded Renovate PRs more aggressively
- [ ] Consider notifications on release failures

---

## Success Criteria

The implementation is considered successful when:
- ✅ Renovate PRs automatically get `auto-release` label
- ✅ Auto-release workflow only runs on merged PRs with the label
- ✅ No workflow runs on non-Renovate PRs
- ✅ No workflow runs on closed-but-not-merged PRs
- ✅ Version tagging and release creation still works
- ✅ Build workflow still triggers after releases
- ✅ Documentation accurately reflects new behavior

---

## Timeline Estimate

- Phase 1: 15 minutes (review and validate)
- Phase 2: 10 minutes (update renovate.json)
- Phase 3: 20 minutes (update workflow)
- Phase 4: 20 minutes (update docs)
- Phase 5: 2-8 hours (testing, waiting for Renovate)
- Phase 6: 1-2 days (monitoring)

**Total estimated time**: ~3-4 hours of active work + 2-3 days monitoring
