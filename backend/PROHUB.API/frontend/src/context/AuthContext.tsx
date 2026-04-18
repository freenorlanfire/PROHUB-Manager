import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from 'react';
import { authApi, type AuthUser, type LoginResponse } from '../api/auth';
import { tokenStore } from '../api/tokenStore';

const STORAGE_KEY = 'prohub_refresh_token';

interface AuthState {
  user: AuthUser | null;
  token: string | null;
  companyId: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
}

interface AuthContextType extends AuthState {
  login: (email: string, password: string) => Promise<string | null>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>({
    user: null,
    token: null,
    companyId: null,
    isAuthenticated: false,
    isLoading: true,
  });

  // ── Apply a successful auth response ──────────────────────────────────────────

  const applyAuth = useCallback((res: LoginResponse) => {
    tokenStore.set(res.token);
    localStorage.setItem(STORAGE_KEY, res.refreshToken);
    setState({
      user: res.user,
      token: res.token,
      companyId: res.companyId,
      isAuthenticated: true,
      isLoading: false,
    });
  }, []);

  // ── Refresh handler (called by tokenStore on 401) ─────────────────────────────

  const doRefresh = useCallback(async (): Promise<string | null> => {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (!stored) return null;

    const res = await authApi.refresh(stored);
    if (res.ok && res.data) {
      tokenStore.set(res.data.token);
      localStorage.setItem(STORAGE_KEY, res.data.refreshToken);
      setState(prev => ({ ...prev, token: res.data!.token }));
      return res.data.token;
    }

    // Refresh failed — clear session
    clearSession();
    return null;
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  function clearSession() {
    tokenStore.set(null);
    localStorage.removeItem(STORAGE_KEY);
    setState({ user: null, token: null, companyId: null, isAuthenticated: false, isLoading: false });
  }

  // ── Bootstrap: try to restore session from stored refresh token ───────────────

  useEffect(() => {
    tokenStore.setRefresher(doRefresh);

    const stored = localStorage.getItem(STORAGE_KEY);
    if (!stored) {
      setState(prev => ({ ...prev, isLoading: false }));
      return;
    }

    authApi.refresh(stored).then(res => {
      if (res.ok && res.data) {
        // We only have refresh response here, need to call /me to get user
        tokenStore.set(res.data.token);
        localStorage.setItem(STORAGE_KEY, res.data.refreshToken);
        // Fetch user info via /me
        fetch('/api/auth/me', {
          headers: { Authorization: `Bearer ${res.data.token}` },
        })
          .then(r => r.json())
          .then(envelope => {
            if (envelope.ok && envelope.data) {
              applyAuth({
                token: res.data!.token,
                refreshToken: res.data!.refreshToken,
                expiresAt: res.data!.expiresAt,
                user: envelope.data.user,
                companyId: envelope.data.companyId,
              });
            } else {
              clearSession();
            }
          })
          .catch(() => clearSession());
      } else {
        clearSession();
      }
    });
  }, [doRefresh, applyAuth]);

  // ── Login ─────────────────────────────────────────────────────────────────────

  const login = useCallback(
    async (email: string, password: string): Promise<string | null> => {
      const res = await authApi.login(email, password);
      if (res.ok && res.data) {
        applyAuth(res.data);
        return null;
      }
      return res.error ?? 'Login failed.';
    },
    [applyAuth]
  );

  // ── Logout ────────────────────────────────────────────────────────────────────

  const logout = useCallback(async () => {
    const stored = localStorage.getItem(STORAGE_KEY);
    const token = tokenStore.get();
    if (stored && token) {
      await authApi.logout(stored, token).catch(() => {});
    }
    clearSession();
  }, []);

  return (
    <AuthContext.Provider value={{ ...state, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextType {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>');
  return ctx;
}
