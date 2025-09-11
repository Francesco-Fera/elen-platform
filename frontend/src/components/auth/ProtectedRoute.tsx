import React from "react";
import { Navigate, useLocation } from "react-router-dom";
import {
  useAuthState,
  useIsAuthenticated,
  useCurrentUser,
  useCurrentOrganization,
} from "../../hooks/auth";
import { LoadingSpinner } from "../common";
import { ROUTES } from "../../constants";
import { Organization } from "../../types";

interface ProtectedRouteProps {
  children: React.ReactNode;
  requireAuth?: boolean;
  requireEmailVerification?: boolean;
  requireOrganization?: boolean;
  fallback?: React.ReactNode;
  loadingComponent?: React.ReactNode;
  allowedRoles?: string[];
  minRole?: string;
}

const ROLE_HIERARCHY = {
  Viewer: 1,
  Member: 2,
  Admin: 3,
  Owner: 4,
} as const;

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
  children,
  requireAuth = true,
  requireEmailVerification = false,
  requireOrganization = false,
  fallback,
  loadingComponent,
  allowedRoles,
  minRole,
}) => {
  const { authState, isInitialized } = useAuthState();
  const isAuthenticated = useIsAuthenticated();
  const user = useCurrentUser();
  const organization = useCurrentOrganization();
  const location = useLocation();

  // Show loading while initializing
  if (!isInitialized || authState === "loading" || authState === "refreshing") {
    if (loadingComponent) {
      return <>{loadingComponent}</>;
    }

    return (
      <div className='min-h-screen flex items-center justify-center'>
        <LoadingSpinner size='lg' />
      </div>
    );
  }

  // Check authentication requirement
  if (requireAuth && !isAuthenticated) {
    if (fallback) {
      return <>{fallback}</>;
    }

    // Redirect to login page with return url
    return <Navigate to={ROUTES.LOGIN} state={{ from: location }} replace />;
  }

  // Redirect authenticated users away from auth pages
  if (!requireAuth && isAuthenticated) {
    const returnTo = location.state?.from?.pathname || ROUTES.DASHBOARD;
    return <Navigate to={returnTo} replace />;
  }

  // If not requiring auth, render children
  if (!requireAuth) {
    return <>{children}</>;
  }

  // From here, user is authenticated - perform additional checks

  // Check email verification requirement
  if (requireEmailVerification && user && !user.isEmailVerified) {
    return (
      <div className='min-h-screen flex items-center justify-center bg-gray-50'>
        <div className='max-w-md w-full bg-white rounded-lg shadow p-6'>
          <div className='text-center'>
            <div className='mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-warning-100'>
              <svg
                className='h-6 w-6 text-warning-600'
                fill='none'
                stroke='currentColor'
                viewBox='0 0 24 24'
              >
                <path
                  strokeLinecap='round'
                  strokeLinejoin='round'
                  strokeWidth={2}
                  d='M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.34 16.5c-.77.833.192 2.5 1.732 2.5z'
                />
              </svg>
            </div>
            <h3 className='mt-2 text-lg font-medium text-gray-900'>
              Email Verification Required
            </h3>
            <p className='mt-1 text-sm text-gray-500'>
              Please verify your email address to access this feature.
            </p>
            <p className='mt-2 text-sm text-gray-600'>
              Check your inbox for a verification email sent to{" "}
              <span className='font-medium'>{user.email}</span>
            </p>
            <div className='mt-4 flex space-x-3 justify-center'>
              <button
                onClick={() => {
                  // Trigger resend verification email
                  console.log("Resending verification email...");
                }}
                className='btn btn-primary btn-sm'
              >
                Resend Email
              </button>
              <button
                onClick={() => {
                  // Refresh user data
                  window.location.reload();
                }}
                className='btn btn-secondary btn-sm'
              >
                I've Verified
              </button>
            </div>
          </div>
        </div>
      </div>
    );
  }

  // Check organization requirement
  if (requireOrganization && !organization) {
    return (
      <div className='min-h-screen flex items-center justify-center bg-gray-50'>
        <div className='max-w-md w-full bg-white rounded-lg shadow p-6'>
          <div className='text-center'>
            <div className='mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-blue-100'>
              <svg
                className='h-6 w-6 text-blue-600'
                fill='none'
                stroke='currentColor'
                viewBox='0 0 24 24'
              >
                <path
                  strokeLinecap='round'
                  strokeLinejoin='round'
                  strokeWidth={2}
                  d='M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4'
                />
              </svg>
            </div>
            <h3 className='mt-2 text-lg font-medium text-gray-900'>
              Organization Required
            </h3>
            <p className='mt-1 text-sm text-gray-500'>
              You need to be part of an organization to access this feature.
            </p>
            <div className='mt-4'>
              <button
                onClick={() => {
                  // Navigate to organization creation/join page
                  window.location.href = ROUTES.ORGANIZATION;
                }}
                className='btn btn-primary'
              >
                Join or Create Organization
              </button>
            </div>
          </div>
        </div>
      </div>
    );
  }

  // Check role-based access
  if (allowedRoles && organization) {
    // Get user's role in current organization
    const userRole = getUserRoleInOrganization(user?.id, organization);

    if (!userRole || !allowedRoles.includes(userRole)) {
      return (
        <div className='min-h-screen flex items-center justify-center bg-gray-50'>
          <div className='max-w-md w-full bg-white rounded-lg shadow p-6'>
            <div className='text-center'>
              <div className='mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-error-100'>
                <svg
                  className='h-6 w-6 text-error-600'
                  fill='none'
                  stroke='currentColor'
                  viewBox='0 0 24 24'
                >
                  <path
                    strokeLinecap='round'
                    strokeLinejoin='round'
                    strokeWidth={2}
                    d='M12 15v2m-6 0h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z'
                  />
                </svg>
              </div>
              <h3 className='mt-2 text-lg font-medium text-gray-900'>
                Access Denied
              </h3>
              <p className='mt-1 text-sm text-gray-500'>
                You don't have the required permissions to access this page.
              </p>
              <p className='mt-2 text-sm text-gray-600'>
                Required roles: {allowedRoles.join(", ")}
              </p>
              <div className='mt-4'>
                <button
                  onClick={() => window.history.back()}
                  className='btn btn-secondary'
                >
                  Go Back
                </button>
              </div>
            </div>
          </div>
        </div>
      );
    }
  }

  // Check minimum role requirement
  if (minRole && organization) {
    const userRole = getUserRoleInOrganization(user?.id, organization);

    if (!userRole || !hasMinimumRole(userRole, minRole)) {
      return (
        <div className='min-h-screen flex items-center justify-center bg-gray-50'>
          <div className='max-w-md w-full bg-white rounded-lg shadow p-6'>
            <div className='text-center'>
              <div className='mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-error-100'>
                <svg
                  className='h-6 w-6 text-error-600'
                  fill='none'
                  stroke='currentColor'
                  viewBox='0 0 24 24'
                >
                  <path
                    strokeLinecap='round'
                    strokeLinejoin='round'
                    strokeWidth={2}
                    d='M12 15v2m-6 0h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z'
                  />
                </svg>
              </div>
              <h3 className='mt-2 text-lg font-medium text-gray-900'>
                Insufficient Permissions
              </h3>
              <p className='mt-1 text-sm text-gray-500'>
                You need {minRole} level access or higher for this page.
              </p>
              <p className='mt-2 text-sm text-gray-600'>
                Your current role: {userRole}
              </p>
              <div className='mt-4'>
                <button
                  onClick={() => window.history.back()}
                  className='btn btn-secondary'
                >
                  Go Back
                </button>
              </div>
            </div>
          </div>
        </div>
      );
    }
  }

  // All checks passed, render children
  return <>{children}</>;
};

// Helper functions
function getUserRoleInOrganization(
  userId?: string,
  organization?: Organization
): string | null {
  // This would typically come from organization members data
  // For now, we'll return a default or get it from stored organization data
  // In a real app, this might be stored in the user object or fetched separately
  return "Member"; // Placeholder
}

function hasMinimumRole(userRole: string, requiredRole: string): boolean {
  const userLevel =
    ROLE_HIERARCHY[userRole as keyof typeof ROLE_HIERARCHY] || 0;
  const requiredLevel =
    ROLE_HIERARCHY[requiredRole as keyof typeof ROLE_HIERARCHY] || 0;

  return userLevel >= requiredLevel;
}
