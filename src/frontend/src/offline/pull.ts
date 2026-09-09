import { apiRequest } from '../lib/api';
import type { Crop, Farm, Field, Season } from '../lib/types';
import { offlineStore } from './indexedDbOfflineStore';
import {
  createOfflineNamespaceKey,
  createOfflineRecordKey
} from './namespace';
import type { OfflineStore } from './store';
import type {
  OfflineEntityKind,
  OfflineNamespace,
  OfflineRecord,
  OfflineSyncMetadata
} from './types';

const MAX_PULL_PAGES_PER_RUN = 20;
const PULL_PAGE_SIZE = 500;
const SUPPORTED_ENTITY_KINDS = new Set<OfflineEntityKind>(['farm', 'field', 'crop', 'season']);

export interface OfflinePullChangeResponse {
  sequence: number;
  entityKind: OfflineEntityKind;
  entityId: string;
  changeType: 'upsert' | 'delete' | string;
  payload: unknown;
  occurredAtUtc: string;
}

export interface OfflinePullResponse {
  farmId: string;
  changes: OfflinePullChangeResponse[];
  cursor: string;
  lastSequence: number;
  hasMore: boolean;
  serverTimeUtc: string;
}

export interface OfflinePullBatch {
  upserts: OfflineRecord[];
  deleteKeys: string[];
  metadata: OfflineSyncMetadata;
  nextFieldIds: Set<string>;
}

export interface OfflinePullResult {
  pages: number;
  changesApplied: number;
  hasMore: boolean;
  lastSyncedAtUtc: string;
}

