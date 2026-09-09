import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { Badge, Button, Card, EmptyState, Modal, PageHeader, Spinner } from '../components/Ui';
import { Icon } from '../components/Icon';
import { useAuth } from '../lib/auth';
import { useFarmScope } from '../lib/farmScope';
import type { Crop, Field, Season } from '../lib/types';
import { bootstrapFarmOffline } from '../offline/bootstrap';
import { reapplyConflict, resolveConflictUsingServer } from '../offline/conflicts';
import { offlineStore } from '../offline/indexedDbOfflineStore';
import {
  createFieldOffline,
  createSeasonOffline,
  deleteFieldOffline,
  deleteSeasonOffline,
  updateFieldOffline,
  updateSeasonOffline
} from '../offline/mutations';
import { createOfflineNamespaceKey } from '../offline/namespace';
import { syncFarmOffline } from '../offline/syncEngine';
import type { OfflineMutation, OfflineNamespace, OfflineRecord, OfflineSyncMetadata } from '../offline/types';

type FieldRecord = OfflineRecord<Field>;
type SeasonRecord = OfflineRecord<Season>;
type CropRecord = OfflineRecord<Crop>;

type Editor =
  | { kind: 'field'; record: FieldRecord | null }
  | { kind: 'season'; record: SeasonRecord | null }
  | null;

function formatDate(value: string | null) {
  if (!value) return 'Nunca';
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? value : parsed.toLocaleString('pt-BR');
}

function stateTone(state: string) {
  if (state === 'Clean' || state === 'Synced' || state === 'Ready') return 'success' as const;
  if (state === 'Conflict' || state === 'Failed' || state === 'Blocked') return 'danger' as const;
  if (state.startsWith('Pending') || state === 'Retryable') return 'warning' as const;
  return 'neutral' as const;
}

