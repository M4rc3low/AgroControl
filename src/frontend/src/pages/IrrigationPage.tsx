import { FormEvent, useEffect, useMemo, useState } from 'react';
import { Icon } from '../components/Icon';
import { Badge, Button, Card, EmptyState, Modal, PageHeader, Spinner } from '../components/Ui';
import { apiRequest, getAllPaged } from '../lib/api';
import { useAuth } from '../lib/auth';
import type { Field, PagedResult } from '../lib/types';
import './IrrigationPage.css';

interface IrrigationZone {
  id: string;
  fieldId: string;
  name: string;
  areaHectares: number;
  method: string;
  minimumMoisturePercent: number;
  targetMoisturePercent: number;
  maximumMoisturePercent: number;
  telemetryDeviceId: string | null;
  isActive: boolean;
}

interface ZoneStatus {
  zoneId: string;
  telemetryDeviceId: string | null;
  telemetryAvailable: boolean;
  hasReading: boolean;
  soilMoisturePercent: number | null;
  capturedAtUtc: string | null;
  condition: string | null;
  recommendation: string;
  message: string;
}

interface IrrigationApplication {
  id: string;
  zoneId: string;
  fieldId: string;
  depthMillimeters: number;
  estimatedVolumeCubicMeters: number;
  source: string;
  startedAtUtc: string;
  notes: string | null;
}

interface IrrigationSummary {
  applicationCount: number;
  totalDepthMillimeters: number;
  estimatedVolumeCubicMeters: number;
}

const methods = ['CenterPivot', 'Drip', 'Sprinkler', 'MicroSprinkler', 'Furrow', 'Other'];

