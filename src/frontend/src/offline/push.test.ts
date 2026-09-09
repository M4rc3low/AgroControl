import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError, apiRequest } from '../lib/api';
import { createOfflineNamespaceKey, createOfflineRecordKey } from './namespace';
import { pushFarmOffline } from './push';
import type { OfflineStore } from './store';
import type { OfflineMutation, OfflineNamespace, OfflineRecord, OfflineSyncMetadata } from './types';

vi.mock('../lib/api', async importOriginal => {
  const actual = await importOriginal<typeof import('../lib/api')>();
  return { ...actual, apiRequest: vi.fn() };
});

const mockedApiRequest = vi.mocked(apiRequest);
const namespace: OfflineNamespace = { userId: 'user-1', organizationId: 'org-1', farmId: 'farm-a' };
const namespaceKey = createOfflineNamespaceKey(namespace);

class MemoryOfflineStore implements OfflineStore {
  records = new Map<string, OfflineRecord>();
  mutations = new Map<string, OfflineMutation>();
  metadata = new Map<string, OfflineSyncMetadata>();

  async initialize() {}
  async putRecord<T>(record: OfflineRecord<T>) { this.records.set(record.key, record as OfflineRecord); }
  async getRecord<T>(key: string) { return (this.records.get(key) as OfflineRecord<T> | undefined) ?? null; }
  async listRecords<T>(target: string, entityKind?: string) {
    return [...this.records.values()].filter(item => item.namespaceKey === target && (!entityKind || item.entityKind === entityKind)) as OfflineRecord<T>[];
  }
  async deleteRecord(key: string) { this.records.delete(key); }
  async enqueueMutation<T>(mutation: OfflineMutation<T>) { this.mutations.set(mutation.operationId, mutation as OfflineMutation); }
  async listMutations<T>(target: string) { return [...this.mutations.values()].filter(item => item.namespaceKey === target) as OfflineMutation<T>[]; }
  async putMutation<T>(mutation: OfflineMutation<T>) { this.mutations.set(mutation.operationId, mutation as OfflineMutation); }
  async deleteMutation(operationId: string) { this.mutations.delete(operationId); }
  async stageMutation<TRecord, TPayload>(record: OfflineRecord<TRecord>, mutation: OfflineMutation<TPayload>) {
    await this.putRecord(record); await this.enqueueMutation(mutation);
  }
  async replaceMutation<TRecord, TPayload>(previousOperationId: string, record: OfflineRecord<TRecord>, mutation: OfflineMutation<TPayload>) {
    this.mutations.delete(previousOperationId); await this.putRecord(record); await this.enqueueMutation(mutation);
  }
  async getSyncMetadata(target: string) { return this.metadata.get(target) ?? null; }
  async putSyncMetadata(value: OfflineSyncMetadata) { this.metadata.set(value.namespaceKey, value); }
  async replaceCleanSnapshot(_target: string, records: OfflineRecord[], metadata: OfflineSyncMetadata) {
    for (const record of records) this.records.set(record.key, record); this.metadata.set(metadata.namespaceKey, metadata);
  }
  async applyCleanServerChanges(_target: string, upserts: OfflineRecord[], deleteKeys: string[], metadata: OfflineSyncMetadata) {
    for (const key of deleteKeys) this.records.delete(key); for (const record of upserts) this.records.set(record.key, record); this.metadata.set(metadata.namespaceKey, metadata);
  }
  async clearNamespace(target: string) {
    for (const [key, record] of this.records) if (record.namespaceKey === target) this.records.delete(key);
    for (const [key, mutation] of this.mutations) if (mutation.namespaceKey === target) this.mutations.delete(key);
    this.metadata.delete(target);
  }
  async blockAndPurgeNamespace(target: string, reason: string) {
    await this.clearNamespace(target);
    this.metadata.set(target, { namespaceKey: target, cursor: null, lastSyncedAtUtc: null, preparationState: 'Blocked', schemaVersion: 1, lastError: reason });
  }
  async clearAll() { this.records.clear(); this.mutations.clear(); this.metadata.clear(); }
}

function readyStore() {
  const store = new MemoryOfflineStore();
  store.metadata.set(namespaceKey, {
    namespaceKey,
    cursor: 'cursor-1',
    lastSyncedAtUtc: '2026-09-09T01:00:00Z',
    preparationState: 'Ready',
    schemaVersion: 1,
    lastError: null
  });
  return store;
}

function fieldRecord(state: OfflineRecord['syncState'], serverVersion: string | null = null): OfflineRecord {
  return {
    key: createOfflineRecordKey(namespace, 'field', 'field-1'),
    namespaceKey,
    entityKind: 'field',
    entityId: 'field-1',
    data: { id: 'field-1', farmId: 'farm-a', name: 'Local', areaHectares: 10, isActive: true, createdAtUtc: '2026-09-09T00:00:00Z', updatedAtUtc: '2026-09-09T01:00:00Z' },
    serverVersion,
    updatedAtUtc: '2026-09-09T01:00:00Z',
    syncState: state,
    lastSyncedAtUtc: '2026-09-09T01:00:00Z',
    conflict: null
  };
}

