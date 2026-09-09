import { describe, expect, it } from 'vitest';
import { computeOfflineRetryDelay } from './syncEngine';

describe('offline sync retry backoff', () => {
  it('uses bounded exponential delays', () => {
    expect(computeOfflineRetryDelay(0)).toBe(1_000);
    expect(computeOfflineRetryDelay(1)).toBe(1_000);
    expect(computeOfflineRetryDelay(2)).toBe(2_000);
    expect(computeOfflineRetryDelay(3)).toBe(4_000);
    expect(computeOfflineRetryDelay(5)).toBe(15_000);
    expect(computeOfflineRetryDelay(50)).toBe(15_000);
  });
});
