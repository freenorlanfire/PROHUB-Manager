import { api } from './client';
import type { ProjectLink } from './types';

export const linksApi = {
  list: (projectId: string) => api.get<ProjectLink[]>(`/projects/${projectId}/links`),
  add: (projectId: string, body: { label: string; url: string }) =>
    api.post<ProjectLink>(`/projects/${projectId}/links`, body),
  delete: (projectId: string, linkId: string) =>
    api.del<null>(`/projects/${projectId}/links/${linkId}`),
};
