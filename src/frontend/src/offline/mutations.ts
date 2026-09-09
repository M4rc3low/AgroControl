import type { Field, Season } from '../lib/types';
import { offlineStore } from './indexedDbOfflineStore';
import { createOfflineNamespaceKey, createOfflineRecordKey } from './namespace';
import type { OfflineStore } from './store';
import type { OfflineMutation, OfflineNamespace, OfflineRecord } from './types';

export interface OfflineFieldInput {
  name: string;
  areaHectares: number;
}

export interface OfflineSeasonCreateInput {
  fieldId: string;
  cropId: string;
  name: string;
  startDate: string;
  endDate: string | null;
  expectedYieldPerHectare: number | null;
}

export interface OfflineSeasonUpdateInput extends OfflineSeasonCreateInput {
  actualYieldPerHectare: number | null;
  status: string;
}

function newId() {
  if (!globalThis.crypto?.randomUUID) {
    throw new Error('This runtime cannot generate secure UUIDs for offline operations.');
  }
  return globalThis.crypto.randomUUID();
}

function nowUtc() {
  return new Date().toISOString();
}

function requirePrepared(metadata: Awaited<ReturnType<OfflineStore['getSyncMetadata']>>) {
  if (!metadata || metadata.preparationState !== 'Ready') {
    throw new Error('Prepare this farm for offline use before creating local changes.');
  }
  return metadata;
}

async function currentRecord<T>(
  namespace: OfflineNamespace,
  entityKind: 'field' | 'season',
  entityId: string,
  store: OfflineStore
) {
  const record = await store.getRecord<T>(createOfflineRecordKey(namespace, entityKind, entityId));
  if (!record) throw new Error(`${entityKind} is not available in this offline farm copy.`);
  if (record.syncState !== 'Clean') {
    throw new Error('Synchronize or resolve the existing local change before editing this record again.');
  }
  if (!record.serverVersion) {
    throw new Error('The offline record does not contain a server version for concurrency control.');
  }
  return record;
}

function mutation<T>(
  namespaceKey: string,
  entityKind: 'field' | 'season',
  entityId: string,
  operation: 'create' | 'update' | 'delete',
  payload: T | null,
  baseServerVersion: string | null,
  createdAtUtc: string
): OfflineMutation<T> {
  return {
    operationId: newId(),
    namespaceKey,
    entityKind,
    entityId,
    operation,
    payload,
    baseServerVersion,
    createdAtUtc,
    attempts: 0,
    state: 'Pending',
    lastError: null
  };
}

export async function createFieldOffline(
  namespace: OfflineNamespace,
  input: OfflineFieldInput,
  store: OfflineStore = offlineStore
) {
  if (!input.name.trim() || !Number.isFinite(input.areaHectares) || input.areaHectares <= 0) {
    throw new Error('Field name and a positive area are required.');
  }
  await store.initialize();
  const namespaceKey = createOfflineNamespaceKey(namespace);
  const metadata = requirePrepared(await store.getSyncMetadata(namespaceKey));
  const timestamp = nowUtc();
  const entityId = newId();
  const data: Field = {
    id: entityId,
    farmId: namespace.farmId,
    name: input.name.trim(),
    areaHectares: input.areaHectares,
    isActive: true,
    createdAtUtc: timestamp,
    updatedAtUtc: timestamp
  };
  const record: OfflineRecord<Field> = {
    key: createOfflineRecordKey(namespace, 'field', entityId),
    namespaceKey,
    entityKind: 'field',
    entityId,
    data,
    serverVersion: null,
    updatedAtUtc: timestamp,
    syncState: 'PendingCreate',
    lastSyncedAtUtc: metadata.lastSyncedAtUtc,
    conflict: null
  };
  await store.stageMutation(record, mutation(
    namespaceKey,
    'field',
    entityId,
    'create',
    { farmId: namespace.farmId, name: data.name, areaHectares: data.areaHectares },
    null,
    timestamp
  ));
  return record;
}

export async function updateFieldOffline(
  namespace: OfflineNamespace,
  fieldId: string,
  input: OfflineFieldInput,
  store: OfflineStore = offlineStore
) {
  if (!input.name.trim() || !Number.isFinite(input.areaHectares) || input.areaHectares <= 0) {
    throw new Error('Field name and a positive area are required.');
  }
  await store.initialize();
  const namespaceKey = createOfflineNamespaceKey(namespace);
  requirePrepared(await store.getSyncMetadata(namespaceKey));
  const current = await currentRecord<Field>(namespace, 'field', fieldId, store);
  if (current.data.farmId !== namespace.farmId) throw new Error('Field belongs to another farm.');
  const timestamp = nowUtc();
  const data: Field = {
    ...current.data,
    name: input.name.trim(),
    areaHectares: input.areaHectares,
    updatedAtUtc: timestamp
  };
  const record: OfflineRecord<Field> = {
    ...current,
    data,
    updatedAtUtc: timestamp,
    syncState: 'PendingUpdate',
    conflict: null
  };
  await store.stageMutation(record, mutation(
    namespaceKey,
    'field',
    fieldId,
    'update',
    { farmId: namespace.farmId, name: data.name, areaHectares: data.areaHectares },
    current.serverVersion,
    timestamp
  ));
  return record;
}

