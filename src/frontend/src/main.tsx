import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { App } from './App';
import { ConnectivityBanner } from './components/ConnectivityBanner';
import { AuthProvider } from './lib/auth';
import { FarmScopeProvider } from './lib/farmScope';
import { registerPwa } from './lib/pwa';
import './styles.css';
import './components/AppShellMultiFarm.css';

registerPwa();

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <AuthProvider>
        <FarmScopeProvider>
          <App />
          <ConnectivityBanner />
        </FarmScopeProvider>
      </AuthProvider>
    </BrowserRouter>
  </StrictMode>
);
