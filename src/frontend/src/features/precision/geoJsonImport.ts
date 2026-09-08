export interface GeoJsonImportInspection {
  featureCount: number;
  featureCollection: { type: 'FeatureCollection'; features: unknown[] };
}

export function inspectGeoJsonImport(document: unknown): GeoJsonImportInspection {
  if (!document || typeof document !== 'object') throw new Error('GeoJSON precisa ser um objeto.');
  const candidate = document as { type?: unknown; features?: unknown };

  if (candidate.type === 'Feature') {
    return { featureCount: 1, featureCollection: { type: 'FeatureCollection', features: [document] } };
  }

  if (candidate.type !== 'FeatureCollection' || !Array.isArray(candidate.features)) {
    throw new Error('Use um GeoJSON Feature ou FeatureCollection.');
  }

  if (candidate.features.length === 0) throw new Error('O FeatureCollection precisa conter pelo menos uma feature.');
  if (candidate.features.length > 250) throw new Error('Uma importação pode conter no máximo 250 features.');

  return {
    featureCount: candidate.features.length,
    featureCollection: { type: 'FeatureCollection', features: candidate.features }
  };
}
