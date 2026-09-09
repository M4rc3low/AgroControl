import { createOfflineRecordKeyFromNamespace } from './namespace';
import type { OfflineMutation, OfflineRecord, OfflineSyncMetadata } from './types';

function requireValue(value: string, fieldName: string) {
  if (!value.trim()) throw new Error(`${fieldName} is required for offline storage.`);
}

export function assertOfflineRecord(record: OfflineRecord) {
  requireValue(record.namespaceKey, 'namespaceKey');
  requireValue(record.entityKind, 'entityKind');
  requireValue(record.entityId, 'entityId');
  requireValue(record.key, 'key');
  requireValue(record.updatedAtUtc, 'updatedAtUtc');

  const expectedKey = createOfflineRecordKeyFromNamespace(
    record.namespaceKey,
    record.entityKind,
    record.entityId
  );

  if (record.key !== expectedKey) {
    throw new Error('Offline record key does not match its namespace/entity identity.');
  }
}

export function assertOfflineMutation(mutation: OfflineMutation) {
  requireValue(mutation.operationId, 'operationId');
  requireValue(mutation.namespaceKey, 'namespaceKey');
  requireValue(mutation.entityKind, 'entityKind');
  requireValue(mutation.entityId, 'entityId');
  requireValue(mutation.createdAtUtc, 'createdAtUtc');

  if (!Number.isInteger(mutation.attempts) || mutation.attempts < 0) {
    throw new Error('Offline mutation attempts must be a non-negative integer.');
  }

  if (mutation.operation === 'delete' && mutation.payload !== null) {
    throw new Error('Offline delete mutations must not persist a payload.');
  }
}

export function assertOfflineSyncMetadata(metadata: OfflineSyncMetadata) {
  requireValue(metadata.namespaceKey, 'namespaceKey');

  if (!Number.isInteger(metadata.schemaVersion) || metadata.schemaVersion < 1) {
    throw new Error('Offline sync metadata schemaVersion must be a positive integer.');
  }
}
