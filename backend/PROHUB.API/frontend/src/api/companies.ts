import { api } from './client';
import type { Company } from './types';

export const companiesApi = {
  list: () => api.get<Company[]>('/companies'),
  get: (id: string) => api.get<Company>(`/companies/${id}`),
  create: (body: { name: string; description?: string; logoUrl?: string; websiteUrl?: string }) =>
    api.post<Company>('/companies', body),
  update: (
    id: string,
    body: { name: string; description?: string; logoUrl?: string; websiteUrl?: string }
  ) => api.put<Company>(`/companies/${id}`, body),
  delete: (id: string) => api.del<null>(`/companies/${id}`),
};
