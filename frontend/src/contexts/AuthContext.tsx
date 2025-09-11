import React, { createContext, useContext, useReducer, useEffect } from "react";
import { storage } from "../utils/storage";
import type {
  User,
  Organization,
  LoginRequest,
  RegisterRequest,
  AuthTokens,
} from "../types";
import { authService } from "../services/api/authService";

interface AuthState {
  user: User | null;
  organization: Organization | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}

type AuthAction =
  | { type: "AUTH_START" }
  | {
      type: "AUTH_SUCCESS";
      payload: { user: User; organization: Organization };
    }
  | { type: "AUTH_FAILURE"; payload: string }
  | { type: "LOGOUT" }
  | { type: "UPDATE_USER"; payload: User }
  | { type: "UPDATE_ORGANIZATION"; payload: Organization }
  | { type: "CLEAR_ERROR" };

const initialState: AuthState = {
  user: null,
  organization: null,
  isAuthenticated: false,
  isLoading: true,
  error: null,
};

function authReducer(state: AuthState, action: AuthAction): AuthState {
  switch (action.type) {
    case "AUTH_START":
      return {
        ...state,
        isLoading: true,
        error: null,
      };
    case "AUTH_SUCCESS":
      return {
        ...state,
        user: action.payload.user,
        organization: action.payload.organization,
        isAuthenticated: true,
        isLoading: false,
        error: null,
      };
    case "AUTH_FAILURE":
      return {
        ...state,
        user: null,
        organization: null,
        isAuthenticated: false,
        isLoading: false,
        error: action.payload,
      };
    case "LOGOUT":
      return {
        ...state,
        user: null,
        organization: null,
        isAuthenticated: false,
        isLoading: false,
        error: null,
      };
    case "UPDATE_USER":
      return {
        ...state,
        user: action.payload,
      };
    case "UPDATE_ORGANIZATION":
      return {
        ...state,
        organization: action.payload,
      };
    case "CLEAR_ERROR":
      return {
        ...state,
        error: null,
      };
    default:
      return state;
  }
}

interface AuthContextType extends AuthState {
  login: (credentials: LoginRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => void;
  refreshAuth: () => Promise<void>;
  updateUser: (user: User) => void;
  updateOrganization: (organization: Organization) => void;
  clearError: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
};

interface AuthProviderProps {
  children: React.ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [state, dispatch] = useReducer(authReducer, initialState);

  // Initialize auth on app start
  useEffect(() => {
    const initializeAuth = async () => {
      console.log("🔧 AuthContext: Starting initialization...");
      dispatch({ type: "AUTH_START" });

      try {
        const user = storage.getUser();
        const organization = storage.getOrganization();
        const accessToken = storage.getAccessToken();

        console.log("🔧 AuthContext: Stored data check:", {
          hasUser: !!user,
          hasOrganization: !!organization,
          hasToken: !!accessToken,
          user: user,
          organization: organization,
          tokenLength: accessToken?.length,
        });

        if (user && organization && accessToken) {
          console.log(
            "🔧 AuthContext: Found stored data, verifying with API..."
          );

          // Verify token is still valid by making a profile request
          try {
            const response = await authService.getProfile();
            console.log("🔧 AuthContext: Profile API response:", response);

            if (response.success && response.data) {
              console.log("✅ AuthContext: Token is valid, updating state...");
              dispatch({
                type: "AUTH_SUCCESS",
                payload: {
                  user: response.data.user,
                  organization: response.data.organization,
                },
              });

              // Update stored data in case it changed
              storage.setUser(response.data.user);
              storage.setOrganization(response.data.organization);
              return;
            } else {
              console.log(
                "❌ AuthContext: API response not successful:",
                response
              );
            }
          } catch (apiError) {
            console.log("❌ AuthContext: API call failed:", apiError);
          }
        } else {
          console.log(
            "🔧 AuthContext: Missing stored data, redirecting to login..."
          );
        }

        // If we get here, auth failed
        console.log(
          "🔧 AuthContext: Authentication failed, clearing storage..."
        );
        storage.clearAll();
        dispatch({ type: "AUTH_FAILURE", payload: "" });
      } catch (error) {
        console.log("❌ AuthContext: Initialization error:", error);
        storage.clearAll();
        dispatch({ type: "AUTH_FAILURE", payload: "" });
      }
    };

    initializeAuth();
  }, []);

  const login = async (credentials: LoginRequest) => {
    dispatch({ type: "AUTH_START" });

    try {
      const response = await authService.login(credentials);

      if (response.success && response.data) {
        const { user, organization, accessToken, refreshToken } = response.data;

        // Create tokens object from the flat response structure
        const tokens: AuthTokens = {
          accessToken,
          refreshToken,
          expiresAt: new Date(Date.now() + 15 * 60 * 1000).toISOString(), // 15 minutes from now
        };

        // Store tokens and user data
        storage.setTokens(tokens);
        storage.setUser(user);
        storage.setOrganization(organization);

        dispatch({
          type: "AUTH_SUCCESS",
          payload: { user, organization },
        });
      } else {
        throw new Error(response.message || "Login failed");
      }
    } catch (error) {
      const errorMessage =
        error instanceof Error ? error.message : "Login failed";
      dispatch({ type: "AUTH_FAILURE", payload: errorMessage });
      throw error;
    }
  };

  const register = async (data: RegisterRequest) => {
    dispatch({ type: "AUTH_START" });

    try {
      const response = await authService.register(data);

      if (response.success && response.data) {
        const { user, organization, accessToken, refreshToken } = response.data;

        const tokens: AuthTokens = {
          accessToken,
          refreshToken,
          expiresAt: new Date(Date.now() + 15 * 60 * 1000).toISOString(), // 15 minutes from now
        };

        // Store tokens and user data
        storage.setTokens(tokens);
        storage.setUser(user);
        storage.setOrganization(organization);

        dispatch({
          type: "AUTH_SUCCESS",
          payload: { user, organization },
        });
      } else {
        throw new Error(response.message || "Registration failed");
      }
    } catch (error) {
      const errorMessage =
        error instanceof Error ? error.message : "Registration failed";
      dispatch({ type: "AUTH_FAILURE", payload: errorMessage });
      throw error;
    }
  };

  const logout = async () => {
    try {
      await authService.logout();
    } catch (error) {
      // Continue with logout even if API call fails
      console.error("Logout API call failed:", error);
    } finally {
      storage.clearAll();
      dispatch({ type: "LOGOUT" });
    }
  };

  const refreshAuth = async () => {
    try {
      const response = await authService.getProfile();
      if (response.success && response.data) {
        storage.setUser(response.data.user);
        storage.setOrganization(response.data.organization);

        dispatch({
          type: "AUTH_SUCCESS",
          payload: {
            user: response.data.user,
            organization: response.data.organization,
          },
        });
      }
    } catch (error) {
      console.error("Failed to refresh auth:", error);
    }
  };

  const updateUser = (user: User) => {
    storage.setUser(user);
    dispatch({ type: "UPDATE_USER", payload: user });
  };

  const updateOrganization = (organization: Organization) => {
    storage.setOrganization(organization);
    dispatch({ type: "UPDATE_ORGANIZATION", payload: organization });
  };

  const clearError = () => {
    dispatch({ type: "CLEAR_ERROR" });
  };

  const value: AuthContextType = {
    ...state,
    login,
    register,
    logout,
    refreshAuth,
    updateUser,
    updateOrganization,
    clearError,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};
