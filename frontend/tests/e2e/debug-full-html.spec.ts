import { test, expect } from '@playwright/test';

test('debug full table HTML', async ({ page }) => {
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
  
  const tableHTML = await imageList.innerHTML();
  const lines = tableHTML.split('\n');
  console.log('First 30 lines of table HTML:');
  lines.slice(0, 30).forEach((line, i) => console.log(`${i}: ${line}`));
});
