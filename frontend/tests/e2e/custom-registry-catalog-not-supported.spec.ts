import { test, expect } from '@playwright/test';

test.describe('Custom Registry - Catalog Not Supported', () => {
	test.beforeEach(async ({ page }) => {
		await page.goto('/');
	});

	test('should show catalog not supported error and allow direct tag access', async ({ page }) => {
		const selector = page.getByTestId('registry-selector');
		await selector.selectOption('custom');

		const mainUrlInput = page.locator('#registry-url-input');
		await mainUrlInput.fill('docker.redpanda.com');

		await page.getByRole('button', { name: 'Check' }).click();
		
		await expect(page.getByText('✓ OCI Registry Detected')).toBeVisible({ timeout: 10000 });
		await expect(page.getByText('https://docker.redpanda.com')).toBeVisible();
		
		await page.getByRole('button', { name: 'Close' }).click();

		const ownerInput = page.locator('#owner-input');
		await ownerInput.fill('redpandadata');

		await page.getByRole('button', { name: 'Browse Images' }).click();

		const browseDialog = page.getByRole('dialog');
		await expect(browseDialog).toBeVisible();

		const dialogOwnerInput = browseDialog.locator('input[placeholder*="Owner"]');
		await dialogOwnerInput.clear();
		await dialogOwnerInput.fill('redpandadata');

		await browseDialog.getByRole('button', { name: 'Browse', exact: true }).click();

		await expect(page.getByTestId('error-message')).toContainText('does not support the OCI catalog API', { timeout: 10000 });
		await expect(page.getByText('You can still access tags by entering the image name directly')).toBeVisible();

		await page.getByRole('button', { name: 'Close and Enter Image Name' }).click();

		await expect(browseDialog).not.toBeVisible();

		const imageInput = page.locator('#image-input');
		await imageInput.fill('console');

		await page.getByRole('button', { name: 'Search' }).click();

		await expect(page.locator('.ag-theme-alpine')).toBeVisible();
		const rows = page.locator('.ag-center-cols-container .ag-row');
		await expect(rows.first()).toBeVisible({ timeout: 10000 });
		await expect(rows.count()).resolves.toBeGreaterThan(20);
	});
});
