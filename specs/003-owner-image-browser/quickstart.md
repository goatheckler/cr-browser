# Quickstart Guide: Owner Image Browser

**Feature**: 003-owner-image-browser  
**Audience**: End users and QA testers  
**Purpose**: Step-by-step guide to using the owner image browser feature

---

## What is the Owner Image Browser?

The Owner Image Browser lets you discover all container images published by a specific owner or organization across different registries (Docker Hub, Quay.io, GHCR, GCR) without knowing the exact image names in advance.

---

## Prerequisites

- Access to the Container Registry Browser application
- (For GHCR only) A GitHub Personal Access Token with `read:packages` scope

---

## Quick Start

### Scenario 1: Browse Docker Hub Images

**Goal**: Find all official Docker images published by the "library" namespace

1. **Open the application** in your browser
2. **Select registry**: Choose "Docker Hub" from the registry dropdown
3. **Enter owner**: Type `library` in the Owner field
4. **Click "Browse Images"** button
5. **View results**: A dialog shows all images published by "library"
   - You should see: `nginx`, `ubuntu`, `alpine`, `postgres`, etc.
   - Each row shows: Owner | Image Name
6. **Optional - Filter**: Type in the search box to filter results (e.g., "nginx")
7. **Select an image**: Click on a row (e.g., "nginx")
8. **Observe**: The dialog closes and the main form is populated:
   - Owner: `library`
   - Image: `nginx`
   - Tags list automatically loads

**Expected Result**: You can now view and copy tags for the selected image

---

### Scenario 2: Browse Quay.io Images

**Goal**: Find images published by the CoreOS organization on Quay.io

1. **Select registry**: Choose "Quay.io" from the registry dropdown
2. **Enter owner**: Type `coreos` in the Owner field
3. **Click "Browse Images"** button
4. **View results**: A dialog shows all public images from the "coreos" namespace
   - You should see: `etcd`, `flannel`, `clair`, etc.
5. **Select an image**: Click on any image (e.g., "etcd")
6. **Observe**: Main form populated with `coreos/etcd`, tags loading

**Expected Result**: Browsing works without authentication for public Quay.io repositories

---

### Scenario 3: Browse GHCR Images (Requires GitHub PAT)

**Goal**: Find container packages published by a GitHub user

#### Step 1: Generate GitHub Personal Access Token (First Time Only)

1. **Navigate to**: https://github.com/settings/tokens/new
2. **Token name**: Enter a descriptive name (e.g., "Container Browser Read Access")
3. **Expiration**: Choose your preferred expiration (recommend: 90 days)
4. **Scopes**: Check **only** `read:packages`
5. **Click "Generate token"**
6. **Copy the token**: It starts with `ghp_` (e.g., `ghp_abc123...`)
   - ⚠️ Save it securely - you won't see it again!

#### Step 2: Browse GHCR Images

1. **Select registry**: Choose "GHCR" from the registry dropdown
2. **Enter owner**: Type a GitHub username (e.g., `microsoft`)
3. **Click "Browse Images"** button
4. **Authentication prompt**: A dialog appears asking for GitHub PAT
5. **Paste your token**: Enter the token you generated (starts with `ghp_`)
6. **Click "Authenticate"**
7. **View results**: A list of container packages appears
   - Each row shows: Owner | Package Name
8. **Select a package**: Click on any package
9. **Observe**: Main form populated, tags loading

**Expected Result**: 
- Token is saved in browser for future use (no need to re-enter)
- You can browse GHCR packages for any GitHub user/org
- If token is invalid, you'll see an error message

#### Managing Your GHCR Token

**To clear/revoke your token**:
1. Click "Manage Token" in the GHCR auth dialog
2. Click "Clear Stored Token"
3. Next browse will prompt for a new token

**If you see "Invalid Token" error**:
1. Your token may have expired or been revoked
2. Generate a new token (see Step 1 above)
3. The app will automatically prompt you to re-enter it

---

### Scenario 4: GCR Project ID (Special Handling)

**Goal**: Understand GCR's project-based model

1. **Select registry**: Choose "GCR" from the registry dropdown
2. **Observe**: The "Owner" field label changes to "Project ID"
3. **Read help text**: Below the field, you'll see:
   > "GCR uses GCP Project IDs instead of usernames. Example: google-containers"
4. **Enter project ID**: Type `google-containers`
5. **Note**: Browse Images button may be disabled or show "Not Supported in MVP"
6. **Manual entry**: You can still manually enter an image name and view tags
   - Example: `google-containers` / `pause`

**Expected Result**: Clear guidance that GCR requires a project ID, not a username

---

## User Scenarios Validation

### Scenario 5: Pagination (100+ Images)

**Setup**: Use Docker Hub with `library` namespace (178+ images)

1. Select "Docker Hub", owner: `library`
2. Click "Browse Images"
3. **Observe**: Initial page shows 25 images
4. **Scroll to bottom**: More images load automatically
5. **Continue scrolling**: Pages continue loading until all 178 images shown
6. **Check scroll position**: Smooth loading without jumps

