import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { PropsWithChildren } from 'react';
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

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: PropsWithChildren) {
  const [session, setSession] = useState<AuthResponse | null>(() => loadSession());
  const [platform, setPlatform] = useState<PlatformContext | null>(null);
  const [contextLoading, setContextLoading] = useState(Boolean(session));
  const [contextError, setContextError] = useState<string | null>(null);

  const logout = useCallback(() => {
    clearSession();
    setSession(null);
    setPlatform(null);
    setContextError(null);
    setContextLoading(false);
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
    window.addEventListener('agrocontrol:unauthorized', logout);
    return () => window.removeEventListener('agrocontrol:unauthorized', logout);
  }, [logout]);

  const login = useCallback(async (email: string, password: string) => {
    const result = await apiRequest<AuthResponse>('/api/v1/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
      auth: false
    });
    saveSession(result);
    setSession(result);
  }, []);

  const register = useCallback(async (input: RegisterInput) => {
    const result = await apiRequest<AuthResponse>('/api/v1/auth/register', {
      method: 'POST',
      body: JSON.stringify(input),
      auth: false
    });
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
