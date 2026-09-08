import { Link } from 'react-router-dom';
import { Icon } from '../components/Icon';
import { Badge, Card, PageHeader } from '../components/Ui';
import { useAuth } from '../lib/auth';

const highlights: Record<string, string[]> = {
  Inventory: ['Itens, categorias e SKU', 'Depósitos e movimentações', 'Saldo e estoque mínimo'],
  Finance: ['Receitas e despesas', 'Contas a pagar e receber', 'Rentabilidade por safra'],
  Machinery: ['Horímetro e combustível', 'Manutenção preventiva', 'Custo operacional'],
  Market: ['Commodities e cotações', 'Variação de preço', 'Alertas por alvo'],
  Intelligence: ['Previsão de produtividade', 'Baseline e regressão', 'Apoio à decisão'],
  Telemetry: ['Dispositivos e sensores', 'MQTT e eventos', 'Última leitura e histórico']
};

export function ModulePage({ moduleKey, title, description }: { moduleKey: string; title: string; description: string }) {
  const { platform } = useAuth();
  const allowed = Boolean(platform?.entitlements.modules[moduleKey]);
  const definition = platform?.modules.find(module => module.key === moduleKey);

  return <div className="page-stack"><PageHeader eyebrow="Módulo" title={title} description={description} actions={<Badge tone={allowed ? 'success' : 'warning'}>{allowed ? 'Habilitado' : 'Bloqueado pelo plano'}</Badge>} /><Card className="module-hero"><div className={`module-hero__symbol ${allowed ? '' : 'module-hero__symbol--locked'}`}><Icon name={allowed ? 'grid' : 'lock'} size={32} /></div><div><h2>{allowed ? `${title} está conectado à plataforma` : `${title} não está disponível neste plano`}</h2><p>{allowed ? 'A API deste módulo já faz parte do AgroControl. Nesta Sprint a navegação web ganha a base visual; os fluxos operacionais detalhados deste módulo entram progressivamente sem duplicar regras do backend.' : 'O frontend apenas apresenta o estado. A autorização real continua sendo aplicada pela API e retorna 403 quando o entitlement não existe.'}</p>{definition && <p className="module-definition">{definition.description}</p>}</div></Card><div className="module-feature-grid">{(highlights[moduleKey] ?? []).map((item, index) => <Card key={item} className="module-feature"><span>0{index + 1}</span><strong>{item}</strong><p>{allowed ? 'Contrato já previsto na plataforma e pronto para receber experiência operacional dedicada.' : 'Disponível quando o plano liberar este módulo.'}</p></Card>)}</div>{!allowed && <Card className="upgrade-card"><div><span className="eyebrow">Entitlements</span><h2>Veja o que sua organização possui</h2><p>O catálogo mostra status técnico e acesso efetivo por módulo.</p></div><Link className="button button--primary" to="/modules">Abrir catálogo</Link></Card>}</div>;
}
