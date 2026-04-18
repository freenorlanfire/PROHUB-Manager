import { api } from './client';
import type { Tag } from './types';

export const tagsApi = {
  list: (projectId: string) => api.get<Tag[]>(`/projects/${projectId}/tags`),
  add: (projectId: string, tag: string) =>
    api.post<Tag>(`/projects/${projectId}/tags`, { tag }),
  delete: (projectId: string, tag: string) =>
    api.del<null>(`/projects/${projectId}/tags/${encodeURIComponent(tag)}`),
};
