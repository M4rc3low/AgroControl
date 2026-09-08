import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Button } from '../components/Ui';
import { useAuth } from '../lib/auth';

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await login(email.trim(), password);
      navigate('/', { replace: true });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível entrar.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main className="auth-layout">
      <section className="auth-visual"><div className="auth-visual__content"><div className="brand brand--light"><div className="brand__mark brand__mark--light"><span>A</span></div><div><strong>AgroControl</strong><small>gestão inteligente</small></div></div><span className="auth-kicker">Sua operação em uma visão única</span><h1>Decisões melhores começam com dados organizados.</h1><p>Produção, estoque, financeiro, máquinas, inteligência e telemetria conectados ao mesmo contexto operacional.</p><div className="auth-proof"><strong>9</strong><span>sprints de base técnica<br />transformadas em produto</span></div></div><div className="field-lines" aria-hidden="true"><i /><i /><i /><i /></div></section>
      <section className="auth-panel"><div className="auth-card"><div><span className="eyebrow">Bem-vindo de volta</span><h2>Acesse o AgroControl</h2><p>Entre com o usuário da sua organização.</p></div><form onSubmit={onSubmit} className="form-stack"><label>E-mail<input type="email" autoComplete="email" required value={email} onChange={event => setEmail(event.target.value)} placeholder="voce@empresa.com" /></label><label>Senha<input type="password" autoComplete="current-password" required value={password} onChange={event => setPassword(event.target.value)} placeholder="••••••••" /></label>{error && <div className="form-error" role="alert">{error}</div>}<Button type="submit" disabled={submitting}>{submitting ? 'Entrando…' : 'Entrar'}</Button></form><p className="auth-switch">Ainda não tem uma organização? <Link to="/register">Criar acesso</Link></p></div></section>
    </main>
  );
}