export async function deleteFieldOffline(
  namespace: OfflineNamespace,
  fieldId: string,
  store: OfflineStore = offlineStore
) {
  await store.initialize();
  const namespaceKey = createOfflineNamespaceKey(namespace);
  requirePrepared(await store.getSyncMetadata(namespaceKey));
  const current = await currentRecord<Field>(namespace, 'field', fieldId, store);
  if (current.data.farmId !== namespace.farmId) throw new Error('Field belongs to another farm.');
  const timestamp = nowUtc();
  const record: OfflineRecord<Field> = {
    ...current,
    updatedAtUtc: timestamp,
    syncState: 'PendingDelete',
    conflict: null
  };
  await store.stageMutation(record, mutation(
    namespaceKey,
    'field',
    fieldId,
    'delete',
    null,
    current.serverVersion,
    timestamp
  ));
  return record;
}

export async function createSeasonOffline(
  namespace: OfflineNamespace,
  input: OfflineSeasonCreateInput,
  store: OfflineStore = offlineStore
) {
  if (!input.name.trim() || !input.fieldId || !input.cropId || !input.startDate) {
    throw new Error('Season field, crop, name and start date are required.');
  }
  await store.initialize();
  const namespaceKey = createOfflineNamespaceKey(namespace);
  const metadata = requirePrepared(await store.getSyncMetadata(namespaceKey));
  const field = await store.getRecord<Field>(createOfflineRecordKey(namespace, 'field', input.fieldId));
  if (!field || field.data.farmId !== namespace.farmId || field.syncState === 'PendingDelete') {
    throw new Error('Season must reference an active field available in this offline farm.');
  }
  const crop = await store.getRecord(createOfflineRecordKey(namespace, 'crop', input.cropId));
  if (!crop) throw new Error('Season crop is not available in this offline farm.');

  const timestamp = nowUtc();
  const entityId = newId();
  const data: Season = {
    id: entityId,
    fieldId: input.fieldId,
    cropId: input.cropId,
    name: input.name.trim(),
    startDate: input.startDate,
    endDate: input.endDate,
    expectedYieldPerHectare: input.expectedYieldPerHectare,
    actualYieldPerHectare: null,
    status: 'Planned',
    isActive: true,
    createdAtUtc: timestamp,
    updatedAtUtc: timestamp
  };
  const record: OfflineRecord<Season> = {
    key: createOfflineRecordKey(namespace, 'season', entityId),
    namespaceKey,
    entityKind: 'season',
    entityId,
    data,
    serverVersion: null,
    updatedAtUtc: timestamp,
    syncState: 'PendingCreate',
    lastSyncedAtUtc: metadata.lastSyncedAtUtc,
    conflict: null
  };
  await store.stageMutation(record, mutation(
    namespaceKey,
    'season',
    entityId,
    'create',
    {
      fieldId: data.fieldId,
      cropId: data.cropId,
      name: data.name,
      startDate: data.startDate,
      endDate: data.endDate,
      expectedYieldPerHectare: data.expectedYieldPerHectare
    },
    null,
    timestamp
  ));
  return record;
}

export async function updateSeasonOffline(
  namespace: OfflineNamespace,
  seasonId: string,
  input: OfflineSeasonUpdateInput,
  store: OfflineStore = offlineStore
) {
  if (!input.name.trim() || !input.fieldId || !input.cropId || !input.startDate || !input.status.trim()) {
    throw new Error('Season field, crop, name, status and start date are required.');
  }
  await store.initialize();
  const namespaceKey = createOfflineNamespaceKey(namespace);
  requirePrepared(await store.getSyncMetadata(namespaceKey));
  const current = await currentRecord<Season>(namespace, 'season', seasonId, store);
  const field = await store.getRecord<Field>(createOfflineRecordKey(namespace, 'field', input.fieldId));
  if (!field || field.data.farmId !== namespace.farmId || field.syncState === 'PendingDelete') {
    throw new Error('Season must reference a field from this farm.');
  }
  const crop = await store.getRecord(createOfflineRecordKey(namespace, 'crop', input.cropId));
  if (!crop) throw new Error('Season crop is not available in this offline farm.');

  const timestamp = nowUtc();
  const data: Season = {
    ...current.data,
    fieldId: input.fieldId,
    cropId: input.cropId,
    name: input.name.trim(),
    startDate: input.startDate,
    endDate: input.endDate,
    expectedYieldPerHectare: input.expectedYieldPerHectare,
    actualYieldPerHectare: input.actualYieldPerHectare,
    status: input.status,
    updatedAtUtc: timestamp
  };
  const record: OfflineRecord<Season> = {
    ...current,
    data,
    updatedAtUtc: timestamp,
    syncState: 'PendingUpdate',
    conflict: null
  };
  await store.stageMutation(record, mutation(
    namespaceKey,
    'season',
    seasonId,
    'update',
    {
      fieldId: data.fieldId,
      cropId: data.cropId,
      name: data.name,
      startDate: data.startDate,
      endDate: data.endDate,
      expectedYieldPerHectare: data.expectedYieldPerHectare,
      actualYieldPerHectare: data.actualYieldPerHectare,
      status: data.status
    },
    current.serverVersion,
    timestamp
  ));
  return record;
}

export async function deleteSeasonOffline(
  namespace: OfflineNamespace,
  seasonId: string,
  store: OfflineStore = offlineStore
) {
  await store.initialize();
  const namespaceKey = createOfflineNamespaceKey(namespace);
  requirePrepared(await store.getSyncMetadata(namespaceKey));
  const current = await currentRecord<Season>(namespace, 'season', seasonId, store);
  const timestamp = nowUtc();
  const record: OfflineRecord<Season> = {
    ...current,
    updatedAtUtc: timestamp,
    syncState: 'PendingDelete',
    conflict: null
  };
  await store.stageMutation(record, mutation(
    namespaceKey,
    'season',
    seasonId,
    'delete',
    null,
    current.serverVersion,
    timestamp
  ));
  return record;
}
