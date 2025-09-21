import { test, expect } from '@playwright/test';

// Assumes backend is reverse-proxied or same-origin during dev; here we directly call /api
// Requires backend running separately when executing tests.

test('health endpoint shows healthy status on page', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByText(/API healthy/)).toBeVisible();
});
