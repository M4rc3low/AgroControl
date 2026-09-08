import { useEffect, useState } from 'react';
import { Icon } from '../components/Icon';
import { Button, EmptyState, PageHeader, Spinner } from '../components/Ui';
import { getAllPaged } from '../lib/api';
import type { Crop, Farm, Field, Season } from '../lib/types';
import { CropsTab } from '../features/production/CropsTab';
import { FarmsTab } from '../features/production/FarmsTab';
import { FieldsTab } from '../features/production/FieldsTab';
import { SeasonsTab } from '../features/production/SeasonsTab';

type Tab = 'farms' | 'fields' | 'crops' | 'seasons';

interface ProductionData {
  farms: Farm[];
  fields: Field[];
  crops: Crop[];
  seasons: Season[];
}

const tabs: { key: Tab; label: string }[] = [
  { key: 'farms', label: 'Propriedades' },
  { key: 'fields', label: 'Talhões' },
  { key: 'crops', label: 'Culturas' },
  { key: 'seasons', label: 'Safras' }
];

export function ProductionPage() {
  const [activeTab, setActiveTab] = useState<Tab>('farms');
  const [data, setData] = useState<ProductionData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const [farms, fields, crops, seasons] = await Promise.all([
        getAllPaged<Farm>('/api/v1/farms'),
        getAllPaged<Field>('/api/v1/fields'),
        getAllPaged<Crop>('/api/v1/crops'),
        getAllPaged<Season>('/api/v1/seasons')
      ]);
      setData({ farms, fields, crops, seasons });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível carregar a produção rural.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { void load(); }, []);

  if (loading && !data) return <Spinner label="Carregando produção rural" />;
  if (error && !data) return <EmptyState title="Produção rural indisponível" description={error} action={<Button onClick={() => void load()}><Icon name="refresh" size={16} /> Tentar novamente</Button>} />;
  if (!data) return null;

  return (
    <div className="page-stack">
      <PageHeader eyebrow="Operação" title="Produção rural" description="Organize a cadeia produtiva da propriedade até a safra, mantendo cada registro dentro do contexto da organização." actions={<Button variant="secondary" onClick={() => void load()} disabled={loading}><Icon name="refresh" size={16} /> Atualizar</Button>} />
      {error && <div className="inline-warning">{error}</div>}
      <div className="tabs" role="tablist" aria-label="Cadastros de produção">
        {tabs.map(tab => <button key={tab.key} role="tab" aria-selected={activeTab === tab.key} className={activeTab === tab.key ? 'tabs__button tabs__button--active' : 'tabs__button'} onClick={() => setActiveTab(tab.key)}>{tab.label}<span>{data[tab.key].length}</span></button>)}
      </div>
      {activeTab === 'farms' && <FarmsTab farms={data.farms} reload={load} />}
      {activeTab === 'fields' && <FieldsTab fields={data.fields} farms={data.farms} reload={load} />}
      {activeTab === 'crops' && <CropsTab crops={data.crops} reload={load} />}
      {activeTab === 'seasons' && <SeasonsTab seasons={data.seasons} fields={data.fields} farms={data.farms} crops={data.crops} reload={load} />}
    </div>
  );
}
