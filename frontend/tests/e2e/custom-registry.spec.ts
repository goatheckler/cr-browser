import { test, expect } from '@playwright/test';

test.describe('Custom Registry', () => {
	test.beforeEach(async ({ page }) => {
		await page.goto('/');
	});

	test('should show custom registry option in selector', async ({ page }) => {
		const selector = page.getByTestId('registry-selector');
		await expect(selector).toBeVisible();

		const options = await selector.locator('option').allTextContents();
		expect(options).toContain('Custom Registry');
	});

	test('should allow selecting custom registry', async ({ page }) => {
		const selector = page.getByTestId('registry-selector');
		await selector.selectOption('custom');
		await expect(selector).toHaveValue('custom');
	});

	test('should show custom registry validation dialog via Check button', async ({ page }) => {
		const selector = page.getByTestId('registry-selector');
		await selector.selectOption('custom');

		const mainUrlInput = page.locator('#registry-url-input');
		await mainUrlInput.fill('docker.redpanda.com');

		await page.getByRole('button', { name: 'Check' }).click();

		await expect(page.locator('role=dialog')).toBeVisible({ timeout: 10000 });
		await expect(page.getByText('✓ OCI Registry Detected')).toBeVisible();
	});

	test('should detect valid custom registry URL', async ({ page }) => {
		const selector = page.getByTestId('registry-selector');
		await selector.selectOption('custom');

		const mainUrlInput = page.locator('#registry-url-input');
		await mainUrlInput.fill('docker.redpanda.com');

		await page.getByRole('button', { name: 'Check' }).click();

		await expect(page.getByText('✓ OCI Registry Detected')).toBeVisible({ timeout: 10000 });
		await expect(page.getByText('https://docker.redpanda.com')).toBeVisible();
	});

	test('should show error for invalid custom registry URL', async ({ page }) => {
		const selector = page.getByTestId('registry-selector');
		await selector.selectOption('custom');

		const mainUrlInput = page.locator('#registry-url-input');
		await mainUrlInput.fill('invalid-registry-url-that-does-not-exist.com');

		await page.getByRole('button', { name: 'Check' }).click();

		await expect(page.getByText('✗ Registry Not Detected')).toBeVisible({ timeout: 10000 });
		await expect(page.getByText(/Error:/)).toBeVisible();
	});

	test('should allow canceling custom registry validation', async ({ page }) => {
		const selector = page.getByTestId('registry-selector');
		await selector.selectOption('custom');

		const mainUrlInput = page.locator('#registry-url-input');
		await mainUrlInput.fill('docker.redpanda.com');

		await page.getByRole('button', { name: 'Check' }).click();
		await expect(page.locator('role=dialog')).toBeVisible({ timeout: 10000 });

		await page.getByRole('button', { name: 'Close' }).click();

		await expect(page.locator('role=dialog')).not.toBeVisible();
	});
});
