import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Button, Card, EmptyState, PageHeader, Spinner } from '../components/Ui';
import { Icon } from '../components/Icon';
import { editableOuterRing, toPolygon } from '../features/precision/geometry';
import { apiRequest, getAllPaged } from '../lib/api';
import { useAuth } from '../lib/auth';
import type { Farm, GeoJsonPolygon, ManagementZone, ManagementZoneImportResult, PrecisionField } from '../lib/types';
import './precision.css';

type MapInstance = any;
type MapLibreGlobal = { Map: new (options: any) => MapInstance; NavigationControl: new () => any; LngLatBounds: new () => any };
type PendingImport = { name: string; document: any; featureCount: number };

declare global { interface Window { maplibregl?: MapLibreGlobal; } }

const MAP_SCRIPT = import.meta.env.VITE_MAPLIBRE_SCRIPT_URL?.trim() || 'https://unpkg.com/maplibre-gl@5/dist/maplibre-gl.js';
const MAP_CSS = import.meta.env.VITE_MAPLIBRE_CSS_URL?.trim() || 'https://unpkg.com/maplibre-gl@5/dist/maplibre-gl.css';
const MAP_STYLE = import.meta.env.VITE_MAP_STYLE_URL?.trim() || 'https://demotiles.maplibre.org/style.json';

function ensureMapLibre(): Promise<MapLibreGlobal> {
  if (window.maplibregl) return Promise.resolve(window.maplibregl);
  if (!document.querySelector('link[data-agrocontrol-maplibre]')) {
    const link = document.createElement('link'); link.rel = 'stylesheet'; link.href = MAP_CSS; link.dataset.agrocontrolMaplibre = 'true'; document.head.appendChild(link);
  }
  return new Promise((resolve, reject) => {
    const existing = document.querySelector<HTMLScriptElement>('script[data-agrocontrol-maplibre]');
    const resolveLoaded = () => window.maplibregl ? resolve(window.maplibregl) : reject(new Error('MapLibre não ficou disponível no navegador.'));
    if (existing) { existing.addEventListener('load', resolveLoaded, { once: true }); existing.addEventListener('error', () => reject(new Error('Não foi possível carregar o motor do mapa.')), { once: true }); return; }
    const script = document.createElement('script'); script.src = MAP_SCRIPT; script.async = true; script.dataset.agrocontrolMaplibre = 'true';
    script.addEventListener('load', resolveLoaded, { once: true }); script.addEventListener('error', () => reject(new Error('Não foi possível carregar o motor do mapa.')), { once: true }); document.head.appendChild(script);
  });
}

const emptyCollection = () => ({ type: 'FeatureCollection', features: [] as any[] });
function boundaryCollection(fields: PrecisionField[]) { return { type: 'FeatureCollection', features: fields.filter(f => f.boundary).map(f => ({ type: 'Feature', properties: { fieldId: f.fieldId, name: f.name }, geometry: f.boundary })) }; }
function zoneCollection(zones: ManagementZone[]) { return { type: 'FeatureCollection', features: zones.map(z => ({ type: 'Feature', properties: { zoneId: z.id, name: z.name, zoneType: z.type, classification: z.classification }, geometry: z.geometry })) }; }
function draftCollection(points: number[][]) { const features: any[] = points.map((point, index) => ({ type: 'Feature', properties: { index }, geometry: { type: 'Point', coordinates: point } })); if (points.length >= 2) features.push({ type: 'Feature', properties: {}, geometry: { type: 'LineString', coordinates: points } }); return { type: 'FeatureCollection', features }; }
function formatArea(value: number | null) { return value == null ? '—' : `${value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 4 })} ha`; }

