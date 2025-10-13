import { test, expect } from '@playwright/test';

test('shows error for unknown Docker Hub owner', async ({ page }) => {
  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  
  await page.getByRole('combobox').selectOption('dockerhub');
  await page.getByRole('button', { name: 'Browse Images' }).click();
  
  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  
  await dialog.getByPlaceholder(/owner|namespace/i).fill('thisownerdef1nitelydoesnotex1st');
  await dialog.getByRole('button', { name: /load|browse/i }).click();
  
  await expect(page.getByText(/not found|no.*images|error/i)).toBeVisible({ timeout: 5000 });
});

test('shows error for invalid GHCR token', async ({ page }) => {
  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  
  await page.evaluate(() => {
    localStorage.setItem('cr-browser:ghcr:pat', JSON.stringify({ 
      tokenValue: 'ghp_invalidtoken123456789012345678901234', 
      validatedAt: new Date().toISOString() 
    }));
  });
  
  await page.reload();
  await page.getByText(/API healthy/).waitFor();
  
  await page.getByRole('combobox').selectOption('ghcr');
  await page.getByRole('button', { name: 'Browse Images' }).click();
  
  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  
  await dialog.getByPlaceholder(/owner/i).fill('testorg');
  await dialog.getByRole('button', { name: /load|browse/i }).click();
  
  await expect(page.getByText(/auth.*failed|invalid.*token|unauthorized/i)).toBeVisible({ timeout: 5000 });
});

test('shows retry button on error', async ({ page }) => {
  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  
  await page.route('**/v2/repositories/**', route => route.abort());
  
  await page.getByRole('combobox').selectOption('dockerhub');
  await page.getByRole('button', { name: 'Browse Images' }).click();
  
  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  
  await dialog.getByPlaceholder(/owner|namespace/i).fill('library');
  await dialog.getByRole('button', { name: /load|browse/i }).click();
  
  await expect(page.getByText(/error|failed/i)).toBeVisible({ timeout: 5000 });
  
  const retryButton = page.getByRole('button', { name: /retry|try again/i });
  await expect(retryButton).toBeVisible();
});

test('retry button clears error and reloads', async ({ page }) => {
  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  
  let requestCount = 0;
  await page.route('**/v2/repositories/**', route => {
    requestCount++;
    if (requestCount === 1) {
      route.abort();
    } else {
      route.continue();
    }
  });
  
  await page.getByRole('combobox').selectOption('dockerhub');
  await page.getByRole('button', { name: 'Browse Images' }).click();
  
  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  
  await dialog.getByPlaceholder(/owner|namespace/i).fill('library');
  await dialog.getByRole('button', { name: /load|browse/i }).click();
  
  await expect(page.getByText(/error|failed/i)).toBeVisible({ timeout: 5000 });
  
  const retryButton = page.getByRole('button', { name: /retry|try again/i });
  await retryButton.click();
  
  const imageList = page.locator('[data-testid="image-list"]');
  await expect(imageList).toBeVisible({ timeout: 10000 });
  
  expect(requestCount).toBe(2);
});

test('shows network error for failed requests', async ({ page }) => {
  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  
  await page.route('**/v2/repositories/**', route => route.abort('failed'));
  
  await page.getByRole('combobox').selectOption('dockerhub');
  await page.getByRole('button', { name: 'Browse Images' }).click();
  
  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  
  await dialog.getByPlaceholder(/owner|namespace/i).fill('library');
  await dialog.getByRole('button', { name: /load|browse/i }).click();
  
  await expect(page.getByText(/network.*error|connection.*failed/i)).toBeVisible({ timeout: 5000 });
});
