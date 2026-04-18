import { api } from './client';
import type { StatusEntry } from './types';

export const statusApi = {
  history: (projectId: string) =>
    api.get<StatusEntry[]>(`/projects/${projectId}/status/history`),
  add: (projectId: string, body: { status: string; note?: string; createdBy?: string }) =>
    api.post<StatusEntry>(`/projects/${projectId}/status`, body),
};
