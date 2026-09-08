import { FormEvent, useEffect, useMemo, useState } from 'react';
import { Icon } from '../components/Icon';
import { Badge, Button, Card, EmptyState, Modal, PageHeader, Spinner } from '../components/Ui';
import { apiRequest } from '../lib/api';
import { useAuth } from '../lib/auth';
import type { PagedResult } from '../lib/types';
import './CommercialPage.css';

type EnumValue = number | string;

interface Customer {
  id: string;
  name: string;
  tradeName: string | null;
  taxId: string | null;
  email: string | null;
  phone: string | null;
  countryCode: string | null;
  city: string | null;
  state: string | null;
  status: EnumValue;
  notes: string | null;
}

interface Contact {
  id: string;
  customerId: string;
  name: string;
  role: string | null;
  email: string | null;
  phone: string | null;
  isPrimary: boolean;
  isActive: boolean;
}

interface Opportunity {
  id: string;
  customerId: string;
  title: string;
  expectedValue: number;
  currency: string;
  probabilityPercent: number;
  weightedValue: number;
  stage: EnumValue;
  expectedCloseDate: string | null;
  closedOn: string | null;
  ownerName: string | null;
  nextStep: string | null;
  notes: string | null;
}

interface StageEvent {
  id: string;
  opportunityId: string;
  fromStage: EnumValue | null;
  toStage: EnumValue;
  occurredOn: string;
  notes: string | null;
}

interface CommercialSummary {
  opportunityCount: number;
  openCount: number;
  wonCount: number;
  lostCount: number;
  conversionRatePercent: number;
  byCurrency: Array<{ currency: string; openCount: number; openPipelineValue: number; weightedPipelineValue: number; wonCount: number; wonValue: number }>;
  byStage: Array<{ stage: EnumValue; count: number }>;
}

const customerStatuses = [
  { value: 1, key: 'Lead', label: 'Lead' },
  { value: 2, key: 'Prospect', label: 'Prospect' },
  { value: 3, key: 'Customer', label: 'Cliente' },
  { value: 4, key: 'Inactive', label: 'Inativo' }
];
const stages = [
  { value: 1, key: 'Lead', label: 'Lead' },
  { value: 2, key: 'Qualification', label: 'Qualificação' },
  { value: 3, key: 'Proposal', label: 'Proposta' },
  { value: 4, key: 'Negotiation', label: 'Negociação' },
  { value: 5, key: 'Won', label: 'Ganha' },
  { value: 6, key: 'Lost', label: 'Perdida' }
];

function enumNumber(value: EnumValue, values: Array<{ value: number; key: string }>) {
  if (typeof value === 'number') return value;
  const match = values.find(item => item.key === value);
  return match?.value ?? Number(value);
}
function stageLabel(value: EnumValue) { return stages.find(item => item.value === enumNumber(value, stages))?.label ?? String(value); }
function customerStatusLabel(value: EnumValue) { return customerStatuses.find(item => item.value === enumNumber(value, customerStatuses))?.label ?? String(value); }
function stageTone(value: EnumValue): 'neutral' | 'warning' | 'success' | 'danger' | 'info' {
  const number = enumNumber(value, stages);
  if (number === 5) return 'success';
  if (number === 6) return 'danger';
  if (number === 4) return 'warning';
  if (number === 2 || number === 3) return 'info';
  return 'neutral';
}
function formatMoney(value: number, currency: string) {
  return value.toLocaleString('pt-BR', { style: 'currency', currency });
}
function nextStages(value: EnumValue) {
  const current = enumNumber(value, stages);
  const map: Record<number, number[]> = { 1: [2, 6], 2: [3, 6], 3: [4, 5, 6], 4: [3, 5, 6] };
  return stages.filter(item => (map[current] ?? []).includes(item.value));
}

