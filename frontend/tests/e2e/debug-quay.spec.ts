import { test, expect } from '@playwright/test';

test('debug Quay loading state', async ({ page }) => {
  // Capture console logs
  const consoleLogs: string[] = [];
  page.on('console', msg => {
    consoleLogs.push(`${msg.type()}: ${msg.text()}`);
  });

  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  
  await page.getByRole('combobox').selectOption('quay');
  await page.getByRole('button', { name: 'Browse Images' }).click();
  
  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  
  await dialog.getByPlaceholder(/owner|namespace/i).fill('coreos');
  
  // Before clicking browse
  console.log('About to click browse button');
  await dialog.getByRole('button', { name: /load|browse/i }).click();
  
  // Wait a bit for any logs
  await page.waitForTimeout(2000);
  
  // Check what's visible
  const loadingVisible = await dialog.getByText('Loading images...').isVisible();
  const errorVisible = await dialog.getByText(/Error:/i).isVisible().catch(() => false);
  
  console.log('Loading visible:', loadingVisible);
  console.log('Error visible:', errorVisible);
  console.log('Console logs:', consoleLogs.join('\n'));
  
  // Check for image list
  const imageList = page.locator('[data-testid="image-list"]');
  const imageListVisible = await imageList.isVisible().catch(() => false);
  console.log('Image list visible:', imageListVisible);
  
  // Get the dialog HTML
  const dialogHtml = await dialog.innerHTML();
  console.log('Dialog HTML length:', dialogHtml.length);
});
