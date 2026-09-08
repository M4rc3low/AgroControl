import { describe, expect, it } from 'vitest';
import { closeRing, editableOuterRing, toPolygon } from './geometry';

describe('precision geometry helpers', () => {
  it('closes an open ring without mutating the source points', () => {
    const points = [[-47, -15], [-46, -15], [-46, -14]];
    const closed = closeRing(points);
    expect(closed).toHaveLength(4);
    expect(closed[0]).toEqual(closed[3]);
    expect(points).toHaveLength(3);
  });

  it('creates a GeoJSON polygon', () => {
    const polygon = toPolygon([[-47, -15], [-46, -15], [-46, -14]]);
    expect(polygon.type).toBe('Polygon');
    expect(polygon.coordinates[0]).toHaveLength(4);
  });

  it('removes the closing coordinate when preparing a polygon for redraw', () => {
    const points = editableOuterRing({ type: 'Polygon', coordinates: [[[-47, -15], [-46, -15], [-46, -14], [-47, -15]]] });
    expect(points).toHaveLength(3);
  });
});
