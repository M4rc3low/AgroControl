import { describe, expect, it } from 'vitest';
import { inspectGeoJsonImport } from './geoJsonImport';

describe('GeoJSON import inspection', () => {
  it('wraps a single Feature for map preview', () => {
    const feature = { type: 'Feature', properties: { name: 'Zona A' }, geometry: { type: 'Polygon', coordinates: [] } };
    const result = inspectGeoJsonImport(feature);
    expect(result.featureCount).toBe(1);
    expect(result.featureCollection.type).toBe('FeatureCollection');
    expect(result.featureCollection.features).toHaveLength(1);
  });

  it('accepts a FeatureCollection and preserves its features', () => {
    const features = [{ type: 'Feature' }, { type: 'Feature' }];
    const result = inspectGeoJsonImport({ type: 'FeatureCollection', features });
    expect(result.featureCount).toBe(2);
    expect(result.featureCollection.features).toEqual(features);
  });

  it('rejects empty, unsupported and oversized imports', () => {
    expect(() => inspectGeoJsonImport({ type: 'Polygon' })).toThrow(/Feature/);
    expect(() => inspectGeoJsonImport({ type: 'FeatureCollection', features: [] })).toThrow(/pelo menos/);
    expect(() => inspectGeoJsonImport({ type: 'FeatureCollection', features: Array.from({ length: 251 }, () => ({ type: 'Feature' })) })).toThrow(/250/);
  });
});
