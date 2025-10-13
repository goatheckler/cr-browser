import { test, expect } from '@playwright/test';

test('Docker Hub pagination loads next page on scroll', async ({ page }) => {
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
  
  const initialRows = imageList.locator('tbody tr');
  const initialCount = await initialRows.count();
  expect(initialCount).toBeGreaterThan(10);
  
  const scrollableContainer = page.locator('[data-testid="scrollable-container"]');
  await scrollableContainer.evaluate(el => el.scrollTop = el.scrollHeight);
  
  await page.waitForTimeout(3000);
  
  const updatedRows = imageList.locator('tbody tr');
  const updatedCount = await updatedRows.count();
  
  expect(updatedCount).toBeGreaterThan(initialCount);
});

test('pagination shows loading indicator', async ({ page }) => {
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
  
  const scrollableContainer = page.locator('[data-testid="scrollable-container"]');
  await scrollableContainer.evaluate(el => el.scrollTop = el.scrollHeight);
  
  await expect(page.getByText(/loading.*more|loading.*/i)).toBeVisible();
});

test('pagination maintains scroll position', async ({ page }) => {
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
  
  const midRow = imageList.locator('tbody tr').nth(10);
  await midRow.scrollIntoViewIfNeeded();
  
  const scrollPosBefore = await imageList.evaluate(el => el.scrollTop);
  
  await page.waitForTimeout(500);
  
  const scrollPosAfter = await imageList.evaluate(el => el.scrollTop);
  
  expect(Math.abs(scrollPosAfter - scrollPosBefore)).toBeLessThan(50);
});
