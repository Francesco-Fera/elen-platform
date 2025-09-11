// API endpoints
export const API_ENDPOINTS = {
  AUTH: {
    LOGIN: "/auth/login",
    REGISTER: "/auth/register",
    REFRESH: "/auth/refresh",
    LOGOUT: "/auth/logout",
    VERIFY_EMAIL: "/auth/verify-email",
    FORGOT_PASSWORD: "/auth/forgot-password",
    RESET_PASSWORD: "/auth/reset-password",
  },
  ORGANIZATIONS: {
    BASE: "/organizations",
    MEMBERS: (orgId: string) => `/organizations/${orgId}/members`,
    INVITES: (orgId: string) => `/organizations/${orgId}/invites`,
    SWITCH: (orgId: string) => `/organizations/${orgId}/switch`,
  },
  WORKFLOWS: {
    BASE: "/workflows",
    BY_ID: (id: string) => `/workflows/${id}`,
    EXECUTE: (id: string) => `/workflows/${id}/execute`,
  },
  USER: {
    PROFILE: "/user/profile",
    UPDATE_PROFILE: "/user/profile",
    CHANGE_PASSWORD: "/user/change-password",
  },
} as const;

// Local storage keys
export const STORAGE_KEYS = {
  ACCESS_TOKEN: "access_token",
  REFRESH_TOKEN: "refresh_token",
  USER: "user",
  ORGANIZATION: "organization",
  THEME: "theme",
} as const;

// App routes
export const ROUTES = {
  HOME: "/",
  LOGIN: "/login",
  REGISTER: "/register",
  FORGOT_PASSWORD: "/forgot-password",
  RESET_PASSWORD: "/reset-password",
  VERIFY_EMAIL: "/verify-email",
  DASHBOARD: "/dashboard",
  WORKFLOWS: "/workflows",
  WORKFLOW_EDITOR: (id?: string) =>
    id ? `/workflows/${id}/edit` : "/workflows/new",
  ORGANIZATION: "/organization",
  PROFILE: "/profile",
} as const;

// Validation constants
export const VALIDATION = {
  PASSWORD_MIN_LENGTH: 8,
  ORGANIZATION_NAME_MAX_LENGTH: 100,
  WORKFLOW_NAME_MAX_LENGTH: 200,
} as const;

// Theme colors
export const THEME_COLORS = {
  primary: "#3b82f6",
  secondary: "#6b7280",
  success: "#10b981",
  warning: "#f59e0b",
  error: "#ef4444",
} as const;
