# Test Run Guide

**Last Updated**: 2025-10-06  
**Status**: Complete

## Overview

This document provides instructions for running all test suites locally for the ghcr-browser project.

## Test Suite Summary

**Total Tests**: 20
- Backend Unit Tests: 6
- Backend Integration Tests: 3
- Backend Contract Tests: 4
- Frontend E2E Tests: 7

## Prerequisites

### Backend Tests
- .NET 8 SDK installed
- No external dependencies (uses in-memory test server)

### Frontend E2E Tests
- Node 20 installed
- Docker and Docker Compose installed
- Backend and frontend services running

## Running Tests

### 1. Backend Unit Tests (6 tests)

**Location**: `backend/tests/unit/ValidationTests.cs`

**Command**:
```bash
cd backend
dotnet test tests/unit
```

**Expected Output**:
```
Passed! - Failed:  0, Passed:  6, Skipped:  0, Total:  6
```

**Tests Included**:
- 2 valid reference formats (validowner/valid-image, abc/repo)
- 4 invalid reference formats (spaces, uppercase, special chars, too long)

**Dependencies**: None (pure unit tests)

### 2. Backend Integration Tests (3 tests)

**Location**: `backend/tests/integration/`

**Command**:
```bash
cd backend
dotnet test tests/integration
```

**Expected Output**:
```
Passed! - Failed:  0, Passed:  3, Skipped:  0, Total:  3
```

**Tests Included**:
- InvalidFormatTests: Invalid owner returns 400
- NotFoundTests: Unknown repository returns 404
- SuccessTests: Valid repository returns 200 with tags

**Dependencies**: 
- Internet connection (makes real HTTP calls to ghcr.io)
- Uses test server (WebApplicationFactory)

### 3. Backend Contract Tests (4 tests)

**Location**: `backend/tests/contract/`

**Command**:
```bash
cd backend
dotnet test tests/contract
```

**Expected Output**:
```
Passed! - Failed:  0, Passed:  4, Skipped:  0, Total:  4
```

**Tests Included**:
- HealthTests: Health endpoint schema validation
- TagsContractTests: Tags endpoint success response schema
- ErrorContractTests (2 tests): InvalidFormat and NotFound error schemas

**Dependencies**: 
- Internet connection (makes real HTTP calls to ghcr.io)
- Uses test server (WebApplicationFactory)

### 4. All Backend Tests (13 tests total)

**Command**:
```bash
cd backend
dotnet test
```

**Expected Output**:
```
Passed! - Failed:  0, Passed: 13, Skipped:  0, Total: 13
```

### 5. Frontend E2E Tests (7 tests)

**Location**: `frontend/tests/e2e/`

**Command**:
```bash
# Terminal 1: Start services
docker-compose up -d

# Terminal 2: Run E2E tests
cd frontend
npm run test:e2e
```

**Expected Output**:
```
7 passed (Xms)
```

**Tests Included**:
- health.spec.ts: Health indicator displays on page load
- tags.spec.ts: Valid search displays tag results
- user-interactions.spec.ts (5 tests):
  - Invalid format error display
  - Not found error display
  - Enter key triggers search
  - Copy button functionality
  - Empty repository state

**Dependencies**:
- Docker Compose services running (backend on port 5214, frontend on port 5173)
- Playwright browsers installed (auto-installed on first run)
- Internet connection (searches real GHCR repositories)

**Cleanup**:
```bash
docker-compose down
```

### 6. Run All Tests

**Script**: `scripts/run-e2e.sh`

This script:
1. Starts Docker Compose services
2. Waits for health checks
3. Runs frontend E2E tests
4. Stops services

**Command**:
```bash
./scripts/run-e2e.sh
```

## Test Data

### Valid Repositories (used in tests)
- `stefanprodan/podinfo` - Used in E2E successful search tests
- `anchore/syft` - Used in manual validation (1094+ tags)

### Invalid Test Inputs
- `@@invalid@@` - Tests invalid owner format
- `validowner/invalid@image` - Tests invalid image format
- `nonexistentuser123456/nonexistentrepo123456` - Tests not found error

## Troubleshooting

### E2E Tests Fail with Connection Error
**Cause**: Backend/frontend services not running  
**Fix**: Run `docker-compose up -d` before tests

### Integration Tests Timeout
**Cause**: Network connectivity or GHCR rate limiting  
**Fix**: Check internet connection; wait a few minutes if rate limited

### Playwright Browsers Missing
**Cause**: First-time Playwright setup  
**Fix**: Run `npx playwright install` in frontend directory

## Manual Validation

Manual validation was performed using Playwright MCP browser tools to verify:
1. Health endpoint displays correctly
2. Successful searches populate grid with tags
3. Invalid format errors display appropriate messages
4. Not found errors display appropriate messages
5. Enter key triggers search
6. Copy button functionality works with clipboard confirmation
7. Empty state displays when no results

See `TEST-PLAN.md` Phase 5 for detailed validation results.

## CI/CD Integration

These test commands will be integrated into GitHub Actions workflows:

**test.yml workflow**:
- `test-backend`: Run `dotnet test` in backend directory
- `test-frontend`: Run linting and type checking
- `test-e2e`: Start services with docker-compose, run `npm run test:e2e`

See `CICD.md` for complete CI/CD strategy.

## Coverage Status

All 6 acceptance scenarios from spec.md are covered by automated tests:
1. ✅ Valid repository lookup displays tags
2. ✅ Invalid format shows error message
3. ✅ Non-existent repository shows not found error
4. ✅ Empty repository shows empty state
5. ✅ Enter key triggers search
6. ✅ Copy button copies full reference to clipboard
