import { offlineStore } from './indexedDbOfflineStore';
import { createOfflineNamespaceKey, createOfflineRecordKey } from './namespace';
import type { OfflineStore } from './store';
import type { OfflineMutation, OfflineNamespace, OfflineRecord } from './types';

function newId() {
  if (!globalThis.crypto?.randomUUID) {
    throw new Error('This runtime cannot generate secure UUIDs for offline operations.');
  }
  return globalThis.crypto.randomUUID();
}

function serverEntity(value: unknown, entityId: string) {
  if (typeof value !== 'object' || value === null) {
    throw new Error('Conflict does not contain a server record that can be used.');
  }
  const record = value as Record<string, unknown>;
  if (record.id !== entityId || typeof record.updatedAtUtc !== 'string' || !record.updatedAtUtc) {
    throw new Error('Conflict server record identity is invalid.');
  }
  return record;
}

async function findConflictMutation(
  namespaceKey: string,
  operationId: string,
  store: OfflineStore
) {
  const mutations = await store.listMutations(namespaceKey);
  const mutation = mutations.find(item => item.operationId === operationId);
  if (!mutation || mutation.state !== 'Conflict') {
    throw new Error('Conflict mutation is no longer available.');
  }
  return mutation;
}

export async function resolveConflictUsingServer(
  namespace: OfflineNamespace,
  entityKind: 'field' | 'season',
  entityId: string,
  store: OfflineStore = offlineStore
) {
  const namespaceKey = createOfflineNamespaceKey(namespace);
  const key = createOfflineRecordKey(namespace, entityKind, entityId);
  const current = await store.getRecord(key);
  if (!current || current.syncState !== 'Conflict' || !current.conflict) {
    throw new Error('Offline record is not in conflict.');
  }
  const mutation = await findConflictMutation(namespaceKey, current.conflict.operationId, store);
  const data = serverEntity(current.conflict.serverData, entityId);
  const updatedAtUtc = data.updatedAtUtc as string;
  const clean: OfflineRecord = {
    key,
    namespaceKey,
    entityKind,
    entityId,
    data,
    serverVersion: current.conflict.serverVersion ?? updatedAtUtc,
    updatedAtUtc,
    syncState: 'Clean',
    lastSyncedAtUtc: new Date().toISOString(),
    conflict: null
  };
  await store.putRecord(clean);
  await store.deleteMutation(mutation.operationId);
  return clean;
}

export async function reapplyConflict(
  namespace: OfflineNamespace,
  entityKind: 'field' | 'season',
  entityId: string,
  store: OfflineStore = offlineStore
) {
  const namespaceKey = createOfflineNamespaceKey(namespace);
  const key = createOfflineRecordKey(namespace, entityKind, entityId);
  const current = await store.getRecord(key);
  if (!current || current.syncState !== 'Conflict' || !current.conflict) {
    throw new Error('Offline record is not in conflict.');
  }
  const previous = await findConflictMutation(namespaceKey, current.conflict.operationId, store);
  if (previous.operation === 'create') {
    throw new Error('A create conflict cannot be reapplied automatically. Review the record first.');
  }
  if (!current.conflict.serverVersion) {
    throw new Error('Conflict does not contain a server version for safe reapply.');
  }

  const replacement: OfflineMutation = {
    ...previous,
    operationId: newId(),
    baseServerVersion: current.conflict.serverVersion,
    createdAtUtc: new Date().toISOString(),
    attempts: 0,
    state: 'Pending',
    lastError: null
  };
  const record: OfflineRecord = {
    ...current,
    serverVersion: current.conflict.serverVersion,
    syncState: previous.operation === 'delete' ? 'PendingDelete' : 'PendingUpdate',
    conflict: null
  };
  await store.replaceMutation(previous.operationId, record, replacement);
  return record;
}
