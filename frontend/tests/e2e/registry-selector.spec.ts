import { test, expect } from '@playwright/test';

test.describe('Registry Selector', () => {
	test('displays all four registry options', async ({ page }) => {
		await page.goto('/');

		const registrySelector = page.locator('select[data-testid="registry-selector"]');
		await expect(registrySelector).toBeVisible();

		const options = await registrySelector.locator('option').allTextContents();
		expect(options).toContain('GitHub Container Registry');
		expect(options).toContain('Docker Hub');
		expect(options).toContain('Quay.io');
		expect(options).toContain('Google Container Registry');
	});

	test('defaults to GHCR when no registry parameter in URL', async ({ page }) => {
		await page.goto('/');

		const registrySelector = page.locator('select[data-testid="registry-selector"]');
		await expect(registrySelector).toHaveValue('ghcr');
	});

	test('selects registry based on URL parameter', async ({ page }) => {
		await page.goto('/?registry=dockerhub');

		const registrySelector = page.locator('select[data-testid="registry-selector"]');
		await expect(registrySelector).toHaveValue('dockerhub');
	});

	test('updates URL when registry is changed', async ({ page }) => {
		await page.goto('/');

		const registrySelector = page.locator('select[data-testid="registry-selector"]');
		await registrySelector.selectOption('quay');

		await expect(page).toHaveURL('/?registry=quay');
	});

	test('preserves other URL parameters when changing registry', async ({ page }) => {
		await page.goto('/?owner=testowner&image=testimage&registry=ghcr');

		const registrySelector = page.locator('select[data-testid="registry-selector"]');
		await registrySelector.selectOption('dockerhub');

		const url = new URL(page.url());
		expect(url.searchParams.get('registry')).toBe('dockerhub');
		expect(url.searchParams.get('owner')).toBe('testowner');
		expect(url.searchParams.get('image')).toBe('testimage');
	});

	test('triggers new search when registry is changed', async ({ page }) => {
		await page.goto('/?owner=stefanprodan&image=podinfo&registry=ghcr');

		await page.waitForLoadState('networkidle');

		const registrySelector = page.locator('select[data-testid="registry-selector"]');
		
		await registrySelector.selectOption('dockerhub');

		// Verify the URL updated to reflect the new registry
		await expect(page).toHaveURL(/registry=dockerhub/);
		
		// Verify search was triggered by checking for loading state or results
		// This is less brittle than waiting for specific API responses
		await expect(page.getByText(/Found|Search|Loading/i)).toBeVisible({ timeout: 10_000 });
	});

	test('maintains registry selection across page navigations', async ({ page }) => {
		await page.goto('/?registry=quay');

		const registrySelector = page.locator('select[data-testid="registry-selector"]');
		await expect(registrySelector).toHaveValue('quay');

		await page.reload();

		await expect(registrySelector).toHaveValue('quay');
	});
});