export function CommercialPage() {
  const { platform } = useAuth();
  const allowed = Boolean(platform?.entitlements.modules.Commercial);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [opportunities, setOpportunities] = useState<Opportunity[]>([]);
  const [summary, setSummary] = useState<CommercialSummary | null>(null);
  const [selectedOpportunity, setSelectedOpportunity] = useState<Opportunity | null>(null);
  const [timeline, setTimeline] = useState<StageEvent[]>([]);
  const [selectedCustomer, setSelectedCustomer] = useState<Customer | null>(null);
  const [contacts, setContacts] = useState<Contact[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [customerModal, setCustomerModal] = useState(false);
  const [opportunityModal, setOpportunityModal] = useState(false);
  const [contactModal, setContactModal] = useState(false);
  const [search, setSearch] = useState('');
  const [stageFilter, setStageFilter] = useState('');

  async function load() {
    if (!allowed) { setLoading(false); return; }
    setLoading(true); setError(null);
    try {
      const query = new URLSearchParams({ page: '1', pageSize: '100' });
      if (search) query.set('search', search);
      if (stageFilter) query.set('stage', stageFilter);
      const [customerPage, opportunityPage, summaryData] = await Promise.all([
        apiRequest<PagedResult<Customer>>('/api/v1/commercial/customers?page=1&pageSize=100'),
        apiRequest<PagedResult<Opportunity>>(`/api/v1/commercial/opportunities?${query.toString()}`),
        apiRequest<CommercialSummary>('/api/v1/commercial/summary')
      ]);
      setCustomers(customerPage.items); setOpportunities(opportunityPage.items); setSummary(summaryData);
      if (selectedOpportunity) {
        const updated = opportunityPage.items.find(item => item.id === selectedOpportunity.id) ?? null;
        setSelectedOpportunity(updated);
        if (updated) await loadTimeline(updated.id);
      }
    } catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível carregar o comercial.'); }
    finally { setLoading(false); }
  }

  async function loadTimeline(id: string) {
    setTimeline(await apiRequest<StageEvent[]>(`/api/v1/commercial/opportunities/${id}/timeline`));
  }
  async function loadContacts(customer: Customer) {
    setSelectedCustomer(customer);
    try { setContacts(await apiRequest<Contact[]>(`/api/v1/commercial/customers/${customer.id}/contacts`)); }
    catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível carregar os contatos.'); }
  }
  async function chooseOpportunity(item: Opportunity) {
    setSelectedOpportunity(item);
    try { await loadTimeline(item.id); }
    catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível carregar o histórico.'); }
  }
  async function transition(item: Opportunity, stage: number) {
    try {
      const updated = await apiRequest<Opportunity>(`/api/v1/commercial/opportunities/${item.id}/stage`, {
        method: 'POST', body: JSON.stringify({ stage, occurredOn: new Date().toISOString().slice(0, 10), notes: null })
      });
      setSelectedOpportunity(updated); await load();
    } catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível avançar a oportunidade.'); }
  }

  useEffect(() => { void load(); }, [allowed]);
  const customerNames = useMemo(() => Object.fromEntries(customers.map(item => [item.id, item.name])), [customers]);

  if (!allowed) return <EmptyState title="Comercial não disponível no plano" description="O módulo Commercial depende do entitlement da organização." />;
  if (loading && !summary) return <Spinner label="Carregando comercial" />;
  if (error && !summary) return <EmptyState title="Comercial indisponível" description={error} action={<Button onClick={() => void load()}>Tentar novamente</Button>} />;

  return <div className="page-stack commercial-page">
    <PageHeader eyebrow="Relacionamento e vendas" title="Comercial e CRM" description="Clientes, contatos e oportunidades conectados à operação rural." actions={<div className="commercial-actions"><Button variant="secondary" onClick={() => void load()}><Icon name="refresh" size={16} /> Atualizar</Button><Button onClick={() => setCustomerModal(true)}><Icon name="plus" size={16} /> Cliente</Button><Button onClick={() => setOpportunityModal(true)}><Icon name="plus" size={16} /> Oportunidade</Button></div>} />
    <div className="commercial-disclaimer"><strong>Gestão comercial.</strong> O AgroControl organiza relacionamento e pipeline, mas não substitui contrato, faturamento, emissão fiscal ou assinatura eletrônica.</div>
    {error && <div className="inline-warning">{error}</div>}

    <div className="commercial-metrics">
      <Card><span>Pipeline aberto</span><strong>{summary?.openCount ?? 0}</strong><small>{summary?.opportunityCount ?? 0} oportunidade(s) no total</small></Card>
      <Card><span>Conversão</span><strong>{(summary?.conversionRatePercent ?? 0).toLocaleString('pt-BR')}%</strong><small>{summary?.wonCount ?? 0} ganha(s) · {summary?.lostCount ?? 0} perdida(s)</small></Card>
      <Card><span>Pipeline por moeda</span><strong>{summary?.byCurrency.length ?? 0}</strong><small>{summary?.byCurrency.map(item => `${item.currency}: ${formatMoney(item.weightedPipelineValue, item.currency)}`).join(' · ') || 'Sem valores'}</small></Card>
    </div>

    <Card className="commercial-filters"><label>Busca<input value={search} onChange={event => setSearch(event.target.value)} placeholder="Oportunidade ou próximo passo" /></label><label>Etapa<select value={stageFilter} onChange={event => setStageFilter(event.target.value)}><option value="">Todas</option>{stages.map(item => <option key={item.value} value={item.value}>{item.label}</option>)}</select></label><Button variant="secondary" onClick={() => void load()}>Aplicar</Button></Card>

    <div className="commercial-grid">
      <Card className="commercial-panel"><div className="section-heading"><div><span className="eyebrow">Relacionamento</span><h2>Clientes</h2></div></div>{customers.length === 0 ? <EmptyState title="Nenhum cliente" description="Cadastre o primeiro lead ou cliente." /> : <div className="commercial-list">{customers.map(customer => <button key={customer.id} className={`commercial-list__item ${selectedCustomer?.id === customer.id ? 'is-selected' : ''}`} onClick={() => void loadContacts(customer)}><div><strong>{customer.name}</strong><small>{customer.tradeName ?? customer.email ?? 'Sem contato principal'}</small></div><Badge tone={enumNumber(customer.status, customerStatuses) === 4 ? 'neutral' : 'info'}>{customerStatusLabel(customer.status)}</Badge></button>)}</div>}
        {selectedCustomer && <div className="commercial-detail"><div className="section-heading"><div><h3>Contatos de {selectedCustomer.name}</h3></div><Button variant="secondary" onClick={() => setContactModal(true)}>Adicionar contato</Button></div>{contacts.length === 0 ? <p className="muted">Nenhum contato ativo.</p> : contacts.map(contact => <div className="contact-row" key={contact.id}><div><strong>{contact.name}</strong><small>{contact.role ?? 'Contato'} · {contact.email ?? contact.phone ?? 'Sem canal'}</small></div>{contact.isPrimary && <Badge tone="success">Principal</Badge>}</div>)}</div>}
      </Card>

      <Card className="commercial-panel"><div className="section-heading"><div><span className="eyebrow">Pipeline</span><h2>Oportunidades</h2></div></div>{opportunities.length === 0 ? <EmptyState title="Nenhuma oportunidade" description="Crie uma oportunidade comercial para iniciar o pipeline." /> : <div className="commercial-list">{opportunities.map(item => <button key={item.id} className={`commercial-list__item ${selectedOpportunity?.id === item.id ? 'is-selected' : ''}`} onClick={() => void chooseOpportunity(item)}><div><strong>{item.title}</strong><small>{customerNames[item.customerId] ?? 'Cliente'} · {formatMoney(item.expectedValue, item.currency)} · {item.probabilityPercent}%</small></div><Badge tone={stageTone(item.stage)}>{stageLabel(item.stage)}</Badge></button>)}</div>}
        {selectedOpportunity && <div className="commercial-detail"><h3>{selectedOpportunity.title}</h3><p>{selectedOpportunity.nextStep ?? 'Nenhum próximo passo registrado.'}</p><div className="commercial-stage-actions">{nextStages(selectedOpportunity.stage).map(item => <Button key={item.value} variant={item.value === 6 ? 'danger' : 'secondary'} onClick={() => void transition(selectedOpportunity, item.value)}>{item.label}</Button>)}</div><div className="timeline"><h4>Histórico</h4>{timeline.map(event => <div className="timeline__item" key={event.id}><strong>{stageLabel(event.toStage)}</strong><span>{event.occurredOn}</span>{event.notes && <small>{event.notes}</small>}</div>)}</div></div>}
      </Card>
    </div>

    <CustomerModal open={customerModal} onClose={() => setCustomerModal(false)} onSaved={async () => { setCustomerModal(false); await load(); }} />
    <OpportunityModal open={opportunityModal} customers={customers} onClose={() => setOpportunityModal(false)} onSaved={async () => { setOpportunityModal(false); await load(); }} />
    <ContactModal open={contactModal} customer={selectedCustomer} onClose={() => setContactModal(false)} onSaved={async () => { setContactModal(false); if (selectedCustomer) await loadContacts(selectedCustomer); }} />
  </div>;
}

function CustomerModal({ open, onClose, onSaved }: { open: boolean; onClose(): void; onSaved(): Promise<void> }) {
  const [saving, setSaving] = useState(false);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); const data = new FormData(event.currentTarget); setSaving(true);
    try { await apiRequest('/api/v1/commercial/customers', { method: 'POST', body: JSON.stringify({ name: data.get('name'), tradeName: data.get('tradeName') || null, taxId: data.get('taxId') || null, email: data.get('email') || null, phone: data.get('phone') || null, countryCode: data.get('countryCode') || null, city: data.get('city') || null, state: data.get('state') || null, status: Number(data.get('status')), notes: null }) }); await onSaved(); }
    finally { setSaving(false); }
  }
  return <Modal open={open} title="Novo cliente" description="Cadastre um lead, prospect ou cliente." onClose={onClose}><form className="form-grid" onSubmit={submit}><label>Nome<input name="name" required /></label><label>Nome fantasia<input name="tradeName" /></label><label>Documento<input name="taxId" /></label><label>E-mail<input name="email" type="email" /></label><label>Telefone<input name="phone" /></label><label>País<input name="countryCode" maxLength={2} defaultValue="BR" /></label><label>Cidade<input name="city" /></label><label>UF<input name="state" maxLength={2} /></label><label>Status<select name="status" defaultValue="1">{customerStatuses.map(item => <option key={item.value} value={item.value}>{item.label}</option>)}</select></label><div className="form-actions"><Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button><Button type="submit" disabled={saving}>{saving ? 'Salvando...' : 'Salvar'}</Button></div></form></Modal>;
}

