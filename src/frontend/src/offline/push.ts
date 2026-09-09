import { ApiError, apiRequest } from '../lib/api';
import { offlineStore } from './indexedDbOfflineStore';
import { createOfflineNamespaceKey, createOfflineRecordKey } from './namespace';
import type { OfflineStore } from './store';
import type { OfflineMutation, OfflineNamespace, OfflineRecord } from './types';

const PUSH_BATCH_SIZE = 100;
type OfflinePushTransport = typeof apiRequest;

interface OfflinePushOperationResultResponse {
  operationId: string;
  entityKind: string;
  entityId: string;
  status: 'Applied' | 'RetryableError' | 'Forbidden' | 'ValidationError' | 'Conflict' | 'NotFound' | string;
  serverVersion: string | null;
  serverEntity: unknown;
  errorCode: string | null;
  message: string | null;
  replayed: boolean;
}

interface OfflinePushResponse {
  farmId: string;
  results: OfflinePushOperationResultResponse[];
  serverTimeUtc: string;
}

export interface OfflinePushResult {
  attempted: number;
  applied: number;
  retryable: number;
  conflicts: number;
  failed: number;
  blocked: boolean;
  blockedReason: string | null;
}

function isObject(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function serverEntityIdentity(value: unknown, expectedId: string) {
  if (!isObject(value) || value.id !== expectedId || typeof value.updatedAtUtc !== 'string' || !value.updatedAtUtc) {
    throw new Error('Offline push returned an invalid server entity payload.');
  }
  return value;
}

async function markRetryable(
  mutation: OfflineMutation,
  message: string,
  store: OfflineStore
) {
  await store.putMutation({
    ...mutation,
    attempts: mutation.attempts + 1,
    state: 'Retryable',
    lastError: message
  });
}

async function applyAck(
  namespace: OfflineNamespace,
  mutation: OfflineMutation,
  result: OfflinePushOperationResultResponse,
  serverTimeUtc: string,
  store: OfflineStore
) {
  const key = createOfflineRecordKey(namespace, mutation.entityKind, mutation.entityId);
  const current = await store.getRecord(key);
  if (!current) {
    throw new Error('Server acknowledged a mutation whose local record was unexpectedly missing.');
  }

  if (result.serverEntity === null || result.serverEntity === undefined) {
    await store.deleteRecord(key);
    await store.deleteMutation(mutation.operationId);
    return;
  }

  const serverEntity = serverEntityIdentity(result.serverEntity, mutation.entityId);
  const updatedAtUtc = serverEntity.updatedAtUtc as string;
  const clean: OfflineRecord = {
    key,
    namespaceKey: mutation.namespaceKey,
    entityKind: mutation.entityKind,
    entityId: mutation.entityId,
    data: serverEntity,
    serverVersion: result.serverVersion ?? updatedAtUtc,
    updatedAtUtc,
    syncState: 'Clean',
    lastSyncedAtUtc: serverTimeUtc,
    conflict: null
  };
  await store.putRecord(clean);
  await store.deleteMutation(mutation.operationId);
}

async function applyConflict(
  namespace: OfflineNamespace,
  mutation: OfflineMutation,
  result: OfflinePushOperationResultResponse,
  store: OfflineStore
) {
  const key = createOfflineRecordKey(namespace, mutation.entityKind, mutation.entityId);
  const current = await store.getRecord(key);
  const message = result.message ?? 'The server record changed while this device was offline.';

  await store.putMutation({
    ...mutation,
    attempts: mutation.attempts + 1,
    state: 'Conflict',
    lastError: message
  });

  if (current) {
    await store.putRecord({
      ...current,
      syncState: 'Conflict',
      conflict: {
        operationId: mutation.operationId,
        serverVersion: result.serverVersion,
        serverData: result.serverEntity,
        message
      }
    });
  }
}

async function applyFailure(
  namespace: OfflineNamespace,
  mutation: OfflineMutation,
  result: OfflinePushOperationResultResponse,
  store: OfflineStore
) {
  const message = result.message ?? 'Offline mutation was rejected by the server.';
  await store.putMutation({
    ...mutation,
    attempts: mutation.attempts + 1,
    state: 'Failed',
    lastError: message
  });
  const key = createOfflineRecordKey(namespace, mutation.entityKind, mutation.entityId);
  const current = await store.getRecord(key);
  if (current) {
    await store.putRecord({ ...current, syncState: 'Failed', conflict: null });
  }
}

export async function pushFarmOffline(
  namespace: OfflineNamespace,
  store: OfflineStore = offlineStore,
  request: OfflinePushTransport = apiRequest
): Promise<OfflinePushResult> {
  const namespaceKey = createOfflineNamespaceKey(namespace);
  await store.initialize();
  const metadata = await store.getSyncMetadata(namespaceKey);
  if (!metadata || metadata.preparationState !== 'Ready') {
    throw new Error('Farm is not prepared for offline synchronization.');
  }

  const allMutations = await store.listMutations(namespaceKey);
  const candidates = allMutations
    .filter(item => item.state === 'Pending' || item.state === 'Retryable')
    .slice(0, PUSH_BATCH_SIZE);

  if (candidates.length === 0) {
    return {
      attempted: 0,
      applied: 0,
      retryable: 0,
      conflicts: allMutations.filter(item => item.state === 'Conflict').length,
      failed: allMutations.filter(item => item.state === 'Failed').length,
      blocked: false,
      blockedReason: null
    };
  }

  let response: OfflinePushResponse;
  try {
    response = await request<OfflinePushResponse>('/api/v1/sync/push', {
      method: 'POST',
      body: JSON.stringify({
        farmId: namespace.farmId,
        operations: candidates.map(item => ({
          operationId: item.operationId,
          entityKind: item.entityKind,
          entityId: item.entityId,
          operation: item.operation,
          baseServerVersion: item.baseServerVersion,
          payload: item.payload
        }))
      })
    });
  } catch (error) {
    if (error instanceof ApiError && (error.status === 401 || error.status === 403 || error.status === 404)) {
      const reason = error.message || 'Offline farm access is no longer available.';
      await store.blockAndPurgeNamespace(namespaceKey, reason);
      return {
        attempted: candidates.length,
        applied: 0,
        retryable: 0,
        conflicts: 0,
        failed: 0,
        blocked: true,
        blockedReason: reason
      };
    }

    const message = error instanceof Error ? error.message : 'Network failure while pushing offline changes.';
    for (const mutation of candidates) await markRetryable(mutation, message, store);
    return {
      attempted: candidates.length,
      applied: 0,
      retryable: candidates.length,
      conflicts: 0,
      failed: 0,
      blocked: false,
      blockedReason: null
    };
  }

  if (response.farmId !== namespace.farmId) {
    throw new Error('Offline push response farm does not match the local namespace.');
  }

  const byOperationId = new Map(candidates.map(item => [item.operationId, item]));
  const seen = new Set<string>();
  let applied = 0;
  let retryable = 0;
  let conflicts = 0;
  let failed = 0;

  for (const result of response.results) {
    const mutation = byOperationId.get(result.operationId);
    if (!mutation || seen.has(result.operationId)) {
      throw new Error('Offline push response contains an unknown or duplicate operationId.');
    }
    if (result.entityId !== mutation.entityId || result.entityKind.toLowerCase() !== mutation.entityKind.toLowerCase()) {
      throw new Error('Offline push response changed an operation entity identity.');
    }
    seen.add(result.operationId);

    if (result.status === 'Applied') {
      await applyAck(namespace, mutation, result, response.serverTimeUtc, store);
      applied += 1;
      continue;
    }
    if (result.status === 'RetryableError') {
      await markRetryable(mutation, result.message ?? 'Server asked this operation to be retried.', store);
      retryable += 1;
      continue;
    }
    if (result.status === 'Conflict' && result.errorCode !== 'OperationIdReuse') {
      await applyConflict(namespace, mutation, result, store);
      conflicts += 1;
      continue;
    }
    if (result.status === 'Forbidden' && result.errorCode === 'FarmAccessRevoked') {
      const reason = result.message ?? 'Offline farm access was revoked.';
      await store.blockAndPurgeNamespace(namespaceKey, reason);
      return {
        attempted: candidates.length,
        applied,
        retryable,
        conflicts,
        failed,
        blocked: true,
        blockedReason: reason
      };
    }

    await applyFailure(namespace, mutation, result, store);
    failed += 1;
  }

  for (const mutation of candidates) {
    if (!seen.has(mutation.operationId)) {
      await markRetryable(mutation, 'Server response did not acknowledge this operation.', store);
      retryable += 1;
    }
  }

  return {
    attempted: candidates.length,
    applied,
    retryable,
    conflicts,
    failed,
    blocked: false,
    blockedReason: null
  };
}
