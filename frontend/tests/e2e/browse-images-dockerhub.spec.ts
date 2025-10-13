import { test, expect } from '@playwright/test';

test('browsing Docker Hub library namespace shows images', async ({ page }) => {
  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  
  await page.getByRole('combobox').selectOption('dockerhub');
  
  const browseButton = page.getByRole('button', { name: 'Browse Images' });
  await expect(browseButton).toBeVisible();
  await browseButton.click();
  
  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  
  await dialog.getByPlaceholder(/owner|namespace/i).fill('library');
  await dialog.getByRole('button', { name: /load|browse/i }).click();
  
  await expect(page.getByText(/loading/i)).toBeVisible();
  
  const imageList = page.locator('[data-testid="image-list"]');
  await expect(imageList).toBeVisible({ timeout: 10000 });
  
  const rows = imageList.locator('tbody tr');
  await expect(rows.count()).resolves.toBeGreaterThan(5);
  
  await expect(page.getByText(/nginx/i)).toBeVisible();
});

test('filtering Docker Hub images works', async ({ page }) => {
  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  
  await page.getByRole('combobox').selectOption('dockerhub');
  await page.getByRole('button', { name: 'Browse Images' }).click();
  
  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  
  await dialog.getByPlaceholder(/owner|namespace/i).fill('library');
  await dialog.getByRole('button', { name: /load|browse/i }).click();
  
  const imageList = page.locator('[data-testid="image-list"]');
  await expect(imageList).toBeVisible({ timeout: 10000 });
  
  const filterInput = page.getByPlaceholder(/filter|search/i);
  await filterInput.fill('nginx');
  
  const rows = imageList.locator('tbody tr');
  const count = await rows.count();
  expect(count).toBeGreaterThanOrEqual(1);
  expect(count).toBeLessThan(10);
  
  await expect(page.getByText(/nginx/i).first()).toBeVisible();
});
