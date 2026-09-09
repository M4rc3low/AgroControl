import { apiRequest } from '../lib/api';
import type { Crop, Farm, Field, Season } from '../lib/types';
import { offlineStore } from './indexedDbOfflineStore';
import { createOfflineNamespaceKey, createOfflineRecordKey } from './namespace';
import type { OfflineStore } from './store';
import type { OfflineNamespace, OfflineRecord, OfflineSyncMetadata } from './types';

const LOCAL_SCHEMA_VERSION = 1;

export interface OfflineBootstrapResponse {
  farm: Farm;
  fields: Field[];
  crops: Crop[];
  seasons: Season[];
  protocolVersion: number;
  localSchemaVersion: number;
  serverTimeUtc: string;
  cursor: string;
  watermarkSequence: number;
}

export interface OfflineBootstrapSnapshot {
  namespaceKey: string;
  records: OfflineRecord[];
  metadata: OfflineSyncMetadata;
}

export interface OfflineBootstrapResult {
  namespaceKey: string;
  farmId: string;
  fieldCount: number;
  cropCount: number;
  seasonCount: number;
  lastSyncedAtUtc: string;
}

function cleanRecord<T>(
  namespace: OfflineNamespace,
  namespaceKey: string,
  entityKind: 'farm' | 'field' | 'crop' | 'season',
  entityId: string,
  data: T,
  updatedAtUtc: string,
  lastSyncedAtUtc: string
): OfflineRecord<T> {
  return {
    key: createOfflineRecordKey(namespace, entityKind, entityId),
    namespaceKey,
    entityKind,
    entityId,
    data,
    serverVersion: updatedAtUtc,
    updatedAtUtc,
    syncState: 'Clean',
    lastSyncedAtUtc
  };
}

export function buildOfflineBootstrapSnapshot(
  namespace: OfflineNamespace,
  response: OfflineBootstrapResponse
): OfflineBootstrapSnapshot {
  if (response.localSchemaVersion !== LOCAL_SCHEMA_VERSION) {
    throw new Error(
      `Offline schema ${response.localSchemaVersion} is not supported by this client.`
    );
  }
  if (!response.cursor?.trim() || !Number.isSafeInteger(response.watermarkSequence) || response.watermarkSequence < 0) {
    throw new Error('Offline bootstrap did not return a valid synchronization cursor.');
  }

  if (response.farm.id !== namespace.farmId) {
    throw new Error('Offline bootstrap farm does not match the requested namespace.');
  }

  const fieldIds = new Set<string>();
  for (const field of response.fields) {
    if (field.farmId !== namespace.farmId) {
      throw new Error('Offline bootstrap contains a field from another farm.');
    }
    if (fieldIds.has(field.id)) {
      throw new Error('Offline bootstrap contains duplicate fields.');
    }
    fieldIds.add(field.id);
  }

  const referencedCropIds = new Set<string>();
  const seasonIds = new Set<string>();
  for (const season of response.seasons) {
    if (!fieldIds.has(season.fieldId)) {
      throw new Error('Offline bootstrap contains a season outside the selected farm.');
    }
    if (seasonIds.has(season.id)) {
      throw new Error('Offline bootstrap contains duplicate seasons.');
    }
    seasonIds.add(season.id);
    referencedCropIds.add(season.cropId);
  }

  const cropIds = new Set<string>();
  for (const crop of response.crops) {
    if (!referencedCropIds.has(crop.id)) {
      throw new Error('Offline bootstrap contains an unrelated crop.');
    }
    if (cropIds.has(crop.id)) {
      throw new Error('Offline bootstrap contains duplicate crops.');
    }
    cropIds.add(crop.id);
  }

  for (const cropId of referencedCropIds) {
    if (!cropIds.has(cropId)) {
      throw new Error('Offline bootstrap is missing a crop referenced by a season.');
    }
  }

  const namespaceKey = createOfflineNamespaceKey(namespace);
  const lastSyncedAtUtc = response.serverTimeUtc;
  const records: OfflineRecord[] = [
    cleanRecord(
      namespace,
      namespaceKey,
      'farm',
      response.farm.id,
      response.farm,
      response.farm.updatedAtUtc,
      lastSyncedAtUtc
    ),
    ...response.fields.map((field) => cleanRecord(
      namespace,
      namespaceKey,
      'field',
      field.id,
      field,
      field.updatedAtUtc,
      lastSyncedAtUtc
    )),
    ...response.crops.map((crop) => cleanRecord(
      namespace,
      namespaceKey,
      'crop',
      crop.id,
      crop,
      crop.updatedAtUtc,
      lastSyncedAtUtc
    )),
    ...response.seasons.map((season) => cleanRecord(
      namespace,
      namespaceKey,
      'season',
      season.id,
      season,
      season.updatedAtUtc,
      lastSyncedAtUtc
    ))
  ];

  return {
    namespaceKey,
    records,
    metadata: {
      namespaceKey,
      cursor: response.cursor,
      lastSyncedAtUtc,
      preparationState: 'Ready',
      schemaVersion: LOCAL_SCHEMA_VERSION,
      lastError: null
    }
  };
}

export async function bootstrapFarmOffline(
  namespace: OfflineNamespace,
  store: OfflineStore = offlineStore
): Promise<OfflineBootstrapResult> {
  const response = await apiRequest<OfflineBootstrapResponse>(
    `/api/v1/sync/bootstrap?farmId=${encodeURIComponent(namespace.farmId)}`
  );
  const snapshot = buildOfflineBootstrapSnapshot(namespace, response);

  await store.initialize();
  await store.replaceCleanSnapshot(
    snapshot.namespaceKey,
    snapshot.records,
    snapshot.metadata
  );

  return {
    namespaceKey: snapshot.namespaceKey,
    farmId: response.farm.id,
    fieldCount: response.fields.length,
    cropCount: response.crops.length,
    seasonCount: response.seasons.length,
    lastSyncedAtUtc: response.serverTimeUtc
  };
}
