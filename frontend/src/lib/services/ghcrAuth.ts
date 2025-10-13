import type { RegistryCredential } from '$lib/types/browse';
import { STORAGE_KEYS, REGISTRY_CONFIG, ERROR_CODES } from '$lib/constants/browse';

export function createCredential(tokenValue: string): RegistryCredential {
  return {
    registryType: 'GHCR',
    tokenValue,
    tokenPrefix: REGISTRY_CONFIG.GHCR.tokenPrefix,
    requiredScopes: REGISTRY_CONFIG.GHCR.requiredScopes,
    isValid: false,
    validatedAt: null,
    expiresAt: null,
    storageLocation: 'localStorage',
    storageKey: STORAGE_KEYS.GHCR_PAT
  };
}

export async function validateCredential(credential: RegistryCredential): Promise<boolean> {
  if (!isTokenFormatValid(credential.tokenValue)) {
    return false;
  }

  try {
    const response = await fetch('https://api.github.com/user', {
      headers: {
        'Authorization': `Bearer ${credential.tokenValue}`,
        'Accept': 'application/vnd.github+json',
        'X-GitHub-Api-Version': '2022-11-28'
      }
    });

    return response.ok;
  } catch (error) {
    console.error('GHCR credential validation failed:', error);
    return false;
  }
}

export function saveCredential(credential: RegistryCredential): void {
  if (!isHttpsContext() && !isLocalhost()) {
    throw new Error('HTTPS required for storing credentials');
  }

  const storageData = {
    tokenValue: credential.tokenValue,
    validatedAt: credential.validatedAt ? credential.validatedAt.toISOString() : null,
    expiresAt: credential.expiresAt ? credential.expiresAt.toISOString() : null
  };

  try {
    localStorage.setItem(credential.storageKey, JSON.stringify(storageData));
  } catch (error) {
    console.error('Failed to save credential:', error);
    throw new Error('Failed to save credential to storage');
  }
}

export function loadCredential(): RegistryCredential | null {
  try {
    const stored = localStorage.getItem(STORAGE_KEYS.GHCR_PAT);
    if (!stored) {
      return null;
    }

    const data = JSON.parse(stored);
    const credential = createCredential(data.tokenValue);
    
    if (data.validatedAt) {
      credential.validatedAt = new Date(data.validatedAt);
      credential.isValid = true;
    }
    
    if (data.expiresAt) {
      credential.expiresAt = new Date(data.expiresAt);
    }

    return credential;
  } catch (error) {
    console.error('Failed to load credential:', error);
    return null;
  }
}

export function clearCredential(): void {
  try {
    localStorage.removeItem(STORAGE_KEYS.GHCR_PAT);
    localStorage.removeItem(STORAGE_KEYS.GHCR_PAT_VALIDATED_AT);
  } catch (error) {
    console.error('Failed to clear credential:', error);
  }
}

export function isTokenFormatValid(tokenValue: string): boolean {
  if (!tokenValue) {
    return false;
  }

  if (!tokenValue.startsWith(REGISTRY_CONFIG.GHCR.tokenPrefix)) {
    return false;
  }

  if (tokenValue.length < REGISTRY_CONFIG.GHCR.minTokenLength) {
    return false;
  }

  return true;
}

function isHttpsContext(): boolean {
  if (typeof window === 'undefined') {
    return false;
  }
  return window.location.protocol === 'https:';
}

function isLocalhost(): boolean {
  if (typeof window === 'undefined') {
    return false;
  }
  return window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1';
}
