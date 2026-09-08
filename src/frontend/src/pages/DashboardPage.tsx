import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { Icon } from '../components/Icon';
import { Badge, Button, Card, EmptyState, MetricCard, PageHeader, Spinner } from '../components/Ui';
import { MultiFarmMap } from '../features/multifarm/MultiFarmMap';
import { apiRequest, getAllPaged } from '../lib/api';
import { useAuth } from '../lib/auth';
import { useFarmScope } from '../lib/farmScope';
import { formatCurrency, formatDateTimeInTimeZone, formatNumber } from '../lib/format';
import type { Field, FinancialSummary, LowStockItem, Season } from '../lib/types';

interface DashboardData {
  fields: Field[];
  seasons: Season[];
  lowStock: LowStockItem[] | null;
  finance: FinancialSummary | null;
}

function combineFinance(items: FinancialSummary[]): FinancialSummary | null {
  if (!items.length) return null;
  const total = items.reduce((sum, item) => ({
    accruedRevenue: sum.accruedRevenue + item.accruedRevenue,
    accruedExpense: sum.accruedExpense + item.accruedExpense,
    accruedResult: sum.accruedResult + item.accruedResult,
    cashRevenue: sum.cashRevenue + item.cashRevenue,
    cashExpense: sum.cashExpense + item.cashExpense,
    cashResult: sum.cashResult + item.cashResult,
    pendingReceivables: sum.pendingReceivables + item.pendingReceivables,
    pendingPayables: sum.pendingPayables + item.pendingPayables
  }), { accruedRevenue: 0, accruedExpense: 0, accruedResult: 0, cashRevenue: 0, cashExpense: 0, cashResult: 0, pendingReceivables: 0, pendingPayables: 0 });
  return {
    ...total,
    accruedMarginPercent: total.accruedRevenue === 0 ? null : (total.accruedResult / total.accruedRevenue) * 100
  };
}

