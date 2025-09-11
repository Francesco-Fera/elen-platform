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
  subscriptionPlan: "Free" | "Pro" | "Enterprise"; // This maps to Plan enum
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
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
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

// Workflow types (basic structure for now)
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

// Form validation types
export interface FormErrors {
  [key: string]: string;
}

export interface LoadingState {
  [key: string]: boolean;
}
