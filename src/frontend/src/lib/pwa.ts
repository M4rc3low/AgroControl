function isDesktopShell() {
  return window.location.hostname === 'tauri.localhost' || window.location.protocol === 'tauri:';
}

export function registerPwa() {
  if (!import.meta.env.PROD || isDesktopShell() || !('serviceWorker' in navigator)) return;

  let refreshing = false;
  navigator.serviceWorker.addEventListener('controllerchange', () => {
    if (refreshing) return;
    refreshing = true;
    window.location.reload();
  });

  window.addEventListener('load', () => {
    void navigator.serviceWorker.register('/sw.js').catch(error => {
      console.warn('AgroControl service worker registration failed.', error);
    });
  });
}
