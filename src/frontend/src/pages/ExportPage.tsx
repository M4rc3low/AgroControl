import { FormEvent, useEffect, useMemo, useState } from 'react';
import { Icon } from '../components/Icon';
import { Badge, Button, Card, EmptyState, Modal, PageHeader, Spinner } from '../components/Ui';
import { apiRequest, getAllPaged } from '../lib/api';
import { useAuth } from '../lib/auth';
import type { Crop, Farm, Field, PagedResult, Season } from '../lib/types';
import './ExportPage.css';

type EnumValue = number | string;

interface ExportOrder {
  id: string;
  orderNumber: string;
  buyerName: string;
  buyerReference: string | null;
  destinationCountryCode: string;
  farmId: string | null;
  fieldId: string | null;
  cropId: string | null;
  seasonId: string | null;
  productDescription: string;
  quantity: number;
  unit: string;
  currency: string;
  unitPrice: number;
  commercialValue: number;
  exchangeRateToBrl: number;
  estimatedValueBrl: number;
  incoterm: EnumValue;
  status: EnumValue;
  originLocation: string | null;
  destinationLocation: string | null;
  contractedOn: string | null;
  estimatedShipmentDate: string | null;
  actualShipmentDate: string | null;
  estimatedDeliveryDate: string | null;
  actualDeliveryDate: string | null;
  shipmentReference: string | null;
  bookingReference: string | null;
  containerReference: string | null;
  notes: string | null;
}

interface ExportDocument {
  id: string;
  orderId: string;
  type: EnumValue;
  customLabel: string | null;
  status: EnumValue;
  referenceNumber: string | null;
  issuedOn: string | null;
  notes: string | null;
}

interface ExportCost {
  id: string;
  orderId: string;
  type: EnumValue;
  description: string;
  amount: number;
  currency: string;
  exchangeRateToBrl: number;
  amountBrl: number;
  incurredOn: string;
  notes: string | null;
}

interface ExportStatusEvent {
  id: string;
  orderId: string;
  fromStatus: EnumValue | null;
  toStatus: EnumValue;
  occurredOn: string;
  notes: string | null;
}

interface ExportSummary {
  from: string | null;
  to: string | null;
  orderCount: number;
  contractedCount: number;
  inTransitCount: number;
  deliveredCount: number;
  estimatedCommercialValueBrl: number;
  totalLogisticsCostBrl: number;
  estimatedOperationalMarginBrl: number;
  byStatus: Array<{ key: string; orderCount: number; estimatedValueBrl: number }>;
  byCountry: Array<{ key: string; orderCount: number; estimatedValueBrl: number }>;
  byCurrency: Array<{ key: string; orderCount: number; estimatedValueBrl: number }>;
}

const statuses = [
  { value: 1, key: 'Draft', label: 'Rascunho' },
  { value: 2, key: 'Negotiation', label: 'Negociação' },
  { value: 3, key: 'Contracted', label: 'Contratado' },
  { value: 4, key: 'InTransit', label: 'Em trânsito' },
  { value: 5, key: 'Delivered', label: 'Entregue' },
  { value: 6, key: 'Cancelled', label: 'Cancelado' }
];

const incoterms = ['EXW', 'FCA', 'CPT', 'CIP', 'DAP', 'DPU', 'DDP', 'FAS', 'FOB', 'CFR', 'CIF'];
const documentTypes = ['Commercial Invoice', 'Packing List', 'Certificate of Origin', 'Phytosanitary Certificate', 'Bill of Lading', 'Custom'];
const documentStatuses = ['Pending', 'Ready', 'Issued', 'NotApplicable'];
const costTypes = ['Freight', 'Insurance', 'Port', 'Customs', 'Inspection', 'Other'];

function enumNumber(value: EnumValue, labels: string[]) {
  if (typeof value === 'number') return value;
  const index = labels.findIndex(item => item === value);
  return index >= 0 ? index + 1 : Number(value);
}

