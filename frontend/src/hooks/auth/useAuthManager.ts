import { useEffect, useCallback } from "react";
import { useAuthState } from "./useAuthState";
import { useAuthActions } from "./useAuthActions";
import { tokenManager } from "../../services/tokenManager";
import { storage } from "../../utils/storage";

export const useAuthManager = () => {
  const {
    authState,
    isInitialized,
    setAuthState,
    setUser,
    setOrganization,
    setInitialized,
  } = useAuthState();

  const { logout, refreshAuth } = useAuthActions();

  // Initialize auth on app start
  const initializeAuth = useCallback(async () => {
    console.log("🔧 AuthManager: Starting initialization...");

    try {
      const user = storage.getUser();
      const organization = storage.getOrganization();
      const token = await tokenManager.getValidToken();

      console.log("🔧 AuthManager: Stored data check:", {
        hasUser: !!user,
        hasOrganization: !!organization,
        hasToken: !!token,
      });

      if (user && organization && token) {
        console.log("🔧 AuthManager: Found valid stored data, verifying...");

        try {
          await refreshAuth();
          console.log("✅ AuthManager: Initialization successful");
        } catch (error) {
          console.log("❌ AuthManager: Verification failed:", error);
          setAuthState("unauthenticated");
        }
      } else {
        console.log(
          "🔧 AuthManager: No valid stored data, user not authenticated"
        );
        setAuthState("unauthenticated");
      }
    } catch (error) {
      console.error("❌ AuthManager: Initialization error:", error);
      setAuthState("unauthenticated");
    } finally {
      setInitialized(true);
    }
  }, [refreshAuth, setAuthState, setInitialized]);

  // Handle token refresh failures
  useEffect(() => {
    const handleTokenRefreshFailed = () => {
      console.log("🔧 AuthManager: Token refresh failed, logging out");
      logout();
    };

    window.addEventListener("token-refresh-failed", handleTokenRefreshFailed);

    return () => {
      window.removeEventListener(
        "token-refresh-failed",
        handleTokenRefreshFailed
      );
    };
  }, [logout]);

  // Initialize on mount
  useEffect(() => {
    if (!isInitialized && authState === "idle") {
      initializeAuth();
    }
  }, [initializeAuth, isInitialized, authState]);

  return {
    isInitialized,
    authState,
  };
};
