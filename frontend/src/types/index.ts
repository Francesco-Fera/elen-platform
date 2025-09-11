// Auth types
export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  currentOrganizationId?: string;
  isEmailVerified: boolean;
  timeZone?: string;
  createdAt: string;
  updatedAt: string;
}

export interface Organization {
  id: string;
  name: string;
  slug?: string;
  description?: string;
  domain?: string;
  isActive: boolean;
  isTrialAccount: boolean;
  trialExpiresAt?: string;
  subscriptionPlan: "Free" | "Pro" | "Enterprise";
  maxUsers: number;
  maxWorkflows: number;
  createdAt: string;
  updatedAt?: string;
}

export interface OrganizationMember {
  id: string;
  userId: string;
  organizationId: string;
  role: "Owner" | "Admin" | "Member" | "Viewer";
  joinedAt: string;
  user: User;
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  expiresAt?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  organizationName: string;
  timeZone?: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn?: number;
  user: User;
  organization: Organization;
}

// API Response types
export interface ApiResponse<T = any> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: Record<string, string[]>;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// Auth States
export type AuthState =
  | "idle"
  | "loading"
  | "authenticating"
  | "authenticated"
  | "unauthenticated"
  | "refreshing"
  | "error";

// Error types
export type AuthError =
  | "INVALID_CREDENTIALS"
  | "TOKEN_EXPIRED"
  | "TOKEN_INVALID"
  | "NETWORK_ERROR"
  | "USER_DEACTIVATED"
  | "EMAIL_NOT_VERIFIED"
  | "ORGANIZATION_INACTIVE"
  | "UNKNOWN_ERROR";

export interface AuthErrorDetails {
  type: AuthError;
  message: string;
  code?: string;
  field?: string;
}

// Form validation types
export interface FormErrors {
  [key: string]: string;
}

export interface LoadingStates {
  login: boolean;
  register: boolean;
  logout: boolean;
  refresh: boolean;
  profile: boolean;
}

// Workflow types (keeping existing structure)
export interface WorkflowNode {
  id: string;
  type: string;
  position: { x: number; y: number };
  data: Record<string, any>;
}

export interface WorkflowConnection {
  id: string;
  sourceNodeId: string;
  targetNodeId: string;
  sourcePort?: string;
  targetPort?: string;
}

export interface Workflow {
  id: string;
  name: string;
  description?: string;
  organizationId: string;
  isActive: boolean;
  nodes: WorkflowNode[];
  connections: WorkflowConnection[];
  createdAt: string;
  updatedAt: string;
}