export function OfflinePage() {
  const { session } = useAuth();
  const { farms, selected } = useFarmScope();
  const [farmId, setFarmId] = useState('');
  const [metadata, setMetadata] = useState<OfflineSyncMetadata | null>(null);
  const [fields, setFields] = useState<FieldRecord[]>([]);
  const [crops, setCrops] = useState<CropRecord[]>([]);
  const [seasons, setSeasons] = useState<SeasonRecord[]>([]);
  const [mutations, setMutations] = useState<OfflineMutation[]>([]);
  const [online, setOnline] = useState(() => typeof navigator === 'undefined' ? true : navigator.onLine);
  const [working, setWorking] = useState(false);
  const [loadingLocal, setLoadingLocal] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [editor, setEditor] = useState<Editor>(null);
  const autoSyncing = useRef(false);

  useEffect(() => {
    if (!farmId && farms.length) {
      const selectedId = selected.kind === 'farm' && farms.some(farm => farm.id === selected.farmId)
        ? selected.farmId
        : farms[0].id;
      setFarmId(selectedId);
    }
  }, [farmId, farms, selected]);

  const namespace = useMemo<OfflineNamespace | null>(() => {
    if (!session || !farmId) return null;
    return { userId: session.userId, organizationId: session.organizationId, farmId };
  }, [session, farmId]);
  const namespaceKey = namespace ? createOfflineNamespaceKey(namespace) : null;
  const selectedFarm = farms.find(farm => farm.id === farmId) ?? null;

  const refreshLocal = useCallback(async () => {
    if (!namespaceKey) {
      setMetadata(null); setFields([]); setCrops([]); setSeasons([]); setMutations([]);
      return;
    }
    setLoadingLocal(true);
    try {
      await offlineStore.initialize();
      const [nextMetadata, nextFields, nextCrops, nextSeasons, nextMutations] = await Promise.all([
        offlineStore.getSyncMetadata(namespaceKey),
        offlineStore.listRecords<Field>(namespaceKey, 'field'),
        offlineStore.listRecords<Crop>(namespaceKey, 'crop'),
        offlineStore.listRecords<Season>(namespaceKey, 'season'),
        offlineStore.listMutations(namespaceKey)
      ]);
      setMetadata(nextMetadata);
      setFields(nextFields);
      setCrops(nextCrops);
      setSeasons(nextSeasons);
      setMutations(nextMutations);
    } finally {
      setLoadingLocal(false);
    }
  }, [namespaceKey]);

  useEffect(() => { void refreshLocal(); }, [refreshLocal]);

  useEffect(() => {
    const handleOnline = () => setOnline(true);
    const handleOffline = () => setOnline(false);
    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);
    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, []);

  const runSync = useCallback(async (automatic = false) => {
    if (!namespace || autoSyncing.current) return;
    autoSyncing.current = true;
    if (!automatic) setWorking(true);
    setError(null); setMessage(null);
    try {
      const result = await syncFarmOffline(namespace);
      setMessage(result.message ?? (
        result.state === 'Synced'
          ? `Sincronização concluída: ${result.pushed} enviada(s), ${result.pulled} recebida(s).`
          : `Sincronização finalizada em estado ${result.state}.`
      ));
      await refreshLocal();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível sincronizar a cópia offline.');
      await refreshLocal();
    } finally {
      autoSyncing.current = false;
      if (!automatic) setWorking(false);
    }
  }, [namespace, refreshLocal]);

  useEffect(() => {
    if (!namespace || !online || metadata?.preparationState !== 'Ready') return;
    let timer: ReturnType<typeof setTimeout> | null = null;
    const handleReconnect = () => {
      if (timer) clearTimeout(timer);
      timer = setTimeout(() => void runSync(true), 1500);
    };
    window.addEventListener('online', handleReconnect);
    return () => {
      if (timer) clearTimeout(timer);
      window.removeEventListener('online', handleReconnect);
    };
  }, [namespace, online, metadata?.preparationState, runSync]);

  async function prepare() {
    if (!namespace || !online) return;
    setWorking(true); setError(null); setMessage(null);
    try {
      const result = await bootstrapFarmOffline(namespace);
      setMessage(`${result.fieldCount} talhão(ões) e ${result.seasonCount} safra(s) disponíveis offline.`);
      await refreshLocal();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível preparar a fazenda para uso offline.');
    } finally {
      setWorking(false);
    }
  }

  async function removeCopy() {
    if (!namespaceKey || !window.confirm('Remover desta máquina a cópia offline desta fazenda e todas as alterações locais pendentes?')) return;
    await offlineStore.clearNamespace(namespaceKey);
    setMessage('Cópia offline removida deste dispositivo.');
    setError(null);
    await refreshLocal();
  }

  async function resolveServer(record: FieldRecord | SeasonRecord) {
    if (!namespace) return;
    setWorking(true); setError(null);
    try {
      await resolveConflictUsingServer(namespace, record.entityKind as 'field' | 'season', record.entityId);
      setMessage('Versão do servidor aplicada.');
      await refreshLocal();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível resolver o conflito.');
    } finally { setWorking(false); }
  }

  async function reapply(record: FieldRecord | SeasonRecord) {
    if (!namespace) return;
    setWorking(true); setError(null);
    try {
      await reapplyConflict(namespace, record.entityKind as 'field' | 'season', record.entityId);
      setMessage('Alteração local preparada novamente com a versão atual do servidor.');
      await refreshLocal();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível reaplicar a alteração.');
    } finally { setWorking(false); }
  }

  async function removeField(record: FieldRecord) {
    if (!namespace || record.syncState !== 'Clean' || !window.confirm(`Desativar “${record.data.name}” quando a conexão voltar?`)) return;
    try { await deleteFieldOffline(namespace, record.entityId); await refreshLocal(); }
    catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível registrar a exclusão offline.'); }
  }

  async function removeSeason(record: SeasonRecord) {
    if (!namespace || record.syncState !== 'Clean' || !window.confirm(`Desativar a safra “${record.data.name}” quando a conexão voltar?`)) return;
    try { await deleteSeasonOffline(namespace, record.entityId); await refreshLocal(); }
    catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível registrar a exclusão offline.'); }
  }

  const prepared = metadata?.preparationState === 'Ready';
  const conflicts = [...fields, ...seasons].filter(record => record.syncState === 'Conflict');
  const pendingCount = mutations.filter(item => item.state === 'Pending' || item.state === 'Retryable').length;
  const failedCount = mutations.filter(item => item.state === 'Failed').length;

  if (!session) return null;

  return (
    <div className="page-stack">
      <PageHeader
        eyebrow="Operação resiliente"
        title="Modo offline"
        description="Escolha uma fazenda, mantenha uma cópia local controlada e sincronize alterações de talhões e safras sem transformar o dispositivo em autoridade dos dados."
        actions={<>
          <Badge tone={online ? 'success' : 'warning'}>{online ? 'Online' : 'Offline'}</Badge>
          {prepared && <Button onClick={() => void runSync()} disabled={working || !online}><Icon name="refresh" size={16} /> Sincronizar</Button>}
        </>}
      />

      <Card className="resource-card">
        <div className="resource-toolbar resource-toolbar--wrap">
          <label className="field-inline">Fazenda
            <select value={farmId} onChange={event => setFarmId(event.target.value)} disabled={working}>
              {farms.map(farm => <option key={farm.id} value={farm.id}>{farm.name}{farm.stateCode ? ` — ${farm.stateCode}` : ''}</option>)}
            </select>
          </label>
          <Badge tone={stateTone(metadata?.preparationState ?? 'NotPrepared')}>
            {metadata?.preparationState ?? 'Não preparada'}
          </Badge>
          {!prepared && <Button onClick={() => void prepare()} disabled={working || !online || !selectedFarm}>Disponibilizar offline</Button>}
          {metadata && <Button variant="danger" onClick={() => void removeCopy()} disabled={working}>Remover cópia</Button>}
        </div>
        <div className="dashboard-summary-grid">
          <div><span>Última sincronização</span><strong>{formatDate(metadata?.lastSyncedAtUtc ?? null)}</strong></div>
          <div><span>Alterações pendentes</span><strong>{pendingCount}</strong></div>
          <div><span>Conflitos</span><strong>{conflicts.length}</strong></div>
          <div><span>Rejeitadas</span><strong>{failedCount}</strong></div>
        </div>
        {metadata?.preparationState === 'Blocked' && <div className="form-error">Acesso offline bloqueado: {metadata.lastError}</div>}
        {message && <div className="inline-success">{message}</div>}
        {error && <div className="form-error" role="alert">{error}</div>}
      </Card>

      {loadingLocal ? <Spinner label="Carregando cópia offline" /> : !prepared ? (
        <EmptyState
          title="Esta fazenda ainda não está disponível offline"
          description={online ? 'Faça o download inicial enquanto estiver conectado.' : 'Reconecte para preparar a primeira cópia desta fazenda.'}
        />
      ) : <>
        {conflicts.length > 0 && <Card className="resource-card">
          <div className="section-heading"><div><span className="eyebrow">Ação necessária</span><h2>Conflitos de sincronização</h2></div></div>
          <div className="table-wrap"><table><thead><tr><th>Registro</th><th>Motivo</th><th>Ações</th></tr></thead><tbody>
            {conflicts.map(record => <tr key={record.key}>
              <td><strong>{(record.data as Field | Season).name}</strong><span className="table-sub">{record.entityKind}</span></td>
              <td>{record.conflict?.message ?? 'O registro mudou no servidor.'}</td>
              <td><div className="row-actions"><Button variant="secondary" onClick={() => void resolveServer(record)}>Usar servidor</Button><Button onClick={() => void reapply(record)}>Reaplicar minha alteração</Button></div></td>
            </tr>)}
          </tbody></table></div>
        </Card>}

        <Card className="resource-card">
          <div className="resource-toolbar resource-toolbar--wrap">
            <div><span className="eyebrow">Permitido offline</span><h2>Talhões</h2></div>
            <Button onClick={() => setEditor({ kind: 'field', record: null })}><Icon name="plus" size={16} /> Novo talhão</Button>
          </div>
          {fields.length ? <div className="table-wrap"><table><thead><tr><th>Talhão</th><th>Área</th><th>Estado local</th><th aria-label="Ações" /></tr></thead><tbody>
            {fields.map(record => <tr key={record.key}>
              <td><strong>{record.data.name}</strong><span className="table-sub">{record.entityId.slice(0, 8)}</span></td>
              <td>{record.data.areaHectares} ha</td>
              <td><Badge tone={stateTone(record.syncState)}>{record.syncState}</Badge></td>
              <td><div className="row-actions">
                <button className="icon-button" disabled={record.syncState !== 'Clean'} onClick={() => setEditor({ kind: 'field', record })} aria-label={`Editar ${record.data.name}`}><Icon name="edit" size={17} /></button>
                <button className="icon-button icon-button--danger" disabled={record.syncState !== 'Clean'} onClick={() => void removeField(record)} aria-label={`Desativar ${record.data.name}`}><Icon name="trash" size={17} /></button>
              </div></td>
            </tr>)}
          </tbody></table></div> : <EmptyState title="Nenhum talhão na cópia offline" description="Crie um talhão localmente ou sincronize com o servidor." />}
        </Card>

        <Card className="resource-card">
          <div className="resource-toolbar resource-toolbar--wrap">
            <div><span className="eyebrow">Permitido offline</span><h2>Safras</h2></div>
            <Button onClick={() => setEditor({ kind: 'season', record: null })} disabled={!fields.length || !crops.length}><Icon name="plus" size={16} /> Nova safra</Button>
          </div>
          {seasons.length ? <div className="table-wrap"><table><thead><tr><th>Safra</th><th>Talhão</th><th>Status</th><th>Estado local</th><th aria-label="Ações" /></tr></thead><tbody>
            {seasons.map(record => <tr key={record.key}>
              <td><strong>{record.data.name}</strong><span className="table-sub">{record.data.startDate}</span></td>
              <td>{fields.find(field => field.entityId === record.data.fieldId)?.data.name ?? 'Talhão indisponível'}</td>
              <td>{record.data.status}</td>
              <td><Badge tone={stateTone(record.syncState)}>{record.syncState}</Badge></td>
              <td><div className="row-actions">
                <button className="icon-button" disabled={record.syncState !== 'Clean'} onClick={() => setEditor({ kind: 'season', record })} aria-label={`Editar ${record.data.name}`}><Icon name="edit" size={17} /></button>
                <button className="icon-button icon-button--danger" disabled={record.syncState !== 'Clean'} onClick={() => void removeSeason(record)} aria-label={`Desativar ${record.data.name}`}><Icon name="trash" size={17} /></button>
              </div></td>
            </tr>)}
          </tbody></table></div> : <EmptyState title="Nenhuma safra na cópia offline" description="Cadastre uma safra localmente para enviá-la na próxima sincronização." />}
        </Card>
      </>}

      <OfflineEditor
        editor={editor}
        namespace={namespace}
        fields={fields}
        crops={crops}
        onClose={() => setEditor(null)}
        onSaved={async () => { setEditor(null); setMessage('Alteração salva localmente na fila de sincronização.'); await refreshLocal(); }}
      />
    </div>
  );
}

