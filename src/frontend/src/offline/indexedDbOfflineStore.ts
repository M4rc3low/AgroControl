import type { OfflineStore } from './store';
import type {
  OfflineEntityKind,
  OfflineMutation,
  OfflineRecord,
  OfflineSyncMetadata
} from './types';
import {
  assertOfflineMutation,
  assertOfflineRecord,
  assertOfflineSyncMetadata
} from './validation';

const DB_NAME = 'agrocontrol.offline';
const DB_VERSION = 1;

const RECORDS_STORE = 'records';
const OUTBOX_STORE = 'outbox';
const SYNC_METADATA_STORE = 'syncMetadata';
const NAMESPACE_INDEX = 'byNamespace';

function requestToPromise<T>(request: IDBRequest<T>) {
  return new Promise<T>((resolve, reject) => {
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error ?? new Error('IndexedDB request failed.'));
  });
}

function transactionToPromise(transaction: IDBTransaction) {
  return new Promise<void>((resolve, reject) => {
    transaction.oncomplete = () => resolve();
    transaction.onerror = () => reject(transaction.error ?? new Error('IndexedDB transaction failed.'));
    transaction.onabort = () => reject(transaction.error ?? new Error('IndexedDB transaction was aborted.'));
  });
}

function requireIndexedDb() {
  if (typeof indexedDB === 'undefined') {
    throw new Error('IndexedDB is not available in this runtime.');
  }
  return indexedDB;
}

function expectedRecordState(operation: OfflineMutation['operation']) {
  switch (operation) {
    case 'create': return 'PendingCreate';
    case 'update': return 'PendingUpdate';
    case 'delete': return 'PendingDelete';
  }
}

function assertStagedPair(record: OfflineRecord, mutation: OfflineMutation) {
  assertOfflineRecord(record);
  assertOfflineMutation(mutation);
  if (record.namespaceKey !== mutation.namespaceKey ||
      record.entityKind !== mutation.entityKind ||
      record.entityId !== mutation.entityId) {
    throw new Error('Offline mutation and record must describe the same namespaced entity.');
  }
  if (mutation.state !== 'Pending') {
    throw new Error('New offline mutations must start in Pending state.');
  }
  if (record.syncState !== expectedRecordState(mutation.operation)) {
    throw new Error('Offline record state does not match the staged mutation operation.');
  }
}

export class IndexedDbOfflineStore implements OfflineStore {
  private dbPromise: Promise<IDBDatabase> | null = null;

  async initialize() {
    await this.getDb();
  }

  async putRecord<T>(record: OfflineRecord<T>) {
    assertOfflineRecord(record);
    const db = await this.getDb();
    const transaction = db.transaction(RECORDS_STORE, 'readwrite');
    transaction.objectStore(RECORDS_STORE).put(record);
    await transactionToPromise(transaction);
  }

  async getRecord<T>(key: string) {
    const db = await this.getDb();
    const transaction = db.transaction(RECORDS_STORE, 'readonly');
    const result = await requestToPromise(transaction.objectStore(RECORDS_STORE).get(key));
    await transactionToPromise(transaction);
    return (result as OfflineRecord<T> | undefined) ?? null;
  }

  async listRecords<T>(namespaceKey: string, entityKind?: OfflineEntityKind) {
    const db = await this.getDb();
    const transaction = db.transaction(RECORDS_STORE, 'readonly');
    const store = transaction.objectStore(RECORDS_STORE);
    const records = await requestToPromise(store.index(NAMESPACE_INDEX).getAll(IDBKeyRange.only(namespaceKey)));
    await transactionToPromise(transaction);

    return (records as OfflineRecord<T>[])
      .filter((record) => !entityKind || record.entityKind === entityKind)
      .sort((left, right) => left.entityId.localeCompare(right.entityId));
  }

  async deleteRecord(key: string) {
    const db = await this.getDb();
    const transaction = db.transaction(RECORDS_STORE, 'readwrite');
    transaction.objectStore(RECORDS_STORE).delete(key);
    await transactionToPromise(transaction);
  }

