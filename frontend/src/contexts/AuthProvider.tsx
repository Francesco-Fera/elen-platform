import React, { createContext, useContext, useEffect } from "react";
import { useAuthManager, useAuthActions, useAuthState } from "../hooks/auth";
import type {
  LoginRequest,
  RegisterRequest,
  User,
  Organization,
} from "../types";

// Simplified context - just provides the actions
interface AuthContextType {
  // State
  user: User | null;
  organization: Organization | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
  isInitialized: boolean;

  // Actions
  login: (credentials: LoginRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
  refreshAuth: () => Promise<void>;
  clearError: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = (): AuthContextType => {
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
  // Use the separated hooks
  const { isInitialized } = useAuthManager();
  const { login, register, logout, refreshAuth, clearError } = useAuthActions();
  const {
    user,
    organization,
    authState,
    error: authError,
    loading,
  } = useAuthState();

  // Derive computed values
  const isAuthenticated = authState === "authenticated";
  const isLoading =
    Object.values(loading).some(Boolean) || authState === "loading";
  const error = authError?.message || null;

  // Log state changes for debugging
  useEffect(() => {
    console.log("🔧 AuthProvider: State changed", {
      authState,
      isAuthenticated,
      hasUser: !!user,
      hasOrganization: !!organization,
      isInitialized,
    });
  }, [authState, isAuthenticated, user, organization, isInitialized]);

  const value: AuthContextType = {
    // State
    user,
    organization,
    isAuthenticated,
    isLoading,
    error,
    isInitialized,

    // Actions
    login,
    register,
    logout,
    refreshAuth,
    clearError,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

// HOC for class components (if needed)
export const withAuth = <P extends object>(
  Component: React.ComponentType<P>
): React.FC<P> => {
  return (props: P) => {
    const auth = useAuth();
    return <Component {...props} auth={auth} />;
  };
};

// Higher-order component for role-based access
export const withRequiredRole = <P extends object>(
  Component: React.ComponentType<P>,
  requiredRole: string
) => {
  return (props: P) => {
    const { user, organization, isAuthenticated } = useAuth();

    if (!isAuthenticated) {
      return <div>Please log in to access this feature.</div>;
    }

    // Check if user has required role
    const userRole = getUserRole(user, organization);
    if (!hasRequiredRole(userRole, requiredRole)) {
      return (
        <div className='text-center p-8'>
          <h3 className='text-lg font-medium text-gray-900'>Access Denied</h3>
          <p className='mt-2 text-sm text-gray-600'>
            You need {requiredRole} access to view this content.
          </p>
        </div>
      );
    }

    return <Component {...props} />;
  };
};

// Helper functions
function getUserRole(
  user: User | null,
  organization: Organization | null
): string | null {
  // This would typically be stored in the user object or organization membership
  // For now, return a default role
  return "Member";
}

function hasRequiredRole(
  userRole: string | null,
  requiredRole: string
): boolean {
  if (!userRole) return false;

  const roleHierarchy = {
    Viewer: 1,
    Member: 2,
    Admin: 3,
    Owner: 4,
  };

  const userLevel = roleHierarchy[userRole as keyof typeof roleHierarchy] || 0;
  const requiredLevel =
    roleHierarchy[requiredRole as keyof typeof roleHierarchy] || 0;

  return userLevel >= requiredLevel;
}
