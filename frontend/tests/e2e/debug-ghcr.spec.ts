import { test, expect } from '@playwright/test';

test('debug GHCR loading', async ({ page }) => {
  test.skip(!process.env.GITHUB_PAT, 'Requires real GITHUB_PAT environment variable');
  const pageErrors: any[] = [];
  page.on('pageerror', err => {
    pageErrors.push({ message: err.message });
  });
  
  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  
  await page.getByRole('combobox').selectOption('ghcr');
  await page.getByRole('button', { name: 'Browse Images' }).click();
  
  const dialog = page.getByRole('dialog');
  const authDialog = page.getByRole('dialog').filter({ hasText: /github.*token|personal access token/i });
  
  if (await authDialog.isVisible().catch(() => false)) {
    await authDialog.getByPlaceholder(/username/i).fill('testuser');
    await authDialog.getByPlaceholder(/token|pat/i).fill('ghp_test123');
    await authDialog.getByRole('button', { name: /save|continue/i }).click();
  }
  
  await dialog.getByPlaceholder(/owner/i).fill('opencontainers');
  await dialog.getByRole('button', { name: /load|browse/i }).click();
  
  await page.waitForTimeout(3000);
  
  console.log('Page errors:', JSON.stringify(pageErrors, null, 2));
  
  const loadingVisible = await dialog.getByText('Loading images...').isVisible().catch(() => false);
  const imageListVisible = await page.locator('[data-testid="image-list"]').isVisible().catch(() => false);
  const errorVisible = await dialog.getByText(/Error:/i).isVisible().catch(() => false);
  
  console.log('Loading:', loadingVisible, 'ImageList:', imageListVisible, 'Error:', errorVisible);
});
