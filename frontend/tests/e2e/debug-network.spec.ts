import { test, expect } from '@playwright/test';

test('debug network and errors', async ({ page }) => {
  const networkRequests: any[] = [];
  const consoleMessages: any[] = [];
  const pageErrors: any[] = [];
  
  page.on('request', req => {
    if (req.url().includes('/api/')) {
      networkRequests.push({ url: req.url(), method: req.method() });
    }
  });
  
  page.on('response', async res => {
    if (res.url().includes('/api/')) {
      const body = await res.text().catch(() => 'could not read');
      networkRequests.push({ 
        url: res.url(), 
        status: res.status(),
        body: body.substring(0, 200)
      });
    }
  });
  
  page.on('console', msg => {
    consoleMessages.push({ type: msg.type(), text: msg.text() });
  });
  
  page.on('pageerror', err => {
    pageErrors.push({ message: err.message, stack: err.stack });
  });
  
  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  
  await page.getByRole('combobox').selectOption('quay');
  await page.getByRole('button', { name: 'Browse Images' }).click();
  
  const dialog = page.getByRole('dialog');
  await dialog.getByPlaceholder(/owner|namespace/i).fill('coreos');
  await dialog.getByRole('button', { name: /load|browse/i }).click();
  
  await page.waitForTimeout(3000);
  
  console.log('Network requests:', JSON.stringify(networkRequests, null, 2));
  console.log('Console messages:', JSON.stringify(consoleMessages, null, 2));
  console.log('Page errors:', JSON.stringify(pageErrors, null, 2));
});