export function DashboardPage() {
  const { platform } = useAuth();
  const { selected, scopedFarms, scopeLabel, setSelected, loading: scopeLoading, error: scopeError } = useFarmScope();
  const [data, setData] = useState<DashboardData | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const scopeKey = useMemo(() => JSON.stringify(selected), [selected]);

  async function load() {
    if (!platform) return;
    setLoading(true);
    setError(null);
    try {
      const [fields, seasons] = await Promise.all([
        getAllPaged<Field>('/api/v1/fields'),
        getAllPaged<Season>('/api/v1/seasons')
      ]);

      let lowStock: LowStockItem[] | null = null;
      let finance: FinancialSummary | null = null;

      if (platform.entitlements.modules.Inventory) {
        try { lowStock = await apiRequest<LowStockItem[]>('/api/v1/inventory/low-stock?take=8'); }
        catch { lowStock = []; }
      }

      if (platform.entitlements.modules.Finance) {
        try {
          if (selected.kind === 'all') {
            finance = await apiRequest<FinancialSummary>('/api/v1/finance/summary');
          } else {
            const summaries = await Promise.all(scopedFarms.map(farm =>
              apiRequest<FinancialSummary>(`/api/v1/finance/summary?farmId=${encodeURIComponent(farm.id)}`)));
            finance = combineFinance(summaries);
          }
        } catch { finance = null; }
      }

      setData({ fields, seasons, lowStock, finance });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível carregar o dashboard.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { void load(); }, [platform?.organization.id, scopeKey]);

  const scopedFarmIds = useMemo(() => new Set(scopedFarms.map(farm => farm.id)), [scopedFarms]);
  const visibleFields = useMemo(() => data?.fields.filter(field => scopedFarmIds.has(field.farmId)) ?? [], [data?.fields, scopedFarmIds]);
  const visibleFieldIds = useMemo(() => new Set(visibleFields.map(field => field.id)), [visibleFields]);
  const visibleSeasons = useMemo(() => data?.seasons.filter(season => visibleFieldIds.has(season.fieldId)) ?? [], [data?.seasons, visibleFieldIds]);

  const comparisons = useMemo(() => scopedFarms.map(farm => {
    const farmFields = visibleFields.filter(field => field.farmId === farm.id);
    const fieldIds = new Set(farmFields.map(field => field.id));
    const farmSeasons = visibleSeasons.filter(season => fieldIds.has(season.fieldId));
    return {
      farm,
      fieldCount: farmFields.length,
      activeSeasonCount: farmSeasons.filter(season => season.status === 'Active').length,
      harvestedSeasonCount: farmSeasons.filter(season => season.status === 'Harvested').length
    };
  }).sort((a, b) => b.farm.totalAreaHectares - a.farm.totalAreaHectares), [scopedFarms, visibleFields, visibleSeasons]);

  const selectFarmFromMap = useCallback((farmId: string) => setSelected({ kind: 'farm', farmId }), [setSelected]);

  if (!platform || scopeLoading || (loading && !data)) return <Spinner label="Montando a visão da operação" />;
  if ((error || scopeError) && !data) return <EmptyState title="Não foi possível montar o dashboard" description={error ?? scopeError ?? 'Falha ao carregar o contexto operacional.'} action={<Button onClick={() => void load()}><Icon name="refresh" size={16} /> Tentar novamente</Button>} />;

  const totalArea = scopedFarms.reduce((sum, farm) => sum + farm.totalAreaHectares, 0);
  const activeSeasons = visibleSeasons.filter(season => season.status === 'Active').length;
  const harvestedSeasons = visibleSeasons.filter(season => season.status === 'Harvested').length;
  const lowStockCount = data?.lowStock?.length ?? 0;
  const selectedFarm = selected.kind === 'farm' ? scopedFarms.find(farm => farm.id === selected.farmId) ?? null : null;
  const localTime = selectedFarm ? formatDateTimeInTimeZone(new Date(), selectedFarm.timeZoneId) : null;

  return (
    <div className="page-stack">
      <PageHeader
        eyebrow="Visão operacional multi-fazenda"
        title={scopeLabel}
        description="Compare propriedades, regiões e estados sem perder a fronteira de acesso definida para cada usuário."
        actions={<Button variant="secondary" onClick={() => void load()} disabled={loading}><Icon name="refresh" size={16} /> Atualizar</Button>}
      />

      {(scopeError || error) && <div className="inline-warning">{scopeError ?? error}</div>}

      <div className="scope-summary">
        <span className="scope-summary__chip"><strong>{scopedFarms.length}</strong> propriedade(s) no contexto</span>
        <span className="scope-summary__chip"><strong>{formatNumber(totalArea)}</strong> ha</span>
        {selectedFarm && <span className="scope-summary__chip"><strong>{selectedFarm.stateCode ?? selectedFarm.state ?? '—'}</strong> {selectedFarm.city ?? 'município não informado'}</span>}
        {selectedFarm && <span className="scope-summary__chip"><strong>{selectedFarm.timeZoneId}</strong> {localTime}</span>}
      </div>

      <div className="metrics-grid">
        <MetricCard icon={<Icon name="leaf" />} label="Propriedades" value={formatNumber(scopedFarms.length, 0)} hint={`${formatNumber(totalArea)} ha no contexto`} />
        <MetricCard icon={<Icon name="grid" />} label="Talhões" value={formatNumber(visibleFields.length, 0)} hint="unidades produtivas visíveis" />
        <MetricCard icon={<Icon name="chart" />} label="Safras ativas" value={formatNumber(activeSeasons, 0)} hint={`${harvestedSeasons} concluídas no contexto`} />
        <MetricCard icon={<Icon name="wallet" />} label="Resultado por competência" value={data?.finance ? formatCurrency(data.finance.accruedResult) : '—'} hint={data?.finance ? `${formatNumber(data.finance.accruedMarginPercent)}% de margem` : 'módulo sem leitura disponível'} />
      </div>

      <div className="dashboard-grid">
        <Card className="dashboard-card dashboard-card--wide">
          <div className="card-heading"><div><span className="eyebrow">Mapa operacional</span><h2>Fazendas no contexto atual</h2></div><Badge tone="info">{scopeLabel}</Badge></div>
          <MultiFarmMap farms={scopedFarms} onSelectFarm={selectFarmFromMap} />
        </Card>

        <Card className="dashboard-card">
          <div className="card-heading"><div><span className="eyebrow">Estoque</span><h2>Pontos de atenção</h2></div>{platform.entitlements.modules.Inventory && <Badge tone={lowStockCount ? 'warning' : 'success'}>{lowStockCount} alertas</Badge>}</div>
          {!platform.entitlements.modules.Inventory ? <ModuleMiniLock /> : data?.lowStock?.length ? <><p className="muted">Alertas considerando todas as fazendas permitidas ao seu usuário.</p><div className="alert-list">{data.lowStock.slice(0, 5).map(item => <div key={item.itemId} className="alert-row"><div><strong>{item.name}</strong><span>{item.sku} · {item.unit}</span></div><div><span>Falta</span><strong>{formatNumber(item.shortage)}</strong></div></div>)}</div></> : <div className="good-state"><span>✓</span><strong>Sem alertas críticos</strong><p>Nenhum item abaixo do estoque mínimo no escopo permitido.</p></div>}
        </Card>

        <Card className="dashboard-card dashboard-card--wide">
          <div className="card-heading"><div><span className="eyebrow">Comparativo</span><h2>Desempenho por propriedade</h2></div><Link className="text-link" to="/production">Gerenciar <Icon name="arrow" size={15} /></Link></div>
          {comparisons.length ? <div className="multi-farm-list">{comparisons.slice(0, 20).map(item => <div className="multi-farm-row" key={item.farm.id}><div><strong>{item.farm.name}</strong><span>{[item.farm.city, item.farm.stateCode ?? item.farm.state].filter(Boolean).join(' / ') || 'Localização não informada'} · {item.farm.timeZoneId}</span></div><div className="multi-farm-row__metric"><span>Área</span><strong>{formatNumber(item.farm.totalAreaHectares)} ha</strong></div><div className="multi-farm-row__metric"><span>Talhões / safras ativas</span><strong>{item.fieldCount} / {item.activeSeasonCount}</strong></div><div className="multi-farm-row__metric"><span>Safras concluídas</span><strong>{item.harvestedSeasonCount}</strong></div><Button variant="secondary" onClick={() => setSelected({ kind: 'farm', farmId: item.farm.id })}>Abrir</Button></div>)}</div> : <EmptyState title="Nenhuma propriedade neste contexto" description="Ajuste o seletor operacional ou solicite acesso a uma propriedade." />}
        </Card>

        <Card className="dashboard-card">
          <div className="card-heading"><div><span className="eyebrow">Financeiro</span><h2>Compromissos</h2></div></div>
          {!platform.entitlements.modules.Finance ? <ModuleMiniLock /> : data?.finance ? <div className="finance-summary"><div><span>Receitas</span><strong>{formatCurrency(data.finance.accruedRevenue)}</strong></div><div><span>Despesas</span><strong>{formatCurrency(data.finance.accruedExpense)}</strong></div><div><span>A receber</span><strong>{formatCurrency(data.finance.pendingReceivables)}</strong></div><div><span>A pagar</span><strong>{formatCurrency(data.finance.pendingPayables)}</strong></div></div> : <p className="muted">Resumo financeiro indisponível nesta atualização.</p>}
        </Card>

        <Card className="dashboard-card dashboard-card--wide quick-card"><div><span className="eyebrow">Atalhos</span><h2>Continue trabalhando</h2><p>O contexto selecionado permanece ativo enquanto você navega pela operação.</p></div><div className="quick-actions"><Link to="/production"><Icon name="leaf" /><span>Produção</span></Link><Link to="/precision"><Icon name="map" /><span>Precisão</span></Link><Link to="/finance"><Icon name="wallet" /><span>Financeiro</span></Link><Link to="/telemetry"><Icon name="radio" /><span>Telemetry</span></Link></div></Card>
      </div>
    </div>
  );
}

function ModuleMiniLock() {
  return <div className="mini-lock"><Icon name="lock" /><strong>Módulo não habilitado</strong><p>Consulte os módulos disponíveis para o plano desta organização.</p><Link className="text-link" to="/modules">Ver plano <Icon name="arrow" size={14} /></Link></div>;
}