export function IrrigationPage() {
  const { platform } = useAuth();
  const allowed = Boolean(platform?.entitlements.modules.Irrigation);
  const [fields, setFields] = useState<Field[]>([]);
  const [zones, setZones] = useState<IrrigationZone[]>([]);
  const [applications, setApplications] = useState<IrrigationApplication[]>([]);
  const [summary, setSummary] = useState<IrrigationSummary | null>(null);
  const [statuses, setStatuses] = useState<Record<string, ZoneStatus>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [zoneModal, setZoneModal] = useState(false);
  const [applicationModal, setApplicationModal] = useState(false);
  const [selectedZoneId, setSelectedZoneId] = useState('');

  async function load() {
    if (!allowed) { setLoading(false); return; }
    setLoading(true);
    setError(null);
    try {
      const [fieldItems, zoneItems, applicationPage, summaryData] = await Promise.all([
        getAllPaged<Field>('/api/v1/fields'),
        getAllPaged<IrrigationZone>('/api/v1/irrigation/zones'),
        apiRequest<PagedResult<IrrigationApplication>>('/api/v1/irrigation/applications?page=1&pageSize=50'),
        apiRequest<IrrigationSummary>('/api/v1/irrigation/summary')
      ]);
      setFields(fieldItems);
      setZones(zoneItems);
      setApplications(applicationPage.items);
      setSummary(summaryData);
      const pairs = await Promise.all(zoneItems.filter(zone => zone.isActive).map(async zone => {
        try { return [zone.id, await apiRequest<ZoneStatus>(`/api/v1/irrigation/zones/${zone.id}/status`)] as const; }
        catch { return [zone.id, { zoneId: zone.id, telemetryDeviceId: zone.telemetryDeviceId, telemetryAvailable: false, hasReading: false, soilMoisturePercent: null, capturedAtUtc: null, condition: null, recommendation: 'Monitor', message: 'Telemetria indisponível no momento.' }] as const; }
      }));
      setStatuses(Object.fromEntries(pairs));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível carregar o manejo de irrigação.');
    } finally { setLoading(false); }
  }

  useEffect(() => { void load(); }, [allowed]);

  const fieldNames = useMemo(() => Object.fromEntries(fields.map(field => [field.id, field.name])), [fields]);
  const zoneNames = useMemo(() => Object.fromEntries(zones.map(zone => [zone.id, zone.name])), [zones]);

  if (!allowed) return <EmptyState title="Irrigação não disponível no plano" description="O módulo existe na plataforma, mas o acesso depende do entitlement Irrigation da sua organização." />;
  if (loading && zones.length === 0) return <Spinner label="Carregando manejo hídrico" />;
  if (error && zones.length === 0) return <EmptyState title="Irrigação indisponível" description={error} action={<Button onClick={() => void load()}><Icon name="refresh" size={16} /> Tentar novamente</Button>} />;

  return <div className="page-stack irrigation-page">
    <PageHeader eyebrow="Manejo hídrico" title="Irrigação" description="Acompanhe zonas, umidade do solo e aplicações de água. As recomendações apoiam a decisão e não acionam equipamentos automaticamente." actions={<div className="irrigation-actions"><Button variant="secondary" onClick={() => void load()}><Icon name="refresh" size={16} /> Atualizar</Button><Button onClick={() => setZoneModal(true)}><Icon name="plus" size={16} /> Nova zona</Button></div>} />
    {error && <div className="inline-warning">{error}</div>}

    <div className="irrigation-metrics">
      <Card><span>Zonas ativas</span><strong>{zones.filter(zone => zone.isActive).length}</strong><small>áreas de manejo configuradas</small></Card>
      <Card><span>Aplicações registradas</span><strong>{summary?.applicationCount ?? 0}</strong><small>histórico append-only</small></Card>
      <Card><span>Volume estimado</span><strong>{(summary?.estimatedVolumeCubicMeters ?? 0).toLocaleString('pt-BR', { maximumFractionDigits: 1 })} m³</strong><small>no período consultado</small></Card>
    </div>

    <section className="irrigation-section">
      <div className="section-heading"><div><span className="eyebrow">Condição atual</span><h2>Zonas de irrigação</h2></div></div>
      {zones.length === 0 ? <EmptyState title="Nenhuma zona cadastrada" description="Crie a primeira zona para organizar limites de umidade e aplicações de água." /> : <div className="irrigation-zone-grid">{zones.map(zone => {
        const status = statuses[zone.id];
        const tone = status?.condition === 'Critical' ? 'danger' : status?.condition === 'Dry' ? 'warning' : status?.condition === 'Wet' ? 'info' : status?.condition === 'Target' ? 'success' : 'neutral';
        return <Card key={zone.id} className={!zone.isActive ? 'irrigation-zone irrigation-zone--inactive' : 'irrigation-zone'}>
          <div className="irrigation-zone__head"><div><strong>{zone.name}</strong><small>{fieldNames[zone.fieldId] ?? 'Talhão'} · {zone.areaHectares.toLocaleString('pt-BR')} ha</small></div><Badge tone={zone.isActive ? 'success' : 'neutral'}>{zone.isActive ? 'Ativa' : 'Inativa'}</Badge></div>
          <div className="irrigation-moisture"><span>Umidade do solo</span><strong>{status?.soilMoisturePercent != null ? `${status.soilMoisturePercent.toLocaleString('pt-BR')}%` : '—'}</strong><Badge tone={tone}>{status?.condition ?? 'Sem leitura'}</Badge></div>
          <p>{status?.message ?? 'Consultando telemetria...'}</p>
          <div className="irrigation-thresholds"><span>mín. {zone.minimumMoisturePercent}%</span><span>alvo {zone.targetMoisturePercent}%</span><span>máx. {zone.maximumMoisturePercent}%</span></div>
          {zone.isActive && <Button variant="secondary" onClick={() => { setSelectedZoneId(zone.id); setApplicationModal(true); }}>Registrar aplicação</Button>}
        </Card>;
      })}</div>}
    </section>

    <section className="irrigation-section">
      <div className="section-heading"><div><span className="eyebrow">Histórico</span><h2>Aplicações de água</h2></div><Button variant="secondary" onClick={() => setApplicationModal(true)}><Icon name="plus" size={16} /> Registrar</Button></div>
      {applications.length === 0 ? <EmptyState title="Nenhuma aplicação registrada" description="As aplicações reais aparecerão aqui sem reescrever o histórico." /> : <div className="table-wrap"><table><thead><tr><th>Data</th><th>Zona</th><th>Lâmina</th><th>Volume estimado</th><th>Origem</th></tr></thead><tbody>{applications.map(item => <tr key={item.id}><td>{new Date(item.startedAtUtc).toLocaleString('pt-BR')}</td><td>{zoneNames[item.zoneId] ?? item.zoneId.slice(0, 8)}</td><td>{item.depthMillimeters.toLocaleString('pt-BR')} mm</td><td>{item.estimatedVolumeCubicMeters.toLocaleString('pt-BR')} m³</td><td>{item.source}</td></tr>)}</tbody></table></div>}
    </section>

    <ZoneModal open={zoneModal} fields={fields} onClose={() => setZoneModal(false)} onSaved={load} />
    <ApplicationModal open={applicationModal} zones={zones.filter(zone => zone.isActive)} selectedZoneId={selectedZoneId} onClose={() => { setApplicationModal(false); setSelectedZoneId(''); }} onSaved={load} />
  </div>;
}