function mutation(operation: 'create' | 'update' = 'update'): OfflineMutation {
  return {
    operationId: 'op-1', namespaceKey, entityKind: 'field', entityId: 'field-1', operation,
    payload: { farmId: 'farm-a', name: 'Local', areaHectares: 10 },
    baseServerVersion: operation === 'create' ? null : '2026-09-09T01:00:00.0000000Z',
    createdAtUtc: '2026-09-09T01:05:00Z', attempts: 0, state: 'Pending', lastError: null
  };
}

beforeEach(() => mockedApiRequest.mockReset());

describe('offline push client', () => {
  it('removes the outbox entry only after an Applied ACK and stores the clean server snapshot', async () => {
    const store = readyStore();
    await store.putRecord(fieldRecord('PendingCreate'));
    await store.enqueueMutation(mutation('create'));
    mockedApiRequest.mockResolvedValue({
      farmId: 'farm-a', serverTimeUtc: '2026-09-09T02:00:00Z',
      results: [{ operationId: 'op-1', entityKind: 'field', entityId: 'field-1', status: 'Applied', serverVersion: '2026-09-09T02:00:00.0000000Z', serverEntity: { id: 'field-1', farmId: 'farm-a', name: 'Servidor', areaHectares: 10, isActive: true, createdAtUtc: '2026-09-09T02:00:00Z', updatedAtUtc: '2026-09-09T02:00:00Z' }, errorCode: null, message: null, replayed: false }]
    });

    const result = await pushFarmOffline(namespace, store);

    expect(result.applied).toBe(1);
    expect(await store.listMutations(namespaceKey)).toHaveLength(0);
    const record = await store.getRecord(createOfflineRecordKey(namespace, 'field', 'field-1'));
    expect(record?.syncState).toBe('Clean');
    expect((record?.data as { name: string }).name).toBe('Servidor');
  });

  it('keeps a network-failed operation retryable instead of losing it', async () => {
    const store = readyStore();
    await store.putRecord(fieldRecord('PendingUpdate', '2026-09-09T01:00:00.0000000Z'));
    await store.enqueueMutation(mutation());
    mockedApiRequest.mockRejectedValue(new TypeError('network down'));

    const result = await pushFarmOffline(namespace, store);

    expect(result.retryable).toBe(1);
    const queued = await store.listMutations(namespaceKey);
    expect(queued).toHaveLength(1);
    expect(queued[0].state).toBe('Retryable');
    expect(queued[0].attempts).toBe(1);
  });

  it('preserves the local proposal and server snapshot on an explicit conflict', async () => {
    const store = readyStore();
    await store.putRecord(fieldRecord('PendingUpdate', '2026-09-09T01:00:00.0000000Z'));
    await store.enqueueMutation(mutation());
    mockedApiRequest.mockResolvedValue({
      farmId: 'farm-a', serverTimeUtc: '2026-09-09T02:00:00Z',
      results: [{ operationId: 'op-1', entityKind: 'field', entityId: 'field-1', status: 'Conflict', serverVersion: '2026-09-09T01:30:00.0000000Z', serverEntity: { id: 'field-1', farmId: 'farm-a', name: 'Alterado no servidor', updatedAtUtc: '2026-09-09T01:30:00Z' }, errorCode: 'VersionConflict', message: 'changed', replayed: false }]
    });

    const result = await pushFarmOffline(namespace, store);

    expect(result.conflicts).toBe(1);
    expect((await store.listMutations(namespaceKey))[0].state).toBe('Conflict');
    const record = await store.getRecord(createOfflineRecordKey(namespace, 'field', 'field-1'));
    expect(record?.syncState).toBe('Conflict');
    expect(record?.conflict?.serverVersion).toBe('2026-09-09T01:30:00.0000000Z');
  });

  it('purges records and outbox and blocks the namespace when farm access is revoked', async () => {
    const store = readyStore();
    await store.putRecord(fieldRecord('PendingUpdate', '2026-09-09T01:00:00.0000000Z'));
    await store.enqueueMutation(mutation());
    mockedApiRequest.mockRejectedValue(new ApiError(403, 'revoked'));

    const result = await pushFarmOffline(namespace, store);

    expect(result.blocked).toBe(true);
    expect(await store.listRecords(namespaceKey)).toHaveLength(0);
    expect(await store.listMutations(namespaceKey)).toHaveLength(0);
    expect((await store.getSyncMetadata(namespaceKey))?.preparationState).toBe('Blocked');
  });
});
