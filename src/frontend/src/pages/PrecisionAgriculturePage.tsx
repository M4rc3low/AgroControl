import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Button, Card, EmptyState, PageHeader, Spinner } from '../components/Ui';
import { Icon } from '../components/Icon';
import { editableOuterRing, toPolygon } from '../features/precision/geometry';
import { apiRequest, getAllPaged } from '../lib/api';
import { useAuth } from '../lib/auth';
import type { Farm, GeoJsonPolygon, PrecisionField } from '../lib/types';
import './precision.css';

type MapInstance = any;
type MapLibreGlobal = { Map: new (options: any) => MapInstance; NavigationControl: new () => any; LngLatBounds: new () => any };

declare global {
  interface Window { maplibregl?: MapLibreGlobal; }
}

const MAP_SCRIPT = import.meta.env.VITE_MAPLIBRE_SCRIPT_URL?.trim() || 'https://unpkg.com/maplibre-gl@5/dist/maplibre-gl.js';
const MAP_CSS = import.meta.env.VITE_MAPLIBRE_CSS_URL?.trim() || 'https://unpkg.com/maplibre-gl@5/dist/maplibre-gl.css';
const MAP_STYLE = import.meta.env.VITE_MAP_STYLE_URL?.trim() || 'https://demotiles.maplibre.org/style.json';

function ensureMapLibre(): Promise<MapLibreGlobal> {
  if (window.maplibregl) return Promise.resolve(window.maplibregl);

  if (!document.querySelector('link[data-agrocontrol-maplibre]')) {
    const link = document.createElement('link');
    link.rel = 'stylesheet';
    link.href = MAP_CSS;
    link.dataset.agrocontrolMaplibre = 'true';
    document.head.appendChild(link);
  }

  return new Promise((resolve, reject) => {
    const existing = document.querySelector<HTMLScriptElement>('script[data-agrocontrol-maplibre]');
    const resolveLoaded = () => window.maplibregl ? resolve(window.maplibregl) : reject(new Error('MapLibre não ficou disponível no navegador.'));
    if (existing) {
      if (window.maplibregl) resolveLoaded();
      else {
        existing.addEventListener('load', resolveLoaded, { once: true });
        existing.addEventListener('error', () => reject(new Error('Não foi possível carregar o motor do mapa.')), { once: true });
      }
      return;
    }

    const script = document.createElement('script');
    script.src = MAP_SCRIPT;
    script.async = true;
    script.dataset.agrocontrolMaplibre = 'true';
    script.addEventListener('load', resolveLoaded, { once: true });
    script.addEventListener('error', () => reject(new Error('Não foi possível carregar o motor do mapa.')), { once: true });
    document.head.appendChild(script);
  });
}

function boundaryCollection(fields: PrecisionField[]) {
  return {
    type: 'FeatureCollection',
    features: fields.filter(field => field.boundary).map(field => ({
      type: 'Feature',
      properties: { fieldId: field.fieldId, name: field.name },
      geometry: field.boundary
    }))
  };
}

function draftCollection(points: number[][]) {
  const features: any[] = points.map((point, index) => ({ type: 'Feature', properties: { index }, geometry: { type: 'Point', coordinates: point } }));
  if (points.length >= 2) features.push({ type: 'Feature', properties: {}, geometry: { type: 'LineString', coordinates: points } });
  return { type: 'FeatureCollection', features };
}

function formatArea(value: number | null) {
  return value == null ? '—' : `${value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 4 })} ha`;
}

