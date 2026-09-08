import { FormEvent, useCallback, useEffect, useMemo, useState } from 'react';
import { apiRequest } from '../../lib/api';
import type { ManagementZone, PagedResult, RasterProcessingResponse, RasterProcessingRun, RasterZonalResult, RemoteSensingScene } from '../../lib/types';
import { Badge, Button, Card, EmptyState, Modal, Spinner } from '../../components/Ui';
import './RasterProcessingPanel.css';

function statusTone(status: string): 'success' | 'warning' | 'danger' | 'neutral' {
  if (status === 'Succeeded') return 'success';
  if (status === 'Failed') return 'danger';
  if (status === 'Pending' || status === 'Processing') return 'warning';
  return 'neutral';
}

function statusLabel(status: string) {
  return ({ Pending: 'Pendente', Processing: 'Processando', Succeeded: 'Concluído', Failed: 'Falhou' } as Record<string, string>)[status] ?? status;
}

function formatIndex(value: number) {
  return value.toLocaleString('pt-BR', { minimumFractionDigits: 3, maximumFractionDigits: 4 });
}

export function RasterProcessingPanel({ scene, zones, onProcessed }: { scene: RemoteSensingScene | null; zones: ManagementZone[]; onProcessed(): Promise<void> }) {
  const [runs, setRuns] = useState<RasterProcessingRun[]>([]);
  const [results, setResults] = useState<RasterZonalResult[]>([]);
  const [loading, setLoading] = useState(false);
  const [processing, setProcessing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [modalOpen, setModalOpen] = useState(false);

  const zoneNames = useMemo(() => Object.fromEntries(zones.map(zone => [zone.id, zone.name])), [zones]);

  const load = useCallback(async () => {
    if (!scene) { setRuns([]); setResults([]); return; }
    setLoading(true); setError(null);
    try {
      const [runItems, resultPage] = await Promise.all([
        apiRequest<RasterProcessingRun[]>(`/api/v1/precision/remote-sensing/scenes/${scene.id}/processings`),
        apiRequest<PagedResult<RasterZonalResult>>(`/api/v1/precision/remote-sensing/processing-results?fieldId=${scene.fieldId}&page=1&pageSize=100`)
      ]);
      setRuns(runItems);
      setResults(resultPage.items.filter(item => item.sceneId === scene.id));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível carregar os processamentos raster.');
    } finally { setLoading(false); }
  }, [scene]);

  useEffect(() => { void load(); }, [load]);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!scene) return;
    const form = new FormData(event.currentTarget);
    setProcessing(true); setError(null);
    try {
      const productType = String(form.get('productType') ?? 'NDVI');
      const response = await apiRequest<RasterProcessingResponse>(`/api/v1/precision/remote-sensing/scenes/${scene.id}/process`, {
        method: 'POST',
        body: JSON.stringify({
          productType,
          customProductName: productType === 'Custom' ? String(form.get('customProductName') ?? '').trim() || null : null,
          assetReference: String(form.get('assetReference') ?? '').trim() || null,
          band: Number(form.get('band') ?? 1),
          includeManagementZones: form.get('includeManagementZones') === 'on',
          processingKey: null
        })
      });
      setModalOpen(false);
      setRuns(current => [response.run, ...current.filter(item => item.id !== response.run.id)]);
      setResults(current => [...response.results, ...current.filter(item => item.runId !== response.run.id)]);
      await onProcessed();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível processar o raster.');
    } finally { setProcessing(false); }
  }

  if (!scene) return <Card className="raster-panel"><EmptyState title="Selecione uma cena" description="Escolha uma cena para consultar ou executar processamento raster." /></Card>;

  const latest = runs[0] ?? null;
  const latestResults = latest ? results.filter(item => item.runId === latest.id) : [];

  return <Card className="raster-panel">
    <div className="raster-panel__header">
      <div><span className="eyebrow">Processamento científico</span><h2>Raster e estatísticas zonais</h2><p>Rasterio + NumPy processam o asset no Intelligence; o core persiste somente metadados e resultados rastreáveis.</p></div>
      <div className="raster-panel__actions"><Button variant="secondary" onClick={() => void load()} disabled={loading}>Atualizar</Button><Button onClick={() => setModalOpen(true)} disabled={!scene.assetReference && processing}>Processar raster</Button></div>
    </div>
    {error && <div className="inline-warning">{error}</div>}
    {loading && !runs.length ? <Spinner label="Carregando processamentos" /> : <>
      <div className="raster-run-grid">
        <div><span>Último status</span>{latest ? <Badge tone={statusTone(latest.status)}>{statusLabel(latest.status)}</Badge> : <strong>—</strong>}</div>
        <div><span>Produto</span><strong>{latest ? latest.customProductName || latest.productType : '—'}</strong></div>
        <div><span>Resolução</span><strong>{latest?.resolutionX == null ? '—' : `${latest.resolutionX.toLocaleString('pt-BR')} × ${latest.resolutionY?.toLocaleString('pt-BR')} m`}</strong></div>
        <div><span>CRS</span><strong>{latest?.crs ?? '—'}</strong></div>
      </div>
      {latest?.failureMessage && <div className="inline-warning">{latest.failureMessage}</div>}
      {!latest ? <EmptyState title="Nenhum processamento executado" description="Execute o primeiro produto raster para gerar estatísticas no limite do talhão e, opcionalmente, nas zonas de manejo." /> : latestResults.length ? <div className="raster-results">
        <div className="raster-results__head"><strong>Contexto</strong><strong>Média</strong><strong>Faixa</strong><strong>Cobertura</strong><strong>Amostras</strong></div>
        {latestResults.map(item => <div className="raster-results__row" key={item.id}><span>{item.managementZoneId ? zoneNames[item.managementZoneId] ?? 'Zona de manejo' : 'Talhão inteiro'}</span><strong>{formatIndex(item.mean)}</strong><span>{formatIndex(item.minimum)} – {formatIndex(item.maximum)}</span><span>{item.validCoveragePercent.toLocaleString('pt-BR', { maximumFractionDigits: 1 })}%</span><span>{item.sampleCount.toLocaleString('pt-BR')}</span></div>)}
      </div> : <p className="raster-panel__note">O processamento ainda não possui resultados zonais disponíveis.</p>}
      {runs.length > 1 && <details className="raster-history"><summary>Histórico de processamentos ({runs.length})</summary>{runs.map(run => <div key={run.id}><Badge tone={statusTone(run.status)}>{statusLabel(run.status)}</Badge><span>{run.customProductName || run.productType} · banda {run.band}</span><small>{new Date(run.requestedAtUtc).toLocaleString('pt-BR')}</small></div>)}</details>}
    </>}

    <Modal open={modalOpen} onClose={() => setModalOpen(false)} title="Processar raster" description="O asset precisa estar acessível ao AgroControl Intelligence. A repetição da mesma configuração é idempotente.">
      <form className="raster-form" onSubmit={submit}>
        <label>Produto<select name="productType" defaultValue="NDVI"><option>NDVI</option><option>NDRE</option><option>EVI</option><option>Custom</option></select></label>
        <label>Nome customizado<input name="customProductName" placeholder="Somente para Custom" /></label>
        <label>Banda<input name="band" type="number" min="1" max="128" defaultValue="1" required /></label>
        <label className="raster-form__wide">Asset raster<input name="assetReference" defaultValue={scene.assetReference ?? ''} placeholder="/data/scene.tif ou COG autorizado" /></label>
        <label className="raster-check"><input name="includeManagementZones" type="checkbox" defaultChecked /> Incluir zonas de manejo ativas</label>
        <div className="raster-form__actions"><Button type="button" variant="ghost" onClick={() => setModalOpen(false)}>Cancelar</Button><Button type="submit" disabled={processing}>{processing ? 'Processando…' : 'Executar processamento'}</Button></div>
      </form>
    </Modal>
  </Card>;
}
