export interface AuthResponse {
  accessToken: string;
  expiresAtUtc: string;
  userId: string;
  organizationId: string;
  role: string;
}

export interface MeProfile {
  userId: string;
  email: string;
  organizationId: string;
  role: string;
}

export interface Organization {
  id: string;
  name: string;
  slug: string;
  createdAtUtc: string;
}

export interface EntitlementSnapshot {
  plan: string;
  modules: Record<string, boolean>;
}

export interface ModuleDefinition {
  key: string;
  name: string;
  status: 'Active' | 'ComingSoon' | 'Locked' | string;
  description: string;
}

export interface PlatformContext {
  profile: MeProfile;
  organization: Organization;
  entitlements: EntitlementSnapshot;
  modules: ModuleDefinition[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface Farm {
  id: string;
  name: string;
  totalAreaHectares: number;
  city: string | null;
  state: string | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface Field {
  id: string;
  farmId: string;
  name: string;
  areaHectares: number;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface Crop {
  id: string;
  name: string;
  variety: string | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface Season {
  id: string;
  fieldId: string;
  cropId: string;
  name: string;
  startDate: string;
  endDate: string | null;
  expectedYieldPerHectare: number | null;
  actualYieldPerHectare: number | null;
  status: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface LowStockItem {
  itemId: string;
  sku: string;
  name: string;
  unit: string;
  minimumStock: number;
  currentStock: number;
  shortage: number;
}

export interface FinancialSummary {
  accruedRevenue: number;
  accruedExpense: number;
  accruedResult: number;
  accruedMarginPercent: number | null;
  cashRevenue: number;
  cashExpense: number;
  cashResult: number;
  pendingReceivables: number;
  pendingPayables: number;
}

export interface ApiProblem {
  title?: string;
  detail?: string;
  message?: string;
  errors?: Record<string, string[]>;
}