**Expected Result**: All 178+ images can be browsed via infinite scroll

---

### Scenario 6: Error Handling - Unknown Owner

1. Select "Docker Hub", owner: `this-namespace-does-not-exist-12345`
2. Click "Browse Images"
3. **Observe**: Error message appears:
   > "No images found for this owner" or "Namespace not found"
4. **Click "Retry"** (if available): Same error appears
5. **Change owner** to `library`: Browse works normally

**Expected Result**: Clear error messages for non-existent owners

---

### Scenario 7: Error Handling - Network Failure

**Setup**: Disconnect network or use browser DevTools to simulate offline

1. Disconnect network
2. Select "Docker Hub", owner: `library`
3. Click "Browse Images"
4. **Observe**: Error message appears:
   > "Network error. Please check your connection and try again."
5. **Reconnect network**
6. **Click "Retry"**: Browse succeeds

**Expected Result**: Network errors are handled gracefully with retry option

---

### Scenario 8: Filter Within Browse Results

1. Browse Docker Hub `library` namespace
2. Wait for results to load (178 images)
3. **Type in filter box**: "python"
4. **Observe**: List filters to show only images containing "python"
   - Should see: `python`, `pypy`, etc.
5. **Clear filter**: List shows all images again
6. **Type "postgres"**: Only postgres-related images shown

**Expected Result**: Client-side filtering works instantly without new API calls

---

## Common Issues & Solutions

### Issue: "Invalid GitHub Token" for GHCR

**Symptoms**: Error when browsing GHCR packages

**Solutions**:
1. Verify token starts with `ghp_`
2. Ensure token has `read:packages` scope
3. Check token hasn't expired on GitHub
4. Generate a new token if needed
5. Clear browser cache and try again

---

### Issue: GCR Browse Not Working

**Symptoms**: Browse button disabled or shows error

**Expected Behavior**: 
- GCR browse is **not implemented in MVP**
- You must manually enter project ID and image name
- This is intentional due to GCR's authentication complexity

**Workaround**: Enter known project IDs manually (e.g., `google-containers`)

---

### Issue: No Images Shown for Valid Owner

**Possible Causes**:
1. Owner has no published images
2. All images are private (Docker Hub/Quay/GHCR)
3. Rate limiting (rare)

**Solutions**:
1. Verify owner name spelling
2. Try a known public owner (e.g., `library` on Docker Hub)
3. For GHCR: Ensure you're authenticated with a valid PAT
4. Wait a few minutes if rate limited

---

### Issue: Pagination Not Loading

**Symptoms**: Scroll to bottom but no more images load

**Solutions**:
1. Check if you've reached the end (no more images available)
2. Look for error message at bottom of list
3. Check browser console for network errors
4. Refresh page and try again

---

## Browser Compatibility

**Tested and Supported**:
- Chrome/Edge 90+
- Firefox 88+
- Safari 14+

**Required Features**:
- localStorage (for GHCR token storage)
- Fetch API
- ES6+ JavaScript

**HTTPS Requirement**: 
- GHCR token storage requires HTTPS (except localhost)
- Application should warn if accessed over HTTP

---

## Security Notes

### GitHub PAT Storage

- ✅ Stored only in your browser (localStorage)
- ✅ Never sent to the cr-browser backend server
- ✅ Only sent to `api.github.com`
- ✅ Can be cleared at any time
- ⚠️ Not shared across browsers/devices (store separately)

### Best Practices

1. **Use minimal scopes**: Only grant `read:packages` to the PAT
2. **Set expiration**: Use 90-day or shorter expiration
3. **Revoke when done**: Delete token on GitHub if no longer needed
4. **Don't share tokens**: Never share your PAT with others
5. **HTTPS only**: Always access the app over HTTPS (not HTTP)

---

## Testing Checklist

Use this checklist to validate the feature:

- [ ] Browse Docker Hub public images (library namespace)
- [ ] Browse Quay.io public images (coreos namespace)
- [ ] Generate GitHub PAT with correct scope
- [ ] Browse GHCR with PAT authentication
- [ ] Select image from browse list → main form populates
- [ ] Tags automatically load for selected image
- [ ] Pagination works for 100+ images
- [ ] Client-side filter searches within results
- [ ] Error shown for non-existent owner
- [ ] Error shown for invalid GHCR token
- [ ] Network error handling with retry
- [ ] GCR shows "Project ID" label and help text
- [ ] Token persists across page refreshes (GHCR)
- [ ] Can clear stored GHCR token
- [ ] Close dialog without selection → no change to main form

---

## Success Criteria

✅ **Feature is successful when**:

1. Users can discover images without knowing exact names
2. Works for Docker Hub and Quay.io without authentication
3. GHCR authentication flow is clear and works reliably
4. GCR clearly explains project-based model
5. Selection from browse list automatically loads tags
6. Pagination handles large result sets smoothly
7. Error messages are clear and actionable
8. GitHub PAT is stored securely (browser-only, HTTPS)

---
