import { REGISTRY_CONFIG } from '$lib/constants/browse';

export function validateProjectId(projectId: string): boolean {
  if (!projectId) {
    return false;
  }

  const pattern = REGISTRY_CONFIG.GCR.projectIdPattern;
  return pattern.test(projectId);
}
