import { useEffect, useRef, useState } from 'react';
import type { Farm } from '../../lib/types';
import './multiFarm.css';

const MAP_SCRIPT = import.meta.env.VITE_MAPLIBRE_SCRIPT_URL?.trim() || 'https://unpkg.com/maplibre-gl@5/dist/maplibre-gl.js';
const MAP_CSS = import.meta.env.VITE_MAPLIBRE_CSS_URL?.trim() || 'https://unpkg.com/maplibre-gl@5/dist/maplibre-gl.css';
const MAP_STYLE = import.meta.env.VITE_MAP_STYLE_URL?.trim() || 'https://demotiles.maplibre.org/style.json';

async function ensureMapLibre(): Promise<any> {
  const globalWindow = window as any;
  if (globalWindow.maplibregl) return globalWindow.maplibregl;
  if (!document.querySelector('link[data-agrocontrol-maplibre]')) {
    const link = document.createElement('link');
    link.rel = 'stylesheet'; link.href = MAP_CSS; link.dataset.agrocontrolMaplibre = 'true'; document.head.appendChild(link);
  }
  return new Promise((resolve, reject) => {
    const existing = document.querySelector<HTMLScriptElement>('script[data-agrocontrol-maplibre]');
    const resolveLoaded = () => globalWindow.maplibregl ? resolve(globalWindow.maplibregl) : reject(new Error('MapLibre não ficou disponível.'));
    if (existing) {
      existing.addEventListener('load', resolveLoaded, { once: true });
      existing.addEventListener('error', () => reject(new Error('Não foi possível carregar o mapa.')), { once: true });
      return;
    }
    const script = document.createElement('script');
    script.src = MAP_SCRIPT; script.async = true; script.dataset.agrocontrolMaplibre = 'true';
    script.addEventListener('load', resolveLoaded, { once: true });
    script.addEventListener('error', () => reject(new Error('Não foi possível carregar o mapa.')), { once: true });
    document.head.appendChild(script);
  });
}

export function MultiFarmMap({ farms, onSelectFarm }: { farms: Farm[]; onSelectFarm(farmId: string): void }) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const mapRef = useRef<any>(null);
  const markersRef = useRef<any[]>([]);
  const [error, setError] = useState<string | null>(null);

  const locatedFarms = farms.filter(farm => farm.latitude != null && farm.longitude != null).slice(0, 150);

  useEffect(() => {
    let cancelled = false;
    async function render() {
      if (!containerRef.current) return;
      if (!locatedFarms.length) return;
      try {
        const maplibre = await ensureMapLibre();
        if (cancelled || !containerRef.current) return;
        if (!mapRef.current) {
          mapRef.current = new maplibre.Map({ container: containerRef.current, style: MAP_STYLE, center: [-52.5, -15.5], zoom: 3.2, attributionControl: true });
          mapRef.current.addControl(new maplibre.NavigationControl(), 'top-right');
        }

        markersRef.current.forEach(marker => marker.remove());
        markersRef.current = [];
        const bounds = new maplibre.LngLatBounds();
        for (const farm of locatedFarms) {
          const longitude = Number(farm.longitude);
          const latitude = Number(farm.latitude);
          if (!Number.isFinite(longitude) || !Number.isFinite(latitude)) continue;
          const element = document.createElement('button');
          element.type = 'button';
          element.className = 'farm-map-marker';
          element.title = `${farm.name}${farm.stateCode ? ` — ${farm.stateCode}` : ''}`;
          element.setAttribute('aria-label', `Selecionar ${farm.name}`);
          element.addEventListener('click', () => onSelectFarm(farm.id));
          const marker = new maplibre.Marker({ element }).setLngLat([longitude, latitude]).addTo(mapRef.current);
          markersRef.current.push(marker);
          bounds.extend([longitude, latitude]);
        }
        if (!bounds.isEmpty()) mapRef.current.fitBounds(bounds, { padding: 46, maxZoom: 9, duration: 550 });
        setError(null);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Mapa indisponível.');
      }
    }
    void render();
    return () => { cancelled = true; };
  }, [farms, onSelectFarm]);

  useEffect(() => () => {
    markersRef.current.forEach(marker => marker.remove());
    markersRef.current = [];
    mapRef.current?.remove();
    mapRef.current = null;
  }, []);

  if (!locatedFarms.length) return <div className="farm-map-empty"><strong>Mapa aguardando coordenadas</strong><span>Cadastre latitude e longitude da sede ou centro operacional das propriedades para exibi-las no mapa consolidado.</span></div>;

  return <div className="farm-map-shell"><div ref={containerRef} className="farm-map" />{error && <div className="farm-map-error">{error}</div>}<span className="farm-map-caption">{locatedFarms.length} de {farms.length} propriedade(s) com coordenadas no escopo atual</span></div>;
}
