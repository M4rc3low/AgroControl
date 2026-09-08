import { FormEvent, useEffect, useMemo, useState } from 'react';
import { Icon } from '../components/Icon';
import { Badge, Button, Card, EmptyState, Modal, PageHeader, Spinner } from '../components/Ui';
import { apiRequest, getAllPaged } from '../lib/api';
import { useAuth } from '../lib/auth';
import type { Farm, Field, PagedResult, Season } from '../lib/types';
import './SustainabilityPage.css';

type EnumValue = number | string;

interface EmissionFactor {
  id: string;
  name: string;
  category: EnumValue;
  unit: string;
  kgCo2ePerUnit: number;
  methodologyReference: string;
  notes: string | null;
  validFrom: string;
  validTo: string | null;
  isActive: boolean;
}

interface EmissionActivity {
  id: string;
  emissionFactorId: string;
  factorName: string;
  category: EnumValue;
  unit: string;
  factorKgCo2ePerUnit: number;
  quantity: number;
  emissionsKgCo2e: number;
  emissionsTCo2e: number;
  activityDate: string;
  origin: EnumValue;
  dataQuality: EnumValue;
  description: string;
  farmId: string | null;
  fieldId: string | null;
  seasonId: string | null;
  sourceModule: string | null;
  sourceReferenceId: string | null;
}

interface SustainabilityBreakdown {
  category: EnumValue;
  activityCount: number;
  totalKgCo2e: number;
  totalTCo2e: number;
  sharePercent: number;
}

interface SustainabilitySummary {
  from: string | null;
  to: string | null;
  farmId: string | null;
  fieldId: string | null;
  seasonId: string | null;
  activityCount: number;
  estimatedActivityCount: number;
  totalKgCo2e: number;
  totalTCo2e: number;
  breakdown: SustainabilityBreakdown[];
  previousPeriodKgCo2e: number | null;
  changePercent: number | null;
}

const categories = [
  { value: 1, key: 'Fuel', label: 'Combustível' },
  { value: 2, key: 'Fertilizer', label: 'Fertilizante' },
  { value: 3, key: 'Energy', label: 'Energia' },
  { value: 4, key: 'Transport', label: 'Transporte' },
  { value: 5, key: 'Custom', label: 'Customizada' }
];

function categoryLabel(value: EnumValue) {
  const item = categories.find(category => category.value === Number(value) || category.key === value);
  return item?.label ?? String(value);
}

function dataQualityLabel(value: EnumValue) {
  const labels: Record<string, string> = { '1': 'Medido', '2': 'Registrado', '3': 'Estimado', Measured: 'Medido', Recorded: 'Registrado', Estimated: 'Estimado' };
  return labels[String(value)] ?? String(value);
}

function buildQuery(values: Record<string, string>) {
  const params = new URLSearchParams();
  Object.entries(values).forEach(([key, value]) => { if (value) params.set(key, value); });
  const query = params.toString();
  return query ? `?${query}` : '';
}

