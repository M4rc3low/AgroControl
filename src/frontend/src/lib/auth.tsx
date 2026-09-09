import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { PropsWithChildren } from 'react';
import { offlineStore } from '../offline/indexedDbOfflineStore';
import { apiRequest } from './api';
import { clearSession, loadSession, saveSession } from './session';
import type { AuthResponse, EntitlementSnapshot, MeProfile, ModuleDefinition, Organization, PlatformContext } from './types';

interface RegisterInput {
  organizationName: string;
  displayName: string;
  email: string;
  password: string;
}

interface AuthContextValue {
  session: AuthResponse | null;
  platform: PlatformContext | null;
  contextLoading: boolean;
  contextError: string | null;
  login(email: string, password: string): Promise<void>;
  register(input: RegisterInput): Promise<void>;
  logout(): void;
  refreshContext(): Promise<void>;
}

const OFFLINE_PRINCIPAL_KEY = 'agrocontrol.offline.principal.v1';
const AuthContext = createContext<AuthContextValue | null>(null);

function readOfflinePrincipal() {
  try {
    const raw = localStorage.getItem(OFFLINE_PRINCIPAL_KEY);
    if (!raw) return null;
    const value = JSON.parse(raw) as { userId?: string; organizationId?: string };
    return value.userId && value.organizationId
      ? { userId: value.userId, organizationId: value.organizationId }
      : null;
  } catch {
    return null;
  }
}

async function prepareOfflinePrincipal(session: AuthResponse) {
  const previous = readOfflinePrincipal();
  if (previous && (previous.userId !== session.userId || previous.organizationId !== session.organizationId)) {
    await offlineStore.clearAll();
  }
  localStorage.setItem(OFFLINE_PRINCIPAL_KEY, JSON.stringify({
    userId: session.userId,
    organizationId: session.organizationId
  }));
}

export function AuthProvider({ children }: PropsWithChildren) {
  const [session, setSession] = useState<AuthResponse | null>(() => loadSession());
  const [platform, setPlatform] = useState<PlatformContext | null>(null);
  const [contextLoading, setContextLoading] = useState(Boolean(session));
  const [contextError, setContextError] = useState<string | null>(null);

  const logout = useCallback(() => {
    clearSession();
    localStorage.removeItem(OFFLINE_PRINCIPAL_KEY);
    setSession(null);
    setPlatform(null);
    setContextError(null);
    setContextLoading(false);
    void offlineStore.clearAll().catch(() => undefined);
  }, []);

  const refreshContext = useCallback(async () => {
    if (!loadSession()) return;
    setContextLoading(true);
    setContextError(null);
    try {
      const [profile, organization, entitlements, modules] = await Promise.all([
        apiRequest<MeProfile>('/api/v1/me'),
        apiRequest<Organization>('/api/v1/organizations/current'),
        apiRequest<EntitlementSnapshot>('/api/v1/platform/entitlements'),
        apiRequest<ModuleDefinition[]>('/api/v1/platform/modules', { auth: false })
      ]);
      setPlatform({ profile, organization, entitlements, modules });
    } catch (error) {
      setContextError(error instanceof Error ? error.message : 'Não foi possível carregar o contexto da organização.');
    } finally {
      setContextLoading(false);
    }
  }, []);

  useEffect(() => {
    if (session) void refreshContext();
    else setPlatform(null);
  }, [session, refreshContext]);

  useEffect(() => {
    const handleUnauthorized = () => logout();
    window.addEventListener('agrocontrol:unauthorized', handleUnauthorized);
    return () => window.removeEventListener('agrocontrol:unauthorized', handleUnauthorized);
  }, [logout]);

  const login = useCallback(async (email: string, password: string) => {
    const result = await apiRequest<AuthResponse>('/api/v1/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
      auth: false
    });
    await prepareOfflinePrincipal(result);
    saveSession(result);
    setSession(result);
  }, []);

  const register = useCallback(async (input: RegisterInput) => {
    const result = await apiRequest<AuthResponse>('/api/v1/auth/register', {
      method: 'POST',
      body: JSON.stringify(input),
      auth: false
    });
    await prepareOfflinePrincipal(result);
    saveSession(result);
    setSession(result);
  }, []);

  const value = useMemo<AuthContextValue>(() => ({
    session,
    platform,
    contextLoading,
    contextError,
    login,
    register,
    logout,
    refreshContext
  }), [session, platform, contextLoading, contextError, login, register, logout, refreshContext]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const value = useContext(AuthContext);
  if (!value) throw new Error('useAuth must be used inside AuthProvider.');
  return value;
}
