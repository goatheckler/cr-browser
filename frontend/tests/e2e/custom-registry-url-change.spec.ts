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
    
    await expect(page.getByText('✓ OCI Registry Detected')).toBeVisible({ timeout: 10000 });
    
    const closeButton = page.getByRole('button', { name: 'Close' });
    await closeButton.click();
    
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
    
    await expect(page.getByText('✓ OCI Registry Detected')).toBeVisible({ timeout: 10000 });
    
    const closeButton = page.getByRole('button', { name: 'Close' });
    await closeButton.click();
    
    const browseButton = page.locator('button', { hasText: 'Browse Images' });
    await expect(browseButton).toBeEnabled();
    
    const searchButton = page.locator('button', { hasText: 'Search' });
    await expect(searchButton).toBeEnabled();
  });
});
