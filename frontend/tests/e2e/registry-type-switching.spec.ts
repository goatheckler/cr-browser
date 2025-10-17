import { test, expect } from '@playwright/test';

test.describe('Registry Type Switching', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  async function validateCustomRegistry(page: any) {
    await page.selectOption('[data-testid="registry-selector"]', 'custom');
    
    const urlInput = page.locator('#registry-url-input');
    await urlInput.fill('docker.redpanda.com');
    
    const checkButton = page.locator('[data-testid="check-registry-button"]');
    await checkButton.click();
    
    await page.waitForTimeout(500);
  }

  test('switching from custom to built-in clears validation', async ({ page }) => {
    await validateCustomRegistry(page);
    
    await page.selectOption('[data-testid="registry-selector"]', 'dockerhub');
    
    const urlInput = page.locator('#registry-url-input');
    await expect(urlInput).toHaveValue('docker.io');
    await expect(urlInput).toHaveAttribute('readonly');
    
    const checkButton = page.locator('[data-testid="check-registry-button"]');
    await expect(checkButton).toBeDisabled();
    
    const browseButton = page.locator('button', { hasText: 'Browse Images' });
    await expect(browseButton).toBeEnabled();
    
    await page.selectOption('[data-testid="registry-selector"]', 'custom');
    
    await expect(urlInput).toHaveValue('');
    await expect(urlInput).not.toHaveAttribute('readonly');
    
    await expect(checkButton).toBeDisabled();
    await expect(browseButton).toBeDisabled();
  });

  test('switching between built-in registries works normally', async ({ page }) => {
    await page.selectOption('[data-testid="registry-selector"]', 'ghcr');
    
    const urlInput = page.locator('#registry-url-input');
    await expect(urlInput).toHaveValue('ghcr.io');
    
    await page.selectOption('[data-testid="registry-selector"]', 'quay');
    await expect(urlInput).toHaveValue('quay.io');
    
    const browseButton = page.locator('button', { hasText: 'Browse Images' });
    await expect(browseButton).toBeEnabled();
    
    const checkButton = page.locator('[data-testid="check-registry-button"]');
    await expect(checkButton).toBeDisabled();
  });

  test('owner/image values persist when switching built-in registries', async ({ page }) => {
    await page.selectOption('[data-testid="registry-selector"]', 'ghcr');
    
    const ownerInput = page.locator('#owner-input');
    await ownerInput.fill('microsoft');
    
    const imageInput = page.locator('#image-input');
    await imageInput.fill('vscode');
    
    await page.selectOption('[data-testid="registry-selector"]', 'dockerhub');
    
    await expect(ownerInput).toHaveValue('microsoft');
    await expect(imageInput).toHaveValue('vscode');
  });

  test('owner/image values cleared when switching to custom', async ({ page }) => {
    await page.selectOption('[data-testid="registry-selector"]', 'ghcr');
    
    const ownerInput = page.locator('#owner-input');
    await ownerInput.fill('microsoft');
    
    const imageInput = page.locator('#image-input');
    await imageInput.fill('vscode');
    
    await page.selectOption('[data-testid="registry-selector"]', 'custom');
    
    await expect(ownerInput).toHaveValue('');
    await expect(imageInput).toHaveValue('');
  });
});
