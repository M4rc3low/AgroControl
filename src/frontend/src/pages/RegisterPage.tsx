import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Button } from '../components/Ui';
import { useAuth } from '../lib/auth';

export function RegisterPage() {
  const { register } = useAuth();
  const navigate = useNavigate();
  const [organizationName, setOrganizationName] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    if (password.length < 8) { setError('A senha precisa ter pelo menos 8 caracteres.'); return; }
    setError(null);
    setSubmitting(true);
    try {
      await register({ organizationName: organizationName.trim(), displayName: displayName.trim(), email: email.trim(), password });
      navigate('/', { replace: true });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível criar o acesso.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main className="auth-layout">
      <section className="auth-visual auth-visual--register"><div className="auth-visual__content"><div className="brand brand--light"><div className="brand__mark brand__mark--light"><span>A</span></div><div><strong>AgroControl</strong><small>gestão inteligente</small></div></div><span className="auth-kicker">Comece pela sua organização</span><h1>Estruture a operação antes de escalar a complexidade.</h1><p>O primeiro usuário recebe o contexto da organização e os módulos do plano inicial. A segurança de acesso continua sendo validada pela API.</p></div><div className="field-lines" aria-hidden="true"><i /><i /><i /><i /></div></section>
      <section className="auth-panel"><div className="auth-card auth-card--wide"><div><span className="eyebrow">Novo ambiente</span><h2>Crie sua organização</h2><p>Em poucos dados, sua base operacional fica pronta.</p></div><form onSubmit={onSubmit} className="form-stack"><div className="form-grid"><label>Organização<input required value={organizationName} onChange={event => setOrganizationName(event.target.value)} placeholder="Fazenda Horizonte" /></label><label>Seu nome<input required autoComplete="name" value={displayName} onChange={event => setDisplayName(event.target.value)} placeholder="Marcelo Gomes" /></label></div><label>E-mail<input type="email" autoComplete="email" required value={email} onChange={event => setEmail(event.target.value)} placeholder="voce@empresa.com" /></label><label>Senha<input type="password" autoComplete="new-password" minLength={8} required value={password} onChange={event => setPassword(event.target.value)} placeholder="Mínimo de 8 caracteres" /></label>{error && <div className="form-error" role="alert">{error}</div>}<Button type="submit" disabled={submitting}>{submitting ? 'Criando ambiente…' : 'Criar organização'}</Button></form><p className="auth-switch">Já possui acesso? <Link to="/login">Entrar</Link></p></div></section>
    </main>
  );
}
