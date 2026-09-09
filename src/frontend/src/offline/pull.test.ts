import { describe, expect, it } from 'vitest';
import { buildOfflinePullBatch } from './pull';
import type { OfflinePullResponse } from './pull';

const namespace = {
  userId: 'user-1',
  organizationId: 'org-1',
  farmId: 'farm-a'
};

function response(changes: OfflinePullResponse['changes']): OfflinePullResponse {
  return {
    farmId: 'farm-a',
    changes,
    cursor: 'next-cursor',
    lastSequence: changes.at(-1)?.sequence ?? 10,
    hasMore: false,
    serverTimeUtc: '2026-09-09T01:30:00Z'
  };
}

const fieldPayload = {
  id: 'field-1',
  farmId: 'farm-a',
  name: 'Talhão 1',
  areaHectares: 10,
  isActive: true,
  createdAtUtc: '2026-09-09T00:00:00Z',
  updatedAtUtc: '2026-09-09T01:00:00Z'
};

const seasonPayload = {
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
  updatedAtUtc: '2026-09-09T01:00:00Z'
};

describe('offline incremental pull', () => {
  it('builds clean records and advances metadata cursor', () => {
    const payload = response([
      {
        sequence: 11,
        entityKind: 'field',
        entityId: 'field-1',
        changeType: 'upsert',
        payload: fieldPayload,
        occurredAtUtc: '2026-09-09T01:00:00Z'
      },
      {
        sequence: 12,
        entityKind: 'season',
        entityId: 'season-1',
        changeType: 'upsert',
        payload: seasonPayload,
        occurredAtUtc: '2026-09-09T01:01:00Z'
      }
    ]);

    const batch = buildOfflinePullBatch(namespace, payload, new Set(), 1);

    expect(batch.upserts).toHaveLength(2);
    expect(batch.deleteKeys).toHaveLength(0);
    expect(batch.upserts.every((record) => record.syncState === 'Clean')).toBe(true);
    expect(batch.metadata.cursor).toBe('next-cursor');
    expect(batch.nextFieldIds.has('field-1')).toBe(true);
  });

  it('rejects a field payload from another farm', () => {
    const payload = response([{
      sequence: 11,
      entityKind: 'field',
      entityId: 'field-1',
      changeType: 'upsert',
      payload: { ...fieldPayload, farmId: 'farm-b' },
      occurredAtUtc: '2026-09-09T01:00:00Z'
    }]);

    expect(() => buildOfflinePullBatch(namespace, payload, new Set(), 1)).toThrow(/another farm/);
  });

  it('rejects a season after its field was removed from the namespace', () => {
    const payload = response([
      {
        sequence: 11,
        entityKind: 'field',
        entityId: 'field-1',
        changeType: 'delete',
        payload: null,
        occurredAtUtc: '2026-09-09T01:00:00Z'
      },
      {
        sequence: 12,
        entityKind: 'season',
        entityId: 'season-1',
        changeType: 'upsert',
        payload: seasonPayload,
        occurredAtUtc: '2026-09-09T01:01:00Z'
      }
    ]);

    expect(() => buildOfflinePullBatch(namespace, payload, new Set(['field-1']), 1)).toThrow(/season outside/);
  });

  it('rejects duplicate or out-of-order server sequences', () => {
    const payload = response([
      {
        sequence: 12,
        entityKind: 'field',
        entityId: 'field-1',
        changeType: 'upsert',
        payload: fieldPayload,
        occurredAtUtc: '2026-09-09T01:00:00Z'
      },
      {
        sequence: 12,
        entityKind: 'field',
        entityId: 'field-2',
        changeType: 'delete',
        payload: null,
        occurredAtUtc: '2026-09-09T01:01:00Z'
      }
    ]);

    expect(() => buildOfflinePullBatch(namespace, payload, new Set(), 1)).toThrow(/strictly ordered/);
  });
});
