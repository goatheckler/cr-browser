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
    
    await expect(page.locator('role=dialog')).toBeVisible({ timeout: 10000 });
    await expect(page.getByText('✗ Registry Not Detected')).toBeVisible();
    await expect(page.getByText(/Error:/)).toBeVisible();
    
    const browseButton = page.locator('button', { hasText: 'Browse Images' });
    await expect(browseButton).toBeDisabled();
  });

  test('can cancel after validation failure', async ({ page }) => {
    const urlInput = page.locator('#registry-url-input');
    await urlInput.fill('not-a-registry.invalid');
    
    const checkButton = page.locator('[data-testid="check-registry-button"]');
    await checkButton.click();
    
    await expect(page.locator('role=dialog')).toBeVisible({ timeout: 10000 });
    await expect(page.getByText('✗ Registry Not Detected')).toBeVisible();
    
    const closeButton = page.getByRole('button', { name: 'Close' });
    await closeButton.click();
    
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
    
    await expect(page.locator('role=dialog')).toBeVisible({ timeout: 10000 });
    await expect(page.getByText('✗ Registry Not Detected')).toBeVisible();
    
    const closeButton = page.getByRole('button', { name: 'Close' });
    await closeButton.click();
    
    await urlInput.fill('docker.redpanda.com');
    await checkButton.click();
    
    await expect(page.locator('role=dialog')).toBeVisible({ timeout: 10000 });
    await expect(page.getByText('✓ OCI Registry Detected')).toBeVisible();
    
    const closeButton2 = page.getByRole('button', { name: 'Close' });
    await closeButton2.click();
    
    const browseButton = page.locator('button', { hasText: 'Browse Images' });
    await expect(browseButton).toBeEnabled();
  });
});