function ZoneModal({ open, fields, onClose, onSaved }: { open: boolean; fields: Field[]; onClose(): void; onSaved(): Promise<void> }) {
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setSaving(true); setError(null);
    const form = new FormData(event.currentTarget);
    try {
      await apiRequest('/api/v1/irrigation/zones', { method: 'POST', body: JSON.stringify({ fieldId: form.get('fieldId'), name: form.get('name'), areaHectares: Number(form.get('areaHectares')), method: form.get('method'), minimumMoisturePercent: Number(form.get('minimum')), targetMoisturePercent: Number(form.get('target')), maximumMoisturePercent: Number(form.get('maximum')), telemetryDeviceId: String(form.get('telemetryDeviceId') ?? '').trim() || null }) });
      onClose(); await onSaved();
    } catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível criar a zona.'); } finally { setSaving(false); }
  }
  return <Modal open={open} title="Nova zona de irrigação" description="Configure área, método e faixas de umidade." onClose={onClose}><form className="form-grid" onSubmit={submit}>{error && <div className="inline-warning form-grid__full">{error}</div>}<label>Talhão<select name="fieldId" required>{fields.map(field => <option key={field.id} value={field.id}>{field.name} · {field.areaHectares} ha</option>)}</select></label><label>Nome<input name="name" required placeholder="Zona Norte" /></label><label>Área (ha)<input name="areaHectares" required type="number" min="0.0001" step="0.0001" /></label><label>Método<select name="method">{methods.map(method => <option key={method}>{method}</option>)}</select></label><label>Umidade mínima (%)<input name="minimum" type="number" min="0" max="100" step="0.1" defaultValue="35" /></label><label>Umidade alvo (%)<input name="target" type="number" min="0" max="100" step="0.1" defaultValue="50" /></label><label>Umidade máxima (%)<input name="maximum" type="number" min="0" max="100" step="0.1" defaultValue="75" /></label><label>Device ID de telemetria<input name="telemetryDeviceId" placeholder="opcional" /></label><div className="form-actions form-grid__full"><Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button><Button type="submit" disabled={saving}>{saving ? 'Salvando...' : 'Criar zona'}</Button></div></form></Modal>;
}

function ApplicationModal({ open, zones, selectedZoneId, onClose, onSaved }: { open: boolean; zones: IrrigationZone[]; selectedZoneId: string; onClose(): void; onSaved(): Promise<void> }) {
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setSaving(true); setError(null);
    const form = new FormData(event.currentTarget);
    try {
      await apiRequest('/api/v1/irrigation/applications', { method: 'POST', body: JSON.stringify({ zoneId: form.get('zoneId'), depthMillimeters: Number(form.get('depth')), source: 'Manual', startedAtUtc: new Date(String(form.get('startedAt'))).toISOString(), endedAtUtc: null, notes: String(form.get('notes') ?? '').trim() || null }) });
      onClose(); await onSaved();
    } catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível registrar a aplicação.'); } finally { setSaving(false); }
  }
  const defaultDate = new Date(); defaultDate.setMinutes(defaultDate.getMinutes() - defaultDate.getTimezoneOffset());
  return <Modal open={open} title="Registrar aplicação de água" description="O registro é append-only; correções posteriores devem ser novos lançamentos." onClose={onClose}><form className="form-grid" onSubmit={submit}>{error && <div className="inline-warning form-grid__full">{error}</div>}<label>Zona<select name="zoneId" required defaultValue={selectedZoneId}>{!selectedZoneId && <option value="">Selecione</option>}{zones.map(zone => <option key={zone.id} value={zone.id}>{zone.name}</option>)}</select></label><label>Lâmina aplicada (mm)<input name="depth" type="number" min="0.001" step="0.001" required /></label><label>Início<input name="startedAt" type="datetime-local" defaultValue={defaultDate.toISOString().slice(0, 16)} required /></label><label className="form-grid__full">Observação<textarea name="notes" rows={3} /></label><div className="form-actions form-grid__full"><Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button><Button type="submit" disabled={saving}>{saving ? 'Registrando...' : 'Registrar aplicação'}</Button></div></form></Modal>;
}
