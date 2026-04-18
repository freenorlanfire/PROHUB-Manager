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

  let res: Response;
  try {
    res = await fetch(`/api${path}`, { ...fetchOpts, headers });
  } catch (networkErr) {
    const msg = networkErr instanceof Error ? networkErr.message : String(networkErr);
    console.error(`[API] Network error on ${path}:`, msg);
    return { ok: false, data: null, error: `Network error: ${msg}` };
  }

  // On 401, try to refresh once then retry
  if (res.status === 401 && !retried) {
    const newToken = await tokenStore.refresh();
    if (newToken) return request<T>(path, options, true);
    return { ok: false, data: null, error: 'Session expired. Please log in again.' };
  }

  if (res.status === 204) return { ok: true, data: null, error: null };

  // Try to parse as our envelope; fall back to a generic error with status + body preview
  try {
    const json = await res.json();

    // If the server returned our ApiResponse format, use it directly
    if (typeof json?.ok === 'boolean') {
      return json as ApiEnvelope<T>;
    }

    // Otherwise (ProblemDetails, etc.) extract what we can
    const detail = json?.detail ?? json?.title ?? json?.message ?? null;
    const errorMsg = detail
      ? `${res.status}: ${detail}`
      : `HTTP ${res.status}`;

    console.error(`[API] Unexpected response format on ${path} (${res.status}):`, json);
    return { ok: false, data: null, error: errorMsg };
  } catch {
    console.error(`[API] Could not parse response body on ${path} (${res.status})`);
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
