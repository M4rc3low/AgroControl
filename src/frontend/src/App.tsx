import { Navigate, Outlet, Route, Routes } from 'react-router-dom';
import { AppShell } from './components/AppShell';
import { useAuth } from './lib/auth';
import { DashboardPage } from './pages/DashboardPage';
import { LoginPage } from './pages/LoginPage';
import { ModulePage } from './pages/ModulePage';
import { ModulesPage } from './pages/ModulesPage';
import { NotFoundPage } from './pages/NotFoundPage';
import { PrecisionAgriculturePage } from './pages/PrecisionAgriculturePage';
import { IrrigationPage } from './pages/IrrigationPage';
import { SustainabilityPage } from './pages/SustainabilityPage';
import { ProductionPage } from './pages/ProductionPage';
import { RegisterPage } from './pages/RegisterPage';

function ProtectedRoute() {
  const { session } = useAuth();
  return session ? <Outlet /> : <Navigate to="/login" replace />;
}

function PublicOnlyRoute() {
  const { session } = useAuth();
  return session ? <Navigate to="/" replace /> : <Outlet />;
}

export function App() {
  return (
    <Routes>
      <Route element={<PublicOnlyRoute />}>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
      </Route>

      <Route element={<ProtectedRoute />}>
        <Route element={<AppShell />}>
          <Route index element={<DashboardPage />} />
          <Route path="production" element={<ProductionPage />} />
          <Route path="precision" element={<PrecisionAgriculturePage />} />
          <Route path="irrigation" element={<IrrigationPage />} />
          <Route path="sustainability" element={<SustainabilityPage />} />
          <Route path="inventory" element={<ModulePage moduleKey="Inventory" title="Estoque" description="Insumos, depósitos, movimentações e alertas de estoque baixo." />} />
          <Route path="finance" element={<ModulePage moduleKey="Finance" title="Financeiro" description="Custos, receitas, compromissos, resultado e rentabilidade da operação." />} />
          <Route path="machinery" element={<ModulePage moduleKey="Machinery" title="Máquinas" description="Máquinas, horímetro, combustível, manutenção e custo operacional." />} />
          <Route path="market" element={<ModulePage moduleKey="Market" title="Mercado" description="Commodities, cotações, variações e alertas de preço." />} />
          <Route path="intelligence" element={<ModulePage moduleKey="Intelligence" title="Intelligence" description="Modelos de apoio à decisão e previsão de produtividade." />} />
          <Route path="telemetry" element={<ModulePage moduleKey="Telemetry" title="Telemetry" description="Dispositivos, sensores, GPS e ingestão de telemetria via MQTT." />} />
          <Route path="modules" element={<ModulesPage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Route>
    </Routes>
  );
}
