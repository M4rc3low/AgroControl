import type {
  OfflineEntityKind,
  OfflineMutation,
  OfflineRecord,
  OfflineSyncMetadata
} from './types';

export interface OfflineStore {
  initialize(): Promise<void>;

  putRecord<T>(record: OfflineRecord<T>): Promise<void>;
  getRecord<T>(key: string): Promise<OfflineRecord<T> | null>;
  listRecords<T>(namespaceKey: string, entityKind?: OfflineEntityKind): Promise<OfflineRecord<T>[]>;
  deleteRecord(key: string): Promise<void>;

  enqueueMutation<T>(mutation: OfflineMutation<T>): Promise<void>;
  listMutations<T>(namespaceKey: string): Promise<OfflineMutation<T>[]>;
  putMutation<T>(mutation: OfflineMutation<T>): Promise<void>;
  deleteMutation(operationId: string): Promise<void>;

  getSyncMetadata(namespaceKey: string): Promise<OfflineSyncMetadata | null>;
  putSyncMetadata(metadata: OfflineSyncMetadata): Promise<void>;

  replaceCleanSnapshot(
    namespaceKey: string,
    records: OfflineRecord[],
    metadata: OfflineSyncMetadata
  ): Promise<void>;
  clearNamespace(namespaceKey: string): Promise<void>;
}