  async enqueueMutation<T>(mutation: OfflineMutation<T>) {
    assertOfflineMutation(mutation);
    const db = await this.getDb();
    const transaction = db.transaction(OUTBOX_STORE, 'readwrite');
    transaction.objectStore(OUTBOX_STORE).add(mutation);
    await transactionToPromise(transaction);
  }

  async listMutations<T>(namespaceKey: string) {
    const db = await this.getDb();
    const transaction = db.transaction(OUTBOX_STORE, 'readonly');
    const store = transaction.objectStore(OUTBOX_STORE);
    const mutations = await requestToPromise(store.index(NAMESPACE_INDEX).getAll(IDBKeyRange.only(namespaceKey)));
    await transactionToPromise(transaction);

    return (mutations as OfflineMutation<T>[]).sort((left, right) => {
      const dateComparison = left.createdAtUtc.localeCompare(right.createdAtUtc);
      return dateComparison !== 0 ? dateComparison : left.operationId.localeCompare(right.operationId);
    });
  }

  async putMutation<T>(mutation: OfflineMutation<T>) {
    assertOfflineMutation(mutation);
    const db = await this.getDb();
    const transaction = db.transaction(OUTBOX_STORE, 'readwrite');
    transaction.objectStore(OUTBOX_STORE).put(mutation);
    await transactionToPromise(transaction);
  }

  async deleteMutation(operationId: string) {
    const db = await this.getDb();
    const transaction = db.transaction(OUTBOX_STORE, 'readwrite');
    transaction.objectStore(OUTBOX_STORE).delete(operationId);
    await transactionToPromise(transaction);
  }

  async stageMutation<TRecord, TPayload>(
    record: OfflineRecord<TRecord>,
    mutation: OfflineMutation<TPayload>
  ) {
    assertStagedPair(record, mutation);

    const db = await this.getDb();
    const transaction = db.transaction([RECORDS_STORE, OUTBOX_STORE], 'readwrite');
    const outbox = transaction.objectStore(OUTBOX_STORE);
    const existing = await requestToPromise(
      outbox.index(NAMESPACE_INDEX).getAll(IDBKeyRange.only(mutation.namespaceKey))
    ) as OfflineMutation[];
    if (existing.some(item => item.entityKind === mutation.entityKind && item.entityId === mutation.entityId)) {
      transaction.abort();
      throw new Error('This entity already has a pending offline mutation. Synchronize or resolve it first.');
    }

    transaction.objectStore(RECORDS_STORE).put(record);
    outbox.add(mutation);
    await transactionToPromise(transaction);
  }

  async replaceMutation<TRecord, TPayload>(
    previousOperationId: string,
    record: OfflineRecord<TRecord>,
    mutation: OfflineMutation<TPayload>
  ) {
    if (!previousOperationId.trim()) throw new Error('Previous operation id is required.');
    assertStagedPair(record, mutation);

    const db = await this.getDb();
    const transaction = db.transaction([RECORDS_STORE, OUTBOX_STORE], 'readwrite');
    const outbox = transaction.objectStore(OUTBOX_STORE);
    const previous = await requestToPromise(outbox.get(previousOperationId)) as OfflineMutation | undefined;
    if (!previous || previous.state !== 'Conflict' ||
        previous.namespaceKey !== mutation.namespaceKey ||
        previous.entityKind !== mutation.entityKind ||
        previous.entityId !== mutation.entityId) {
      transaction.abort();
      throw new Error('Only the matching conflict mutation can be replaced for reapply.');
    }

    transaction.objectStore(RECORDS_STORE).put(record);
    outbox.delete(previousOperationId);
    outbox.add(mutation);
    await transactionToPromise(transaction);
  }

  async getSyncMetadata(namespaceKey: string) {
    const db = await this.getDb();
    const transaction = db.transaction(SYNC_METADATA_STORE, 'readonly');
    const result = await requestToPromise(transaction.objectStore(SYNC_METADATA_STORE).get(namespaceKey));
    await transactionToPromise(transaction);
    return (result as OfflineSyncMetadata | undefined) ?? null;
  }

  async putSyncMetadata(metadata: OfflineSyncMetadata) {
    assertOfflineSyncMetadata(metadata);
    const db = await this.getDb();
    const transaction = db.transaction(SYNC_METADATA_STORE, 'readwrite');
    transaction.objectStore(SYNC_METADATA_STORE).put(metadata);
    await transactionToPromise(transaction);
  }

