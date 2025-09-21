import { test, expect } from '@playwright/test';

test('searching for an image shows tag rows', async ({ page }) => {
  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  // Use a well-known public image with many tags
  await page.getByPlaceholder('owner').fill('stefanprodan');
  await page.getByPlaceholder('image').fill('podinfo');
  await page.getByRole('button', { name: 'Search' }).click();
  await expect(page.getByText(/Found/)).toBeVisible();
  // Expect at least one tag row
  const rows = page.locator('.ag-center-cols-container .ag-row');
  await expect(rows.first()).toBeVisible();
  // At least 1 row
  await expect(rows.count()).resolves.toBeGreaterThan(0);
});
