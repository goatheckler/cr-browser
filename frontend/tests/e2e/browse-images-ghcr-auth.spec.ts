import { test, expect } from '@playwright/test';

test('GHCR prompts for GitHub PAT when browsing', async ({ page }) => {
  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  
  await page.getByRole('combobox').selectOption('ghcr');
  
  const browseButton = page.getByRole('button', { name: 'Browse Images' });
  await expect(browseButton).toBeVisible();
  await browseButton.click();
  
  const authDialog = page.getByRole('dialog', { name: /github.*token|authentication/i });
  await expect(authDialog).toBeVisible();
  
  await expect(page.getByText(/personal access token|PAT/i)).toBeVisible();
  await expect(page.getByText(/read:packages/i)).toBeVisible();
  
  const tokenInput = page.getByPlaceholder(/token|ghp_/i);
  await expect(tokenInput).toBeVisible();
  await expect(tokenInput).toHaveAttribute('type', 'password');
});

test('GHCR rejects invalid token format', async ({ page }) => {
  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  
  await page.getByRole('combobox').selectOption('ghcr');
  await page.getByRole('button', { name: 'Browse Images' }).click();
  
  const tokenInput = page.getByPlaceholder(/token|ghp_/i);
  await tokenInput.fill('invalid-token-123');
  
  await page.getByRole('button', { name: /save|continue/i }).click();
  
  await expect(page.getByText(/invalid.*format|must start with ghp_/i)).toBeVisible();
});

test('GHCR accepts valid token format and stores it', async ({ page }) => {
  await page.goto('/');
  await page.getByText(/API healthy/).waitFor();
  
  await page.getByRole('combobox').selectOption('ghcr');
  await page.getByRole('button', { name: 'Browse Images' }).click();
  
  const validToken = 'ghp_' + 'a'.repeat(36);
  const tokenInput = page.getByPlaceholder(/token|ghp_/i);
  await tokenInput.fill(validToken);
  
  await page.getByRole('button', { name: /save|continue/i }).click();
  
  const stored = await page.evaluate(() => localStorage.getItem('cr-browser:ghcr:pat'));
  expect(stored).toBeTruthy();
  expect(stored).toContain('ghp_');
});

test('GHCR shows clear token button when authenticated', async ({ page }) => {
  const validToken = 'ghp_' + 'a'.repeat(36);
  await page.goto('/');
  
  await page.evaluate((token) => {
    localStorage.setItem('cr-browser:ghcr:pat', JSON.stringify({ tokenValue: token, validatedAt: new Date().toISOString() }));
  }, validToken);
  
  await page.reload();
  await page.getByText(/API healthy/).waitFor();
  
  await page.getByRole('combobox').selectOption('ghcr');
  
  const clearButton = page.getByRole('button', { name: /clear|remove.*token/i });
  await expect(clearButton).toBeVisible();
  
  await clearButton.click();
  
  const stored = await page.evaluate(() => localStorage.getItem('cr-browser:ghcr:pat'));
  expect(stored).toBeNull();
});
