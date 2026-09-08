import { describe, expect, it } from 'vitest';
import { fallbackScope, filterFarmsByScope, isValidScope, parseStoredScope } from './farmScope';
import type { Farm, FarmAccessScope, OperationalRegion } from './types';

const farms: Farm[] = [
  {
    id: 'farm-sp', operationalRegionId: 'region-south-east', name: 'Horizonte', totalAreaHectares: 100,
    city: 'Barretos', state: 'SP', countryCode: 'BR', stateCode: 'SP', municipalityCode: null,
    postalCode: null, latitude: -20.5, longitude: -48.5, timeZoneId: 'America/Sao_Paulo',
    isActive: true, createdAtUtc: '', updatedAtUtc: ''
  },
  {
    id: 'farm-mt', operationalRegionId: 'region-center-west', name: 'Boa Safra', totalAreaHectares: 200,
    city: 'Sorriso', state: 'MT', countryCode: 'BR', stateCode: 'MT', municipalityCode: null,
    postalCode: null, latitude: -12.5, longitude: -55.7, timeZoneId: 'America/Cuiaba',
    isActive: true, createdAtUtc: '', updatedAtUtc: ''
  }
];

const regions: OperationalRegion[] = [
  { id: 'region-south-east', name: 'Sudeste', code: 'SE', description: null, isActive: true, createdAtUtc: '', updatedAtUtc: '' },
  { id: 'region-center-west', name: 'Centro-Oeste', code: 'CO', description: null, isActive: true, createdAtUtc: '', updatedAtUtc: '' }
];

describe('farm operational scope', () => {
  it('does not allow AllFarms when the effective access is restricted', () => {
    const access: FarmAccessScope = { userId: 'u1', allFarms: false, farmIds: ['farm-mt'], regionIds: [] };
    expect(isValidScope({ kind: 'all' }, access, [farms[1]], regions)).toBe(false);
    expect(fallbackScope(access, [farms[1]])).toEqual({ kind: 'farm', farmId: 'farm-mt' });
  });

  it('filters the same accessible list by UF, region and farm', () => {
    expect(filterFarmsByScope(farms, { kind: 'state', stateCode: 'sp' }).map(item => item.id)).toEqual(['farm-sp']);
    expect(filterFarmsByScope(farms, { kind: 'region', regionId: 'region-center-west' }).map(item => item.id)).toEqual(['farm-mt']);
    expect(filterFarmsByScope(farms, { kind: 'farm', farmId: 'farm-sp' }).map(item => item.id)).toEqual(['farm-sp']);
  });

  it('normalizes a persisted UF selector and rejects malformed session data', () => {
    expect(parseStoredScope('{"kind":"state","stateCode":"mt"}')).toEqual({ kind: 'state', stateCode: 'MT' });
    expect(parseStoredScope('{broken')).toBeNull();
  });
});
