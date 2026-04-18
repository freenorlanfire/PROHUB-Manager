/**
 * Module-level singleton so the API client can read the current token
 * without needing React context.
 *
 * AuthContext writes here; api/client reads here.
 */

let _token: string | null = null;
let _onRefresh: (() => Promise<string | null>) | null = null;

export const tokenStore = {
  get: () => _token,
  set: (t: string | null) => { _token = t; },
  setRefresher: (fn: () => Promise<string | null>) => { _onRefresh = fn; },
  refresh: (): Promise<string | null> => _onRefresh?.() ?? Promise.resolve(null),
};
