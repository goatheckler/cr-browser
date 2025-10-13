import { test, expect } from '@playwright/test';

test('GHCR lists packages with valid authentication', async ({ page }) => {
  test.skip(!process.env.GITHUB_PAT, 'Requires real GITHUB_PAT environment variable');
  const validToken = process.env.GITHUB_PAT || 'ghp_' + 'a'.repeat(36);
  
  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  
  await page.evaluate((token) => {
    localStorage.setItem('cr-browser:ghcr:pat', JSON.stringify({ 
      tokenValue: token, 
      validatedAt: new Date().toISOString() 
    }));
  }, validToken);
  
  await page.reload();
  await page.getByText(/API healthy/).waitFor();
  
  await page.getByRole('combobox').selectOption('ghcr');
  await page.getByRole('button', { name: 'Browse Images' }).click();
  
  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  
  await dialog.getByPlaceholder(/owner/i).fill('testorg');
  await dialog.getByRole('button', { name: /load|browse/i }).click();
  
  await expect(dialog.getByText('Loading images...')).toBeVisible();
  
  const imageList = page.locator('[data-testid="image-list"]');
  await expect(imageList).toBeVisible({ timeout: 10000 });
});

test('GHCR shows package metadata', async ({ page }) => {
  test.skip(!process.env.GITHUB_PAT, 'Requires real GITHUB_PAT environment variable');
  const validToken = process.env.GITHUB_PAT || 'ghp_' + 'a'.repeat(36);
  
  await page.goto('/');
  
  await page.evaluate((token) => {
    localStorage.setItem('cr-browser:ghcr:pat', JSON.stringify({ 
      tokenValue: token, 
      validatedAt: new Date().toISOString() 
    }));
  }, validToken);
  
  await page.reload();
  await page.getByText(/API healthy/).waitFor();
  
  await page.getByRole('combobox').selectOption('ghcr');
  await page.getByRole('button', { name: 'Browse Images' }).click();
  
  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  
  await dialog.getByPlaceholder(/owner/i).fill('testorg');
  await dialog.getByRole('button', { name: /load|browse/i }).click();
  
  const imageList = page.locator('[data-testid="image-list"]');
  await expect(imageList).toBeVisible({ timeout: 10000 });
  
  await expect(page.getByText(/visibility.*public|private/i)).toBeVisible();
});
