import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { PropsWithChildren } from 'react';
import { apiRequest, getAllPaged } from './api';
import { useAuth } from './auth';
import type { Farm, FarmAccessScope, OperationalRegion, OperationalScope } from './types';

interface FarmScopeContextValue {
  access: FarmAccessScope | null;
  farms: Farm[];
  regions: OperationalRegion[];
  selected: OperationalScope;
  scopedFarms: Farm[];
  loading: boolean;
  error: string | null;
  setSelected(scope: OperationalScope): void;
  refresh(): Promise<void>;
  scopeLabel: string;
}

const FarmScopeContext = createContext<FarmScopeContextValue | null>(null);

function storageKey(organizationId: string) {
  return `agrocontrol.operational-scope.${organizationId}`;
}

function parseStoredScope(value: string | null): OperationalScope | null {
  if (!value) return null;
  try {
    const parsed = JSON.parse(value) as OperationalScope;
    if (parsed.kind === 'all') return parsed;
    if (parsed.kind === 'region' && parsed.regionId) return parsed;
    if (parsed.kind === 'state' && parsed.stateCode) return { kind: 'state', stateCode: parsed.stateCode.toUpperCase() };
    if (parsed.kind === 'farm' && parsed.farmId) return parsed;
  } catch {
    return null;
  }
  return null;
}

function isValidScope(scope: OperationalScope, access: FarmAccessScope, farms: Farm[], regions: OperationalRegion[]) {
  switch (scope.kind) {
    case 'all': return access.allFarms;
    case 'farm': return farms.some(farm => farm.id === scope.farmId);
    case 'region': return regions.some(region => region.id === scope.regionId) && farms.some(farm => farm.operationalRegionId === scope.regionId);
    case 'state': return farms.some(farm => (farm.stateCode ?? farm.state)?.toUpperCase() === scope.stateCode.toUpperCase());
  }
}

function fallbackScope(access: FarmAccessScope, farms: Farm[]): OperationalScope {
  if (access.allFarms) return { kind: 'all' };
  if (farms[0]) return { kind: 'farm', farmId: farms[0].id };
  return { kind: 'all' };
}

export function FarmScopeProvider({ children }: PropsWithChildren) {
  const { platform } = useAuth();
  const [access, setAccess] = useState<FarmAccessScope | null>(null);
  const [farms, setFarms] = useState<Farm[]>([]);
  const [regions, setRegions] = useState<OperationalRegion[]>([]);
  const [selected, setSelectedState] = useState<OperationalScope>({ kind: 'all' });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    if (!platform) {
      setAccess(null); setFarms([]); setRegions([]); setLoading(false); setError(null);
      return;
    }

    setLoading(true); setError(null);
    try {
      const [accessData, farmItems, regionItems] = await Promise.all([
        apiRequest<FarmAccessScope>('/api/v1/operations/farm-access/me'),
        getAllPaged<Farm>('/api/v1/farms'),
        getAllPaged<OperationalRegion>('/api/v1/operations/regions')
      ]);

      const accessibleRegionIds = new Set([
        ...accessData.regionIds,
        ...farmItems.map(farm => farm.operationalRegionId).filter((id): id is string => Boolean(id))
      ]);
      const visibleRegions = regionItems.filter(region => accessibleRegionIds.has(region.id));
      const stored = parseStoredScope(sessionStorage.getItem(storageKey(platform.organization.id)));
      const next = stored && isValidScope(stored, accessData, farmItems, visibleRegions)
        ? stored
        : fallbackScope(accessData, farmItems);

      setAccess(accessData);
      setFarms(farmItems);
      setRegions(visibleRegions);
      setSelectedState(next);
      sessionStorage.setItem(storageKey(platform.organization.id), JSON.stringify(next));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível carregar o escopo das propriedades.');
    } finally {
      setLoading(false);
    }
  }, [platform?.organization.id]);

  useEffect(() => { void refresh(); }, [refresh]);

  const setSelected = useCallback((scope: OperationalScope) => {
    if (!platform || !access || !isValidScope(scope, access, farms, regions)) return;
    setSelectedState(scope);
    sessionStorage.setItem(storageKey(platform.organization.id), JSON.stringify(scope));
  }, [platform?.organization.id, access, farms, regions]);

  const scopedFarms = useMemo(() => farms.filter(farm => {
    switch (selected.kind) {
      case 'all': return true;
      case 'farm': return farm.id === selected.farmId;
      case 'region': return farm.operationalRegionId === selected.regionId;
      case 'state': return (farm.stateCode ?? farm.state)?.toUpperCase() === selected.stateCode.toUpperCase();
    }
  }), [farms, selected]);

  const scopeLabel = useMemo(() => {
    switch (selected.kind) {
      case 'all': return 'Todas as fazendas';
      case 'farm': return farms.find(farm => farm.id === selected.farmId)?.name ?? 'Fazenda';
      case 'region': return regions.find(region => region.id === selected.regionId)?.name ?? 'Região';
      case 'state': return `UF ${selected.stateCode}`;
    }
  }, [selected, farms, regions]);

  const value = useMemo<FarmScopeContextValue>(() => ({
    access, farms, regions, selected, scopedFarms, loading, error, setSelected, refresh, scopeLabel
  }), [access, farms, regions, selected, scopedFarms, loading, error, setSelected, refresh, scopeLabel]);

  return <FarmScopeContext.Provider value={value}>{children}</FarmScopeContext.Provider>;
}

export function useFarmScope() {
  const value = useContext(FarmScopeContext);
  if (!value) throw new Error('useFarmScope must be used inside FarmScopeProvider.');
  return value;
}
