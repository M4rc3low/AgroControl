type IconName = 'dashboard' | 'leaf' | 'box' | 'wallet' | 'tractor' | 'chart' | 'spark' | 'radio' | 'grid' | 'menu' | 'logout' | 'plus' | 'search' | 'edit' | 'trash' | 'lock' | 'refresh' | 'close' | 'arrow';

const paths: Record<IconName, string[]> = {
  dashboard: ['M4 13h6V4H4v9Z', 'M14 20h6v-9h-6v9Z', 'M14 4h6v3h-6V4Z', 'M4 20h6v-3H4v3Z'],
  leaf: ['M11 20A7 7 0 0 1 9.8 6.1C15 5 19 2 20 2c0 8-3.8 14-9 14', 'M2 21c3-6 7-10 13-13'],
  box: ['m21 8-9 5-9-5', 'm3 8 9-5 9 5v8l-9 5-9-5Z', 'M12 13v8'],
  wallet: ['M20 7V6a2 2 0 0 0-2-2H5a3 3 0 0 0 0 6h15v8H5a3 3 0 0 1-3-3V7', 'M16 14h.01'],
  tractor: ['M3 4h9l2 7h4l3 3v3h-2', 'M7 17h6', 'M5 20a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z', 'M16 19a2 2 0 1 0 0-4 2 2 0 0 0 0 4Z'],
  chart: ['M3 3v18h18', 'm7 15 4-4 4 2 5-6'],
  spark: ['m12 3-1.3 4.1a2 2 0 0 1-1.3 1.3L5 10l4.4 1.6a2 2 0 0 1 1.3 1.3L12 17l1.3-4.1a2 2 0 0 1 1.3-1.3L19 10l-4.4-1.6a2 2 0 0 1-1.3-1.3L12 3Z'],
  radio: ['M5.6 5.6a9 9 0 0 0 0 12.8', 'M18.4 5.6a9 9 0 0 1 0 12.8', 'M8.8 8.8a4.5 4.5 0 0 0 0 6.4', 'M15.2 8.8a4.5 4.5 0 0 1 0 6.4', 'M12 12h.01'],
  grid: ['M4 4h6v6H4Z', 'M14 4h6v6h-6Z', 'M4 14h6v6H4Z', 'M14 14h6v6h-6Z'],
  menu: ['M4 6h16', 'M4 12h16', 'M4 18h16'],
  logout: ['M10 17l5-5-5-5', 'M15 12H3', 'M21 19V5a2 2 0 0 0-2-2h-6'],
  plus: ['M12 5v14', 'M5 12h14'],
  search: ['m21 21-4.35-4.35', 'M11 19a8 8 0 1 0 0-16 8 8 0 0 0 0 16Z'],
  edit: ['M12 20h9', 'M16.5 3.5a2.1 2.1 0 0 1 3 3L8 18l-4 1 1-4Z'],
  trash: ['M3 6h18', 'M8 6V4h8v2', 'M19 6l-1 14H6L5 6', 'M10 11v5', 'M14 11v5'],
  lock: ['M6 10h12v10H6Z', 'M8 10V7a4 4 0 0 1 8 0v3'],
  refresh: ['M20 6v6h-6', 'M4 18v-6h6', 'M18.5 9A7 7 0 0 0 6 6l-2 6', 'M5.5 15A7 7 0 0 0 18 18l2-6'],
  close: ['M6 6l12 12', 'M18 6 6 18'],
  arrow: ['M5 12h14', 'm13 6 6 6-6 6']
};

export function Icon({ name, size = 20, className }: { name: IconName; size?: number; className?: string }) {
  return (
    <svg className={className} width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      {paths[name].map((path, index) => <path key={`${name}-${index}`} d={path} />)}
    </svg>
  );
}