export function PrecisionAgriculturePage() {
  const { platform } = useAuth();
  const allowed = Boolean(platform?.entitlements.modules.PrecisionAgriculture);
  const mapContainerRef = useRef<HTMLDivElement | null>(null);
  const mapRef = useRef<MapInstance | null>(null);
  const drawingRef = useRef(false);
  const [mapReady, setMapReady] = useState(false);
  const [mapError, setMapError] = useState<string | null>(null);
  const [farms, setFarms] = useState<Farm[]>([]);
  const [fields, setFields] = useState<PrecisionField[]>([]);
  const [farmId, setFarmId] = useState('');
  const [selectedId, setSelectedId] = useState('');
  const [draftPoints, setDraftPoints] = useState<number[][]>([]);
  const [drawing, setDrawing] = useState(false);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  drawingRef.current = drawing;
  const selected = fields.find(field => field.fieldId === selectedId) ?? null;
  const visibleFields = useMemo(() => farmId ? fields.filter(field => field.farmId === farmId) : fields, [fields, farmId]);

  const loadData = useCallback(async () => {
    if (!allowed) return;
    setLoading(true);
    setError(null);
    try {
      const [farmItems, spatialFields] = await Promise.all([
        getAllPaged<Farm>('/api/v1/farms'),
        apiRequest<PrecisionField[]>('/api/v1/precision/fields')
      ]);
      setFarms(farmItems);
      setFields(spatialFields);
      setSelectedId(current => spatialFields.some(item => item.fieldId === current) ? current : (spatialFields[0]?.fieldId ?? ''));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível carregar os dados espaciais.');
    } finally {
      setLoading(false);
    }
  }, [allowed]);

  useEffect(() => { void loadData(); }, [loadData]);

  useEffect(() => {
    if (!allowed || !mapContainerRef.current || mapRef.current) return;
    let disposed = false;
    void ensureMapLibre().then(maplibre => {
      if (disposed || !mapContainerRef.current) return;
      const map = new maplibre.Map({
        container: mapContainerRef.current,
        style: MAP_STYLE,
        center: [-52.5, -15.5],
        zoom: 3.4,
        attributionControl: true
      });
      map.addControl(new maplibre.NavigationControl(), 'top-right');
      map.on('load', () => {
        if (disposed) return;
        map.addSource('field-boundaries', { type: 'geojson', data: boundaryCollection([]) });
        map.addLayer({ id: 'field-boundaries-fill', type: 'fill', source: 'field-boundaries', paint: { 'fill-color': '#2f7d4b', 'fill-opacity': 0.2 } });
        map.addLayer({ id: 'field-boundaries-line', type: 'line', source: 'field-boundaries', paint: { 'line-color': '#1f6338', 'line-width': 2.4 } });
        map.addSource('draft-boundary', { type: 'geojson', data: draftCollection([]) });
        map.addLayer({ id: 'draft-line', type: 'line', source: 'draft-boundary', filter: ['==', '$type', 'LineString'], paint: { 'line-color': '#d97706', 'line-width': 3, 'line-dasharray': [2, 1] } });
        map.addLayer({ id: 'draft-points', type: 'circle', source: 'draft-boundary', filter: ['==', '$type', 'Point'], paint: { 'circle-radius': 6, 'circle-color': '#d97706', 'circle-stroke-color': '#ffffff', 'circle-stroke-width': 2 } });
        setMapReady(true);
      });
      map.on('click', (event: any) => {
        if (!drawingRef.current) return;
        setDraftPoints(current => [...current, [event.lngLat.lng, event.lngLat.lat]]);
      });
      mapRef.current = map;
    }).catch(err => setMapError(err instanceof Error ? err.message : 'Mapa indisponível.'));

    return () => {
      disposed = true;
      mapRef.current?.remove();
      mapRef.current = null;
    };
  }, [allowed]);

  useEffect(() => {
    if (!mapReady || !mapRef.current) return;
    mapRef.current.getSource('field-boundaries')?.setData(boundaryCollection(visibleFields));
  }, [mapReady, visibleFields]);

  useEffect(() => {
    if (!mapReady || !mapRef.current) return;
    mapRef.current.getSource('draft-boundary')?.setData(draftCollection(draftPoints));
    mapRef.current.getCanvas().style.cursor = drawing ? 'crosshair' : '';
  }, [mapReady, draftPoints, drawing]);

  const focusBoundary = (polygon: GeoJsonPolygon | null) => {
    if (!polygon || !mapRef.current || !window.maplibregl) return;
    const ring = polygon.coordinates[0];
    if (!ring?.length) return;
    const bounds = new window.maplibregl.LngLatBounds();
    ring.forEach(point => bounds.extend(point));
    mapRef.current.fitBounds(bounds, { padding: 72, maxZoom: 16, duration: 500 });
  };

  useEffect(() => { if (selected?.boundary) focusBoundary(selected.boundary); }, [selectedId, mapReady]);

  const startNew = () => {
    setDraftPoints([]);
    setDrawing(true);
    setError(null);
  };

  const redrawExisting = () => {
    if (!selected?.boundary) return;
    setDraftPoints(editableOuterRing(selected.boundary));
    setDrawing(true);
    focusBoundary(selected.boundary);
  };

  const saveBoundary = async () => {
    if (!selected || draftPoints.length < 3) return;
    setSaving(true);
    setError(null);
    try {
      const updated = await apiRequest<PrecisionField>(`/api/v1/precision/fields/${selected.fieldId}/boundary`, {
        method: 'PUT',
        body: JSON.stringify(toPolygon(draftPoints))
      });
      setFields(current => current.map(item => item.fieldId === updated.fieldId ? updated : item));
      setDraftPoints([]);
      setDrawing(false);
      focusBoundary(updated.boundary);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível salvar o limite.');
    } finally {
      setSaving(false);
    }
  };

  const deleteBoundary = async () => {
    if (!selected?.hasBoundary || !window.confirm(`Remover o limite geográfico de ${selected.name}?`)) return;
    setSaving(true);
    setError(null);
    try {
      const updated = await apiRequest<PrecisionField>(`/api/v1/precision/fields/${selected.fieldId}/boundary`, { method: 'DELETE' });
      setFields(current => current.map(item => item.fieldId === updated.fieldId ? updated : item));
      setDraftPoints([]);
      setDrawing(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível remover o limite.');
    } finally {
      setSaving(false);
    }
  };

  if (!allowed) {
    return <EmptyState title="Agricultura de precisão não está habilitada" description="Este módulo depende do entitlement PrecisionAgriculture. O backend continua sendo a autoridade final de acesso." />;
  }

  return (
    <div className="precision-page">
      <PageHeader eyebrow="Agricultura de precisão" title="Talhões georreferenciados" description="Desenhe o limite real do talhão, compare a área espacial com a área cadastral e mantenha o mapa ligado aos dados da operação." actions={<Button variant="secondary" onClick={() => void loadData()} disabled={loading}><Icon name="refresh" size={16} /> Atualizar</Button>} />

      {error && <div className="context-error"><span>{error}</span></div>}
      {loading && fields.length === 0 ? <Spinner label="Carregando mapas e talhões" /> : (
        <div className="precision-layout">
          <Card className="precision-sidebar-card">
            <div className="precision-field-group">
              <label htmlFor="precision-farm">Propriedade</label>
              <select id="precision-farm" value={farmId} onChange={event => { setFarmId(event.target.value); setSelectedId(''); setDraftPoints([]); setDrawing(false); }}>
                <option value="">Todas as propriedades</option>
                {farms.map(farm => <option key={farm.id} value={farm.id}>{farm.name}</option>)}
              </select>
            </div>
            <div className="precision-field-group">
              <label htmlFor="precision-field">Talhão</label>
              <select id="precision-field" value={selectedId} onChange={event => { setSelectedId(event.target.value); setDraftPoints([]); setDrawing(false); }}>
                <option value="">Selecione um talhão</option>
                {visibleFields.map(field => <option key={field.fieldId} value={field.fieldId}>{field.name}{field.hasBoundary ? ' • mapeado' : ''}</option>)}
              </select>
            </div>

            {selected ? <>
              <div className="precision-area-grid">
                <div><span>Área cadastral</span><strong>{formatArea(selected.registeredAreaHectares)}</strong></div>
                <div><span>Área georreferenciada</span><strong>{formatArea(selected.spatialAreaHectares)}</strong></div>
                <div><span>Diferença</span><strong>{selected.areaDifferenceHectares == null ? '—' : `${selected.areaDifferenceHectares > 0 ? '+' : ''}${selected.areaDifferenceHectares.toLocaleString('pt-BR')} ha`}</strong></div>
                <div><span>Variação</span><strong>{selected.areaDifferencePercent == null ? '—' : `${selected.areaDifferencePercent > 0 ? '+' : ''}${selected.areaDifferencePercent.toLocaleString('pt-BR')}%`}</strong></div>
              </div>
              <p className="precision-note">A área espacial é calculada pelo PostGIS sobre WGS84 e não altera automaticamente a área cadastral do talhão.</p>

              {!drawing ? <div className="precision-actions">
                <Button onClick={selected.hasBoundary ? redrawExisting : startNew}>{selected.hasBoundary ? 'Redesenhar limite' : 'Desenhar limite'}</Button>
                {selected.hasBoundary && <Button variant="danger" onClick={() => void deleteBoundary()} disabled={saving}>Remover</Button>}
              </div> : <div className="precision-editor">
                <strong>{draftPoints.length} ponto(s)</strong>
                <p>Clique no mapa para adicionar vértices na ordem do perímetro.</p>
                <div className="precision-actions">
                  <Button onClick={() => void saveBoundary()} disabled={draftPoints.length < 3 || saving}>{saving ? 'Salvando…' : 'Salvar limite'}</Button>
                  <Button variant="secondary" onClick={() => setDraftPoints(current => current.slice(0, -1))} disabled={draftPoints.length === 0}>Desfazer ponto</Button>
                  <Button variant="ghost" onClick={() => { setDraftPoints([]); setDrawing(false); }}>Cancelar</Button>
                </div>
              </div>}
            </> : <p className="precision-note">Selecione um talhão para visualizar ou cadastrar seu limite.</p>}
          </Card>

          <Card className="precision-map-card">
            {mapError && <div className="precision-map-error"><strong>Mapa indisponível</strong><span>{mapError}</span></div>}
            <div ref={mapContainerRef} className="precision-map" aria-label="Mapa dos talhões georreferenciados" />
            <div className="precision-map-caption"><span>{visibleFields.filter(field => field.hasBoundary).length} talhão(ões) mapeado(s)</span><span>SRID 4326 • GeoJSON • PostGIS</span></div>
          </Card>
        </div>
      )}
    </div>
  );
}
