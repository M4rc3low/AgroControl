import { describe, expect, it } from 'vitest';
import type { RemoteSensingSeriesPoint } from '../../lib/types';
import { buildSeriesPath, groupRemoteSensingSeries } from './remoteSensing';

const point = (indexType: string, observedAtUtc: string, mean: number, customIndexName: string | null = null): RemoteSensingSeriesPoint => ({
  observationId: crypto.randomUUID(), sceneId: crypto.randomUUID(), managementZoneId: null, indexType, customIndexName,
  mean, minimum: mean - 0.1, maximum: mean + 0.1, median: mean, standardDeviation: 0.05,
  validCoveragePercent: 95, sampleCount: 1000, source: 'Sentinel-2', platform: 'Satellite', observedAtUtc
});

describe('remote sensing series helpers', () => {
  it('groups index series and sorts points chronologically', () => {
    const groups = groupRemoteSensingSeries([
      point('NDVI', '2026-09-08T12:00:00Z', 0.7),
      point('NDRE', '2026-09-07T12:00:00Z', 0.4),
      point('NDVI', '2026-09-01T12:00:00Z', 0.5)
    ]);
    const ndvi = groups.find(group => group.key === 'NDVI');
    expect(ndvi?.points.map(item => item.mean)).toEqual([0.5, 0.7]);
    expect(groups.find(group => group.key === 'NDRE')).toBeTruthy();
  });

  it('builds a stable SVG path for one or more observations', () => {
    expect(buildSeriesPath([])).toBe('');
    const one = buildSeriesPath([point('NDVI', '2026-09-08T12:00:00Z', 0.6)]);
    expect(one).toContain('M 320.00');
    const many = buildSeriesPath([
      point('NDVI', '2026-09-01T12:00:00Z', 0.3),
      point('NDVI', '2026-09-08T12:00:00Z', 0.8)
    ]);
    expect(many).toContain('L 640.00');
    expect(many).not.toContain('NaN');
  });
});
