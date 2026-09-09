export type OfflineSyncState =
  | 'Clean'
  | 'PendingCreate'
  | 'PendingUpdate'
  | 'PendingDelete'
  | 'Conflict'
  | 'Failed';

export type OfflineMutationKind = 'create' | 'update' | 'delete';
export type OfflineMutationState = 'Pending' | 'Retryable' | 'Conflict' | 'Failed';

export type OfflineEntityKind = 'farm' | 'field' | 'crop' | 'season' | string;

export interface OfflineNamespace {
  userId: string;
  organizationId: string;
  farmId: string;
}

export interface OfflineConflictSnapshot {
  operationId: string;
  serverVersion: string | null;
  serverData: unknown;
  message: string | null;
}

export interface OfflineRecord<T = unknown> {
  key: string;
  namespaceKey: string;
  entityKind: OfflineEntityKind;
  entityId: string;
  data: T;
  serverVersion: string | null;
  updatedAtUtc: string;
  syncState: OfflineSyncState;
  lastSyncedAtUtc: string | null;
  conflict?: OfflineConflictSnapshot | null;
}

export interface OfflineMutation<T = unknown> {
  operationId: string;
  namespaceKey: string;
  entityKind: OfflineEntityKind;
  entityId: string;
  operation: OfflineMutationKind;
  payload: T | null;
  baseServerVersion: string | null;
  createdAtUtc: string;
  attempts: number;
  state: OfflineMutationState;
  lastError: string | null;
}

export type OfflinePreparationState = 'NotPrepared' | 'Preparing' | 'Ready' | 'Blocked' | 'Error';

export interface OfflineSyncMetadata {
  namespaceKey: string;
  cursor: string | null;
  lastSyncedAtUtc: string | null;
  preparationState: OfflinePreparationState;
  schemaVersion: number;
  lastError: string | null;
}
