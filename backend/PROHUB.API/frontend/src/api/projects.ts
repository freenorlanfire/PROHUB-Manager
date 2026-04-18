import { api } from './client';
import type { Project } from './types';

export const projectsApi = {
  list: (companyId: string) => api.get<Project[]>(`/projects?companyId=${companyId}`),
  get: (id: string) => api.get<Project>(`/projects/${id}`),
  create: (body: { companyId: string; name: string; description?: string }) =>
    api.post<Project>('/projects', body),
  update: (id: string, body: { name: string; description?: string; status?: string }) =>
    api.put<Project>(`/projects/${id}`, body),
  delete: (id: string) => api.del<null>(`/projects/${id}`),
  archive: (id: string) => api.post<Project>(`/projects/${id}/archive`, {}),
};
