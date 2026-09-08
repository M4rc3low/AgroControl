import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { Button, Card, EmptyState, PageHeader, Spinner } from '../components/Ui';
import { Icon } from '../components/Icon';
import { SceneDiscoveryPanel } from '../features/precision/SceneDiscoveryPanel';
import { apiRequest, getAllPaged } from '../lib/api';
import { useAuth } from '../lib/auth';
import type { Field, GeoJsonPolygon, PrecisionField, RemoteSceneDiscoveryItem, RemoteSensingScene, Season } from '../lib/types';
import './SceneDiscoveryPage.css';

const MAP_SCRIPT = import.meta.env.VITE_MAPLIBRE_SCRIPT_URL?.trim() || 'https://unpkg.com/maplibre-gl@5/dist/maplibre-gl.js';
const MAP_CSS = import.meta.env.VITE_MAPLIBRE_CSS_URL?.trim() || 'https://unpkg.com/maplibre-gl@5/dist/maplibre-gl.css';
const MAP_STYLE = import.meta.env.VITE_MAP_STYLE_URL?.trim() || 'https://demotiles.maplibre.org/style.json';

async function ensureMapLibre(): Promise<any> {
  const globalWindow = window as any;
  if (globalWindow.maplibregl) return globalWindow.maplibregl;
  if (!document.querySelector('link[data-agrocontrol-maplibre]')) {
    const link = document.createElement('link');
    link.rel = 'stylesheet'; link.href = MAP_CSS; link.dataset.agrocontrolMaplibre = 'true'; document.head.appendChild(link);
  }
  return new Promise((resolve, reject) => {
    const existing = document.querySelector<HTMLScriptElement>('script[data-agrocontrol-maplibre]');
    const loaded = () => globalWindow.maplibregl ? resolve(globalWindow.maplibregl) : reject(new Error('MapLibre não ficou disponível.'));
    if (existing) {
      if (globalWindow.maplibregl) return resolve(globalWindow.maplibregl);
      existing.addEventListener('load', loaded, { once: true });
      existing.addEventListener('error', () => reject(new Error('Falha ao carregar MapLibre.')), { once: true });
      return;
    }
    const script = document.createElement('script');
    script.src = MAP_SCRIPT; script.async = true; script.dataset.agrocontrolMaplibre = 'true';
    script.addEventListener('load', loaded, { once: true });
    script.addEventListener('error', () => reject(new Error('Falha ao carregar MapLibre.')), { once: true });
    document.head.appendChild(script);
  });
}

function polygonFeature(polygon: GeoJsonPolygon | null) {
  return polygon ? { type: 'Feature', properties: {}, geometry: polygon } : { type: 'FeatureCollection', features: [] };
}

function parseDiscoveredPolygon(item: RemoteSceneDiscoveryItem | null): GeoJsonPolygon | null {
  if (!item?.geometryGeoJson) return null;
  try {
    const parsed = JSON.parse(item.geometryGeoJson) as { type?: string; coordinates?: number[][][] };
    return parsed.type === 'Polygon' && Array.isArray(parsed.coordinates)
      ? { type: 'Polygon', coordinates: parsed.coordinates }
      : null;
  } catch { return null; }
}

