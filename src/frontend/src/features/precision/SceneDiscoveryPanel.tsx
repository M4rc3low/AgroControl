import { FormEvent, useEffect, useMemo, useState } from 'react';
import { Badge, Button, Card, EmptyState, Spinner } from '../../components/Ui';
import { apiRequest } from '../../lib/api';
import type {
  RemoteSceneDiscoveryItem,
  RemoteSceneDiscoveryPage,
  RemoteSceneDiscoveryProvider,
  RemoteSensingScene
} from '../../lib/types';
import './SceneDiscoveryPanel.css';

interface Props {
  fieldId: string;
  seasonId: string;
  onImported: (scene: RemoteSensingScene) => void | Promise<void>;
  onPreview: (item: RemoteSceneDiscoveryItem | null) => void;
}

function dateInput(value: Date) {
  return value.toISOString().slice(0, 10);
}

function formatDate(value: string) {
  return new Date(value).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' });
}

function metric(value: number | null, suffix: string) {
  return value == null ? '—' : `${value.toLocaleString('pt-BR', { maximumFractionDigits: 1 })}${suffix}`;
}

export function SceneDiscoveryPanel({ fieldId, seasonId, onImported, onPreview }: Props) {
  const [providers, setProviders] = useState<RemoteSceneDiscoveryProvider[]>([]);
  const [provider, setProvider] = useState('');
  const [collection, setCollection] = useState('');
  const [from, setFrom] = useState(() => dateInput(new Date(Date.now() - 30 * 86_400_000)));
  const [to, setTo] = useState(() => dateInput(new Date()));
  const [maxCloud, setMaxCloud] = useState('30');
  const [page, setPage] = useState<RemoteSceneDiscoveryPage | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [continuationToken, setContinuationToken] = useState<string | null>(null);
  const [importing, setImporting] = useState<string | null>(null);
  const [imported, setImported] = useState<Set<string>>(() => new Set());

  const currentProvider = useMemo(() => providers.find(item => item.key === provider) ?? null, [providers, provider]);

  useEffect(() => {
    let active = true;
    void apiRequest<RemoteSceneDiscoveryProvider[]>('/api/v1/precision/remote-sensing/discovery/providers')
      .then(items => {
        if (!active) return;
        setProviders(items);
        setProvider(current => items.some(item => item.key === current) ? current : (items[0]?.key ?? ''));
      })
      .catch(err => active && setError(err instanceof Error ? err.message : 'Não foi possível carregar os provedores STAC.'));
    return () => { active = false; };
  }, []);

  useEffect(() => {
    const collections = currentProvider?.collections ?? [];
    setCollection(current => collections.includes(current) ? current : (collections[0] ?? ''));
  }, [currentProvider]);

  useEffect(() => {
    setPage(null);
    setContinuationToken(null);
    setImported(new Set());
    onPreview(null);
  }, [fieldId, seasonId, onPreview]);

  async function runSearch(event?: FormEvent, token: string | null = null) {
    event?.preventDefault();
    if (!fieldId || !provider) return;
    setLoading(true); setError(null); onPreview(null);
    try {
      const result = await apiRequest<RemoteSceneDiscoveryPage>('/api/v1/precision/remote-sensing/discovery/search', {
        method: 'POST',
        body: JSON.stringify({
          fieldId,
          seasonId: seasonId || null,
          provider,
          collection: collection || null,
          fromUtc: new Date(`${from}T00:00:00Z`).toISOString(),
          toUtc: new Date(`${to}T23:59:59Z`).toISOString(),
          pageSize: 24,
          maxCloudCoveragePercent: maxCloud.trim() ? Number(maxCloud) : null,
          continuationToken: token
        })
      });
      setPage(result);
      setContinuationToken(result.continuationToken);
    } catch (err) {
      setPage(null);
      setContinuationToken(null);
      setError(err instanceof Error ? err.message : 'Não foi possível descobrir cenas.');
    } finally { setLoading(false); }
  }

  async function importAsset(item: RemoteSceneDiscoveryItem, assetKey: string) {
    const operationKey = `${item.provider}:${item.externalId}:${assetKey}`;
    setImporting(operationKey); setError(null);
    try {
      const scene = await apiRequest<RemoteSensingScene>('/api/v1/precision/remote-sensing/discovery/import', {
        method: 'POST',
        body: JSON.stringify({
          fieldId,
          seasonId: seasonId || null,
          provider: item.provider,
          collection: item.collection,
          externalId: item.externalId,
          assetKey
        })
      });
      setImported(current => new Set(current).add(operationKey));
      await onImported(scene);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível importar a cena.');
    } finally { setImporting(null); }
  }

  if (!fieldId) return <EmptyState title="Selecione um talhão" description="A descoberta STAC usa o limite geográfico do talhão selecionado no backend." />;

  return <Card className="scene-discovery">
    <div className="scene-discovery__header">
      <div>
        <span className="eyebrow">Catálogo STAC</span>
        <h2>Descobrir cenas</h2>
        <p>A busca usa a geometria cadastrada do talhão. A importação só acontece depois que você escolhe explicitamente uma cena e um asset raster.</p>
      </div>
      <Badge tone="neutral">STAC</Badge>
    </div>

    <form className="scene-discovery__filters" onSubmit={event => void runSearch(event)}>
      <label>Provedor
        <select value={provider} onChange={event => setProvider(event.target.value)}>
          {providers.map(item => <option key={item.key} value={item.key}>{item.displayName}</option>)}
        </select>
      </label>
      <label>Coleção
        <select value={collection} onChange={event => setCollection(event.target.value)}>
          {(currentProvider?.collections ?? []).map(item => <option key={item} value={item}>{item}</option>)}
        </select>
      </label>
      <label>De<input type="date" value={from} max={to} onChange={event => setFrom(event.target.value)} /></label>
      <label>Até<input type="date" value={to} min={from} onChange={event => setTo(event.target.value)} /></label>
      <label>Nuvens máx. (%)<input type="number" min="0" max="100" step="1" value={maxCloud} onChange={event => setMaxCloud(event.target.value)} /></label>
      <div className="scene-discovery__submit"><Button type="submit" disabled={loading || !provider}>{loading ? 'Buscando…' : 'Buscar cenas'}</Button></div>
    </form>

    {error && <div className="context-error"><span>{error}</span></div>}
    {loading && <div className="scene-discovery__loading"><Spinner /> Consultando catálogo geoespacial…</div>}

    {!loading && page && page.items.length === 0 && <EmptyState title="Nenhuma cena encontrada" description="Tente ampliar o período, aumentar a cobertura máxima de nuvens ou escolher outra coleção." />}

    {!loading && page && page.items.length > 0 && <>
      <div className="scene-discovery__results">
        {page.items.map(item => {
          const rasterAssets = item.assets.filter(asset => asset.isRasterCandidate);
          return <article key={`${item.provider}:${item.collection}:${item.externalId}`} className="scene-discovery__item">
            <div className="scene-discovery__item-head">
              <div><strong>{item.externalId}</strong><small>{item.collection}</small></div>
              <Badge tone={item.cloudCoveragePercent != null && item.cloudCoveragePercent <= 20 ? 'success' : 'neutral'}>{metric(item.cloudCoveragePercent, '% nuvens')}</Badge>
            </div>
            <dl>
              <div><dt>Aquisição</dt><dd>{formatDate(item.acquiredAtUtc)}</dd></div>
              <div><dt>Resolução</dt><dd>{metric(item.spatialResolutionMeters, ' m')}</dd></div>
              <div><dt>Plataforma</dt><dd>{item.platform ?? item.constellation ?? '—'}</dd></div>
              <div><dt>Assets raster</dt><dd>{rasterAssets.length}</dd></div>
            </dl>
            <div className="scene-discovery__actions">
              <Button variant="secondary" type="button" onClick={() => onPreview(item)} disabled={!item.geometryGeoJson}>Ver footprint</Button>
              {rasterAssets.map(asset => {
                const key = `${item.provider}:${item.externalId}:${asset.key}`;
                const done = imported.has(key);
                return <Button key={asset.key} type="button" disabled={done || importing === key} onClick={() => void importAsset(item, asset.key)}>
                  {done ? `Importado: ${asset.key}` : importing === key ? 'Importando…' : `Importar ${asset.key}`}
                </Button>;
              })}
            </div>
            {rasterAssets.length === 0 && <p className="scene-discovery__warning">Este item não expõe um asset GeoTIFF/COG elegível para importação.</p>}
          </article>;
        })}
      </div>
      <div className="scene-discovery__pagination">
        <span>{page.items.length} resultado(s) nesta página</span>
        <Button variant="secondary" type="button" disabled={!continuationToken || loading} onClick={() => void runSearch(undefined, continuationToken)}>Próxima página</Button>
      </div>
    </>}
  </Card>;
}
