import { test, expect } from '@playwright/test';

test('browsing Quay.io coreos namespace shows images', async ({ page }) => {
  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  
  await page.getByRole('combobox').selectOption('quay');
  
  const browseButton = page.getByRole('button', { name: 'Browse Images' });
  await expect(browseButton).toBeVisible();
  await browseButton.click();
  
  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  
  await dialog.getByPlaceholder(/owner|namespace/i).fill('coreos');
  await dialog.getByRole('button', { name: /load|browse/i }).click();
  
  await expect(dialog.getByText('Loading images...')).toBeVisible();
  
  const imageList = page.locator('[data-testid="image-list"]');
  await expect(imageList).toBeVisible({ timeout: 10000 });
  
	const rows = imageList.locator('tbody tr');
	await expect(rows.count()).resolves.toBeGreaterThan(0);
	
	await expect(page.getByText(/etcd|flannel/i).first()).toBeVisible();
});

test('Quay.io shows public repository indicator', async ({ page }) => {
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
	
	await expect(page.getByText(/public|private/i).first()).toBeVisible();
});