export function PrecisionAgriculturePage() {
  const { platform } = useAuth();
  const allowed = Boolean(platform?.entitlements.modules.PrecisionAgriculture);
  const mapContainerRef = useRef<HTMLDivElement | null>(null);
  const mapRef = useRef<MapInstance | null>(null);
  const drawingRef = useRef(false);
  const uploadRef = useRef<HTMLInputElement | null>(null);
  const [mapReady, setMapReady] = useState(false);
  const [mapError, setMapError] = useState<string | null>(null);
  const [farms, setFarms] = useState<Farm[]>([]);
  const [fields, setFields] = useState<PrecisionField[]>([]);
  const [zones, setZones] = useState<ManagementZone[]>([]);
  const [zonesVisible, setZonesVisible] = useState(true);
  const [pendingImport, setPendingImport] = useState<PendingImport | null>(null);
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
    setLoading(true); setError(null);
    try {
      const [farmItems, spatialFields] = await Promise.all([getAllPaged<Farm>('/api/v1/farms'), apiRequest<PrecisionField[]>('/api/v1/precision/fields')]);
      setFarms(farmItems); setFields(spatialFields);
      setSelectedId(current => spatialFields.some(item => item.fieldId === current) ? current : (spatialFields[0]?.fieldId ?? ''));
    } catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível carregar os dados espaciais.'); }
    finally { setLoading(false); }
  }, [allowed]);

  const loadZones = useCallback(async (fieldId: string) => {
    if (!fieldId) { setZones([]); return; }
    try { setZones(await apiRequest<ManagementZone[]>(`/api/v1/precision/zones?fieldId=${fieldId}`)); }
    catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível carregar as zonas de manejo.'); }
  }, []);

  useEffect(() => { void loadData(); }, [loadData]);
  useEffect(() => { setPendingImport(null); void loadZones(selectedId); }, [selectedId, loadZones]);

  useEffect(() => {
    if (!allowed || !mapContainerRef.current || mapRef.current) return;
    let disposed = false;
    void ensureMapLibre().then(maplibre => {
      if (disposed || !mapContainerRef.current) return;
      const map = new maplibre.Map({ container: mapContainerRef.current, style: MAP_STYLE, center: [-52.5, -15.5], zoom: 3.4, attributionControl: true });
      map.addControl(new maplibre.NavigationControl(), 'top-right');
      map.on('load', () => {
        if (disposed) return;
        map.addSource('field-boundaries', { type: 'geojson', data: emptyCollection() });
        map.addLayer({ id: 'field-boundaries-fill', type: 'fill', source: 'field-boundaries', paint: { 'fill-color': '#2f7d4b', 'fill-opacity': 0.16 } });
        map.addLayer({ id: 'field-boundaries-line', type: 'line', source: 'field-boundaries', paint: { 'line-color': '#1f6338', 'line-width': 2.4 } });
        map.addSource('management-zones', { type: 'geojson', data: emptyCollection() });
        map.addLayer({ id: 'management-zones-fill', type: 'fill', source: 'management-zones', paint: { 'fill-color': '#3167a8', 'fill-opacity': 0.32 } });
        map.addLayer({ id: 'management-zones-line', type: 'line', source: 'management-zones', paint: { 'line-color': '#214a7b', 'line-width': 2 } });
        map.addSource('zone-preview', { type: 'geojson', data: emptyCollection() });
        map.addLayer({ id: 'zone-preview-fill', type: 'fill', source: 'zone-preview', paint: { 'fill-color': '#d97706', 'fill-opacity': 0.22 } });
        map.addLayer({ id: 'zone-preview-line', type: 'line', source: 'zone-preview', paint: { 'line-color': '#d97706', 'line-width': 2.5, 'line-dasharray': [2, 1] } });
        map.addSource('draft-boundary', { type: 'geojson', data: draftCollection([]) });
        map.addLayer({ id: 'draft-line', type: 'line', source: 'draft-boundary', filter: ['==', '$type', 'LineString'], paint: { 'line-color': '#d97706', 'line-width': 3, 'line-dasharray': [2, 1] } });
        map.addLayer({ id: 'draft-points', type: 'circle', source: 'draft-boundary', filter: ['==', '$type', 'Point'], paint: { 'circle-radius': 6, 'circle-color': '#d97706', 'circle-stroke-color': '#ffffff', 'circle-stroke-width': 2 } });
        setMapReady(true);
      });
      map.on('click', (event: any) => { if (drawingRef.current) setDraftPoints(current => [...current, [event.lngLat.lng, event.lngLat.lat]]); });
      mapRef.current = map;
    }).catch(err => setMapError(err instanceof Error ? err.message : 'Mapa indisponível.'));
    return () => { disposed = true; mapRef.current?.remove(); mapRef.current = null; };
  }, [allowed]);

  useEffect(() => { if (mapReady) mapRef.current?.getSource('field-boundaries')?.setData(boundaryCollection(visibleFields)); }, [mapReady, visibleFields]);
  useEffect(() => { if (mapReady) mapRef.current?.getSource('management-zones')?.setData(zonesVisible ? zoneCollection(zones) : emptyCollection()); }, [mapReady, zones, zonesVisible]);
  useEffect(() => { if (mapReady) mapRef.current?.getSource('zone-preview')?.setData(pendingImport?.document ?? emptyCollection()); }, [mapReady, pendingImport]);
  useEffect(() => { if (mapReady && mapRef.current) { mapRef.current.getSource('draft-boundary')?.setData(draftCollection(draftPoints)); mapRef.current.getCanvas().style.cursor = drawing ? 'crosshair' : ''; } }, [mapReady, draftPoints, drawing]);

  const focusBoundary = (polygon: GeoJsonPolygon | null) => {
    if (!polygon || !mapRef.current || !window.maplibregl) return;
    const ring = polygon.coordinates[0]; if (!ring?.length) return;
    const bounds = new window.maplibregl.LngLatBounds(); ring.forEach(point => bounds.extend(point)); mapRef.current.fitBounds(bounds, { padding: 72, maxZoom: 16, duration: 500 });
  };
  useEffect(() => { if (selected?.boundary) focusBoundary(selected.boundary); }, [selectedId, mapReady]);

  const saveBoundary = async () => {
    if (!selected || draftPoints.length < 3) return;
    setSaving(true); setError(null);
    try {
      const updated = await apiRequest<PrecisionField>(`/api/v1/precision/fields/${selected.fieldId}/boundary`, { method: 'PUT', body: JSON.stringify(toPolygon(draftPoints)) });
      setFields(current => current.map(item => item.fieldId === updated.fieldId ? updated : item)); setDraftPoints([]); setDrawing(false); focusBoundary(updated.boundary);
    } catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível salvar o limite.'); }
    finally { setSaving(false); }
  };

  const deleteBoundary = async () => {
    if (!selected?.hasBoundary || !window.confirm(`Remover o limite geográfico de ${selected.name}?`)) return;
    setSaving(true); setError(null);
    try { const updated = await apiRequest<PrecisionField>(`/api/v1/precision/fields/${selected.fieldId}/boundary`, { method: 'DELETE' }); setFields(current => current.map(item => item.fieldId === updated.fieldId ? updated : item)); setDraftPoints([]); setDrawing(false); }
    catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível remover o limite.'); }
    finally { setSaving(false); }
  };

  const chooseGeoJson = async (file: File | null) => {
    if (!file) return;
    setError(null);
    try {
      if (file.size > 2 * 1024 * 1024) throw new Error('O arquivo excede o limite de 2 MB.');
      const document = JSON.parse(await file.text());
      const features = document?.type === 'FeatureCollection' && Array.isArray(document.features) ? document.features.length : document?.type === 'Feature' ? 1 : 0;
      if (!features) throw new Error('Use um GeoJSON Feature ou FeatureCollection com pelo menos uma feature.');
      setPendingImport({ name: file.name, document, featureCount: features });
    } catch (err) { setPendingImport(null); setError(err instanceof Error ? err.message : 'Arquivo GeoJSON inválido.'); }
    finally { if (uploadRef.current) uploadRef.current.value = ''; }
  };

  const importZones = async () => {
    if (!selected || !pendingImport) return;
    setSaving(true); setError(null);
    try {
      const result = await apiRequest<ManagementZoneImportResult>(`/api/v1/precision/fields/${selected.fieldId}/zones/import`, { method: 'POST', body: JSON.stringify(pendingImport.document) });
      setPendingImport(null); await loadZones(selected.fieldId); if (result.items[0]) focusBoundary(result.items[0].geometry);
    } catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível importar as zonas.'); }
    finally { setSaving(false); }
  };

  const exportZones = async () => {
    if (!selected) return;
    try {
      const document = await apiRequest<any>(`/api/v1/precision/fields/${selected.fieldId}/zones/export`);
      const url = URL.createObjectURL(new Blob([JSON.stringify(document, null, 2)], { type: 'application/geo+json' }));
      const link = window.document.createElement('a'); link.href = url; link.download = `${selected.name.replace(/[^a-z0-9-_]+/gi, '-')}-zonas.geojson`; link.click(); URL.revokeObjectURL(url);
    } catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível exportar as zonas.'); }
  };

  const deleteZone = async (zone: ManagementZone) => {
    if (!window.confirm(`Desativar a zona ${zone.name}?`)) return;
    try { await apiRequest<ManagementZone>(`/api/v1/precision/zones/${zone.id}`, { method: 'DELETE' }); await loadZones(zone.fieldId); }
    catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível remover a zona.'); }
  };

  if (!allowed) return <EmptyState title="Agricultura de precisão não está habilitada" description="Este módulo depende do entitlement PrecisionAgriculture. O backend continua sendo a autoridade final de acesso." />;

  return <div className="precision-page">
    <PageHeader eyebrow="Agricultura de precisão" title="Talhões, GeoJSON e zonas de manejo" description="Mapeie os limites dos talhões, importe zonas produzidas em ferramentas GIS e mantenha camadas operacionais ligadas ao contexto real da fazenda." actions={<Button variant="secondary" onClick={() => void loadData()} disabled={loading}><Icon name="refresh" size={16} /> Atualizar</Button>} />
    {error && <div className="context-error"><span>{error}</span></div>}
    {loading && fields.length === 0 ? <Spinner label="Carregando mapas e talhões" /> : <div className="precision-layout">
      <Card className="precision-sidebar-card">
        <div className="precision-field-group"><label htmlFor="precision-farm">Propriedade</label><select id="precision-farm" value={farmId} onChange={event => { setFarmId(event.target.value); setSelectedId(''); setDraftPoints([]); setDrawing(false); }}><option value="">Todas as propriedades</option>{farms.map(farm => <option key={farm.id} value={farm.id}>{farm.name}</option>)}</select></div>
        <div className="precision-field-group"><label htmlFor="precision-field">Talhão</label><select id="precision-field" value={selectedId} onChange={event => { setSelectedId(event.target.value); setDraftPoints([]); setDrawing(false); }}><option value="">Selecione um talhão</option>{visibleFields.map(field => <option key={field.fieldId} value={field.fieldId}>{field.name}{field.hasBoundary ? ' • mapeado' : ''}</option>)}</select></div>
        {selected ? <>
          <div className="precision-area-grid"><div><span>Área cadastral</span><strong>{formatArea(selected.registeredAreaHectares)}</strong></div><div><span>Área georreferenciada</span><strong>{formatArea(selected.spatialAreaHectares)}</strong></div><div><span>Diferença</span><strong>{selected.areaDifferenceHectares == null ? '—' : `${selected.areaDifferenceHectares > 0 ? '+' : ''}${selected.areaDifferenceHectares.toLocaleString('pt-BR')} ha`}</strong></div><div><span>Variação</span><strong>{selected.areaDifferencePercent == null ? '—' : `${selected.areaDifferencePercent > 0 ? '+' : ''}${selected.areaDifferencePercent.toLocaleString('pt-BR')}%`}</strong></div></div>
          <p className="precision-note">A área espacial é calculada pelo PostGIS e não altera automaticamente a área cadastral do talhão.</p>
          {!drawing ? <div className="precision-actions"><Button onClick={() => { setDraftPoints(selected.boundary ? editableOuterRing(selected.boundary) : []); setDrawing(true); }}>{selected.hasBoundary ? 'Redesenhar limite' : 'Desenhar limite'}</Button>{selected.hasBoundary && <Button variant="danger" onClick={() => void deleteBoundary()} disabled={saving}>Remover</Button>}</div> : <div className="precision-editor"><strong>{draftPoints.length} ponto(s)</strong><p>Clique no mapa para adicionar vértices na ordem do perímetro.</p><div className="precision-actions"><Button onClick={() => void saveBoundary()} disabled={draftPoints.length < 3 || saving}>{saving ? 'Salvando…' : 'Salvar limite'}</Button><Button variant="secondary" onClick={() => setDraftPoints(current => current.slice(0, -1))} disabled={!draftPoints.length}>Desfazer</Button><Button variant="ghost" onClick={() => { setDraftPoints([]); setDrawing(false); }}>Cancelar</Button></div></div>}
          <section className="zones-panel">
            <div className="zones-panel__header"><div><strong>Zonas de manejo</strong><span>{zones.length} ativa(s)</span></div><button className="zones-visibility" type="button" onClick={() => setZonesVisible(value => !value)}>{zonesVisible ? 'Ocultar' : 'Mostrar'}</button></div>
            <input ref={uploadRef} type="file" accept=".geojson,.json,application/geo+json,application/json" hidden onChange={event => void chooseGeoJson(event.target.files?.[0] ?? null)} />
            <div className="precision-actions"><Button variant="secondary" onClick={() => uploadRef.current?.click()}>Importar GeoJSON</Button><Button variant="ghost" onClick={() => void exportZones()} disabled={!zones.length}>Exportar</Button></div>
            {pendingImport && <div className="zone-preview"><strong>{pendingImport.name}</strong><span>{pendingImport.featureCount} feature(s) em pré-visualização</span><div className="precision-actions"><Button onClick={() => void importZones()} disabled={saving}>{saving ? 'Importando…' : 'Confirmar importação'}</Button><Button variant="ghost" onClick={() => setPendingImport(null)}>Cancelar</Button></div></div>}
            <div className="zone-list">{zones.map(zone => <div className="zone-item" key={zone.id}><button type="button" onClick={() => focusBoundary(zone.geometry)}><strong>{zone.name}</strong><span>{zone.type}{zone.classification ? ` • ${zone.classification}` : ''}</span><small>{formatArea(zone.spatialAreaHectares)}{zone.value != null ? ` • ${zone.value.toLocaleString('pt-BR')} ${zone.unit ?? ''}` : ''}</small></button><button className="zone-item__delete" type="button" aria-label={`Remover ${zone.name}`} onClick={() => void deleteZone(zone)}>×</button></div>)}{!zones.length && <p className="precision-note">Nenhuma zona cadastrada neste talhão.</p>}</div>
          </section>
        </> : <p className="precision-note">Selecione um talhão para visualizar limites e zonas.</p>}
      </Card>
      <Card className="precision-map-card">{mapError && <div className="precision-map-error"><strong>Mapa indisponível</strong><span>{mapError}</span></div>}<div ref={mapContainerRef} className="precision-map" aria-label="Mapa dos talhões e zonas de manejo" /><div className="precision-map-caption"><span>{visibleFields.filter(field => field.hasBoundary).length} talhão(ões) mapeado(s) • {zones.length} zona(s)</span><span>WGS84 • GeoJSON • PostGIS</span></div></Card>
    </div>}
  </div>;
}