  async replaceCleanSnapshot(
    namespaceKey: string,
    records: OfflineRecord[],
    metadata: OfflineSyncMetadata
  ) {
    if (!namespaceKey.trim()) throw new Error('namespaceKey is required for offline bootstrap.');
    assertOfflineSyncMetadata(metadata);
    if (metadata.namespaceKey !== namespaceKey) {
      throw new Error('Offline bootstrap metadata does not match the target namespace.');
    }

    const seenKeys = new Set<string>();
    for (const record of records) {
      assertOfflineRecord(record);
      if (record.namespaceKey !== namespaceKey) {
        throw new Error('Offline bootstrap contains a record from another namespace.');
      }
      if (record.syncState !== 'Clean') {
        throw new Error('Offline bootstrap can only replace clean server snapshots.');
      }
      if (seenKeys.has(record.key)) {
        throw new Error('Offline bootstrap contains duplicate record keys.');
      }
      seenKeys.add(record.key);
    }

    const db = await this.getDb();
    const transaction = db.transaction(
      [RECORDS_STORE, OUTBOX_STORE, SYNC_METADATA_STORE],
      'readwrite'
    );
    const outbox = transaction.objectStore(OUTBOX_STORE);
    const pendingCount = await requestToPromise(
      outbox.index(NAMESPACE_INDEX).count(IDBKeyRange.only(namespaceKey))
    );

    if (pendingCount > 0) {
      transaction.abort();
      throw new Error('Offline bootstrap cannot replace a namespace with pending local mutations.');
    }

    const recordsStore = transaction.objectStore(RECORDS_STORE);
    await this.deleteByNamespace(recordsStore, namespaceKey);
    for (const record of records) recordsStore.put(record);
    transaction.objectStore(SYNC_METADATA_STORE).put(metadata);
    await transactionToPromise(transaction);
  }

  async applyCleanServerChanges(
    namespaceKey: string,
    upserts: OfflineRecord[],
    deleteKeys: string[],
    metadata: OfflineSyncMetadata
  ) {
    if (!namespaceKey.trim()) throw new Error('namespaceKey is required for offline pull.');
    assertOfflineSyncMetadata(metadata);
    if (metadata.namespaceKey !== namespaceKey) {
      throw new Error('Offline pull metadata does not match the target namespace.');
    }

    const namespacePrefix = `${namespaceKey}|`;
    const seenKeys = new Set<string>();
    for (const record of upserts) {
      assertOfflineRecord(record);
      if (record.namespaceKey !== namespaceKey || !record.key.startsWith(namespacePrefix)) {
        throw new Error('Offline pull contains an upsert from another namespace.');
      }
      if (record.syncState !== 'Clean') {
        throw new Error('Offline pull can only apply clean server records.');
      }
      if (seenKeys.has(record.key)) {
        throw new Error('Offline pull contains duplicate upsert keys.');
      }
      seenKeys.add(record.key);
    }

    for (const key of deleteKeys) {
      if (!key.startsWith(namespacePrefix)) {
        throw new Error('Offline pull contains a delete key from another namespace.');
      }
      if (seenKeys.has(key)) {
        throw new Error('Offline pull cannot upsert and delete the same record in one batch.');
      }
      seenKeys.add(key);
    }

    const db = await this.getDb();
    const transaction = db.transaction(
      [RECORDS_STORE, OUTBOX_STORE, SYNC_METADATA_STORE],
      'readwrite'
    );
    const pendingCount = await requestToPromise(
      transaction.objectStore(OUTBOX_STORE).index(NAMESPACE_INDEX).count(IDBKeyRange.only(namespaceKey))
    );
    if (pendingCount > 0) {
      transaction.abort();
      throw new Error('Offline pull cannot overwrite a namespace with pending local mutations. Push local changes first.');
    }

    const recordsStore = transaction.objectStore(RECORDS_STORE);
    for (const key of deleteKeys) recordsStore.delete(key);
    for (const record of upserts) recordsStore.put(record);
    transaction.objectStore(SYNC_METADATA_STORE).put(metadata);
    await transactionToPromise(transaction);
  }

