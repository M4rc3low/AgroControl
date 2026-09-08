import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { App } from './App';
import { AuthProvider } from './lib/auth';
import { FarmScopeProvider } from './lib/farmScope';
import './styles.css';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <AuthProvider>
        <FarmScopeProvider>
          <App />
        </FarmScopeProvider>
      </AuthProvider>
    </BrowserRouter>
  </StrictMode>
);
