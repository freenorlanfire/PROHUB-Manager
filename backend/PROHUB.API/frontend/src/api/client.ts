import { tokenStore } from './tokenStore';

export interface ApiEnvelope<T> {
  ok: boolean;
  data: T | null;
  error: string | null;
}

async function request<T>(
  path: string,
  options?: RequestInit & { companyId?: string },
  retried = false
): Promise<ApiEnvelope<T>> {
  const { companyId, ...fetchOpts } = options ?? {};

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(fetchOpts.headers as Record<string, string>),
  };

  if (companyId) headers['X-Company-Id'] = companyId;

  const token = tokenStore.get();
  if (token) headers['Authorization'] = `Bearer ${token}`;

  const res = await fetch(`/api${path}`, { ...fetchOpts, headers });

  // On 401, try to refresh once then retry
  if (res.status === 401 && !retried) {
    const newToken = await tokenStore.refresh();
    if (newToken) {
      return request<T>(path, options, true);
    }
    // Refresh failed — return a typed error
    return { ok: false, data: null, error: 'Session expired. Please log in again.' };
  }

  if (res.status === 204) return { ok: true, data: null, error: null };

  try {
    return await res.json() as ApiEnvelope<T>;
  } catch {
    return { ok: false, data: null, error: `HTTP ${res.status}` };
  }
}

export const api = {
  get: <T>(path: string, companyId?: string) =>
    request<T>(path, { method: 'GET', companyId }),

  post: <T>(path: string, body: unknown, companyId?: string) =>
    request<T>(path, { method: 'POST', body: JSON.stringify(body), companyId }),

  put: <T>(path: string, body: unknown, companyId?: string) =>
    request<T>(path, { method: 'PUT', body: JSON.stringify(body), companyId }),

  del: <T>(path: string, companyId?: string) =>
    request<T>(path, { method: 'DELETE', companyId }),
};
