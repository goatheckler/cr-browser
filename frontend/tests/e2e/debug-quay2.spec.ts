import { test, expect } from '@playwright/test';

test('debug store state', async ({ page }) => {
  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  
  // Inject code to monitor store
  await page.evaluate(() => {
    (window as any).storeStates = [];
  });
  
  await page.getByRole('combobox').selectOption('quay');
  await page.getByRole('button', { name: 'Browse Images' }).click();
  
  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  
  await dialog.getByPlaceholder(/owner|namespace/i).fill('coreos');
  await dialog.getByRole('button', { name: /load|browse/i }).click();
  
  // Wait for potential state changes
  await page.waitForTimeout(3000);
  
  // Check store state via evaluating component state
  const storeData = await page.evaluate(() => {
    // Try to access the browseSession from window
    return {
      hasImageList: !!document.querySelector('[data-testid="image-list"]'),
      hasLoading: !!document.querySelector('[data-testid="loading-state"]'),
      hasError: !!document.querySelector('[data-testid="error-message"]')
    };
  });
  
  console.log('Store data:', JSON.stringify(storeData, null, 2));
  
  // Make a direct API call to verify backend works
  const apiResponse = await page.evaluate(async () => {
    const res = await fetch('/api/registries/quay/coreos/images?pageSize=100');
    const data = await res.json();
    return {
      status: res.status,
      imageCount: data.images?.length || 0
    };
  });
  
  console.log('API response:', JSON.stringify(apiResponse, null, 2));
});
