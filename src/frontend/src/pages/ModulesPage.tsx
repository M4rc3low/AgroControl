import { Icon } from '../components/Icon';
import { Badge, Card, PageHeader, Spinner } from '../components/Ui';
import { useAuth } from '../lib/auth';

export function ModulesPage() {
  const { platform, contextLoading } = useAuth();
  if (!platform || contextLoading) return <Spinner label="Carregando catálogo de módulos" />;

  return <div className="page-stack"><PageHeader eyebrow="Plataforma" title="Módulos e plano" description="Visibilidade do catálogo técnico e dos entitlements efetivos desta organização." actions={<div className="plan-pill">Plano <strong>{platform.entitlements.plan}</strong></div>} /><div className="module-catalog">{platform.modules.map(module => { const allowed = Boolean(platform.entitlements.modules[module.key]); return <Card key={module.key} className={`module-card ${allowed ? '' : 'module-card--locked'}`}><div className="module-card__top"><div className="module-card__icon"><Icon name={allowed ? 'grid' : 'lock'} /></div><Badge tone={allowed ? 'success' : module.status === 'ComingSoon' ? 'info' : 'warning'}>{allowed ? 'Acesso liberado' : module.status}</Badge></div><h2>{module.name}</h2><p>{module.description}</p><footer><code>{module.key}</code><span>{allowed ? 'Backend autorizado' : 'Backend protegido'}</span></footer></Card>; })}</div><Card className="security-note"><Icon name="lock" /><div><strong>O frontend não é a barreira de segurança.</strong><p>Estados bloqueados melhoram a experiência, mas a decisão final de acesso permanece no backend por organização e módulo.</p></div></Card></div>;
}
