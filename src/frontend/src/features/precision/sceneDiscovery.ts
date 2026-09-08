import type { GeoJsonPolygon, RemoteSceneDiscoveryItem } from '../../lib/types';

export function rasterCandidates(item: RemoteSceneDiscoveryItem) {
  return item.assets.filter(asset => asset.isRasterCandidate);
}

export function discoveryImportKey(item: Pick<RemoteSceneDiscoveryItem, 'provider' | 'externalId'>, assetKey: string) {
  return `${item.provider}:${item.externalId}:${assetKey}`;
}

export function parseDiscoveryPolygon(item: Pick<RemoteSceneDiscoveryItem, 'geometryGeoJson'> | null): GeoJsonPolygon | null {
  if (!item?.geometryGeoJson) return null;
  try {
    const parsed = JSON.parse(item.geometryGeoJson) as { type?: string; coordinates?: number[][][] };
    return parsed.type === 'Polygon' && Array.isArray(parsed.coordinates)
      ? { type: 'Polygon', coordinates: parsed.coordinates }
      : null;
  } catch {
    return null;
  }
}
