import { describe, expect, it } from 'vitest';
import { createOfflineNamespaceKey, createOfflineRecordKey } from './namespace';
import {
  assertOfflineMutation,
  assertOfflineRecord,
  assertOfflineSyncMetadata
} from './validation';

const namespace = {
  userId: 'user-1',
  organizationId: 'org-1',
  farmId: 'farm-1'
};

const namespaceKey = createOfflineNamespaceKey(namespace);

describe('offline persistence validation', () => {
  it('accepts a record whose key matches namespace and entity identity', () => {
    expect(() => assertOfflineRecord({
      key: createOfflineRecordKey(namespace, 'field', 'field-1'),
      namespaceKey,
      entityKind: 'field',
      entityId: 'field-1',
      data: { name: 'Talhão 1' },
      serverVersion: '7',
      updatedAtUtc: '2026-09-09T00:00:00Z',
      syncState: 'Clean',
      lastSyncedAtUtc: '2026-09-09T00:00:00Z'
    })).not.toThrow();
  });

  it('rejects a record key forged for another identity', () => {
    expect(() => assertOfflineRecord({
      key: createOfflineRecordKey(namespace, 'field', 'field-2'),
      namespaceKey,
      entityKind: 'field',
      entityId: 'field-1',
      data: {},
      serverVersion: null,
      updatedAtUtc: '2026-09-09T00:00:00Z',
      syncState: 'Clean',
      lastSyncedAtUtc: null
    })).toThrow(/does not match/);
  });

  it('rejects delete mutations that keep a payload', () => {
    expect(() => assertOfflineMutation({
      operationId: 'op-1',
      namespaceKey,
      entityKind: 'field',
      entityId: 'field-1',
      operation: 'delete',
      payload: { stale: true },
      baseServerVersion: '4',
      createdAtUtc: '2026-09-09T00:00:00Z',
      attempts: 0,
      lastError: null
    })).toThrow(/must not persist a payload/);
  });

  it('rejects invalid retry counters and schema versions', () => {
    expect(() => assertOfflineMutation({
      operationId: 'op-2',
      namespaceKey,
      entityKind: 'field',
      entityId: 'field-1',
      operation: 'update',
      payload: {},
      baseServerVersion: '4',
      createdAtUtc: '2026-09-09T00:00:00Z',
      attempts: -1,
      lastError: null
    })).toThrow(/non-negative integer/);

    expect(() => assertOfflineSyncMetadata({
      namespaceKey,
      cursor: null,
      lastSyncedAtUtc: null,
      preparationState: 'NotPrepared',
      schemaVersion: 0,
      lastError: null
    })).toThrow(/positive integer/);
  });
});