function OfflineEditor({
  editor,
  namespace,
  fields,
  crops,
  onClose,
  onSaved
}: {
  editor: Editor;
  namespace: OfflineNamespace | null;
  fields: FieldRecord[];
  crops: CropRecord[];
  onClose(): void;
  onSaved(): Promise<void>;
}) {
  const fieldRecord = editor?.kind === 'field' ? editor.record : null;
  const seasonRecord = editor?.kind === 'season' ? editor.record : null;
  const [name, setName] = useState('');
  const [area, setArea] = useState('');
  const [fieldId, setFieldId] = useState('');
  const [cropId, setCropId] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [expectedYield, setExpectedYield] = useState('');
  const [actualYield, setActualYield] = useState('');
  const [status, setStatus] = useState('Planned');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!editor) return;
    if (editor.kind === 'field') {
      setName(fieldRecord?.data.name ?? '');
      setArea(fieldRecord ? String(fieldRecord.data.areaHectares) : '');
    } else {
      setName(seasonRecord?.data.name ?? '');
      setFieldId(seasonRecord?.data.fieldId ?? fields.find(item => item.syncState !== 'PendingDelete')?.entityId ?? '');
      setCropId(seasonRecord?.data.cropId ?? crops[0]?.entityId ?? '');
      setStartDate(seasonRecord?.data.startDate ?? '');
      setEndDate(seasonRecord?.data.endDate ?? '');
      setExpectedYield(seasonRecord?.data.expectedYieldPerHectare == null ? '' : String(seasonRecord.data.expectedYieldPerHectare));
      setActualYield(seasonRecord?.data.actualYieldPerHectare == null ? '' : String(seasonRecord.data.actualYieldPerHectare));
      setStatus(seasonRecord?.data.status ?? 'Planned');
    }
    setError(null);
  }, [editor, fieldRecord, seasonRecord, fields, crops]);

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!editor || !namespace) return;
    setSaving(true); setError(null);
    try {
      if (editor.kind === 'field') {
        const areaHectares = Number(area);
        if (editor.record) await updateFieldOffline(namespace, editor.record.entityId, { name, areaHectares });
        else await createFieldOffline(namespace, { name, areaHectares });
      } else {
        const common = {
          fieldId,
          cropId,
          name,
          startDate,
          endDate: endDate || null,
          expectedYieldPerHectare: expectedYield === '' ? null : Number(expectedYield)
        };
        if (editor.record) {
          await updateSeasonOffline(namespace, editor.record.entityId, {
            ...common,
            actualYieldPerHectare: actualYield === '' ? null : Number(actualYield),
            status
          });
        } else {
          await createSeasonOffline(namespace, common);
        }
      }
      await onSaved();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível salvar a alteração offline.');
    } finally { setSaving(false); }
  }

  return <Modal
    open={Boolean(editor)}
    onClose={onClose}
    title={editor?.kind === 'field' ? (fieldRecord ? 'Editar talhão offline' : 'Novo talhão offline') : (seasonRecord ? 'Editar safra offline' : 'Nova safra offline')}
    description="A alteração fica somente neste dispositivo até receber confirmação do servidor."
  >
    <form className="form-stack" onSubmit={submit}>
      {editor?.kind === 'field' ? <>
        <label>Nome<input required value={name} onChange={event => setName(event.target.value)} /></label>
        <label>Área (ha)<input required type="number" min="0.01" step="0.01" value={area} onChange={event => setArea(event.target.value)} /></label>
      </> : <>
        <label>Talhão<select required value={fieldId} onChange={event => setFieldId(event.target.value)}>{fields.filter(item => item.syncState !== 'PendingDelete').map(item => <option key={item.entityId} value={item.entityId}>{item.data.name}</option>)}</select></label>
        <label>Cultura<select required value={cropId} onChange={event => setCropId(event.target.value)}>{crops.map(item => <option key={item.entityId} value={item.entityId}>{item.data.name}{item.data.variety ? ` — ${item.data.variety}` : ''}</option>)}</select></label>
        <label>Nome<input required value={name} onChange={event => setName(event.target.value)} /></label>
        <label>Início<input required type="date" value={startDate} onChange={event => setStartDate(event.target.value)} /></label>
        <label>Fim<input type="date" value={endDate} onChange={event => setEndDate(event.target.value)} /></label>
        <label>Produtividade esperada<input type="number" min="0" step="0.01" value={expectedYield} onChange={event => setExpectedYield(event.target.value)} /></label>
        {seasonRecord && <><label>Produtividade realizada<input type="number" min="0" step="0.01" value={actualYield} onChange={event => setActualYield(event.target.value)} /></label><label>Status<select value={status} onChange={event => setStatus(event.target.value)}><option value="Planned">Planejada</option><option value="Active">Ativa</option><option value="Harvested">Colhida</option><option value="Cancelled">Cancelada</option></select></label></>}
      </>}
      {error && <div className="form-error" role="alert">{error}</div>}
      <div className="modal__actions"><Button type="button" variant="ghost" onClick={onClose}>Cancelar</Button><Button type="submit" disabled={saving}>{saving ? 'Salvando…' : 'Salvar offline'}</Button></div>
    </form>
  </Modal>;
}
