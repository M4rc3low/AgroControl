import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Icon } from '../components/Icon';
import { Badge, Button, Card, EmptyState, MetricCard, PageHeader, Spinner } from '../components/Ui';
import { apiRequest, getAllPaged } from '../lib/api';
import { useAuth } from '../lib/auth';
import { formatCurrency, formatNumber } from '../lib/format';
import type { Farm, Field, FinancialSummary, LowStockItem, Season } from '../lib/types';

interface DashboardData {
  farms: Farm[];
  fields: Field[];
  seasons: Season[];
  lowStock: LowStockItem[] | null;
  finance: FinancialSummary | null;
}

export function DashboardPage() {
  const { platform } = useAuth();
  const [data, setData] = useState<DashboardData | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  async function load() {
    if (!platform) return;
    setLoading(true);
    setError(null);
    try {
      const [farms, fields, seasons] = await Promise.all([
        getAllPaged<Farm>('/api/v1/farms'),
        getAllPaged<Field>('/api/v1/fields'),
        getAllPaged<Season>('/api/v1/seasons')
      ]);
      let lowStock: LowStockItem[] | null = null;
      let finance: FinancialSummary | null = null;
      if (platform.entitlements.modules.Inventory) {
        try { lowStock = await apiRequest<LowStockItem[]>('/api/v1/inventory/low-stock?take=8'); } catch { lowStock = []; }
      }
      if (platform.entitlements.modules.Finance) {
        try { finance = await apiRequest<FinancialSummary>('/api/v1/finance/summary'); } catch { finance = null; }
      }
      setData({ farms, fields, seasons, lowStock, finance });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível carregar o dashboard.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { void load(); }, [platform?.organization.id]);

  if (!platform || (loading && !data)) return <Spinner label="Montando a visão da operação" />;
  if (error && !data) return <EmptyState title="Não foi possível montar o dashboard" description={error} action={<Button onClick={() => void load()}><Icon name="refresh" size={16} /> Tentar novamente</Button>} />;

  const totalArea = data?.farms.reduce((sum, farm) => sum + farm.totalAreaHectares, 0) ?? 0;
  const activeSeasons = data?.seasons.filter(season => season.status === 'Active').length ?? 0;
  const harvestedSeasons = data?.seasons.filter(season => season.status === 'Harvested').length ?? 0;
  const lowStockCount = data?.lowStock?.length ?? 0;

  return (
    <div className="page-stack">
      <PageHeader eyebrow="Visão operacional" title={`Olá, ${platform.organization.name}`} description="Acompanhe os sinais mais importantes da operação e entre direto no que precisa de atenção." actions={<Button variant="secondary" onClick={() => void load()} disabled={loading}><Icon name="refresh" size={16} /> Atualizar</Button>} />

      <div className="metrics-grid">
        <MetricCard icon={<Icon name="leaf" />} label="Propriedades" value={formatNumber(data?.farms.length, 0)} hint={`${formatNumber(totalArea)} ha cadastrados`} />
        <MetricCard icon={<Icon name="grid" />} label="Talhões" value={formatNumber(data?.fields.length, 0)} hint="unidades produtivas" />
        <MetricCard icon={<Icon name="chart" />} label="Safras ativas" value={formatNumber(activeSeasons, 0)} hint={`${harvestedSeasons} concluídas`} />
        <MetricCard icon={<Icon name="wallet" />} label="Resultado por competência" value={data?.finance ? formatCurrency(data.finance.accruedResult) : '—'} hint={data?.finance ? `${formatNumber(data.finance.accruedMarginPercent)}% de margem` : 'módulo sem leitura disponível'} />
      </div>

      <div className="dashboard-grid">
        <Card className="dashboard-card dashboard-card--wide">
          <div className="card-heading"><div><span className="eyebrow">Produção rural</span><h2>Safras e andamento</h2></div><Link className="text-link" to="/production">Gerenciar <Icon name="arrow" size={15} /></Link></div>
          {data?.seasons.length ? <div className="season-list">{data.seasons.slice(0, 6).map(season => <div className="season-row" key={season.id}><div><strong>{season.name}</strong><span>{data.fields.find(field => field.id === season.fieldId)?.name ?? 'Talhão'}</span></div><Badge tone={season.status === 'Active' ? 'success' : season.status === 'Harvested' ? 'info' : season.status === 'Cancelled' ? 'danger' : 'neutral'}>{season.status}</Badge><div className="season-yield"><span>Prevista</span><strong>{season.expectedYieldPerHectare == null ? '—' : `${formatNumber(season.expectedYieldPerHectare)} / ha`}</strong></div></div>)}</div> : <EmptyState title="Nenhuma safra cadastrada" description="Cadastre propriedade, talhão e cultura para iniciar o acompanhamento produtivo." action={<Link className="button button--primary" to="/production">Abrir produção</Link>} />}
        </Card>

        <Card className="dashboard-card">
          <div className="card-heading"><div><span className="eyebrow">Estoque</span><h2>Pontos de atenção</h2></div>{platform.entitlements.modules.Inventory && <Badge tone={lowStockCount ? 'warning' : 'success'}>{lowStockCount} alertas</Badge>}</div>
          {!platform.entitlements.modules.Inventory ? <ModuleMiniLock /> : data?.lowStock?.length ? <div className="alert-list">{data.lowStock.slice(0, 5).map(item => <div key={item.itemId} className="alert-row"><div><strong>{item.name}</strong><span>{item.sku} · {item.unit}</span></div><div><span>Falta</span><strong>{formatNumber(item.shortage)}</strong></div></div>)}</div> : <div className="good-state"><span>✓</span><strong>Sem alertas críticos</strong><p>Nenhum item abaixo do estoque mínimo neste momento.</p></div>}
        </Card>

        <Card className="dashboard-card">
          <div className="card-heading"><div><span className="eyebrow">Financeiro</span><h2>Compromissos</h2></div></div>
          {!platform.entitlements.modules.Finance ? <ModuleMiniLock /> : data?.finance ? <div className="finance-summary"><div><span>Receitas</span><strong>{formatCurrency(data.finance.accruedRevenue)}</strong></div><div><span>Despesas</span><strong>{formatCurrency(data.finance.accruedExpense)}</strong></div><div><span>A receber</span><strong>{formatCurrency(data.finance.pendingReceivables)}</strong></div><div><span>A pagar</span><strong>{formatCurrency(data.finance.pendingPayables)}</strong></div></div> : <p className="muted">Resumo financeiro indisponível nesta atualização.</p>}
        </Card>

        <Card className="dashboard-card dashboard-card--wide quick-card"><div><span className="eyebrow">Atalhos</span><h2>Continue trabalhando</h2><p>Entre diretamente nos módulos que mais movimentam a operação.</p></div><div className="quick-actions"><Link to="/production"><Icon name="leaf" /><span>Produção</span></Link><Link to="/inventory"><Icon name="box" /><span>Estoque</span></Link><Link to="/finance"><Icon name="wallet" /><span>Financeiro</span></Link><Link to="/telemetry"><Icon name="radio" /><span>Telemetry</span></Link></div></Card>
      </div>
    </div>
  );
}

function ModuleMiniLock() {
  return <div className="mini-lock"><Icon name="lock" /><strong>Módulo não habilitado</strong><p>Consulte os módulos disponíveis para o plano desta organização.</p><Link className="text-link" to="/modules">Ver plano <Icon name="arrow" size={14} /></Link></div>;
}
