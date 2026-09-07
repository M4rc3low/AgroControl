import { useEffect } from 'react';
import type { ButtonHTMLAttributes, PropsWithChildren, ReactNode } from 'react';
import { Icon } from './Icon';

export function Button({ children, className = '', variant = 'primary', ...props }: PropsWithChildren<ButtonHTMLAttributes<HTMLButtonElement> & { variant?: 'primary' | 'secondary' | 'danger' | 'ghost' }>) {
  return <button className={`button button--${variant} ${className}`.trim()} {...props}>{children}</button>;
}

export function Card({ children, className = '' }: PropsWithChildren<{ className?: string }>) {
  return <section className={`card ${className}`.trim()}>{children}</section>;
}

export function Badge({ children, tone = 'neutral' }: PropsWithChildren<{ tone?: 'neutral' | 'success' | 'warning' | 'danger' | 'info' }>) {
  return <span className={`badge badge--${tone}`}>{children}</span>;
}

export function Spinner({ label = 'Carregando' }: { label?: string }) {
  return <div className="spinner-wrap" role="status"><span className="spinner" /><span>{label}</span></div>;
}

export function EmptyState({ title, description, action }: { title: string; description: string; action?: ReactNode }) {
  return <div className="empty-state"><div className="empty-state__mark">AC</div><h3>{title}</h3><p>{description}</p>{action}</div>;
}

export function MetricCard({ label, value, hint, icon }: { label: string; value: string; hint?: string; icon: ReactNode }) {
  return <Card className="metric-card"><div className="metric-card__icon">{icon}</div><div><span className="metric-card__label">{label}</span><strong>{value}</strong>{hint && <small>{hint}</small>}</div></Card>;
}

export function Modal({ open, title, description, onClose, children }: PropsWithChildren<{ open: boolean; title: string; description?: string; onClose(): void }>) {
  useEffect(() => {
    if (!open) return;
    const onKeyDown = (event: KeyboardEvent) => { if (event.key === 'Escape') onClose(); };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [open, onClose]);

  if (!open) return null;
  return (
    <div className="modal-backdrop" role="presentation" onMouseDown={event => { if (event.target === event.currentTarget) onClose(); }}>
      <section className="modal" role="dialog" aria-modal="true" aria-labelledby="modal-title">
        <header className="modal__header"><div><h2 id="modal-title">{title}</h2>{description && <p>{description}</p>}</div><button className="icon-button" onClick={onClose} aria-label="Fechar"><Icon name="close" /></button></header>
        <div className="modal__body">{children}</div>
      </section>
    </div>
  );
}

export function PageHeader({ eyebrow, title, description, actions }: { eyebrow?: string; title: string; description?: string; actions?: ReactNode }) {
  return <header className="page-header"><div>{eyebrow && <span className="eyebrow">{eyebrow}</span>}<h1>{title}</h1>{description && <p>{description}</p>}</div>{actions && <div className="page-header__actions">{actions}</div>}</header>;
}
