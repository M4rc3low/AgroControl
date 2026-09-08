import type { GeoJsonPolygon } from '../../lib/types';

export function closeRing(points: number[][]): number[][] {
  if (points.length === 0) return [];
  const normalized = points.map(point => [point[0], point[1]]);
  const first = normalized[0];
  const last = normalized[normalized.length - 1];
  if (first[0] !== last[0] || first[1] !== last[1]) normalized.push([first[0], first[1]]);
  return normalized;
}

export function toPolygon(points: number[][]): GeoJsonPolygon {
  if (points.length < 3) throw new Error('O limite precisa de pelo menos três pontos.');
  return { type: 'Polygon', coordinates: [closeRing(points)] };
}

export function editableOuterRing(polygon: GeoJsonPolygon | null): number[][] {
  if (!polygon?.coordinates?.[0]?.length) return [];
  const ring = polygon.coordinates[0].map(point => [point[0], point[1]]);
  if (ring.length > 1) {
    const first = ring[0];
    const last = ring[ring.length - 1];
    if (first[0] === last[0] && first[1] === last[1]) ring.pop();
  }
  return ring;
}
