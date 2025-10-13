import { test, expect } from '@playwright/test';

test('GCR shows "Project ID" label instead of "Owner"', async ({ page }) => {
  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  
  await page.getByRole('combobox').selectOption('gcr');
  
  await expect(page.getByText(/project id/i)).toBeVisible();
  await expect(page.getByText(/^owner$/i)).not.toBeVisible();
});

test('GCR shows help text for project ID', async ({ page }) => {
  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  
  await page.getByRole('combobox').selectOption('gcr');
  
  await expect(page.getByText(/project.*id.*format|lowercase.*alphanumeric/i)).toBeVisible();
});

test('GCR browse button is visible', async ({ page }) => {
  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  
  await page.getByRole('combobox').selectOption('gcr');
  
  const browseButton = page.getByRole('button', { name: 'Browse Images' });
  await expect(browseButton).toBeVisible();
});
