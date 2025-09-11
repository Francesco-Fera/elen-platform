import { STORAGE_KEYS } from "../constants";
import type { User, Organization, AuthTokens } from "../types";

export const storage = {
  // Auth tokens
  setTokens: (tokens: AuthTokens) => {
    try {
      localStorage.setItem(STORAGE_KEYS.ACCESS_TOKEN, tokens.accessToken);
      localStorage.setItem(STORAGE_KEYS.REFRESH_TOKEN, tokens.refreshToken);

      // Store expiration info for easier validation
      if (tokens.expiresAt) {
        localStorage.setItem("token_expires_at", tokens.expiresAt);
      } else if (tokens.expiresIn) {
        const expiresAt = new Date(
          Date.now() + tokens.expiresIn * 1000
        ).toISOString();
        localStorage.setItem("token_expires_at", expiresAt);
      }
    } catch (error) {
      console.error("Error storing tokens:", error);
    }
  },

  getAccessToken: (): string | null => {
    try {
      return localStorage.getItem(STORAGE_KEYS.ACCESS_TOKEN);
    } catch (error) {
      console.error("Error getting access token:", error);
      return null;
    }
  },

  getRefreshToken: (): string | null => {
    try {
      return localStorage.getItem(STORAGE_KEYS.REFRESH_TOKEN);
    } catch (error) {
      console.error("Error getting refresh token:", error);
      return null;
    }
  },

  getTokenExpiresAt: (): string | null => {
    try {
      return localStorage.getItem("token_expires_at");
    } catch (error) {
      console.error("Error getting token expiration:", error);
      return null;
    }
  },

  isTokenExpired: (): boolean => {
    try {
      const expiresAt = storage.getTokenExpiresAt();
      if (!expiresAt) return true;

      return new Date().getTime() >= new Date(expiresAt).getTime();
    } catch (error) {
      console.error("Error checking token expiration:", error);
      return true;
    }
  },

  clearTokens: () => {
    try {
      localStorage.removeItem(STORAGE_KEYS.ACCESS_TOKEN);
      localStorage.removeItem(STORAGE_KEYS.REFRESH_TOKEN);
      localStorage.removeItem("token_expires_at");
    } catch (error) {
      console.error("Error clearing tokens:", error);
    }
  },

  // User data
  setUser: (user: User) => {
    try {
      localStorage.setItem(STORAGE_KEYS.USER, JSON.stringify(user));
    } catch (error) {
      console.error("Error storing user:", error);
    }
  },

  getUser: (): User | null => {
    try {
      const userData = localStorage.getItem(STORAGE_KEYS.USER);
      return userData ? JSON.parse(userData) : null;
    } catch (error) {
      console.error("Error getting user:", error);
      return null;
    }
  },

  clearUser: () => {
    try {
      localStorage.removeItem(STORAGE_KEYS.USER);
    } catch (error) {
      console.error("Error clearing user:", error);
    }
  },

  // Organization data
  setOrganization: (organization: Organization) => {
    try {
      localStorage.setItem(
        STORAGE_KEYS.ORGANIZATION,
        JSON.stringify(organization)
      );
    } catch (error) {
      console.error("Error storing organization:", error);
    }
  },

  getOrganization: (): Organization | null => {
    try {
      const orgData = localStorage.getItem(STORAGE_KEYS.ORGANIZATION);
      return orgData ? JSON.parse(orgData) : null;
    } catch (error) {
      console.error("Error getting organization:", error);
      return null;
    }
  },

  clearOrganization: () => {
    try {
      localStorage.removeItem(STORAGE_KEYS.ORGANIZATION);
    } catch (error) {
      console.error("Error clearing organization:", error);
    }
  },

  // Theme
  setTheme: (theme: "light" | "dark") => {
    try {
      localStorage.setItem(STORAGE_KEYS.THEME, theme);
      document.documentElement.classList.toggle("dark", theme === "dark");
    } catch (error) {
      console.error("Error storing theme:", error);
    }
  },

  getTheme: (): "light" | "dark" | null => {
    try {
      return localStorage.getItem(STORAGE_KEYS.THEME) as
        | "light"
        | "dark"
        | null;
    } catch (error) {
      console.error("Error getting theme:", error);
      return null;
    }
  },

  // Clear all data
  clearAll: () => {
    try {
      Object.values(STORAGE_KEYS).forEach((key) => {
        localStorage.removeItem(key);
      });
      // Clear additional stored items
      localStorage.removeItem("token_expires_at");
    } catch (error) {
      console.error("Error clearing all storage:", error);
    }
  },

  // Utility methods
  isStorageAvailable: (): boolean => {
    try {
      const test = "__storage_test__";
      localStorage.setItem(test, test);
      localStorage.removeItem(test);
      return true;
    } catch (error) {
      return false;
    }
  },

  getStorageSize: (): number => {
    try {
      let total = 0;
      for (const key in localStorage) {
        if (localStorage.hasOwnProperty(key)) {
          total += localStorage[key].length + key.length;
        }
      }
      return total;
    } catch (error) {
      console.error("Error calculating storage size:", error);
      return 0;
    }
  },

  // Backup and restore functionality
  exportData: (): string => {
    try {
      const data = {
        user: storage.getUser(),
        organization: storage.getOrganization(),
        theme: storage.getTheme(),
        timestamp: new Date().toISOString(),
      };
      return JSON.stringify(data, null, 2);
    } catch (error) {
      console.error("Error exporting data:", error);
      return "{}";
    }
  },

  importData: (jsonData: string): boolean => {
    try {
      const data = JSON.parse(jsonData);

      if (data.user) storage.setUser(data.user);
      if (data.organization) storage.setOrganization(data.organization);
      if (data.theme) storage.setTheme(data.theme);

      return true;
    } catch (error) {
      console.error("Error importing data:", error);
      return false;
    }
  },

  // Session management
  setSessionData: (key: string, value: any) => {
    try {
      sessionStorage.setItem(key, JSON.stringify(value));
    } catch (error) {
      console.error("Error storing session data:", error);
    }
  },

  getSessionData: <T>(key: string): T | null => {
    try {
      const data = sessionStorage.getItem(key);
      return data ? JSON.parse(data) : null;
    } catch (error) {
      console.error("Error getting session data:", error);
      return null;
    }
  },

  clearSessionData: (key?: string) => {
    try {
      if (key) {
        sessionStorage.removeItem(key);
      } else {
        sessionStorage.clear();
      }
    } catch (error) {
      console.error("Error clearing session data:", error);
    }
  },
};
