import { api } from './client';
import type { IntegrationLink } from './types';

export const integrationsApi = {
  get: (projectId: string) =>
    api.get<IntegrationLink[]>(`/projects/${projectId}/integrations`),
  upsert: (
    projectId: string,
    links: Array<{ type: string; url: string; label?: string }>
  ) => api.put<IntegrationLink[]>(`/projects/${projectId}/integrations`, { links }),
};
