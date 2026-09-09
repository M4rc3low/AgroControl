import type { OfflineStore } from './store';
import type {
  OfflineEntityKind,
  OfflineMutation,
  OfflineRecord,
  OfflineSyncMetadata
} from './types';

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

export class IndexedDbOfflineStore implements OfflineStore {
  private dbPromise: Promise<IDBDatabase> | null = null;

  async initialize() {
    await this.getDb();
  }

  async putRecord<T>(record: OfflineRecord<T>) {
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

  async getSyncMetadata(namespaceKey: string) {
    const db = await this.getDb();
    const transaction = db.transaction(SYNC_METADATA_STORE, 'readonly');
    const result = await requestToPromise(transaction.objectStore(SYNC_METADATA_STORE).get(namespaceKey));
    await transactionToPromise(transaction);
    return (result as OfflineSyncMetadata | undefined) ?? null;
  }

  async putSyncMetadata(metadata: OfflineSyncMetadata) {
    const db = await this.getDb();
    const transaction = db.transaction(SYNC_METADATA_STORE, 'readwrite');
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
    for (const key of keys) {
      store.delete(key);
    }
  }
}

export const offlineStore: OfflineStore = new IndexedDbOfflineStore();
