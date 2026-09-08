import { useEffect, useState } from 'react';

export function ConnectivityBanner() {
  const [online, setOnline] = useState(() => navigator.onLine);

  useEffect(() => {
    const handleOnline = () => setOnline(true);
    const handleOffline = () => setOnline(false);
    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);
    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, []);

  if (online) return null;

  return (
    <div
      role="status"
      aria-live="polite"
      style={{
        position: 'fixed',
        left: '50%',
        bottom: 16,
        transform: 'translateX(-50%)',
        zIndex: 9999,
        maxWidth: 'calc(100vw - 32px)',
        padding: '10px 14px',
        borderRadius: 10,
        background: '#1f2937',
        color: '#fff',
        boxShadow: '0 8px 28px rgb(0 0 0 / 18%)',
        fontSize: 14,
        textAlign: 'center'
      }}
    >
      Sem conexão. O AgroControl pode abrir a interface instalada, mas dados da operação continuam exigindo conexão com a API.
    </div>
  );
}