function isObject(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function requireString(value: unknown, field: string) {
  if (typeof value !== 'string' || !value.trim()) {
    throw new Error(`Offline pull payload has invalid ${field}.`);
  }
  return value;
}

function requirePayloadIdentity(payload: unknown, entityId: string) {
  if (!isObject(payload) || requireString(payload.id, 'id') !== entityId) {
    throw new Error('Offline pull payload id does not match the change envelope.');
  }
  return payload;
}

function updatedAtUtc(payload: Record<string, unknown>) {
  return requireString(payload.updatedAtUtc, 'updatedAtUtc');
}

function cleanRecord(
  namespace: OfflineNamespace,
  namespaceKey: string,
  entityKind: OfflineEntityKind,
  entityId: string,
  payload: unknown,
  lastSyncedAtUtc: string
): OfflineRecord {
  const data = requirePayloadIdentity(payload, entityId);
  const serverVersion = updatedAtUtc(data);
  return {
    key: createOfflineRecordKey(namespace, entityKind, entityId),
    namespaceKey,
    entityKind,
    entityId,
    data,
    serverVersion,
    updatedAtUtc: serverVersion,
    syncState: 'Clean',
    lastSyncedAtUtc
  };
}

export function buildOfflinePullBatch(
  namespace: OfflineNamespace,
  response: OfflinePullResponse,
  existingFieldIds: ReadonlySet<string>,
  schemaVersion: number
): OfflinePullBatch {
  if (response.farmId !== namespace.farmId) {
    throw new Error('Offline pull farm does not match the requested namespace.');
  }
  if (!response.cursor?.trim()) {
    throw new Error('Offline pull did not return a valid synchronization cursor.');
  }
  if (!Number.isSafeInteger(response.lastSequence) || response.lastSequence < 0) {
    throw new Error('Offline pull returned an invalid sequence watermark.');
  }

  const namespaceKey = createOfflineNamespaceKey(namespace);
  const fieldIds = new Set(existingFieldIds);
  const upserts: OfflineRecord[] = [];
  const deleteKeys: string[] = [];
  let previousSequence = -1;

  for (const change of response.changes) {
    if (!Number.isSafeInteger(change.sequence) || change.sequence < 0 || change.sequence <= previousSequence) {
      throw new Error('Offline pull changes are not strictly ordered by server sequence.');
    }
    if (change.sequence > response.lastSequence) {
      throw new Error('Offline pull change exceeds the response watermark.');
    }
    previousSequence = change.sequence;

    if (!SUPPORTED_ENTITY_KINDS.has(change.entityKind)) {
      throw new Error(`Offline pull contains unsupported entity kind ${change.entityKind}.`);
    }
    requireString(change.entityId, 'entityId');

    const key = createOfflineRecordKey(namespace, change.entityKind, change.entityId);
    if (change.changeType === 'delete') {
      if (change.entityKind === 'farm' && change.entityId === namespace.farmId) {
        throw new Error('Offline pull cannot delete the prepared farm while access is still valid.');
      }
      if (change.entityKind === 'field') fieldIds.delete(change.entityId);
      deleteKeys.push(key);
      continue;
    }

    if (change.changeType !== 'upsert') {
      throw new Error(`Offline pull contains unsupported change type ${change.changeType}.`);
    }

    const payload = requirePayloadIdentity(change.payload, change.entityId);
    if (change.entityKind === 'farm') {
      if (change.entityId !== namespace.farmId) {
        throw new Error('Offline pull contains a farm outside the requested namespace.');
      }
    } else if (change.entityKind === 'field') {
      if (requireString(payload.farmId, 'farmId') !== namespace.farmId) {
        throw new Error('Offline pull contains a field from another farm.');
      }
      fieldIds.add(change.entityId);
    } else if (change.entityKind === 'season') {
      const fieldId = requireString(payload.fieldId, 'fieldId');
      if (!fieldIds.has(fieldId)) {
        throw new Error('Offline pull contains a season outside the selected farm.');
      }
      requireString(payload.cropId, 'cropId');
    }

    upserts.push(cleanRecord(
      namespace,
      namespaceKey,
      change.entityKind,
      change.entityId,
      change.payload,
      response.serverTimeUtc
    ));
  }

  return {
    upserts,
    deleteKeys,
    nextFieldIds: fieldIds,
    metadata: {
      namespaceKey,
      cursor: response.cursor,
      lastSyncedAtUtc: response.serverTimeUtc,
      preparationState: 'Ready',
      schemaVersion,
      lastError: null
    }
  };
}

export async function pullFarmOffline(
  namespace: OfflineNamespace,
  store: OfflineStore = offlineStore
): Promise<OfflinePullResult> {
  const namespaceKey = createOfflineNamespaceKey(namespace);
  await store.initialize();

  const metadata = await store.getSyncMetadata(namespaceKey);
  if (!metadata || metadata.preparationState !== 'Ready' || !metadata.cursor) {
    throw new Error('Farm is not prepared for incremental offline synchronization.');
  }

  const pending = await store.listMutations(namespaceKey);
  if (pending.length > 0) {
    throw new Error('Pending local mutations must be pushed before pulling server changes.');
  }

  const existingFields = await store.listRecords<Field>(namespaceKey, 'field');
  let fieldIds = new Set(existingFields.map((record) => record.entityId));
  let cursor = metadata.cursor;
  let pages = 0;
  let changesApplied = 0;
  let hasMore = false;
  let lastSyncedAtUtc = metadata.lastSyncedAtUtc ?? new Date(0).toISOString();

  do {
    const response = await apiRequest<OfflinePullResponse>(
      `/api/v1/sync/pull?farmId=${encodeURIComponent(namespace.farmId)}` +
      `&cursor=${encodeURIComponent(cursor)}&take=${PULL_PAGE_SIZE}`
    );
    const batch = buildOfflinePullBatch(namespace, response, fieldIds, metadata.schemaVersion);
    await store.applyCleanServerChanges(
      namespaceKey,
      batch.upserts,
      batch.deleteKeys,
      batch.metadata
    );

    fieldIds = batch.nextFieldIds;
    cursor = response.cursor;
    hasMore = response.hasMore;
    lastSyncedAtUtc = response.serverTimeUtc;
    changesApplied += response.changes.length;
    pages += 1;
  } while (hasMore && pages < MAX_PULL_PAGES_PER_RUN);

  return {
    pages,
    changesApplied,
    hasMore,
    lastSyncedAtUtc
  };
}

export type OfflineFarmPayload = Farm;
export type OfflineFieldPayload = Field;
export type OfflineCropPayload = Crop;
export type OfflineSeasonPayload = Season;