function statusLabel(value: EnumValue) {
  const item = statuses.find(status => status.value === Number(value) || status.key === value);
  return item?.label ?? String(value);
}

function statusTone(value: EnumValue): 'neutral' | 'warning' | 'success' | 'danger' | 'info' {
  const number = enumNumber(value, statuses.map(item => item.key));
  if (number === 5) return 'success';
  if (number === 6) return 'danger';
  if (number === 3 || number === 4) return 'info';
  if (number === 2) return 'warning';
  return 'neutral';
}

function formatBrl(value: number) {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

function nextStatuses(value: EnumValue) {
  const current = enumNumber(value, statuses.map(item => item.key));
  const map: Record<number, number[]> = { 1: [2, 3, 6], 2: [1, 3, 6], 3: [4, 6], 4: [5, 6] };
  return statuses.filter(status => (map[current] ?? []).includes(status.value));
}

function queryString(values: Record<string, string>) {
  const params = new URLSearchParams();
  Object.entries(values).forEach(([key, value]) => { if (value) params.set(key, value); });
  const query = params.toString();
  return query ? `?${query}` : '';
}

export function ExportPage() {
  const { platform } = useAuth();
  const allowed = Boolean(platform?.entitlements.modules.Export);
  const [orders, setOrders] = useState<ExportOrder[]>([]);
  const [summary, setSummary] = useState<ExportSummary | null>(null);
  const [selected, setSelected] = useState<ExportOrder | null>(null);
  const [documents, setDocuments] = useState<ExportDocument[]>([]);
  const [costs, setCosts] = useState<ExportCost[]>([]);
  const [timeline, setTimeline] = useState<ExportStatusEvent[]>([]);
  const [farms, setFarms] = useState<Farm[]>([]);
  const [fields, setFields] = useState<Field[]>([]);
  const [crops, setCrops] = useState<Crop[]>([]);
  const [seasons, setSeasons] = useState<Season[]>([]);
  const [loading, setLoading] = useState(true);
  const [detailLoading, setDetailLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [orderModal, setOrderModal] = useState(false);
  const [costModal, setCostModal] = useState(false);
  const [editing, setEditing] = useState<ExportOrder | null>(null);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [countryFilter, setCountryFilter] = useState('');
  const [seasonFilter, setSeasonFilter] = useState('');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');

  async function load() {
    if (!allowed) { setLoading(false); return; }
    setLoading(true); setError(null);
    try {
      const filters = { search, status: statusFilter, countryCode: countryFilter.toUpperCase(), seasonId: seasonFilter, from, to };
      const [orderPage, summaryData, farmItems, fieldItems, cropItems, seasonItems] = await Promise.all([
        apiRequest<PagedResult<ExportOrder>>(`/api/v1/export/orders${queryString({ page: '1', pageSize: '100', ...filters })}`),
        apiRequest<ExportSummary>(`/api/v1/export/summary${queryString({ status: statusFilter, countryCode: countryFilter.toUpperCase(), from, to })}`),
        getAllPaged<Farm>('/api/v1/farms'),
        getAllPaged<Field>('/api/v1/fields'),
        getAllPaged<Crop>('/api/v1/crops'),
        getAllPaged<Season>('/api/v1/seasons')
      ]);
      setOrders(orderPage.items); setSummary(summaryData); setFarms(farmItems); setFields(fieldItems); setCrops(cropItems); setSeasons(seasonItems);
      if (selected) {
        const updated = orderPage.items.find(item => item.id === selected.id) ?? null;
        setSelected(updated);
        if (updated) await loadDetail(updated.id);
      }
    } catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível carregar as operações de exportação.'); }
    finally { setLoading(false); }
  }

  async function loadDetail(orderId: string) {
    setDetailLoading(true);
    try {
      const [documentItems, costItems, timelineItems] = await Promise.all([
        apiRequest<ExportDocument[]>(`/api/v1/export/orders/${orderId}/documents`),
        apiRequest<ExportCost[]>(`/api/v1/export/orders/${orderId}/costs`),
        apiRequest<ExportStatusEvent[]>(`/api/v1/export/orders/${orderId}/timeline`)
      ]);
      setDocuments(documentItems); setCosts(costItems); setTimeline(timelineItems);
    } catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível carregar os detalhes do pedido.'); }
    finally { setDetailLoading(false); }
  }

  useEffect(() => { void load(); }, [allowed]);

  const farmNames = useMemo(() => Object.fromEntries(farms.map(item => [item.id, item.name])), [farms]);
  const seasonNames = useMemo(() => Object.fromEntries(seasons.map(item => [item.id, item.name])), [seasons]);
  const selectedCostsBrl = costs.reduce((total, cost) => total + cost.amountBrl, 0);

  async function chooseOrder(order: ExportOrder) {
    setSelected(order); await loadDetail(order.id);
  }

  async function transition(order: ExportOrder, status: number) {
    const target = statuses.find(item => item.value === status)?.label ?? status;
    if (!window.confirm(`Alterar ${order.orderNumber} para ${target}?`)) return;
    try {
      const updated = await apiRequest<ExportOrder>(`/api/v1/export/orders/${order.id}/status`, { method: 'POST', body: JSON.stringify({ status, occurredOn: new Date().toISOString().slice(0, 10), notes: null }) });
      setSelected(updated); await load();
    } catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível alterar o status.'); }
  }

  async function updateDocument(document: ExportDocument, status: number) {
    try {
      const issuedOn = status === 3 ? new Date().toISOString().slice(0, 10) : document.issuedOn;
      await apiRequest(`/api/v1/export/orders/${document.orderId}/documents/${document.id}`, { method: 'PUT', body: JSON.stringify({ status, referenceNumber: document.referenceNumber, issuedOn, notes: document.notes }) });
      await loadDetail(document.orderId);
    } catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível atualizar o documento.'); }
  }

  if (!allowed) return <EmptyState title="Exportação não disponível no plano" description="O módulo Export é liberado somente para organizações com esse entitlement." />;
  if (loading && !summary) return <Spinner label="Carregando exportações" />;
  if (error && !summary) return <EmptyState title="Exportação indisponível" description={error} action={<Button onClick={() => void load()}><Icon name="refresh" size={16} /> Tentar novamente</Button>} />;

  return <div className="page-stack export-page">
    <PageHeader eyebrow="Comércio exterior" title="Exportação" description="Gerencie pedidos internacionais, câmbio, logística, custos e documentação sem perder o vínculo com a safra." actions={<div className="export-actions"><Button variant="secondary" onClick={() => void load()}><Icon name="refresh" size={16} /> Atualizar</Button><Button onClick={() => { setEditing(null); setOrderModal(true); }}><Icon name="plus" size={16} /> Novo pedido</Button></div>} />
    <div className="export-disclaimer"><strong>Gestão operacional.</strong> O AgroControl não substitui Siscomex, despachante aduaneiro, emissão fiscal, contratação bancária de câmbio ou documentos oficiais.</div>
    {error && <div className="inline-warning">{error}</div>}

    <div className="export-metrics">
      <Card><span>Valor comercial estimado</span><strong>{formatBrl(summary?.estimatedCommercialValueBrl ?? 0)}</strong><small>{summary?.orderCount ?? 0} pedido(s) no filtro</small></Card>
      <Card><span>Em trânsito</span><strong>{summary?.inTransitCount ?? 0}</strong><small>{summary?.contractedCount ?? 0} contratado(s) · {summary?.deliveredCount ?? 0} entregue(s)</small></Card>
      <Card><span>Custos logísticos</span><strong>{formatBrl(summary?.totalLogisticsCostBrl ?? 0)}</strong><small>Margem operacional estimada: {formatBrl(summary?.estimatedOperationalMarginBrl ?? 0)}</small></Card>
    </div>

    <Card className="export-filters"><label>Busca<input value={search} onChange={event => setSearch(event.target.value)} placeholder="Pedido, comprador ou produto" /></label><label>Status<select value={statusFilter} onChange={event => setStatusFilter(event.target.value)}><option value="">Todos</option>{statuses.map(item => <option key={item.value} value={item.value}>{item.label}</option>)}</select></label><label>País<input value={countryFilter} onChange={event => setCountryFilter(event.target.value.toUpperCase().slice(0, 2))} placeholder="US" maxLength={2} /></label><label>Safra<select value={seasonFilter} onChange={event => setSeasonFilter(event.target.value)}><option value="">Todas</option>{seasons.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label><label>De<input type="date" value={from} onChange={event => setFrom(event.target.value)} /></label><label>Até<input type="date" value={to} onChange={event => setTo(event.target.value)} /></label><Button variant="secondary" onClick={() => void load()}>Aplicar</Button></Card>

    <section className="export-section">
      <div className="section-heading"><div><span className="eyebrow">Pipeline internacional</span><h2>Pedidos de exportação</h2></div></div>
      {orders.length === 0 ? <EmptyState title="Nenhum pedido encontrado" description="Crie a primeira operação internacional ou ajuste os filtros." action={<Button onClick={() => setOrderModal(true)}>Novo pedido</Button>} /> : <div className="table-wrap"><table><thead><tr><th>Pedido</th><th>Comprador</th><th>Destino</th><th>Produto</th><th>Valor</th><th>Status</th><th></th></tr></thead><tbody>{orders.map(order => <tr key={order.id} className={selected?.id === order.id ? 'export-row--selected' : ''}><td><strong>{order.orderNumber}</strong><small className="table-subline">{incoterms[enumNumber(order.incoterm, incoterms) - 1] ?? String(order.incoterm)} · {order.currency}</small></td><td>{order.buyerName}</td><td>{order.destinationCountryCode}<small className="table-subline">{order.destinationLocation ?? 'Destino não informado'}</small></td><td>{order.productDescription}<small className="table-subline">{order.quantity.toLocaleString('pt-BR')} {order.unit}{order.seasonId ? ` · ${seasonNames[order.seasonId] ?? 'Safra'}` : ''}</small></td><td>{order.commercialValue.toLocaleString('pt-BR', { style: 'currency', currency: order.currency })}<small className="table-subline">≈ {formatBrl(order.estimatedValueBrl)}</small></td><td><Badge tone={statusTone(order.status)}>{statusLabel(order.status)}</Badge></td><td><div className="row-actions"><Button variant="secondary" onClick={() => void chooseOrder(order)}>Abrir</Button>{![5, 6].includes(enumNumber(order.status, statuses.map(item => item.key))) && <Button variant="ghost" onClick={() => { setEditing(order); setOrderModal(true); }}>Editar</Button>}</div></td></tr>)}</tbody></table></div>}
    </section>

    {selected && <section className="export-detail">
      <div className="section-heading"><div><span className="eyebrow">Operação selecionada</span><h2>{selected.orderNumber} · {selected.buyerName}</h2></div><div className="export-actions">{nextStatuses(selected.status).map(item => <Button key={item.value} variant={item.value === 6 ? 'danger' : 'secondary'} onClick={() => void transition(selected, item.value)}>{item.label}</Button>)}<Button onClick={() => setCostModal(true)} disabled={enumNumber(selected.status, statuses.map(item => item.key)) === 6}><Icon name="plus" size={16} /> Custo</Button></div></div>
      {detailLoading ? <Spinner label="Carregando operação" /> : <>
        <div className="export-detail-grid">
          <Card><span>Comercial</span><strong>{selected.commercialValue.toLocaleString('pt-BR', { style: 'currency', currency: selected.currency })}</strong><small>Câmbio snapshot {selected.exchangeRateToBrl.toLocaleString('pt-BR', { maximumFractionDigits: 6 })} → {formatBrl(selected.estimatedValueBrl)}</small></Card>
          <Card><span>Logística</span><strong>{selected.originLocation ?? 'Origem pendente'} → {selected.destinationLocation ?? selected.destinationCountryCode}</strong><small>{selected.containerReference ? `Container ${selected.containerReference}` : selected.bookingReference ? `Booking ${selected.bookingReference}` : 'Sem referência de embarque'}</small></Card>
          <Card><span>Custos registrados</span><strong>{formatBrl(selectedCostsBrl)}</strong><small>Margem operacional do pedido: {formatBrl(selected.estimatedValueBrl - selectedCostsBrl)}</small></Card>
        </div>

        <div className="export-detail-columns">
          <Card className="export-panel"><div className="export-panel__header"><div><span className="eyebrow">Checklist</span><h3>Documentos</h3></div></div>{documents.map(document => <div className="document-row" key={document.id}><div><strong>{document.customLabel ?? documentTypes[enumNumber(document.type, ['CommercialInvoice', 'PackingList', 'CertificateOfOrigin', 'PhytosanitaryCertificate', 'BillOfLading', 'Custom']) - 1] ?? String(document.type)}</strong><small>{document.referenceNumber ?? 'Sem referência externa'}</small></div><select value={enumNumber(document.status, documentStatuses)} onChange={event => void updateDocument(document, Number(event.target.value))}>{documentStatuses.map((label, index) => <option key={label} value={index + 1}>{label}</option>)}</select></div>)}</Card>

          <Card className="export-panel"><div className="export-panel__header"><div><span className="eyebrow">Timeline</span><h3>Status operacional</h3></div></div>{timeline.map(item => <div className="timeline-row" key={item.id}><span className="timeline-dot" /><div><strong>{statusLabel(item.toStatus)}</strong><small>{item.occurredOn}{item.notes ? ` · ${item.notes}` : ''}</small></div></div>)}</Card>
        </div>

        <Card className="export-panel"><div className="export-panel__header"><div><span className="eyebrow">Custos</span><h3>Logística e despesas da operação</h3></div><Button variant="secondary" onClick={() => setCostModal(true)}><Icon name="plus" size={16} /> Adicionar</Button></div>{costs.length === 0 ? <p className="muted-copy">Nenhum custo registrado.</p> : <div className="table-wrap"><table><thead><tr><th>Data</th><th>Tipo</th><th>Descrição</th><th>Valor</th><th>Snapshot BRL</th></tr></thead><tbody>{costs.map(cost => <tr key={cost.id}><td>{cost.incurredOn}</td><td>{costTypes[enumNumber(cost.type, costTypes) - 1] ?? String(cost.type)}</td><td>{cost.description}</td><td>{cost.amount.toLocaleString('pt-BR', { style: 'currency', currency: cost.currency })}</td><td>{formatBrl(cost.amountBrl)}</td></tr>)}</tbody></table></div>}</Card>
      </>}
    </section>}

    <OrderModal open={orderModal} order={editing} farms={farms} fields={fields} crops={crops} seasons={seasons} onClose={() => { setOrderModal(false); setEditing(null); }} onSaved={load} />
    {selected && <CostModal open={costModal} order={selected} onClose={() => setCostModal(false)} onSaved={async () => { await loadDetail(selected.id); await load(); }} />}
  </div>;
}

function OrderModal({ open, order, farms, fields, crops, seasons, onClose, onSaved }: { open: boolean; order: ExportOrder | null; farms: Farm[]; fields: Field[]; crops: Crop[]; seasons: Season[]; onClose(): void; onSaved(): Promise<void> }) {
  const [saving, setSaving] = useState(false); const [error, setError] = useState<string | null>(null);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setSaving(true); setError(null);
    const form = new FormData(event.currentTarget);
    const payload = {
      orderNumber: form.get('orderNumber'), buyerName: form.get('buyerName'), buyerReference: String(form.get('buyerReference') ?? '').trim() || null,
      destinationCountryCode: String(form.get('country') ?? '').toUpperCase(), farmId: String(form.get('farmId') ?? '').trim() || null,
      fieldId: String(form.get('fieldId') ?? '').trim() || null, cropId: String(form.get('cropId') ?? '').trim() || null, seasonId: String(form.get('seasonId') ?? '').trim() || null,
      productDescription: form.get('product'), quantity: Number(form.get('quantity')), unit: form.get('unit'), currency: String(form.get('currency') ?? '').toUpperCase(), unitPrice: Number(form.get('unitPrice')),
      exchangeRateToBrl: Number(form.get('exchangeRate')), incoterm: Number(form.get('incoterm')), originLocation: String(form.get('origin') ?? '').trim() || null,
      destinationLocation: String(form.get('destination') ?? '').trim() || null, estimatedShipmentDate: String(form.get('estimatedShipmentDate') ?? '').trim() || null,
      estimatedDeliveryDate: String(form.get('estimatedDeliveryDate') ?? '').trim() || null, shipmentReference: String(form.get('shipmentReference') ?? '').trim() || null,
      bookingReference: String(form.get('bookingReference') ?? '').trim() || null, containerReference: String(form.get('containerReference') ?? '').trim() || null,
      notes: String(form.get('notes') ?? '').trim() || null
    };
    try {
      if (order) await apiRequest(`/api/v1/export/orders/${order.id}`, { method: 'PUT', body: JSON.stringify(payload) });
      else await apiRequest('/api/v1/export/orders', { method: 'POST', body: JSON.stringify(payload) });
      onClose(); await onSaved();
    } catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível salvar o pedido.'); }
    finally { setSaving(false); }
  }
  return <Modal open={open} title={order ? `Editar ${order.orderNumber}` : 'Novo pedido de exportação'} description="Valores e câmbio ficam registrados como snapshot gerencial da operação." onClose={onClose}><form key={order?.id ?? 'new'} className="form-grid" onSubmit={submit}>{error && <div className="inline-warning form-grid__full">{error}</div>}<label>Número do pedido<input name="orderNumber" required defaultValue={order?.orderNumber ?? ''} placeholder="EXP-2026-001" /></label><label>Comprador<input name="buyerName" required defaultValue={order?.buyerName ?? ''} /></label><label>Referência do comprador<input name="buyerReference" defaultValue={order?.buyerReference ?? ''} placeholder="PO-123" /></label><label>País destino<input name="country" required maxLength={2} defaultValue={order?.destinationCountryCode ?? ''} placeholder="US" /></label><label className="form-grid__full">Produto<input name="product" required defaultValue={order?.productDescription ?? ''} placeholder="Soja em grãos" /></label><label>Quantidade<input name="quantity" type="number" min="0.000001" step="0.000001" required defaultValue={order?.quantity ?? ''} /></label><label>Unidade<input name="unit" required defaultValue={order?.unit ?? 't'} /></label><label>Moeda<input name="currency" required maxLength={3} defaultValue={order?.currency ?? 'USD'} /></label><label>Preço unitário<input name="unitPrice" type="number" min="0.0001" step="0.0001" required defaultValue={order?.unitPrice ?? ''} /></label><label>Câmbio para BRL<input name="exchangeRate" type="number" min="0.000001" step="0.000001" required defaultValue={order?.exchangeRateToBrl ?? ''} /></label><label>Incoterm<select name="incoterm" defaultValue={String(order ? enumNumber(order.incoterm, incoterms) : 9)}>{incoterms.map((item, index) => <option key={item} value={index + 1}>{item}</option>)}</select></label><label>Propriedade<select name="farmId" defaultValue={order?.farmId ?? ''}><option value="">Nenhuma</option>{farms.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label><label>Talhão<select name="fieldId" defaultValue={order?.fieldId ?? ''}><option value="">Nenhum</option>{fields.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label><label>Cultura<select name="cropId" defaultValue={order?.cropId ?? ''}><option value="">Nenhuma</option>{crops.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label><label>Safra<select name="seasonId" defaultValue={order?.seasonId ?? ''}><option value="">Nenhuma</option>{seasons.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label><label>Origem<input name="origin" defaultValue={order?.originLocation ?? ''} placeholder="Santos/SP" /></label><label>Destino logístico<input name="destination" defaultValue={order?.destinationLocation ?? ''} placeholder="Rotterdam/NL" /></label><label>Embarque estimado<input name="estimatedShipmentDate" type="date" defaultValue={order?.estimatedShipmentDate ?? ''} /></label><label>Entrega estimada<input name="estimatedDeliveryDate" type="date" defaultValue={order?.estimatedDeliveryDate ?? ''} /></label><label>Shipment ref.<input name="shipmentReference" defaultValue={order?.shipmentReference ?? ''} /></label><label>Booking<input name="bookingReference" defaultValue={order?.bookingReference ?? ''} /></label><label>Container<input name="containerReference" defaultValue={order?.containerReference ?? ''} /></label><label className="form-grid__full">Observações<textarea name="notes" rows={3} defaultValue={order?.notes ?? ''} /></label><div className="form-actions form-grid__full"><Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button><Button type="submit" disabled={saving}>{saving ? 'Salvando...' : 'Salvar pedido'}</Button></div></form></Modal>;
}

function CostModal({ open, order, onClose, onSaved }: { open: boolean; order: ExportOrder; onClose(): void; onSaved(): Promise<void> }) {
  const [saving, setSaving] = useState(false); const [error, setError] = useState<string | null>(null);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setSaving(true); setError(null);
    const form = new FormData(event.currentTarget);
    const payload = { type: Number(form.get('type')), description: form.get('description'), amount: Number(form.get('amount')), currency: String(form.get('currency') ?? '').toUpperCase(), exchangeRateToBrl: Number(form.get('exchangeRate')), incurredOn: form.get('incurredOn'), notes: String(form.get('notes') ?? '').trim() || null };
    try { await apiRequest(`/api/v1/export/orders/${order.id}/costs`, { method: 'POST', body: JSON.stringify(payload) }); onClose(); await onSaved(); }
    catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível registrar o custo.'); } finally { setSaving(false); }
  }
  return <Modal open={open} title={`Adicionar custo · ${order.orderNumber}`} description="A taxa de câmbio informada fica preservada no custo para manter o histórico." onClose={onClose}><form className="form-grid" onSubmit={submit}>{error && <div className="inline-warning form-grid__full">{error}</div>}<label>Tipo<select name="type" defaultValue="1">{costTypes.map((item, index) => <option key={item} value={index + 1}>{item}</option>)}</select></label><label>Descrição<input name="description" required placeholder="Frete marítimo" /></label><label>Valor<input name="amount" type="number" min="0.01" step="0.01" required /></label><label>Moeda<input name="currency" maxLength={3} required defaultValue={order.currency} /></label><label>Câmbio para BRL<input name="exchangeRate" type="number" min="0.000001" step="0.000001" required defaultValue={order.exchangeRateToBrl} /></label><label>Data<input name="incurredOn" type="date" required defaultValue={new Date().toISOString().slice(0, 10)} /></label><label className="form-grid__full">Observação<textarea name="notes" rows={3} /></label><div className="form-actions form-grid__full"><Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button><Button type="submit" disabled={saving}>{saving ? 'Registrando...' : 'Registrar custo'}</Button></div></form></Modal>;
}