  async clearNamespace(namespaceKey: string) {
    const db = await this.getDb();
    const transaction = db.transaction([RECORDS_STORE, OUTBOX_STORE, SYNC_METADATA_STORE], 'readwrite');

    await Promise.all([
      this.deleteByNamespace(transaction.objectStore(RECORDS_STORE), namespaceKey),
      this.deleteByNamespace(transaction.objectStore(OUTBOX_STORE), namespaceKey)
    ]);

    transaction.objectStore(SYNC_METADATA_STORE).delete(namespaceKey);
    await transactionToPromise(transaction);
  }

  async blockAndPurgeNamespace(namespaceKey: string, reason: string) {
    const normalizedReason = reason.trim();
    if (!namespaceKey.trim() || !normalizedReason) {
      throw new Error('Namespace and reason are required when blocking offline data.');
    }

    const db = await this.getDb();
    const transaction = db.transaction([RECORDS_STORE, OUTBOX_STORE, SYNC_METADATA_STORE], 'readwrite');
    await Promise.all([
      this.deleteByNamespace(transaction.objectStore(RECORDS_STORE), namespaceKey),
      this.deleteByNamespace(transaction.objectStore(OUTBOX_STORE), namespaceKey)
    ]);

    const metadataStore = transaction.objectStore(SYNC_METADATA_STORE);
    const current = await requestToPromise(metadataStore.get(namespaceKey)) as OfflineSyncMetadata | undefined;
    metadataStore.put({
      namespaceKey,
      cursor: null,
      lastSyncedAtUtc: current?.lastSyncedAtUtc ?? null,
      preparationState: 'Blocked',
      schemaVersion: current?.schemaVersion ?? 1,
      lastError: normalizedReason
    } satisfies OfflineSyncMetadata);
    await transactionToPromise(transaction);
  }

  async clearAll() {
    const db = await this.getDb();
    const transaction = db.transaction([RECORDS_STORE, OUTBOX_STORE, SYNC_METADATA_STORE], 'readwrite');
    transaction.objectStore(RECORDS_STORE).clear();
    transaction.objectStore(OUTBOX_STORE).clear();
    transaction.objectStore(SYNC_METADATA_STORE).clear();
    await transactionToPromise(transaction);
  }

  private getDb() {
    if (!this.dbPromise) {
      this.dbPromise = this.openDb();
    }
    return this.dbPromise;
  }

  private openDb() {
    return new Promise<IDBDatabase>((resolve, reject) => {
      const request = requireIndexedDb().open(DB_NAME, DB_VERSION);

      request.onupgradeneeded = () => {
        const db = request.result;

        if (!db.objectStoreNames.contains(RECORDS_STORE)) {
          const records = db.createObjectStore(RECORDS_STORE, { keyPath: 'key' });
          records.createIndex(NAMESPACE_INDEX, 'namespaceKey', { unique: false });
        }

        if (!db.objectStoreNames.contains(OUTBOX_STORE)) {
          const outbox = db.createObjectStore(OUTBOX_STORE, { keyPath: 'operationId' });
          outbox.createIndex(NAMESPACE_INDEX, 'namespaceKey', { unique: false });
        }

        if (!db.objectStoreNames.contains(SYNC_METADATA_STORE)) {
          db.createObjectStore(SYNC_METADATA_STORE, { keyPath: 'namespaceKey' });
        }
      };

      request.onsuccess = () => {
        const db = request.result;
        db.onversionchange = () => db.close();
        resolve(db);
      };

      request.onerror = () => {
        this.dbPromise = null;
        reject(request.error ?? new Error('Could not open AgroControl offline database.'));
      };

      request.onblocked = () => {
        this.dbPromise = null;
        reject(new Error('AgroControl offline database upgrade is blocked by another tab or window.'));
      };
    });
  }

  private async deleteByNamespace(store: IDBObjectStore, namespaceKey: string) {
    const keys = await requestToPromise(store.index(NAMESPACE_INDEX).getAllKeys(IDBKeyRange.only(namespaceKey)));
    for (const key of keys) store.delete(key);
  }
}

export const offlineStore: OfflineStore = new IndexedDbOfflineStore();
