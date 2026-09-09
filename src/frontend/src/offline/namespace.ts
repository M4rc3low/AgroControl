import type { OfflineEntityKind, OfflineNamespace } from './types';

const NAMESPACE_VERSION = 'v1';
const SEP = '|';

function encodePart(value: string, fieldName: string) {
  const normalized = value.trim();
  if (!normalized) throw new Error(`${fieldName} is required for offline storage.`);
  return encodeURIComponent(normalized);
}

export function createOfflineNamespaceKey(namespace: OfflineNamespace) {
  return [
    NAMESPACE_VERSION,
    encodePart(namespace.userId, 'userId'),
    encodePart(namespace.organizationId, 'organizationId'),
    encodePart(namespace.farmId, 'farmId')
  ].join(SEP);
}

export function createOfflineRecordKey(
  namespace: OfflineNamespace,
  entityKind: OfflineEntityKind,
  entityId: string
) {
  return [
    createOfflineNamespaceKey(namespace),
    encodePart(entityKind, 'entityKind'),
    encodePart(entityId, 'entityId')
  ].join(SEP);
}

export function createOfflineRecordKeyFromNamespace(
  namespaceKey: string,
  entityKind: OfflineEntityKind,
  entityId: string
) {
  const normalizedNamespace = namespaceKey.trim();
  if (!normalizedNamespace) throw new Error('namespaceKey is required for offline storage.');

  return [
    normalizedNamespace,
    encodePart(entityKind, 'entityKind'),
    encodePart(entityId, 'entityId')
  ].join(SEP);
}
