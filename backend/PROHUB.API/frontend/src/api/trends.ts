import { api } from './client';

export interface TrendItem {
  id: string;
  source: 'hn' | 'devto' | 'github';
  title: string;
  url: string;
  description?: string;
  author?: string;
  points: number;
  publishedAt: string;
  tags: string[];
}

export interface TrendsResult {
  items: TrendItem[];
  queryTags: string[];
  fetchedAtUtc: string;
}

export interface AiEnhanceResponse {
  content: string;
  model: string;
  wasAiGenerated: boolean;
}

export const trendsApi = {
  get: (tags: string[]) =>
    api.get<TrendsResult>(`/trends?tags=${tags.join(',')}`),

  getForProject: (projectId: string) =>
    api.get<TrendsResult>(`/trends/project/${projectId}`),
};

export const aiApi = {
  enhanceContextDoc: (projectId: string, extraInstructions?: string) =>
    api.post<AiEnhanceResponse>(`/projects/${projectId}/context-doc/ai-enhance`, {
      extraInstructions,
    }),

  analyzeProject: (projectId: string) =>
    api.post<{ analysis: string }>(`/projects/${projectId}/ai-analyze`, {}),
};
