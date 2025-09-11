// frontend/src/services/tokenManager.ts

import { storage } from "../utils/storage";
import { apiClient } from "../utils/api";
import type { AuthTokens } from "../types";

export class TokenManager {
  private refreshPromise: Promise<string> | null = null;
  private refreshTimeout: NodeJS.Timeout | null = null;

  constructor() {
    // Set up automatic token refresh
    this.scheduleTokenRefresh();
  }

  /**
   * Get a valid access token, refreshing if necessary
   */
  async getValidToken(): Promise<string | null> {
    const token = storage.getAccessToken();

    if (!token) {
      return null;
    }

    if (this.isTokenExpiring(token)) {
      return this.refreshToken();
    }

    return token;
  }

  /**
   * Refresh access token using refresh token
   */
  private async refreshToken(): Promise<string | null> {
    // Prevent multiple simultaneous refresh requests
    if (this.refreshPromise) {
      return this.refreshPromise;
    }

    const refreshToken = storage.getRefreshToken();
    if (!refreshToken) {
      this.clearTokens();
      return null;
    }

    this.refreshPromise = this.performTokenRefresh(refreshToken);

    try {
      const newToken = await this.refreshPromise;
      this.scheduleTokenRefresh();
      return newToken;
    } catch (error) {
      console.error("Token refresh failed:", error);
      this.clearTokens();
      // Emit event for auth context to handle
      window.dispatchEvent(new CustomEvent("token-refresh-failed"));
      return null;
    } finally {
      this.refreshPromise = null;
    }
  }

  /**
   * Perform the actual token refresh API call
   */
  private async performTokenRefresh(refreshToken: string): Promise<string> {
    const response = await fetch(
      `${process.env.REACT_APP_API_URL || "http://localhost:5000/api"}/auth/refresh`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ refreshToken }),
      }
    );

    if (!response.ok) {
      throw new Error(`Token refresh failed: ${response.status}`);
    }

    const data = await response.json();

    if (!data.success || !data.data) {
      throw new Error("Invalid refresh response");
    }

    const tokens: AuthTokens = {
      accessToken: data.data.accessToken,
      refreshToken: data.data.refreshToken,
      expiresIn: data.data.expiresIn || 15 * 60, // Default 15 minutes
      expiresAt: new Date(
        Date.now() + (data.data.expiresIn || 15 * 60) * 1000
      ).toISOString(),
    };

    storage.setTokens(tokens);
    return tokens.accessToken;
  }

  /**
   * Check if token is expiring soon (within 2 minutes)
   */
  private isTokenExpiring(token: string): boolean {
    try {
      const payload = JSON.parse(atob(token.split(".")[1]));
      const expirationTime = payload.exp * 1000; // Convert to milliseconds
      const currentTime = Date.now();
      const bufferTime = 2 * 60 * 1000; // 2 minutes buffer

      return currentTime >= expirationTime - bufferTime;
    } catch (error) {
      console.error("Error parsing token:", error);
      return true; // Assume expired if we can't parse
    }
  }

  /**
   * Schedule automatic token refresh
   */
  private scheduleTokenRefresh(): void {
    if (this.refreshTimeout) {
      clearTimeout(this.refreshTimeout);
    }

    const token = storage.getAccessToken();
    if (!token) return;

    try {
      const payload = JSON.parse(atob(token.split(".")[1]));
      const expirationTime = payload.exp * 1000;
      const currentTime = Date.now();
      const refreshTime = expirationTime - currentTime - 3 * 60 * 1000; // Refresh 3 minutes before expiry

      if (refreshTime > 0) {
        this.refreshTimeout = setTimeout(() => {
          this.refreshToken();
        }, refreshTime);
      }
    } catch (error) {
      console.error("Error scheduling token refresh:", error);
    }
  }

  /**
   * Store new tokens and schedule refresh
   */
  setTokens(tokens: AuthTokens): void {
    storage.setTokens(tokens);
    this.scheduleTokenRefresh();
  }

  /**
   * Clear all tokens and cancel refresh
   */
  clearTokens(): void {
    storage.clearTokens();
    if (this.refreshTimeout) {
      clearTimeout(this.refreshTimeout);
      this.refreshTimeout = null;
    }
    this.refreshPromise = null;
  }

  /**
   * Get token expiration info
   */
  getTokenInfo(): { isValid: boolean; expiresAt?: Date; expiresIn?: number } {
    const token = storage.getAccessToken();

    if (!token) {
      return { isValid: false };
    }

    try {
      const payload = JSON.parse(atob(token.split(".")[1]));
      const expirationTime = payload.exp * 1000;
      const currentTime = Date.now();
      const expiresIn = Math.max(
        0,
        Math.floor((expirationTime - currentTime) / 1000)
      );

      return {
        isValid: expirationTime > currentTime,
        expiresAt: new Date(expirationTime),
        expiresIn,
      };
    } catch (error) {
      return { isValid: false };
    }
  }
}

// Export singleton instance
export const tokenManager = new TokenManager();
