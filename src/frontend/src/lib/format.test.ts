import { describe, expect, it } from 'vitest';
import { formatDate, formatDateTimeInTimeZone, formatNumber, initials } from './format';

describe('format helpers', () => {
  it('formats decimal values using pt-BR separators', () => {
    expect(formatNumber(1234.5, 1)).toBe('1.234,5');
  });

  it('formats DateOnly values without timezone drift', () => {
    expect(formatDate('2026-09-07')).toBe('07/09/2026');
  });

  it('converts the same UTC instant using operational farm timezones in SP, MT, AM and AC', () => {
    const instant = '2026-09-08T15:00:00Z';
    expect(formatDateTimeInTimeZone(instant, 'America/Sao_Paulo')).toContain('12:00');
    expect(formatDateTimeInTimeZone(instant, 'America/Cuiaba')).toContain('11:00');
    expect(formatDateTimeInTimeZone(instant, 'America/Manaus')).toContain('11:00');
    expect(formatDateTimeInTimeZone(instant, 'America/Rio_Branco')).toContain('10:00');
  });

  it('returns a safe placeholder for an invalid timezone', () => {
    expect(formatDateTimeInTimeZone('2026-09-08T15:00:00Z', 'Mars/Olympus')).toBe('—');
  });

  it('creates stable initials', () => {
    expect(initials('Fazenda Horizonte')).toBe('FH');
  });
});
