import { test, expect } from '@playwright/test';

test('invalid format error is displayed', async ({ page }) => {
  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  await page.getByPlaceholder('owner').fill('@@invalid@@');
  await page.getByPlaceholder('image').fill('test');
  await page.getByRole('button', { name: 'Search' }).click();
  await expect(page.getByText(/invalid|error/i)).toBeVisible();
});

test('not found error is displayed', async ({ page }) => {
  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  await page.getByPlaceholder('owner').fill('thisuserdoesnotexist999999');
  await page.getByPlaceholder('image').fill('thisrepodoesnotexist999999');
  await page.getByRole('button', { name: 'Search' }).click();
  await expect(page.getByText(/not found|404/i)).toBeVisible({ timeout: 15_000 });
});

test('enter key triggers search', async ({ page }) => {
  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  await page.getByPlaceholder('owner').fill('stefanprodan');
  await page.getByPlaceholder('image').fill('podinfo');
  await page.getByPlaceholder('image').press('Enter');
  await expect(page.getByText(/Found/)).toBeVisible();
  const rows = page.locator('.ag-center-cols-container .ag-row');
  await expect(rows.first()).toBeVisible();
});

test('copy button copies to clipboard', async ({ page }) => {
  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  await page.getByPlaceholder('owner').fill('stefanprodan');
  await page.getByPlaceholder('image').fill('podinfo');
  await page.getByRole('button', { name: 'Search' }).click();
  await expect(page.getByText(/Found/)).toBeVisible();
  
  const copyButton = page.locator('.ag-center-cols-container .ag-row').first().locator('button');
  await copyButton.click();
  
  await expect(page.locator('[role="status"]').first()).toHaveText('Copied to clipboard', { timeout: 500 });
});

test('empty or not found shows appropriate message', async ({ page }) => {
  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  await page.getByPlaceholder('owner').fill('microsoft');
  await page.getByPlaceholder('image').fill('emptyrepo999999');
  await page.getByRole('button', { name: 'Search' }).click();
  // Since we can't guarantee an empty repo exists, accept either "not found" or "no tags" messages
  await expect(page.getByText(/no.*tag|empty|0.*tag|not found|404/i)).toBeVisible({ timeout: 15_000 });
});
