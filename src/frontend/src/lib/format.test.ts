import { describe, expect, it } from 'vitest';
import { formatDate, formatNumber, initials } from './format';

describe('format helpers', () => {
  it('formats decimal values using pt-BR separators', () => {
    expect(formatNumber(1234.5, 1)).toBe('1.234,5');
  });

  it('formats DateOnly values without timezone drift', () => {
    expect(formatDate('2026-09-07')).toBe('07/09/2026');
  });

  it('creates stable initials', () => {
    expect(initials('Fazenda Horizonte')).toBe('FH');
  });
});
