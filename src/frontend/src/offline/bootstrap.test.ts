import { describe, expect, it } from 'vitest';
import { buildOfflineBootstrapSnapshot } from './bootstrap';
import type { OfflineBootstrapResponse } from './bootstrap';

const namespace = {
  userId: 'user-1',
  organizationId: 'org-1',
  farmId: 'farm-1'
};

function response(): OfflineBootstrapResponse {
  return {
    farm: {
      id: 'farm-1',
      operationalRegionId: null,
      name: 'Fazenda A',
      totalAreaHectares: 100,
      city: 'Campinas',
      state: 'SP',
      countryCode: 'BR',
      stateCode: 'SP',
      municipalityCode: null,
      postalCode: null,
      latitude: null,
      longitude: null,
      timeZoneId: 'America/Sao_Paulo',
      isActive: true,
      createdAtUtc: '2026-09-09T00:00:00Z',
      updatedAtUtc: '2026-09-09T00:00:00Z'
    },
    fields: [{
      id: 'field-1',
      farmId: 'farm-1',
      name: 'Talhão 1',
      areaHectares: 25,
      isActive: true,
      createdAtUtc: '2026-09-09T00:00:00Z',
      updatedAtUtc: '2026-09-09T00:00:00Z'
    }],
    crops: [{
      id: 'crop-1',
      name: 'Soja',
      variety: null,
      isActive: true,
      createdAtUtc: '2026-09-09T00:00:00Z',
      updatedAtUtc: '2026-09-09T00:00:00Z'
    }],
    seasons: [{
      id: 'season-1',
      fieldId: 'field-1',
      cropId: 'crop-1',
      name: 'Safra 26/27',
      startDate: '2026-09-01',
      endDate: null,
      expectedYieldPerHectare: 4,
      actualYieldPerHectare: null,
      status: 'Planned',
      isActive: true,
      createdAtUtc: '2026-09-09T00:00:00Z',
      updatedAtUtc: '2026-09-09T00:00:00Z'
    }],
    protocolVersion: 1,
    localSchemaVersion: 1,
    serverTimeUtc: '2026-09-09T01:00:00Z'
  };
}

describe('offline bootstrap snapshot', () => {
  it('creates clean namespaced records for one farm', () => {
    const snapshot = buildOfflineBootstrapSnapshot(namespace, response());

    expect(snapshot.namespaceKey).toBe('v1|user-1|org-1|farm-1');
    expect(snapshot.records).toHaveLength(4);
    expect(snapshot.records.every((item) => item.namespaceKey === snapshot.namespaceKey)).toBe(true);
    expect(snapshot.records.every((item) => item.syncState === 'Clean')).toBe(true);
    expect(snapshot.metadata.preparationState).toBe('Ready');
    expect(snapshot.metadata.lastSyncedAtUtc).toBe('2026-09-09T01:00:00Z');
  });

  it('rejects a field from another farm', () => {
    const payload = response();
    payload.fields[0] = { ...payload.fields[0], farmId: 'farm-2' };

    expect(() => buildOfflineBootstrapSnapshot(namespace, payload)).toThrow(/another farm/);
  });

  it('rejects a season whose field is not in the selected farm payload', () => {
    const payload = response();
    payload.seasons[0] = { ...payload.seasons[0], fieldId: 'field-2' };

    expect(() => buildOfflineBootstrapSnapshot(namespace, payload)).toThrow(/season outside/);
  });

  it('rejects unrelated or missing crops', () => {
    const unrelated = response();
    unrelated.crops.push({ ...unrelated.crops[0], id: 'crop-2' });
    expect(() => buildOfflineBootstrapSnapshot(namespace, unrelated)).toThrow(/unrelated crop/);

    const missing = response();
    missing.crops = [];
    expect(() => buildOfflineBootstrapSnapshot(namespace, missing)).toThrow(/missing a crop/);
  });

  it('rejects a server schema newer or different from this client', () => {
    const payload = response();
    payload.localSchemaVersion = 2;

    expect(() => buildOfflineBootstrapSnapshot(namespace, payload)).toThrow(/not supported/);
  });
});
