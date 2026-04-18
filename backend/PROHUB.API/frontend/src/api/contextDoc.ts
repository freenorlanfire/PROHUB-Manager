import { api } from './client';
import type { ContextDoc, ContextDocVersion } from './types';

export const contextDocApi = {
  get: (projectId: string) => api.get<ContextDoc>(`/projects/${projectId}/context-doc`),
  save: (projectId: string, body: { content: string; updatedBy?: string }) =>
    api.put<ContextDoc>(`/projects/${projectId}/context-doc`, body),
  versions: (projectId: string) =>
    api.get<ContextDocVersion[]>(`/projects/${projectId}/context-doc/versions`),
};
