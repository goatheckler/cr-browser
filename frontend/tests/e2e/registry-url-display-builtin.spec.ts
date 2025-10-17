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
    await expect(checkButton).toBeDisabled();
  });

  test('Docker Hub displays correct URL in readonly field', async ({ page }) => {
    await page.selectOption('[data-testid="registry-selector"]', 'dockerhub');
    const urlInput = page.locator('#registry-url-input');
    await expect(urlInput).toHaveValue('docker.io');
    await expect(urlInput).toHaveAttribute('readonly');
    const checkButton = page.locator('[data-testid="check-registry-button"]');
    await expect(checkButton).toBeDisabled();
  });

  test('Quay displays correct URL in readonly field', async ({ page }) => {
    await page.selectOption('[data-testid="registry-selector"]', 'quay');
    const urlInput = page.locator('#registry-url-input');
    await expect(urlInput).toHaveValue('quay.io');
    await expect(urlInput).toHaveAttribute('readonly');
    const checkButton = page.locator('[data-testid="check-registry-button"]');
    await expect(checkButton).toBeDisabled();
  });

  test('GCR displays correct URL in readonly field', async ({ page }) => {
    await page.selectOption('[data-testid="registry-selector"]', 'gcr');
    const urlInput = page.locator('#registry-url-input');
    await expect(urlInput).toHaveValue('gcr.io');
    await expect(urlInput).toHaveAttribute('readonly');
    const checkButton = page.locator('[data-testid="check-registry-button"]');
    await expect(checkButton).toBeDisabled();
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
