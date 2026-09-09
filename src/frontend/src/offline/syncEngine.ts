import { offlineStore } from './indexedDbOfflineStore';
import { createOfflineNamespaceKey } from './namespace';
import { pullFarmOffline } from './pull';
import { pushFarmOffline } from './push';
import type { OfflineStore } from './store';
import type { OfflineNamespace } from './types';

const MAX_PUSH_BATCHES_PER_RUN = 10;

export type OfflineSyncRunState =
  | 'Offline'
  | 'Synced'
  | 'Pending'
  | 'Conflict'
  | 'Failed'
  | 'Blocked';

export interface OfflineSyncRunResult {
  state: OfflineSyncRunState;
  pushed: number;
  pulled: number;
  pending: number;
  conflicts: number;
  failed: number;
  hasMorePull: boolean;
  message: string | null;
}

export async function syncFarmOffline(
  namespace: OfflineNamespace,
  store: OfflineStore = offlineStore
): Promise<OfflineSyncRunResult> {
  if (typeof navigator !== 'undefined' && !navigator.onLine) {
    const pending = await store.listMutations(createOfflineNamespaceKey(namespace));
    return {
      state: 'Offline',
      pushed: 0,
      pulled: 0,
      pending: pending.length,
      conflicts: pending.filter(item => item.state === 'Conflict').length,
      failed: pending.filter(item => item.state === 'Failed').length,
      hasMorePull: false,
      message: 'Sem conexão. As alterações continuam guardadas neste dispositivo.'
    };
  }

  const namespaceKey = createOfflineNamespaceKey(namespace);
  let pushed = 0;
  for (let batch = 0; batch < MAX_PUSH_BATCHES_PER_RUN; batch += 1) {
    const result = await pushFarmOffline(namespace, store);
    pushed += result.applied;
    if (result.blocked) {
      return {
        state: 'Blocked',
        pushed,
        pulled: 0,
        pending: 0,
        conflicts: 0,
        failed: 0,
        hasMorePull: false,
        message: result.blockedReason
      };
    }
    if (result.retryable > 0 || result.conflicts > 0 || result.failed > 0 || result.attempted === 0) break;
  }

  const remaining = await store.listMutations(namespaceKey);
  const conflicts = remaining.filter(item => item.state === 'Conflict').length;
  const failed = remaining.filter(item => item.state === 'Failed').length;
  const sendable = remaining.filter(item => item.state === 'Pending' || item.state === 'Retryable').length;

  if (conflicts > 0) {
    return {
      state: 'Conflict', pushed, pulled: 0, pending: remaining.length, conflicts, failed,
      hasMorePull: false,
      message: 'Há conflitos que precisam ser resolvidos antes de receber novas alterações do servidor.'
    };
  }
  if (failed > 0) {
    return {
      state: 'Failed', pushed, pulled: 0, pending: remaining.length, conflicts, failed,
      hasMorePull: false,
      message: 'Há alterações rejeitadas que precisam ser revisadas.'
    };
  }
  if (sendable > 0) {
    return {
      state: 'Pending', pushed, pulled: 0, pending: remaining.length, conflicts, failed,
      hasMorePull: false,
      message: 'Ainda existem alterações aguardando uma nova tentativa de envio.'
    };
  }

  const pull = await pullFarmOffline(namespace, store);
  return {
    state: pull.hasMore ? 'Pending' : 'Synced',
    pushed,
    pulled: pull.changesApplied,
    pending: 0,
    conflicts: 0,
    failed: 0,
    hasMorePull: pull.hasMore,
    message: pull.hasMore ? 'Ainda há alterações do servidor para baixar.' : null
  };
}