export function SceneDiscoveryPage() {
  const { platform } = useAuth();
  const allowed = Boolean(platform?.entitlements.modules.PrecisionAgriculture);
  const [fields, setFields] = useState<Field[]>([]);
  const [seasons, setSeasons] = useState<Season[]>([]);
  const [fieldId, setFieldId] = useState('');
  const [seasonId, setSeasonId] = useState('');
  const [precisionField, setPrecisionField] = useState<PrecisionField | null>(null);
  const [preview, setPreview] = useState<RemoteSceneDiscoveryItem | null>(null);
  const [lastImported, setLastImported] = useState<RemoteSensingScene | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const mapContainerRef = useRef<HTMLDivElement | null>(null);
  const mapRef = useRef<any>(null);
  const [mapReady, setMapReady] = useState(false);

  const filteredSeasons = useMemo(() => seasons.filter(item => item.fieldId === fieldId), [seasons, fieldId]);

  const loadContext = useCallback(async () => {
    if (!allowed) { setLoading(false); return; }
    setLoading(true); setError(null);
    try {
      const [fieldItems, seasonItems] = await Promise.all([
        getAllPaged<Field>('/api/v1/fields'),
        getAllPaged<Season>('/api/v1/seasons')
      ]);
      setFields(fieldItems);
      setSeasons(seasonItems);
      setFieldId(current => fieldItems.some(item => item.id === current) ? current : (fieldItems[0]?.id ?? ''));
    } catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível carregar os talhões.'); }
    finally { setLoading(false); }
  }, [allowed]);

  useEffect(() => { void loadContext(); }, [loadContext]);
  useEffect(() => { setSeasonId(''); setPreview(null); setLastImported(null); }, [fieldId]);

  useEffect(() => {
    if (!fieldId) { setPrecisionField(null); return; }
    void apiRequest<PrecisionField>(`/api/v1/precision/fields/${fieldId}/boundary`)
      .then(setPrecisionField)
      .catch(err => { setPrecisionField(null); setError(err instanceof Error ? err.message : 'Não foi possível carregar o limite do talhão.'); });
  }, [fieldId]);

  useEffect(() => {
    if (!allowed || !mapContainerRef.current || mapRef.current) return;
    let disposed = false;
    void ensureMapLibre().then(maplibre => {
      if (disposed || !mapContainerRef.current) return;
      const map = new maplibre.Map({ container: mapContainerRef.current, style: MAP_STYLE, center: [-52.5, -15.5], zoom: 3.4 });
      map.addControl(new maplibre.NavigationControl(), 'top-right');
      map.on('load', () => {
        if (disposed) return;
        map.addSource('discovery-field', { type: 'geojson', data: polygonFeature(null) });
        map.addLayer({ id: 'discovery-field-line', type: 'line', source: 'discovery-field', paint: { 'line-color': '#1f6338', 'line-width': 2.5 } });
        map.addSource('discovery-item', { type: 'geojson', data: polygonFeature(null) });
        map.addLayer({ id: 'discovery-item-fill', type: 'fill', source: 'discovery-item', paint: { 'fill-color': '#2563eb', 'fill-opacity': 0.2 } });
        map.addLayer({ id: 'discovery-item-line', type: 'line', source: 'discovery-item', paint: { 'line-color': '#1d4ed8', 'line-width': 2.5, 'line-dasharray': [2, 1] } });
        setMapReady(true);
      });
      mapRef.current = map;
    }).catch(err => setError(err instanceof Error ? err.message : 'Mapa indisponível.'));
    return () => { disposed = true; mapRef.current?.remove(); mapRef.current = null; };
  }, [allowed]);

  useEffect(() => {
    if (!mapReady || !mapRef.current) return;
    const discovered = parseDiscoveredPolygon(preview);
    const field = precisionField?.boundary ?? null;
    mapRef.current.getSource('discovery-field')?.setData(polygonFeature(field));
    mapRef.current.getSource('discovery-item')?.setData(polygonFeature(discovered));
    const polygon = discovered ?? field;
    const maplibre = (window as any).maplibregl;
    if (polygon?.coordinates?.[0]?.length && maplibre) {
      const bounds = new maplibre.LngLatBounds();
      polygon.coordinates[0].forEach((point: number[]) => bounds.extend(point));
      mapRef.current.fitBounds(bounds, { padding: 56, maxZoom: 13, duration: 350 });
    }
  }, [mapReady, precisionField, preview]);

  if (!allowed) return <EmptyState title="Descoberta STAC não está habilitada" description="O recurso depende do módulo Agricultura de Precisão e da autorização do backend." />;
  if (loading) return <Spinner label="Carregando contexto de sensoriamento" />;

  return <div className="scene-discovery-page">
    <PageHeader eyebrow="Sensoriamento remoto" title="Descoberta de cenas STAC" description="Busque cenas públicas pelo limite do talhão, avalie metadados e importe apenas o asset raster escolhido." actions={<><Link className="button button--ghost" to="/precision/remote-sensing"><Icon name="spark" size={16} /> Voltar ao sensoriamento</Link><Button variant="secondary" onClick={() => void loadContext()}><Icon name="refresh" size={16} /> Atualizar</Button></>} />
    <div className="remote-disclaimer">A descoberta consulta catálogos configurados pelo backend. Nenhum asset é baixado ou processado automaticamente.</div>
    {error && <div className="context-error"><span>{error}</span></div>}
    {lastImported && <div className="scene-discovery-page__success"><strong>Cena importada:</strong> {lastImported.externalId}. Ela já está disponível no workspace de Sensoriamento Remoto.</div>}

    <Card className="scene-discovery-page__context">
      <label>Talhão<select value={fieldId} onChange={event => setFieldId(event.target.value)}><option value="">Selecione</option>{fields.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
      <label>Safra<select value={seasonId} onChange={event => setSeasonId(event.target.value)}><option value="">Todas / sem vínculo</option>{filteredSeasons.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
      <div className="scene-discovery-page__boundary"><span>Limite do talhão</span><strong>{precisionField?.hasBoundary ? 'Disponível' : 'Não cadastrado'}</strong></div>
    </Card>

    <div className="scene-discovery-page__layout">
      <div className="scene-discovery-page__map-card"><div ref={mapContainerRef} className="scene-discovery-page__map" /><div className="scene-discovery-page__legend"><span><i className="scene-discovery-page__legend-field" />Talhão</span><span><i className="scene-discovery-page__legend-item" />Cena descoberta</span></div></div>
      <div className="scene-discovery-page__preview"><span className="eyebrow">Pré-visualização</span><h3>{preview?.externalId ?? 'Selecione um resultado'}</h3><p>{preview ? `${preview.collection} · ${new Date(preview.acquiredAtUtc).toLocaleString('pt-BR')}` : 'Use Ver footprint em uma cena descoberta para compará-la com o talhão.'}</p></div>
    </div>

    <SceneDiscoveryPanel fieldId={fieldId} seasonId={seasonId} onPreview={setPreview} onImported={async scene => { setLastImported(scene); }} />
  </div>;
}
