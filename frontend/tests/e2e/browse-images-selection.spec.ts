import { test, expect } from '@playwright/test';

test('selecting Docker Hub image populates main form', async ({ page }) => {
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
  
  const nginxRow = page.locator('tr[data-image-name="nginx"]');
  await nginxRow.click();
  
  await expect(dialog).not.toBeVisible({ timeout: 5000 });
  
  const ownerInput = page.getByPlaceholder('owner');
  const imageInput = page.getByPlaceholder('image');
  
  await expect(ownerInput).toHaveValue('library');
  await expect(imageInput).toHaveValue('nginx');
});

test('selecting image triggers tag load', async ({ page }) => {
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
  
  const nginxRow = page.locator('tr[data-image-name="nginx"]');
  await nginxRow.click();
  
  await expect(page.getByText(/loading.*tags|found.*tag/i)).toBeVisible({ timeout: 5000 });
  
  await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 10000 });
  
  const tagRows = page.locator('.ag-center-cols-container .ag-row');
  await expect(tagRows.count()).resolves.toBeGreaterThan(0);
});

test('selecting different registry type image updates form correctly', async ({ page }) => {
  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  
  await page.getByRole('combobox').selectOption('quay');
  await page.getByRole('button', { name: 'Browse Images' }).click();
  
  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  
  await dialog.getByPlaceholder(/owner|namespace/i).fill('coreos');
  await dialog.getByRole('button', { name: /load|browse/i }).click();
  
  const imageList = page.locator('[data-testid="image-list"]');
  await expect(imageList).toBeVisible({ timeout: 10000 });
  
  const etcdRow = page.getByText(/etcd/i).first();
  await etcdRow.click();
  
  await expect(dialog).not.toBeVisible({ timeout: 5000 });
  
  const registrySelect = page.getByRole('combobox');
  await expect(registrySelect).toHaveValue('quay');
  
  const ownerInput = page.getByPlaceholder('owner');
  await expect(ownerInput).toHaveValue('coreos');
});
