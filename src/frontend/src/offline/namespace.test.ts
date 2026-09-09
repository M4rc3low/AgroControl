import { describe, expect, it } from 'vitest';
import {
  createOfflineNamespaceKey,
  createOfflineRecordKey,
  createOfflineRecordKeyFromNamespace
} from './namespace';

const base = {
  userId: 'user-1',
  organizationId: 'org-1',
  farmId: 'farm-1'
};

describe('offline namespace', () => {
  it('includes user, organization and farm in the namespace key', () => {
    expect(createOfflineNamespaceKey(base)).toBe('v1|user-1|org-1|farm-1');
  });

  it('changes the namespace when any security boundary changes', () => {
    const original = createOfflineNamespaceKey(base);

    expect(createOfflineNamespaceKey({ ...base, userId: 'user-2' })).not.toBe(original);
    expect(createOfflineNamespaceKey({ ...base, organizationId: 'org-2' })).not.toBe(original);
    expect(createOfflineNamespaceKey({ ...base, farmId: 'farm-2' })).not.toBe(original);
  });

  it('encodes separator characters instead of allowing ambiguous composite keys', () => {
    const key = createOfflineNamespaceKey({
      userId: 'user|1',
      organizationId: 'org/1',
      farmId: 'farm 1'
    });

    expect(key).toBe('v1|user%7C1|org%2F1|farm%201');
  });

  it('requires every namespace boundary', () => {
    expect(() => createOfflineNamespaceKey({ ...base, userId: ' ' })).toThrow(/userId/);
    expect(() => createOfflineNamespaceKey({ ...base, organizationId: '' })).toThrow(/organizationId/);
    expect(() => createOfflineNamespaceKey({ ...base, farmId: '' })).toThrow(/farmId/);
  });

  it('builds stable record keys from namespace, entity type and entity id', () => {
    const namespaceKey = createOfflineNamespaceKey(base);

    expect(createOfflineRecordKey(base, 'field', 'field-9')).toBe(
      createOfflineRecordKeyFromNamespace(namespaceKey, 'field', 'field-9')
    );
    expect(createOfflineRecordKey(base, 'field', 'field-9')).toBe('v1|user-1|org-1|farm-1|field|field-9');
  });
});
