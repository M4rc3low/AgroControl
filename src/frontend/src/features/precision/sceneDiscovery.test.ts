import { describe, expect, it } from 'vitest';
import type { RemoteSceneDiscoveryItem } from '../../lib/types';
import { discoveryImportKey, parseDiscoveryPolygon, rasterCandidates } from './sceneDiscovery';

const item = (overrides: Partial<RemoteSceneDiscoveryItem> = {}): RemoteSceneDiscoveryItem => ({
  provider: 'earth-search',
  collection: 'sentinel-2-l2a',
  externalId: 'S2-001',
  acquiredAtUtc: '2026-09-08T12:00:00Z',
  geometryGeoJson: '{"type":"Polygon","coordinates":[[[-50,-10],[-49,-10],[-49,-9],[-50,-9],[-50,-10]]]}',
  bbox: [-50, -10, -49, -9],
  cloudCoveragePercent: 12,
  spatialResolutionMeters: 10,
  platform: 'sentinel-2a',
  constellation: 'sentinel-2',
  assets: [
    { key: 'red', href: 'https://example.test/red.tif', mediaType: 'image/tiff', roles: ['data'], isRasterCandidate: true },
    { key: 'thumbnail', href: 'https://example.test/thumb.jpg', mediaType: 'image/jpeg', roles: ['thumbnail'], isRasterCandidate: false }
  ],
  ...overrides
});

describe('scene discovery helpers', () => {
  it('exposes only provider-approved raster candidates', () => {
    expect(rasterCandidates(item()).map(asset => asset.key)).toEqual(['red']);
  });

  it('builds a stable import key without using asset href', () => {
    expect(discoveryImportKey(item(), 'red')).toBe('earth-search:S2-001:red');
  });

  it('parses Polygon footprint for map preview', () => {
    const polygon = parseDiscoveryPolygon(item());
    expect(polygon?.type).toBe('Polygon');
    expect(polygon?.coordinates[0]).toHaveLength(5);
  });

  it('rejects invalid or unsupported preview geometry', () => {
    expect(parseDiscoveryPolygon(item({ geometryGeoJson: '{"type":"Point","coordinates":[0,0]}' }))).toBeNull();
    expect(parseDiscoveryPolygon(item({ geometryGeoJson: '{invalid' }))).toBeNull();
    expect(parseDiscoveryPolygon(null)).toBeNull();
  });
});