function OpportunityModal({ open, customers, onClose, onSaved }: { open: boolean; customers: Customer[]; onClose(): void; onSaved(): Promise<void> }) {
  const [saving, setSaving] = useState(false);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); const data = new FormData(event.currentTarget); setSaving(true);
    try { await apiRequest('/api/v1/commercial/opportunities', { method: 'POST', body: JSON.stringify({ customerId: data.get('customerId'), title: data.get('title'), farmId: null, cropId: null, seasonId: null, exportOrderId: null, expectedValue: Number(data.get('expectedValue')), currency: data.get('currency'), probabilityPercent: Number(data.get('probability')), expectedCloseDate: data.get('expectedCloseDate') || null, ownerName: data.get('ownerName') || null, nextStep: data.get('nextStep') || null, notes: null }) }); await onSaved(); }
    finally { setSaving(false); }
  }
  return <Modal open={open} title="Nova oportunidade" description="Oportunidades começam na etapa Lead." onClose={onClose}><form className="form-grid" onSubmit={submit}><label>Cliente<select name="customerId" required defaultValue=""><option value="" disabled>Selecione</option>{customers.filter(item => enumNumber(item.status, customerStatuses) !== 4).map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label><label>Título<input name="title" required /></label><label>Valor esperado<input name="expectedValue" type="number" min="0.01" step="0.01" required /></label><label>Moeda<input name="currency" defaultValue="BRL" maxLength={3} required /></label><label>Probabilidade %<input name="probability" type="number" min="0" max="100" step="1" defaultValue="25" required /></label><label>Fechamento previsto<input name="expectedCloseDate" type="date" /></label><label>Responsável<input name="ownerName" /></label><label className="form-span-2">Próximo passo<input name="nextStep" /></label><div className="form-actions"><Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button><Button type="submit" disabled={saving || customers.length === 0}>{saving ? 'Salvando...' : 'Salvar'}</Button></div></form></Modal>;
}

function ContactModal({ open, customer, onClose, onSaved }: { open: boolean; customer: Customer | null; onClose(): void; onSaved(): Promise<void> }) {
  const [saving, setSaving] = useState(false);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!customer) return; const data = new FormData(event.currentTarget); setSaving(true);
    try { await apiRequest(`/api/v1/commercial/customers/${customer.id}/contacts`, { method: 'POST', body: JSON.stringify({ name: data.get('name'), role: data.get('role') || null, email: data.get('email') || null, phone: data.get('phone') || null, isPrimary: data.get('isPrimary') === 'on' }) }); await onSaved(); }
    finally { setSaving(false); }
  }
  return <Modal open={open} title="Novo contato" description={customer ? `Contato de ${customer.name}` : undefined} onClose={onClose}><form className="form-grid" onSubmit={submit}><label>Nome<input name="name" required /></label><label>Função<input name="role" /></label><label>E-mail<input name="email" type="email" /></label><label>Telefone<input name="phone" /></label><label className="checkbox-row"><input name="isPrimary" type="checkbox" /> Contato principal</label><div className="form-actions"><Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button><Button type="submit" disabled={saving || !customer}>{saving ? 'Salvando...' : 'Salvar'}</Button></div></form></Modal>;
}
