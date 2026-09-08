import { Link } from 'react-router-dom';
import { EmptyState } from '../components/Ui';

export function NotFoundPage() {
  return <EmptyState title="Página não encontrada" description="O endereço informado não faz parte da navegação atual do AgroControl." action={<Link className="button button--primary" to="/">Voltar para a visão geral</Link>} />;
}
