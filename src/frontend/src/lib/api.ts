import { getAccessToken } from './session';
import type { ApiProblem, PagedResult } from './types';

const configuredBaseUrl = import.meta.env.VITE_API_BASE_URL?.replace(/\/$/, '') ?? '';

export class ApiError extends Error {
  constructor(public readonly status: number, message: string, public readonly problem?: ApiProblem) {
    super(message);
    this.name = 'ApiError';
  }
}

interface ApiRequestOptions extends RequestInit {
  auth?: boolean;
}

function errorMessage(problem: ApiProblem | undefined, fallback: string) {
  if (problem?.detail) return problem.detail;
  if (problem?.message) return problem.message;
  if (problem?.errors) {
    const first = Object.values(problem.errors).flat()[0];
    if (first) return first;
  }
  return problem?.title ?? fallback;
}

export async function apiRequest<T>(path: string, options: ApiRequestOptions = {}): Promise<T> {
  const { auth = true, headers: incomingHeaders, ...init } = options;
  const headers = new Headers(incomingHeaders);
  headers.set('Accept', 'application/json');
  if (init.body && !(init.body instanceof FormData)) headers.set('Content-Type', 'application/json');

  if (auth) {
    const token = getAccessToken();
    if (token) headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(`${configuredBaseUrl}${path}`, { ...init, headers });
  if (response.status === 204) return undefined as T;

  const text = await response.text();
  let body: unknown;
  try {
    body = text ? JSON.parse(text) : undefined;
  } catch {
    body = undefined;
  }

  if (!response.ok) {
    const problem = body as ApiProblem | undefined;
    if (response.status === 401 && auth) window.dispatchEvent(new Event('agrocontrol:unauthorized'));
    throw new ApiError(response.status, errorMessage(problem, `Erro HTTP ${response.status}`), problem);
  }

  return body as T;
}

export async function getAllPaged<T>(path: string, pageSize = 100): Promise<T[]> {
  const items: T[] = [];
  let page = 1;

  while (true) {
    const separator = path.includes('?') ? '&' : '?';
    const result = await apiRequest<PagedResult<T>>(`${path}${separator}page=${page}&pageSize=${pageSize}`);
    items.push(...result.items);
    if (items.length >= result.totalCount || result.items.length === 0) return items;
    page += 1;
  }
}
