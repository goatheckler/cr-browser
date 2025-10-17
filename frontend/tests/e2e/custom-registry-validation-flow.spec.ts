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
    
    await expect(page.getByText('✓ OCI Registry Detected')).toBeVisible({ timeout: 10000 });
    await expect(page.getByText('https://docker.redpanda.com')).toBeVisible();
    
    const closeButton = page.getByRole('button', { name: 'Close' });
    await closeButton.click();
    
    await expect(page.locator('role=dialog')).not.toBeVisible();
    
    await expect(urlInput).toHaveValue(/https:\/\/docker\.redpanda\.com/);
    await expect(urlInput).not.toHaveAttribute('readonly');
    
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

  test('can close validation result with Escape', async ({ page }) => {
    const urlInput = page.locator('#registry-url-input');
    await urlInput.fill('docker.redpanda.com');
    
    const checkButton = page.locator('[data-testid="check-registry-button"]');
    await checkButton.click();
    
    await expect(page.getByText('✓ OCI Registry Detected')).toBeVisible({ timeout: 10000 });
    
    await page.keyboard.press('Escape');
    
    await expect(page.locator('role=dialog')).not.toBeVisible();
    
    await expect(checkButton).toBeDisabled();
    
    const browseButton = page.locator('button', { hasText: 'Browse Images' });
    await expect(browseButton).toBeEnabled();
    
    const searchButton = page.locator('button', { hasText: 'Search' });
    await expect(searchButton).toBeEnabled();
  });
});
