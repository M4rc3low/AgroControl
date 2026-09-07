import type { AuthResponse } from './types';

const SESSION_KEY = 'agrocontrol.session.v1';

export function loadSession(): AuthResponse | null {
  try {
    const raw = sessionStorage.getItem(SESSION_KEY);
    if (!raw) return null;
    const session = JSON.parse(raw) as AuthResponse;
    if (!session.accessToken || !session.expiresAtUtc || Date.parse(session.expiresAtUtc) <= Date.now() + 5_000) {
      sessionStorage.removeItem(SESSION_KEY);
      return null;
    }
    return session;
  } catch {
    sessionStorage.removeItem(SESSION_KEY);
    return null;
  }
}

export function saveSession(session: AuthResponse) {
  sessionStorage.setItem(SESSION_KEY, JSON.stringify(session));
}

export function clearSession() {
  sessionStorage.removeItem(SESSION_KEY);
}

export function getAccessToken() {
  return loadSession()?.accessToken ?? null;
}
