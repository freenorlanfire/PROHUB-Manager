import { api } from './client';
import type { ApiEnvelope } from './client';

export interface AuthUser {
  id: string;
  email: string;
  name: string;
  role: string;
}

export interface LoginResponse {
  token: string;
  refreshToken: string;
  expiresAt: string;
  user: AuthUser;
  companyId: string | null;
}

export interface RefreshResponse {
  token: string;
  refreshToken: string;
  expiresAt: string;
}

async function post<T>(path: string, body: unknown, token?: string): Promise<ApiEnvelope<T>> {
  const headers: Record<string, string> = { 'Content-Type': 'application/json' };
  if (token) headers['Authorization'] = `Bearer ${token}`;
  const res = await fetch(`/api${path}`, { method: 'POST', headers, body: JSON.stringify(body) });
  return res.json();
}

export const authApi = {
  login: (email: string, password: string) =>
    post<LoginResponse>('/auth/login', { email, password }),

  refresh: (refreshToken: string) =>
    post<RefreshResponse>('/auth/refresh', { refreshToken }),

  logout: (refreshToken: string, token: string) =>
    post<null>('/auth/logout', { refreshToken }, token),

  changePassword: (body: { currentPassword: string; newPassword: string }) =>
    api.patch<null>('/auth/password', body),

  updateProfile: (body: { name: string }) =>
    api.patch<null>('/auth/profile', body),
};
