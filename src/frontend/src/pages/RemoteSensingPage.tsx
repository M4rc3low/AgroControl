import { FormEvent, useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { Badge, Button, Card, EmptyState, Modal, PageHeader, Spinner } from '../components/Ui';
import { Icon } from '../components/Icon';
import { buildSeriesPath, formatIndexValue, groupRemoteSensingSeries } from '../features/precision/remoteSensing';
import { apiRequest, getAllPaged } from '../lib/api';
import { useAuth } from '../lib/auth';
import type {
  Field, GeoJsonPolygon, ManagementZone, PagedResult, PrecisionField, RemoteSensingScene,
  RemoteSensingSeriesPoint, RemoteSensingSummary, Season, VegetationIndexObservation
} from '../lib/types';
import './RemoteSensingPage.css';

const MAP_SCRIPT = import.meta.env.VITE_MAPLIBRE_SCRIPT_URL?.trim() || 'https://unpkg.com/maplibre-gl@5/dist/maplibre-gl.js';
const MAP_CSS = import.meta.env.VITE_MAPLIBRE_CSS_URL?.trim() || 'https://unpkg.com/maplibre-gl@5/dist/maplibre-gl.css';
const MAP_STYLE = import.meta.env.VITE_MAP_STYLE_URL?.trim() || 'https://demotiles.maplibre.org/style.json';

function query(values: Record<string, string>) {
  const params = new URLSearchParams();
  Object.entries(values).forEach(([key, value]) => { if (value) params.set(key, value); });
  return params.toString() ? `?${params.toString()}` : '';
}

function toInputDateTime(value = new Date()) {
  const local = new Date(value.getTime() - value.getTimezoneOffset() * 60_000);
  return local.toISOString().slice(0, 16);
}

function parseOptionalNumber(value: FormDataEntryValue | null) {
  const text = String(value ?? '').trim();
  return text ? Number(text) : null;
}

function parseFootprint(value: FormDataEntryValue | null): GeoJsonPolygon | null {
  const text = String(value ?? '').trim();
  if (!text) return null;
  const parsed = JSON.parse(text) as GeoJsonPolygon;
  if (parsed.type !== 'Polygon' || !Array.isArray(parsed.coordinates)) throw new Error('O footprint precisa ser um GeoJSON Polygon válido.');
  return parsed;
}

function formatDate(value: string) {
  return new Date(value).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' });
}

function coverageTone(value: number | null): 'success' | 'warning' | 'danger' | 'neutral' {
  if (value == null) return 'neutral';
  if (value >= 90) return 'success';
  if (value >= 70) return 'warning';
  return 'danger';
}

async function ensureMapLibre(): Promise<any> {
  const globalWindow = window as any;
  if (globalWindow.maplibregl) return globalWindow.maplibregl;
  if (!document.querySelector('link[data-agrocontrol-maplibre]')) {
    const link = document.createElement('link'); link.rel = 'stylesheet'; link.href = MAP_CSS; link.dataset.agrocontrolMaplibre = 'true'; document.head.appendChild(link);
  }
  return new Promise((resolve, reject) => {
    const existing = document.querySelector<HTMLScriptElement>('script[data-agrocontrol-maplibre]');
    const loaded = () => globalWindow.maplibregl ? resolve(globalWindow.maplibregl) : reject(new Error('MapLibre não ficou disponível.'));
    if (existing) { existing.addEventListener('load', loaded, { once: true }); existing.addEventListener('error', () => reject(new Error('Falha ao carregar MapLibre.')), { once: true }); return; }
    const script = document.createElement('script'); script.src = MAP_SCRIPT; script.async = true; script.dataset.agrocontrolMaplibre = 'true';
    script.addEventListener('load', loaded, { once: true }); script.addEventListener('error', () => reject(new Error('Falha ao carregar MapLibre.')), { once: true }); document.head.appendChild(script);
  });
}

function polygonFeature(polygon: GeoJsonPolygon | null) {
  return polygon ? { type: 'Feature', properties: {}, geometry: polygon } : { type: 'FeatureCollection', features: [] };
}

export function RemoteSensingPage() {
  const { platform } = useAuth();
  const allowed = Boolean(platform?.entitlements.modules.PrecisionAgriculture);
  const mapContainerRef = useRef<HTMLDivElement | null>(null);
  const mapRef = useRef<any>(null);
  const [mapReady, setMapReady] = useState(false);
  const [fields, setFields] = useState<Field[]>([]);
  const [seasons, setSeasons] = useState<Season[]>([]);
  const [zones, setZones] = useState<ManagementZone[]>([]);
  const [precisionField, setPrecisionField] = useState<PrecisionField | null>(null);
  const [fieldId, setFieldId] = useState('');
  const [seasonId, setSeasonId] = useState('');
  const [zoneId, setZoneId] = useState('');
  const [scenes, setScenes] = useState<RemoteSensingScene[]>([]);
  const [selectedSceneId, setSelectedSceneId] = useState('');
  const [observations, setObservations] = useState<VegetationIndexObservation[]>([]);
  const [series, setSeries] = useState<RemoteSensingSeriesPoint[]>([]);
  const [summary, setSummary] = useState<RemoteSensingSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [sceneModal, setSceneModal] = useState(false);
  const [observationModal, setObservationModal] = useState(false);
  const [indexFilter, setIndexFilter] = useState('');

  const filteredSeasons = useMemo(() => seasons.filter(item => item.fieldId === fieldId), [seasons, fieldId]);
  const selectedScene = scenes.find(item => item.id === selectedSceneId) ?? scenes[0] ?? null;
  const seriesGroups = useMemo(() => groupRemoteSensingSeries(series), [series]);

  const loadBase = useCallback(async () => {
    if (!allowed) { setLoading(false); return; }
    setLoading(true); setError(null);
    try {
      const [fieldItems, seasonItems] = await Promise.all([getAllPaged<Field>('/api/v1/fields'), getAllPaged<Season>('/api/v1/seasons')]);
      setFields(fieldItems); setSeasons(seasonItems);
      setFieldId(current => fieldItems.some(item => item.id === current) ? current : (fieldItems[0]?.id ?? ''));
    } catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível carregar o contexto produtivo.'); }
    finally { setLoading(false); }
  }, [allowed]);

  const loadRemote = useCallback(async () => {
    if (!allowed || !fieldId) { setScenes([]); setSeries([]); setSummary(null); return; }
    setLoading(true); setError(null);
    try {
      const filters = { fieldId, seasonId, managementZoneId: zoneId, indexType: indexFilter };
      const sceneQuery = query({ fieldId, seasonId, page: '1', pageSize: '50' });
      const observationQuery = query({ ...filters, page: '1', pageSize: '50' });
      const seriesQuery = query({ ...filters, take: '500' });
      const summaryQuery = query({ fieldId, seasonId, managementZoneId: zoneId });
      const [zoneItems, spatialField, scenePage, observationPage, seriesItems, summaryData] = await Promise.all([
        apiRequest<ManagementZone[]>(`/api/v1/precision/zones?fieldId=${fieldId}`),
        apiRequest<PrecisionField>(`/api/v1/precision/fields/${fieldId}/boundary`),
        apiRequest<PagedResult<RemoteSensingScene>>(`/api/v1/precision/remote-sensing/scenes${sceneQuery}`),
        apiRequest<PagedResult<VegetationIndexObservation>>(`/api/v1/precision/remote-sensing/observations${observationQuery}`),
        apiRequest<RemoteSensingSeriesPoint[]>(`/api/v1/precision/remote-sensing/series${seriesQuery}`),
        apiRequest<RemoteSensingSummary>(`/api/v1/precision/remote-sensing/summary${summaryQuery}`)
      ]);
      setZones(zoneItems); setPrecisionField(spatialField); setScenes(scenePage.items); setObservations(observationPage.items);
      setSeries(seriesItems); setSummary(summaryData);
      setSelectedSceneId(current => scenePage.items.some(item => item.id === current) ? current : (scenePage.items[0]?.id ?? ''));
    } catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível carregar o sensoriamento remoto.'); }
    finally { setLoading(false); }
  }, [allowed, fieldId, seasonId, zoneId, indexFilter]);

  useEffect(() => { void loadBase(); }, [loadBase]);
  useEffect(() => { setSeasonId(''); setZoneId(''); }, [fieldId]);
  useEffect(() => { void loadRemote(); }, [loadRemote]);

  useEffect(() => {
    if (!allowed || !mapContainerRef.current || mapRef.current) return;
    let disposed = false;
    void ensureMapLibre().then(maplibre => {
      if (disposed || !mapContainerRef.current) return;
      const map = new maplibre.Map({ container: mapContainerRef.current, style: MAP_STYLE, center: [-52.5, -15.5], zoom: 3.4 });
      map.addControl(new maplibre.NavigationControl(), 'top-right');
      map.on('load', () => {
        if (disposed) return;
        map.addSource('remote-field', { type: 'geojson', data: polygonFeature(null) });
        map.addLayer({ id: 'remote-field-line', type: 'line', source: 'remote-field', paint: { 'line-color': '#1f6338', 'line-width': 2.5 } });
        map.addSource('remote-scene', { type: 'geojson', data: polygonFeature(null) });
        map.addLayer({ id: 'remote-scene-fill', type: 'fill', source: 'remote-scene', paint: { 'fill-color': '#7c3aed', 'fill-opacity': 0.25 } });
        map.addLayer({ id: 'remote-scene-line', type: 'line', source: 'remote-scene', paint: { 'line-color': '#6d28d9', 'line-width': 2.5, 'line-dasharray': [2, 1] } });
        setMapReady(true);
      });
      mapRef.current = map;
    }).catch(err => setError(err instanceof Error ? err.message : 'Mapa indisponível.'));
    return () => { disposed = true; mapRef.current?.remove(); mapRef.current = null; };
  }, [allowed]);

  useEffect(() => {
    if (!mapReady || !mapRef.current) return;
    mapRef.current.getSource('remote-field')?.setData(polygonFeature(precisionField?.boundary ?? null));
    mapRef.current.getSource('remote-scene')?.setData(polygonFeature(selectedScene?.footprint ?? null));
    const polygon = selectedScene?.footprint ?? precisionField?.boundary;
    const maplibre = (window as any).maplibregl;
    if (polygon?.coordinates?.[0]?.length && maplibre) {
      const bounds = new maplibre.LngLatBounds(); polygon.coordinates[0].forEach((point: number[]) => bounds.extend(point));
      mapRef.current.fitBounds(bounds, { padding: 56, maxZoom: 15, duration: 350 });
    }
  }, [mapReady, precisionField, selectedSceneId, selectedScene]);

  async function createScene(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!fieldId) return;
    const form = new FormData(event.currentTarget); setSaving(true); setError(null);
    try {
      const created = await apiRequest<RemoteSensingScene>('/api/v1/precision/remote-sensing/scenes', {
        method: 'POST', body: JSON.stringify({
          fieldId, seasonId: seasonId || null, provider: String(form.get('provider') ?? ''), externalId: String(form.get('externalId') ?? ''),
          platform: String(form.get('platform') ?? 'Satellite'), acquiredAtUtc: new Date(String(form.get('acquiredAtUtc'))).toISOString(),
          cloudCoveragePercent: parseOptionalNumber(form.get('cloudCoveragePercent')), spatialResolutionMeters: parseOptionalNumber(form.get('spatialResolutionMeters')),
          assetReference: String(form.get('assetReference') ?? '').trim() || null, notes: String(form.get('notes') ?? '').trim() || null,
          footprint: parseFootprint(form.get('footprint'))
        })
      });
      setSceneModal(false); setSelectedSceneId(created.id); await loadRemote();
    } catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível cadastrar a cena.'); }
    finally { setSaving(false); }
  }

  async function createObservation(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget); const sceneId = String(form.get('sceneId') ?? ''); if (!sceneId) return;
    setSaving(true); setError(null);
    try {
      await apiRequest<VegetationIndexObservation>('/api/v1/precision/remote-sensing/observations', {
        method: 'POST', body: JSON.stringify({
          sceneId, managementZoneId: String(form.get('managementZoneId') ?? '') || null, indexType: String(form.get('indexType') ?? 'NDVI'),
          customIndexName: String(form.get('customIndexName') ?? '').trim() || null, minimum: Number(form.get('minimum')), maximum: Number(form.get('maximum')),
          mean: Number(form.get('mean')), median: Number(form.get('median')), standardDeviation: Number(form.get('standardDeviation')),
          validCoveragePercent: Number(form.get('validCoveragePercent')), sampleCount: parseOptionalNumber(form.get('sampleCount'))
        })
      });
      setObservationModal(false); await loadRemote();
    } catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível registrar o índice vegetativo.'); }
    finally { setSaving(false); }
  }

  async function deactivateScene(scene: RemoteSensingScene) {
    if (!window.confirm(`Desativar a cena ${scene.externalId}? O histórico de índices será preservado.`)) return;
    try { await apiRequest(`/api/v1/precision/remote-sensing/scenes/${scene.id}`, { method: 'DELETE' }); await loadRemote(); }
    catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível desativar a cena.'); }
  }

  if (!allowed) return <EmptyState title="Sensoriamento remoto não está habilitado" description="O recurso usa o entitlement PrecisionAgriculture e o backend continua sendo a autoridade final de acesso." />;

  return <div className="remote-page">
    <PageHeader eyebrow="Agricultura de precisão" title="Sensoriamento remoto e índices vegetativos" description="Acompanhe cenas de satélite ou drone, NDVI/NDRE/EVI e sua evolução temporal com fonte e data rastreáveis." actions={<><Link className="button button--ghost" to="/precision"><Icon name="map" size={16} /> Mapas e zonas</Link><Button variant="secondary" onClick={() => void loadRemote()}><Icon name="refresh" size={16} /> Atualizar</Button><Button onClick={() => setSceneModal(true)}>Nova cena</Button><Button onClick={() => setObservationModal(true)} disabled={!scenes.length}>Novo índice</Button></>} />
    <div className="remote-disclaimer">NDVI, NDRE e EVI são indicadores de sensoriamento remoto para apoio à decisão. Eles não constituem diagnóstico agronômico automático.</div>
    {error && <div className="context-error"><span>{error}</span></div>}

    <Card className="remote-filters">
      <label>Talhão<select value={fieldId} onChange={event => setFieldId(event.target.value)}><option value="">Selecione</option>{fields.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
      <label>Safra<select value={seasonId} onChange={event => setSeasonId(event.target.value)}><option value="">Todas</option>{filteredSeasons.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
      <label>Zona<select value={zoneId} onChange={event => setZoneId(event.target.value)}><option value="">Talhão inteiro</option>{zones.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
      <label>Índice<select value={indexFilter} onChange={event => setIndexFilter(event.target.value)}><option value="">Todos</option><option>NDVI</option><option>NDRE</option><option>EVI</option><option>Custom</option></select></label>
    </Card>

    {loading && !summary ? <Spinner label="Carregando cenas e índices" /> : <>
      <div className="remote-metrics">
        <Card><span>Cenas no contexto</span><strong>{summary?.sceneCount ?? 0}</strong><small>metadados rastreáveis</small></Card>
        {(summary?.latestMetrics ?? []).slice(0, 3).map(metric => <Card key={`${metric.indexType}-${metric.customIndexName ?? ''}`}><span>{metric.customIndexName || metric.indexType}</span><strong>{formatIndexValue(metric.mean)}</strong><small>{formatDate(metric.observedAtUtc)} • {metric.source}</small></Card>)}
      </div>

      <div className="remote-grid">
        <Card className="remote-chart-card">
          <div className="remote-section-title"><div><span>Histórico temporal</span><strong>{series.length} observação(ões)</strong></div><Badge tone="info">fonte + data preservadas</Badge></div>
          {!seriesGroups.length ? <EmptyState title="Sem índices neste contexto" description="Registre uma observação NDVI, NDRE, EVI ou Custom para iniciar a série temporal." /> : <div className="remote-series-list">{seriesGroups.map(group => {
            const latest = group.points[group.points.length - 1];
            return <div className="remote-series" key={group.key}><div className="remote-series__header"><strong>{group.label}</strong><span>último {formatIndexValue(latest.mean)} • {formatDate(latest.observedAtUtc)}</span></div><svg viewBox="0 0 640 180" role="img" aria-label={`Série temporal ${group.label}`}><line x1="0" x2="640" y1="90" y2="90" className="remote-chart-zero" /><path d={buildSeriesPath(group.points)} className="remote-chart-path" /></svg><div className="remote-series__footer"><span>{formatDate(group.points[0].observedAtUtc)}</span><span>{latest.source} • {latest.platform}</span><span>{formatDate(latest.observedAtUtc)}</span></div></div>;
          })}</div>}
        </Card>

        <Card className="remote-map-card">
          <div className="remote-section-title"><div><span>Contexto espacial</span><strong>{selectedScene?.externalId ?? 'Nenhuma cena'}</strong></div>{selectedScene?.footprint ? <Badge tone="success">footprint disponível</Badge> : <Badge>sem footprint</Badge>}</div>
          <div ref={mapContainerRef} className="remote-map" aria-label="Mapa do talhão e footprint da cena" />
          <p>Limite do talhão em verde; footprint da cena em roxo quando informado. Raster pesado permanece fora do banco relacional principal.</p>
        </Card>
      </div>

      <div className="remote-grid remote-grid--tables">
        <Card>
          <div className="remote-section-title"><div><span>Linha do tempo de cenas</span><strong>{scenes.length} carregada(s)</strong></div></div>
          <div className="remote-list">{scenes.map(scene => <button key={scene.id} type="button" className={`remote-list-item ${selectedScene?.id === scene.id ? 'remote-list-item--active' : ''}`} onClick={() => setSelectedSceneId(scene.id)}><div><strong>{scene.provider} • {scene.externalId}</strong><span>{scene.platform} • {formatDate(scene.acquiredAtUtc)}</span></div><div className="remote-list-item__meta"><Badge tone={coverageTone(scene.cloudCoveragePercent)}>{scene.cloudCoveragePercent == null ? 'nuvem —' : `${scene.cloudCoveragePercent}% nuvens`}</Badge><button type="button" aria-label={`Desativar ${scene.externalId}`} onClick={event => { event.stopPropagation(); void deactivateScene(scene); }}>×</button></div></button>)}{!scenes.length && <p className="remote-empty-line">Nenhuma cena cadastrada.</p>}</div>
        </Card>
        <Card>
          <div className="remote-section-title"><div><span>Observações recentes</span><strong>{observations.length} carregada(s)</strong></div></div>
          <div className="remote-observation-table">{observations.slice(0, 12).map(item => <div key={item.id}><strong>{item.customIndexName || item.indexType}</strong><span>{formatIndexValue(item.mean)}</span><span>{item.validCoveragePercent}% válido</span><small>{formatDate(item.observedAtUtc)} • {item.source}</small></div>)}{!observations.length && <p className="remote-empty-line">Nenhuma observação registrada.</p>}</div>
        </Card>
      </div>
    </>}

    <Modal open={sceneModal} onClose={() => setSceneModal(false)} title="Nova cena" description="Registre metadados e referências; o raster permanece fora do banco principal.">
      <form className="remote-form" onSubmit={createScene}>
        <label>Provedor<input name="provider" required placeholder="Sentinel-2, Planet, Drone DJI..." /></label>
        <label>ID externo<input name="externalId" required placeholder="Identificador único do provedor" /></label>
        <label>Plataforma<select name="platform" defaultValue="Satellite"><option>Satellite</option><option>Drone</option><option>Other</option></select></label>
        <label>Data/hora da aquisição<input name="acquiredAtUtc" type="datetime-local" required defaultValue={toInputDateTime()} /></label>
        <label>Cobertura de nuvens (%)<input name="cloudCoveragePercent" type="number" min="0" max="100" step="0.001" /></label>
        <label>Resolução espacial (m)<input name="spatialResolutionMeters" type="number" min="0.000001" step="0.000001" /></label>
        <label className="remote-form__wide">Referência do asset<input name="assetReference" placeholder="s3://..., gs://..., URL assinada ou chave externa" /></label>
        <label className="remote-form__wide">Footprint GeoJSON Polygon<textarea name="footprint" rows={5} placeholder='{"type":"Polygon","coordinates":[...]}' /></label>
        <label className="remote-form__wide">Observações<textarea name="notes" rows={3} /></label>
        <div className="remote-form__actions"><Button variant="ghost" type="button" onClick={() => setSceneModal(false)}>Cancelar</Button><Button type="submit" disabled={saving}>{saving ? 'Salvando…' : 'Salvar cena'}</Button></div>
      </form>
    </Modal>

    <Modal open={observationModal} onClose={() => setObservationModal(false)} title="Novo índice vegetativo" description="Registre estatísticas processadas externamente, mantendo a fonte e a data da cena.">
      <form className="remote-form" onSubmit={createObservation}>
        <label>Cena<select name="sceneId" required defaultValue={selectedScene?.id ?? ''}><option value="">Selecione</option>{scenes.map(item => <option key={item.id} value={item.id}>{item.provider} • {item.externalId}</option>)}</select></label>
        <label>Zona<select name="managementZoneId" defaultValue={zoneId}><option value="">Talhão inteiro</option>{zones.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
        <label>Índice<select name="indexType" defaultValue="NDVI"><option>NDVI</option><option>NDRE</option><option>EVI</option><option>Custom</option></select></label>
        <label>Nome customizado<input name="customIndexName" placeholder="Obrigatório apenas para Custom" /></label>
        <label>Mínimo<input name="minimum" type="number" step="0.00000001" required defaultValue="0.2" /></label>
        <label>Máximo<input name="maximum" type="number" step="0.00000001" required defaultValue="0.9" /></label>
        <label>Média<input name="mean" type="number" step="0.00000001" required defaultValue="0.65" /></label>
        <label>Mediana<input name="median" type="number" step="0.00000001" required defaultValue="0.66" /></label>
        <label>Desvio-padrão<input name="standardDeviation" type="number" min="0" step="0.00000001" required defaultValue="0.08" /></label>
        <label>Cobertura válida (%)<input name="validCoveragePercent" type="number" min="0" max="100" step="0.001" required defaultValue="95" /></label>
        <label>Amostras/pixels<input name="sampleCount" type="number" min="0" step="1" /></label>
        <div className="remote-form__actions"><Button variant="ghost" type="button" onClick={() => setObservationModal(false)}>Cancelar</Button><Button type="submit" disabled={saving}>{saving ? 'Salvando…' : 'Registrar índice'}</Button></div>
      </form>
    </Modal>
  </div>;
}