export function SustainabilityPage() {
  const { platform } = useAuth();
  const allowed = Boolean(platform?.entitlements.modules.Sustainability);
  const [factors, setFactors] = useState<EmissionFactor[]>([]);
  const [activities, setActivities] = useState<EmissionActivity[]>([]);
  const [summary, setSummary] = useState<SustainabilitySummary | null>(null);
  const [farms, setFarms] = useState<Farm[]>([]);
  const [fields, setFields] = useState<Field[]>([]);
  const [seasons, setSeasons] = useState<Season[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [factorModal, setFactorModal] = useState(false);
  const [activityModal, setActivityModal] = useState(false);
  const [editingFactor, setEditingFactor] = useState<EmissionFactor | null>(null);
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [farmId, setFarmId] = useState('');
  const [seasonId, setSeasonId] = useState('');

  async function load() {
    if (!allowed) { setLoading(false); return; }
    setLoading(true);
    setError(null);
    try {
      const filterQuery = buildQuery({ from, to, farmId, seasonId });
      const activityQuery = buildQuery({ page: '1', pageSize: '50', from, to, farmId, seasonId });
      const [factorItems, activityPage, summaryData, farmItems, fieldItems, seasonItems] = await Promise.all([
        getAllPaged<EmissionFactor>('/api/v1/sustainability/emission-factors?includeInactive=true'),
        apiRequest<PagedResult<EmissionActivity>>(`/api/v1/sustainability/activities${activityQuery}`),
        apiRequest<SustainabilitySummary>(`/api/v1/sustainability/summary${filterQuery}`),
        getAllPaged<Farm>('/api/v1/farms'),
        getAllPaged<Field>('/api/v1/fields'),
        getAllPaged<Season>('/api/v1/seasons')
      ]);
      setFactors(factorItems);
      setActivities(activityPage.items);
      setSummary(summaryData);
      setFarms(farmItems);
      setFields(fieldItems);
      setSeasons(seasonItems);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível carregar os indicadores de sustentabilidade.');
    } finally { setLoading(false); }
  }

  useEffect(() => { void load(); }, [allowed]);

  const farmNames = useMemo(() => Object.fromEntries(farms.map(item => [item.id, item.name])), [farms]);
  const fieldNames = useMemo(() => Object.fromEntries(fields.map(item => [item.id, item.name])), [fields]);
  const seasonNames = useMemo(() => Object.fromEntries(seasons.map(item => [item.id, item.name])), [seasons]);
  const estimatedShare = summary && summary.activityCount > 0 ? (summary.estimatedActivityCount / summary.activityCount) * 100 : 0;

  async function deactivateFactor(factor: EmissionFactor) {
    if (!window.confirm(`Desativar o fator ${factor.name}? O histórico já calculado será preservado.`)) return;
    try { await apiRequest(`/api/v1/sustainability/emission-factors/${factor.id}`, { method: 'DELETE' }); await load(); }
    catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível desativar o fator.'); }
  }

  if (!allowed) return <EmptyState title="Sustentabilidade não disponível no plano" description="O acesso depende do entitlement Sustainability da organização." />;
  if (loading && !summary) return <Spinner label="Carregando sustentabilidade" />;
  if (error && !summary) return <EmptyState title="Sustentabilidade indisponível" description={error} action={<Button onClick={() => void load()}><Icon name="refresh" size={16} /> Tentar novamente</Button>} />;

  return <div className="page-stack sustainability-page">
    <PageHeader eyebrow="Gestão ambiental" title="Sustentabilidade" description="Estime emissões de CO₂e por atividade, propriedade e safra com rastreabilidade do fator utilizado." actions={<div className="sustainability-actions"><Button variant="secondary" onClick={() => void load()}><Icon name="refresh" size={16} /> Atualizar</Button><Button onClick={() => { setEditingFactor(null); setFactorModal(true); }}><Icon name="plus" size={16} /> Novo fator</Button><Button onClick={() => setActivityModal(true)} disabled={!factors.some(item => item.isActive)}><Icon name="plus" size={16} /> Registrar atividade</Button></div>} />
    <div className="sustainability-disclaimer"><strong>Estimativa gerencial.</strong> Estes indicadores não constituem inventário certificado, auditoria ambiental ou crédito de carbono.</div>
    {error && <div className="inline-warning">{error}</div>}

    <Card className="sustainability-filters"><label>De<input type="date" value={from} onChange={event => setFrom(event.target.value)} /></label><label>Até<input type="date" value={to} onChange={event => setTo(event.target.value)} /></label><label>Propriedade<select value={farmId} onChange={event => setFarmId(event.target.value)}><option value="">Todas</option>{farms.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label><label>Safra<select value={seasonId} onChange={event => setSeasonId(event.target.value)}><option value="">Todas</option>{seasons.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label><Button variant="secondary" onClick={() => void load()}>Aplicar filtros</Button></Card>

    <div className="sustainability-metrics">
      <Card><span>Emissões estimadas</span><strong>{(summary?.totalTCo2e ?? 0).toLocaleString('pt-BR', { maximumFractionDigits: 4 })} tCO₂e</strong><small>{(summary?.totalKgCo2e ?? 0).toLocaleString('pt-BR', { maximumFractionDigits: 2 })} kgCO₂e</small></Card>
      <Card><span>Atividades</span><strong>{summary?.activityCount ?? 0}</strong><small>{estimatedShare.toLocaleString('pt-BR', { maximumFractionDigits: 1 })}% com quantidade estimada</small></Card>
      <Card><span>Variação vs. período anterior</span><strong>{summary?.changePercent == null ? '—' : `${summary.changePercent > 0 ? '+' : ''}${summary.changePercent.toLocaleString('pt-BR')}%`}</strong><small>{summary?.previousPeriodKgCo2e == null ? 'Defina início e fim para comparar' : `${summary.previousPeriodKgCo2e.toLocaleString('pt-BR', { maximumFractionDigits: 2 })} kgCO₂e no período anterior`}</small></Card>
    </div>

    <section className="sustainability-section">
      <div className="section-heading"><div><span className="eyebrow">Composição</span><h2>Emissões por categoria</h2></div></div>
      {!summary?.breakdown.length ? <EmptyState title="Sem emissões no período" description="Registre atividades para começar a acompanhar a composição das emissões." /> : <Card className="emission-breakdown">{summary.breakdown.map(item => <div key={String(item.category)} className="emission-breakdown__row"><div><strong>{categoryLabel(item.category)}</strong><span>{item.totalTCo2e.toLocaleString('pt-BR', { maximumFractionDigits: 4 })} tCO₂e · {item.activityCount} atividade(s)</span></div><div className="emission-breakdown__track"><span style={{ width: `${Math.min(100, Math.max(0, Math.abs(item.sharePercent)))}%` }} /></div><b>{item.sharePercent.toLocaleString('pt-BR', { maximumFractionDigits: 1 })}%</b></div>)}</Card>}
    </section>

    <section className="sustainability-section">
      <div className="section-heading"><div><span className="eyebrow">Metodologia</span><h2>Fatores de emissão</h2></div></div>
      {factors.length === 0 ? <EmptyState title="Nenhum fator cadastrado" description="Cadastre um fator com unidade, valor e referência metodológica antes de registrar atividades." /> : <div className="table-wrap"><table><thead><tr><th>Fator</th><th>Categoria</th><th>Valor</th><th>Vigência</th><th>Status</th><th></th></tr></thead><tbody>{factors.map(item => <tr key={item.id}><td><strong>{item.name}</strong><small className="table-subline">{item.methodologyReference}</small></td><td>{categoryLabel(item.category)}</td><td>{item.kgCo2ePerUnit.toLocaleString('pt-BR', { maximumFractionDigits: 6 })} kgCO₂e/{item.unit}</td><td>{item.validFrom}{item.validTo ? ` → ${item.validTo}` : ' → vigente'}</td><td><Badge tone={item.isActive ? 'success' : 'neutral'}>{item.isActive ? 'Ativo' : 'Inativo'}</Badge></td><td><div className="row-actions"><Button variant="secondary" onClick={() => { setEditingFactor(item); setFactorModal(true); }}>Editar</Button>{item.isActive && <Button variant="secondary" onClick={() => void deactivateFactor(item)}>Desativar</Button>}</div></td></tr>)}</tbody></table></div>}
    </section>

    <section className="sustainability-section">
      <div className="section-heading"><div><span className="eyebrow">Ledger</span><h2>Atividades de emissão</h2></div><Button variant="secondary" onClick={() => setActivityModal(true)} disabled={!factors.some(item => item.isActive)}><Icon name="plus" size={16} /> Registrar</Button></div>
      {activities.length === 0 ? <EmptyState title="Nenhuma atividade registrada" description="O histórico append-only aparecerá aqui conforme as fontes forem registradas." /> : <div className="table-wrap"><table><thead><tr><th>Data</th><th>Atividade</th><th>Origem</th><th>Quantidade</th><th>Emissão</th><th>Contexto</th></tr></thead><tbody>{activities.map(item => <tr key={item.id}><td>{item.activityDate}</td><td><strong>{item.description}</strong><small className="table-subline">{item.factorName} · {dataQualityLabel(item.dataQuality)}</small></td><td>{item.sourceModule ? `${item.sourceModule} · ${item.sourceReferenceId}` : String(item.origin) === '3' || item.origin === 'Correction' ? 'Correção' : 'Manual'}</td><td>{item.quantity.toLocaleString('pt-BR')} {item.unit}</td><td><strong>{item.emissionsKgCo2e.toLocaleString('pt-BR', { maximumFractionDigits: 3 })} kgCO₂e</strong></td><td>{item.seasonId ? seasonNames[item.seasonId] : item.fieldId ? fieldNames[item.fieldId] : item.farmId ? farmNames[item.farmId] : 'Organização'}</td></tr>)}</tbody></table></div>}
    </section>

    <FactorModal open={factorModal} factor={editingFactor} onClose={() => { setFactorModal(false); setEditingFactor(null); }} onSaved={load} />
    <ActivityModal open={activityModal} factors={factors.filter(item => item.isActive)} farms={farms} fields={fields} seasons={seasons} onClose={() => setActivityModal(false)} onSaved={load} />
  </div>;
}

function FactorModal({ open, factor, onClose, onSaved }: { open: boolean; factor: EmissionFactor | null; onClose(): void; onSaved(): Promise<void> }) {
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setSaving(true); setError(null);
    const form = new FormData(event.currentTarget);
    const payload = { name: form.get('name'), category: Number(form.get('category')), unit: form.get('unit'), kgCo2ePerUnit: Number(form.get('factor')), methodologyReference: form.get('methodologyReference'), notes: String(form.get('notes') ?? '').trim() || null, validFrom: form.get('validFrom'), validTo: String(form.get('validTo') ?? '').trim() || null };
    try {
      if (factor) await apiRequest(`/api/v1/sustainability/emission-factors/${factor.id}`, { method: 'PUT', body: JSON.stringify(payload) });
      else await apiRequest('/api/v1/sustainability/emission-factors', { method: 'POST', body: JSON.stringify(payload) });
      onClose(); await onSaved();
    } catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível salvar o fator.'); } finally { setSaving(false); }
  }
  return <Modal open={open} title={factor ? 'Editar fator de emissão' : 'Novo fator de emissão'} description="O valor usado em atividades já registradas permanece preservado por snapshot." onClose={onClose}><form key={factor?.id ?? 'new'} className="form-grid" onSubmit={submit}>{error && <div className="inline-warning form-grid__full">{error}</div>}<label>Nome<input name="name" required defaultValue={factor?.name ?? ''} placeholder="Diesel agrícola" /></label><label>Categoria<select name="category" required defaultValue={String(factor ? categories.find(item => item.value === Number(factor.category) || item.key === factor.category)?.value ?? 1 : 1)}>{categories.map(item => <option key={item.value} value={item.value}>{item.label}</option>)}</select></label><label>Unidade<input name="unit" required defaultValue={factor?.unit ?? 'L'} placeholder="L, kg, kWh..." /></label><label>kgCO₂e por unidade<input name="factor" type="number" min="0.000001" step="0.000001" required defaultValue={factor?.kgCo2ePerUnit ?? ''} /></label><label>Válido desde<input name="validFrom" type="date" required defaultValue={factor?.validFrom ?? new Date().toISOString().slice(0, 10)} /></label><label>Válido até<input name="validTo" type="date" defaultValue={factor?.validTo ?? ''} /></label><label className="form-grid__full">Referência metodológica<input name="methodologyReference" required defaultValue={factor?.methodologyReference ?? ''} placeholder="Fonte, documento, versão ou metodologia utilizada" /></label><label className="form-grid__full">Observação<textarea name="notes" rows={3} defaultValue={factor?.notes ?? ''} /></label><div className="form-actions form-grid__full"><Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button><Button type="submit" disabled={saving}>{saving ? 'Salvando...' : 'Salvar fator'}</Button></div></form></Modal>;
}

function ActivityModal({ open, factors, farms, fields, seasons, onClose, onSaved }: { open: boolean; factors: EmissionFactor[]; farms: Farm[]; fields: Field[]; seasons: Season[]; onClose(): void; onSaved(): Promise<void> }) {
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setSaving(true); setError(null);
    const form = new FormData(event.currentTarget);
    const payload = { emissionFactorId: form.get('factorId'), quantity: Number(form.get('quantity')), activityDate: form.get('activityDate'), origin: Number(form.get('origin')), dataQuality: Number(form.get('dataQuality')), description: form.get('description'), farmId: String(form.get('farmId') ?? '').trim() || null, fieldId: String(form.get('fieldId') ?? '').trim() || null, seasonId: String(form.get('seasonId') ?? '').trim() || null, sourceModule: null, sourceReferenceId: null, notes: String(form.get('notes') ?? '').trim() || null };
    try { await apiRequest('/api/v1/sustainability/activities', { method: 'POST', body: JSON.stringify(payload) }); onClose(); await onSaved(); }
    catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível registrar a atividade.'); } finally { setSaving(false); }
  }
  return <Modal open={open} title="Registrar atividade de emissão" description="O lançamento é append-only. Para corrigir um valor, registre uma atividade de correção." onClose={onClose}><form className="form-grid" onSubmit={submit}>{error && <div className="inline-warning form-grid__full">{error}</div>}<label>Fator<select name="factorId" required><option value="">Selecione</option>{factors.map(item => <option key={item.id} value={item.id}>{item.name} · {item.kgCo2ePerUnit} kgCO₂e/{item.unit}</option>)}</select></label><label>Quantidade<input name="quantity" type="number" step="0.000001" required /></label><label>Data<input name="activityDate" type="date" required defaultValue={new Date().toISOString().slice(0, 10)} /></label><label>Tipo de lançamento<select name="origin" defaultValue="1"><option value="1">Manual</option><option value="3">Correção</option></select></label><label>Qualidade do dado<select name="dataQuality" defaultValue="2"><option value="1">Medido</option><option value="2">Registrado</option><option value="3">Estimado</option></select></label><label>Descrição<input name="description" required placeholder="Consumo de diesel no plantio" /></label><label>Propriedade<select name="farmId"><option value="">Nenhuma</option>{farms.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label><label>Talhão<select name="fieldId"><option value="">Nenhum</option>{fields.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label><label>Safra<select name="seasonId"><option value="">Nenhuma</option>{seasons.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label><label className="form-grid__full">Observação<textarea name="notes" rows={3} /></label><div className="form-actions form-grid__full"><Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button><Button type="submit" disabled={saving || factors.length === 0}>{saving ? 'Registrando...' : 'Registrar atividade'}</Button></div></form></Modal>;
}
