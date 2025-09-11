import { STORAGE_KEYS } from "../constants";
import type { User, Organization, AuthTokens } from "../types";

export const storage = {
  // Auth tokens
  setTokens: (tokens: AuthTokens) => {
    localStorage.setItem(STORAGE_KEYS.ACCESS_TOKEN, tokens.accessToken);
    localStorage.setItem(STORAGE_KEYS.REFRESH_TOKEN, tokens.refreshToken);
  },

  getAccessToken: (): string | null => {
    return localStorage.getItem(STORAGE_KEYS.ACCESS_TOKEN);
  },

  getRefreshToken: (): string | null => {
    return localStorage.getItem(STORAGE_KEYS.REFRESH_TOKEN);
  },

  clearTokens: () => {
    localStorage.removeItem(STORAGE_KEYS.ACCESS_TOKEN);
    localStorage.removeItem(STORAGE_KEYS.REFRESH_TOKEN);
  },

  // User data
  setUser: (user: User) => {
    localStorage.setItem(STORAGE_KEYS.USER, JSON.stringify(user));
  },

  getUser: (): User | null => {
    const userData = localStorage.getItem(STORAGE_KEYS.USER);
    return userData ? JSON.parse(userData) : null;
  },

  clearUser: () => {
    localStorage.removeItem(STORAGE_KEYS.USER);
  },

  // Organization data
  setOrganization: (organization: Organization) => {
    localStorage.setItem(
      STORAGE_KEYS.ORGANIZATION,
      JSON.stringify(organization)
    );
  },

  getOrganization: (): Organization | null => {
    const orgData = localStorage.getItem(STORAGE_KEYS.ORGANIZATION);
    return orgData ? JSON.parse(orgData) : null;
  },

  clearOrganization: () => {
    localStorage.removeItem(STORAGE_KEYS.ORGANIZATION);
  },

  // Theme
  setTheme: (theme: "light" | "dark") => {
    localStorage.setItem(STORAGE_KEYS.THEME, theme);
  },

  getTheme: (): "light" | "dark" | null => {
    return localStorage.getItem(STORAGE_KEYS.THEME) as "light" | "dark" | null;
  },

  // Clear all data
  clearAll: () => {
    Object.values(STORAGE_KEYS).forEach((key) => {
      localStorage.removeItem(key);
    });
  },
};
