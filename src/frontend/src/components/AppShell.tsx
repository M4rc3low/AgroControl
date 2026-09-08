import { useState } from 'react';
import { NavLink, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../lib/auth';
import { initials } from '../lib/format';
import { Icon } from './Icon';
import { Button, Spinner } from './Ui';

const navigation = [
  { to: '/', label: 'Visão geral', icon: 'dashboard' as const },
  { to: '/production', label: 'Produção rural', icon: 'leaf' as const, module: 'Farms' },
  { to: '/precision', label: 'Agricultura de precisão', icon: 'map' as const, module: 'PrecisionAgriculture' },
  { to: '/inventory', label: 'Estoque', icon: 'box' as const, module: 'Inventory' },
  { to: '/finance', label: 'Financeiro', icon: 'wallet' as const, module: 'Finance' },
  { to: '/machinery', label: 'Máquinas', icon: 'tractor' as const, module: 'Machinery' },
  { to: '/market', label: 'Mercado', icon: 'chart' as const, module: 'Market' },
  { to: '/intelligence', label: 'Intelligence', icon: 'spark' as const, module: 'Intelligence' },
  { to: '/telemetry', label: 'Telemetry', icon: 'radio' as const, module: 'Telemetry' },
  { to: '/modules', label: 'Módulos e plano', icon: 'grid' as const }
];

export function AppShell() {
  const { platform, contextLoading, contextError, logout, refreshContext } = useAuth();
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const location = useLocation();
  const closeSidebar = () => setSidebarOpen(false);

  return (
    <div className="app-shell">
      <aside className={`sidebar ${sidebarOpen ? 'sidebar--open' : ''}`}>
        <div className="brand"><div className="brand__mark"><span>A</span></div><div><strong>AgroControl</strong><small>gestão inteligente</small></div></div>
        <nav className="nav" aria-label="Navegação principal">
          <span className="nav__section">Operação</span>
          {navigation.map(item => {
            const allowed = !item.module || Boolean(platform?.entitlements.modules[item.module]);
            return <NavLink key={item.to} to={item.to} end={item.to === '/'} onClick={closeSidebar} className={({ isActive }) => `nav__link ${isActive ? 'nav__link--active' : ''} ${allowed ? '' : 'nav__link--locked'}`.trim()}><Icon name={item.icon} size={19} /><span>{item.label}</span>{!allowed && <Icon name="lock" size={14} className="nav__lock" />}</NavLink>;
          })}
        </nav>
        <div className="sidebar__footer"><div className="plan-chip"><span>Plano atual</span><strong>{platform?.entitlements.plan ?? '—'}</strong></div><button className="nav__link nav__link--button" onClick={logout}><Icon name="logout" size={19} /><span>Sair</span></button></div>
      </aside>
      {sidebarOpen && <button className="sidebar-overlay" aria-label="Fechar menu" onClick={closeSidebar} />}
      <div className="app-main">
        <header className="topbar">
          <button className="icon-button topbar__menu" onClick={() => setSidebarOpen(true)} aria-label="Abrir menu"><Icon name="menu" /></button>
          <div className="topbar__context"><span>{platform?.organization.name ?? 'AgroControl'}</span><small>{platform?.organization.slug ?? 'carregando contexto'}</small></div>
          <div className="topbar__user"><div className="avatar">{initials(platform?.organization.name)}</div><div><strong>{platform?.profile.email ?? 'Usuário'}</strong><small>{platform?.profile.role ?? '—'}</small></div></div>
        </header>
        {contextError && <div className="context-error"><span>{contextError}</span><Button variant="secondary" onClick={() => void refreshContext()}><Icon name="refresh" size={16} /> Tentar novamente</Button></div>}
        {contextLoading && !platform ? <main className="content content--center"><Spinner label="Carregando sua operação" /></main> : <main className="content" key={location.pathname}><Outlet /></main>}
      </div>
    </div>
  );
}
