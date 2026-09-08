import type { RemoteSensingSeriesPoint } from '../../lib/types';

export interface RemoteSensingSeriesGroup {
  key: string;
  label: string;
  points: RemoteSensingSeriesPoint[];
}

export function seriesKey(point: RemoteSensingSeriesPoint) {
  return point.indexType === 'Custom' ? `Custom:${point.customIndexName ?? 'Custom'}` : point.indexType;
}

export function groupRemoteSensingSeries(points: RemoteSensingSeriesPoint[]): RemoteSensingSeriesGroup[] {
  const groups = new Map<string, RemoteSensingSeriesPoint[]>();
  [...points].sort((a, b) => new Date(a.observedAtUtc).getTime() - new Date(b.observedAtUtc).getTime()).forEach(point => {
    const key = seriesKey(point);
    const current = groups.get(key) ?? [];
    current.push(point);
    groups.set(key, current);
  });
  return [...groups.entries()].map(([key, items]) => ({
    key,
    label: key.startsWith('Custom:') ? key.slice('Custom:'.length) : key,
    points: items
  }));
}

export function buildSeriesPath(points: RemoteSensingSeriesPoint[], width = 640, height = 180): string {
  if (!points.length) return '';
  const ordered = [...points].sort((a, b) => new Date(a.observedAtUtc).getTime() - new Date(b.observedAtUtc).getTime());
  const times = ordered.map(point => new Date(point.observedAtUtc).getTime());
  const minTime = Math.min(...times);
  const maxTime = Math.max(...times);
  const values = ordered.map(point => point.mean);
  const minValue = Math.min(-1, ...values);
  const maxValue = Math.max(1, ...values);
  const timeSpan = Math.max(1, maxTime - minTime);
  const valueSpan = Math.max(0.000001, maxValue - minValue);
  return ordered.map((point, index) => {
    const x = ordered.length === 1 ? width / 2 : ((times[index] - minTime) / timeSpan) * width;
    const y = height - ((point.mean - minValue) / valueSpan) * height;
    return `${index === 0 ? 'M' : 'L'} ${x.toFixed(2)} ${y.toFixed(2)}`;
  }).join(' ');
}

export function formatIndexValue(value: number) {
  return value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 4 });
}
