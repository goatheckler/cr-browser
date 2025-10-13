export const ERROR_CODES = {
  NETWORK_ERROR: 'NETWORK_ERROR',
  AUTH_REQUIRED: 'AUTH_REQUIRED',
  AUTH_FAILED: 'AUTH_FAILED',
  INVALID_TOKEN: 'INVALID_TOKEN',
  RATE_LIMITED: 'RATE_LIMITED',
  NOT_FOUND: 'NOT_FOUND',
  INVALID_INPUT: 'INVALID_INPUT',
  UNKNOWN: 'UNKNOWN'
} as const;

export const STORAGE_KEYS = {
  GHCR_PAT: 'cr-browser:ghcr:pat',
  GHCR_PAT_VALIDATED_AT: 'cr-browser:ghcr:pat:validated'
} as const;

export const REGISTRY_CONFIG = {
  GHCR: {
    name: 'GitHub Container Registry',
    baseUrl: 'https://ghcr.io',
    apiUrl: 'https://api.github.com',
    requiresAuth: true,
    tokenPrefix: 'ghp_',
    minTokenLength: 40,
    requiredScopes: ['read:packages']
  },
  DockerHub: {
    name: 'Docker Hub',
    baseUrl: 'https://hub.docker.com',
    apiUrl: 'https://hub.docker.com/v2',
    requiresAuth: false,
    defaultPageSize: 25,
    maxPageSize: 100
  },
  Quay: {
    name: 'Quay.io',
    baseUrl: 'https://quay.io',
    apiUrl: 'https://quay.io/api/v1',
    requiresAuth: false
  },
  GCR: {
    name: 'Google Container Registry',
    baseUrl: 'https://gcr.io',
    requiresAuth: false,
    projectIdPattern: /^[a-z][a-z0-9-]{4,28}[a-z0-9]$/,
    labelOverride: 'Project ID'
  }
} as const;

export const DEFAULT_PAGE_SIZE = 25;

export const ERROR_MESSAGES = {
  [ERROR_CODES.NETWORK_ERROR]: 'Network error occurred. Please check your connection.',
  [ERROR_CODES.AUTH_REQUIRED]: 'Authentication required. Please provide credentials.',
  [ERROR_CODES.AUTH_FAILED]: 'Authentication failed. Please check your credentials.',
  [ERROR_CODES.INVALID_TOKEN]: 'Invalid token format or expired token.',
  [ERROR_CODES.RATE_LIMITED]: 'Rate limit exceeded. Please try again later.',
  [ERROR_CODES.NOT_FOUND]: 'Resource not found.',
  [ERROR_CODES.INVALID_INPUT]: 'Invalid input provided.',
  [ERROR_CODES.UNKNOWN]: 'An unknown error occurred.'
} as const;
