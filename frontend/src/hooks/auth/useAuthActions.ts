import { useCallback } from "react";
import { useAuthState } from "./useAuthState";
import { authService } from "../../services/api/authService";
import { tokenManager } from "../../services/tokenManager";
import { storage } from "../../utils/storage";
import type {
  LoginRequest,
  RegisterRequest,
  AuthErrorDetails,
} from "../../types";

export const useAuthActions = () => {
  const {
    setAuthState,
    setUser,
    setOrganization,
    setError,
    setLoading,
    reset,
  } = useAuthState();

  const handleError = useCallback((error: unknown): AuthErrorDetails => {
    console.error("Auth error:", error);

    if (error instanceof Error) {
      // Map specific error messages to error types
      if (error.message.includes("Invalid email or password")) {
        return {
          type: "INVALID_CREDENTIALS",
          message: "Invalid email or password",
        };
      }
      if (error.message.includes("Network")) {
        return {
          type: "NETWORK_ERROR",
          message: "Network error. Please check your connection.",
        };
      }
      if (error.message.includes("Token expired")) {
        return {
          type: "TOKEN_EXPIRED",
          message: "Your session has expired. Please log in again.",
        };
      }
    }

    return { type: "UNKNOWN_ERROR", message: "An unexpected error occurred" };
  }, []);

  const login = useCallback(
    async (credentials: LoginRequest): Promise<void> => {
      try {
        setLoading("login", true);
        setError(null);
        setAuthState("authenticating");

        const response = await authService.login(credentials);

        if (!response.success || !response.data) {
          throw new Error(response.message || "Login failed");
        }

        const { user, organization, accessToken, refreshToken, expiresIn } =
          response.data;

        // Store tokens
        const tokens = {
          accessToken,
          refreshToken,
          expiresIn: expiresIn || 15 * 60, // Default 15 minutes
          expiresAt: new Date(
            Date.now() + (expiresIn || 15 * 60) * 1000
          ).toISOString(),
        };

        tokenManager.setTokens(tokens);
        storage.setUser(user);
        storage.setOrganization(organization);

        // Update state
        setUser(user);
        setOrganization(organization);
        setAuthState("authenticated");
      } catch (error) {
        const authError = handleError(error);
        setError(authError);
        setAuthState("unauthenticated");
        throw error;
      } finally {
        setLoading("login", false);
      }
    },
    [setLoading, setError, setAuthState, setUser, setOrganization, handleError]
  );

  const register = useCallback(
    async (data: RegisterRequest): Promise<void> => {
      try {
        setLoading("register", true);
        setError(null);
        setAuthState("authenticating");

        const response = await authService.register(data);

        if (!response.success || !response.data) {
          throw new Error(response.message || "Registration failed");
        }

        const { user, organization, accessToken, refreshToken, expiresIn } =
          response.data;

        // Store tokens
        const tokens = {
          accessToken,
          refreshToken,
          expiresIn: expiresIn || 15 * 60,
          expiresAt: new Date(
            Date.now() + (expiresIn || 15 * 60) * 1000
          ).toISOString(),
        };

        tokenManager.setTokens(tokens);
        storage.setUser(user);
        storage.setOrganization(organization);

        // Update state
        setUser(user);
        setOrganization(organization);
        setAuthState("authenticated");
      } catch (error) {
        const authError = handleError(error);
        setError(authError);
        setAuthState("unauthenticated");
        throw error;
      } finally {
        setLoading("register", false);
      }
    },
    [setLoading, setError, setAuthState, setUser, setOrganization, handleError]
  );

  const logout = useCallback(async (): Promise<void> => {
    try {
      setLoading("logout", true);

      const refreshToken = storage.getRefreshToken();
      if (refreshToken) {
        try {
          await authService.logout(refreshToken);
        } catch (error) {
          // Continue with logout even if API call fails
          console.error("Logout API call failed:", error);
        }
      }
    } finally {
      // Always clear local state
      tokenManager.clearTokens();
      storage.clearAll();
      reset();
      setAuthState("unauthenticated");
      setLoading("logout", false);
    }
  }, [setLoading, reset, setAuthState]);

  const refreshAuth = useCallback(async (): Promise<void> => {
    try {
      setLoading("refresh", true);
      setAuthState("refreshing");

      const response = await authService.getProfile();

      if (response.success && response.data) {
        storage.setUser(response.data.user);
        storage.setOrganization(response.data.organization);

        setUser(response.data.user);
        setOrganization(response.data.organization);
        setAuthState("authenticated");
      } else {
        throw new Error("Failed to refresh auth");
      }
    } catch (error) {
      console.error("Failed to refresh auth:", error);
      // Don't set error state for refresh failures, just logout
      await logout();
    } finally {
      setLoading("refresh", false);
    }
  }, [setLoading, setAuthState, setUser, setOrganization, logout]);

  const clearError = useCallback(() => {
    setError(null);
  }, [setError]);

  return {
    login,
    register,
    logout,
    refreshAuth,
    clearError,
  };
};
