import { test, expect } from '@playwright/test';

test('debug console', async ({ page }) => {
  const messages: string[] = [];
  page.on('console', msg => messages.push(`[${msg.type()}] ${msg.text()}`));
  page.on('pageerror', err => messages.push(`[ERROR] ${err.message}`));
  page.on('response', response => {
    if (response.url().includes('/api/')) {
      messages.push(`[API] ${response.status()} ${response.url()}`);
    }
  });
  
  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  
  await page.getByRole('combobox').selectOption('dockerhub');
  await page.getByRole('button', { name: 'Browse Images' }).click();
  
  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  
  await dialog.getByPlaceholder(/owner|namespace/i).fill('library');
  await dialog.getByRole('button', { name: /load|browse/i }).click();
  
  await page.waitForTimeout(8000);
  
  const html = await page.content();
  const hasNginx = html.includes('nginx');
  const hasLoading = html.includes('Loading');
  const hasNoImages = html.includes('No images');
  const hasError = html.includes('Error');
  
  console.log('\n=== Page State ===');
  console.log('Has "nginx":', hasNginx);
  console.log('Has "Loading":', hasLoading);
  console.log('Has "No images":', hasNoImages);
  console.log('Has "Error":', hasError);
  console.log('\n=== Console/Network Messages ===');
  messages.forEach(m => console.log(m));
  console.log('========================\n');
});
