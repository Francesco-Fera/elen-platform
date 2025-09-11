// frontend/src/pages/dashboard/DashboardPage.tsx

import React, { useState, useEffect } from "react";
import { useAuth } from "../../contexts/AuthProvider";
import { useAuthState } from "../../hooks/auth";
import { tokenManager } from "../../services/tokenManager";
import { Button } from "../../components/common";
import { formatUtils } from "../../utils/format";

interface SystemStats {
  tokenInfo: {
    isValid: boolean;
    expiresAt?: Date;
    expiresIn?: number;
  };
  storageSize: number;
  authState: string;
  isInitialized: boolean;
}

export const DashboardPage: React.FC = () => {
  const { user, organization, logout, isLoading } = useAuth();
  const { authState, loading } = useAuthState();
  const [systemStats, setSystemStats] = useState<SystemStats | null>(null);
  const [showDebugInfo, setShowDebugInfo] = useState(false);

  // Load system stats
  useEffect(() => {
    const loadStats = () => {
      const tokenInfo = tokenManager.getTokenInfo();
      const storageSize = calculateStorageSize();

      setSystemStats({
        tokenInfo,
        storageSize,
        authState,
        isInitialized: true,
      });
    };

    loadStats();
    const interval = setInterval(loadStats, 5000); // Update every 5 seconds

    return () => clearInterval(interval);
  }, [authState]);

  const calculateStorageSize = (): number => {
    try {
      let total = 0;
      for (const key in localStorage) {
        if (localStorage.hasOwnProperty(key)) {
          total += localStorage[key].length + key.length;
        }
      }
      return total;
    } catch (error) {
      return 0;
    }
  };

  const handleLogout = async () => {
    try {
      await logout();
    } catch (error) {
      console.error("Logout error:", error);
    }
  };

  const formatTokenExpiry = (expiresIn?: number): string => {
    if (!expiresIn) return "Unknown";

    if (expiresIn < 60) return `${expiresIn}s`;
    if (expiresIn < 3600)
      return `${Math.floor(expiresIn / 60)}m ${expiresIn % 60}s`;
    return `${Math.floor(expiresIn / 3600)}h ${Math.floor((expiresIn % 3600) / 60)}m`;
  };

  const getStatusColor = (status: string): string => {
    switch (status) {
      case "authenticated":
        return "text-success-600 bg-success-100";
      case "loading":
      case "refreshing":
        return "text-warning-600 bg-warning-100";
      case "unauthenticated":
        return "text-error-600 bg-error-100";
      default:
        return "text-gray-600 bg-gray-100";
    }
  };

  const getPlanColor = (plan: string): string => {
    switch (plan) {
      case "Free":
        return "bg-gray-100 text-gray-800";
      case "Pro":
        return "bg-primary-100 text-primary-800";
      case "Enterprise":
        return "bg-purple-100 text-purple-800";
      default:
        return "bg-gray-100 text-gray-800";
    }
  };

  if (isLoading) {
    return (
      <div className='min-h-screen flex items-center justify-center'>
        <div className='text-center'>
          <div className='animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600 mx-auto'></div>
          <p className='mt-4 text-gray-600'>Loading dashboard...</p>
        </div>
      </div>
    );
  }

  return (
    <div className='min-h-screen bg-gray-50'>
      {/* Header */}
      <header className='bg-white shadow-sm border-b border-gray-200'>
        <div className='max-w-7xl mx-auto px-4 sm:px-6 lg:px-8'>
          <div className='flex justify-between items-center py-6'>
            <div className='flex items-center space-x-4'>
              <div className='flex items-center'>
                <div className='h-8 w-8 bg-gradient-to-r from-primary-600 to-purple-600 rounded-lg flex items-center justify-center'>
                  <svg
                    className='h-5 w-5 text-white'
                    fill='currentColor'
                    viewBox='0 0 20 20'
                  >
                    <path
                      fillRule='evenodd'
                      d='M11.3 1.046A1 1 0 0112 2v5h4a1 1 0 01.82 1.573l-7 10A1 1 0 018 18v-5H4a1 1 0 01-.82-1.573l7-10a1 1 0 011.12-.38z'
                      clipRule='evenodd'
                    />
                  </svg>
                </div>
                <h1 className='ml-3 text-2xl font-bold text-gradient'>
                  WorkflowEngine
                </h1>
              </div>

              <div
                className={`px-3 py-1 rounded-full text-xs font-medium ${getStatusColor(authState)}`}
              >
                {authState.charAt(0).toUpperCase() + authState.slice(1)}
              </div>
            </div>

            <div className='flex items-center space-x-4'>
              <div className='hidden sm:block text-sm text-gray-600'>
                <span className='font-medium'>
                  {user?.firstName} {user?.lastName}
                </span>
                {organization && (
                  <>
                    <span className='mx-2'>•</span>
                    <span>{organization.name}</span>
                  </>
                )}
              </div>

              <button
                onClick={() => setShowDebugInfo(!showDebugInfo)}
                className='p-2 text-gray-400 hover:text-gray-600 rounded-md'
                title='Toggle Debug Info'
              >
                <svg
                  className='w-5 h-5'
                  fill='none'
                  stroke='currentColor'
                  viewBox='0 0 24 24'
                >
                  <path
                    strokeLinecap='round'
                    strokeLinejoin='round'
                    strokeWidth={2}
                    d='M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z'
                  />
                </svg>
              </button>

              <Button variant='secondary' size='sm' onClick={handleLogout}>
                Logout
              </Button>
            </div>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className='max-w-7xl mx-auto py-6 sm:px-6 lg:px-8'>
        <div className='px-4 py-6 sm:px-0'>
          {/* Welcome Section */}
          <div className='mb-8'>
            <h2 className='text-3xl font-bold text-gray-900'>
              Welcome back, {user?.firstName}! 👋
            </h2>
            <p className='mt-2 text-gray-600'>
              Here's what's happening with your account and organization.
            </p>
          </div>

          {/* Stats Grid */}
          <div className='grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8'>
            <div className='bg-white rounded-lg shadow p-6'>
              <div className='flex items-center'>
                <div className='flex-shrink-0'>
                  <div className='h-8 w-8 bg-blue-100 rounded-md flex items-center justify-center'>
                    <svg
                      className='h-5 w-5 text-blue-600'
                      fill='none'
                      stroke='currentColor'
                      viewBox='0 0 24 24'
                    >
                      <path
                        strokeLinecap='round'
                        strokeLinejoin='round'
                        strokeWidth={2}
                        d='M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z'
                      />
                    </svg>
                  </div>
                </div>
                <div className='ml-4'>
                  <p className='text-sm font-medium text-gray-500'>
                    Account Status
                  </p>
                  <p className='text-lg font-semibold text-gray-900'>
                    {user?.isEmailVerified ? "Verified" : "Pending"}
                  </p>
                </div>
              </div>
            </div>

            <div className='bg-white rounded-lg shadow p-6'>
              <div className='flex items-center'>
                <div className='flex-shrink-0'>
                  <div className='h-8 w-8 bg-green-100 rounded-md flex items-center justify-center'>
                    <svg
                      className='h-5 w-5 text-green-600'
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
                </div>
                <div className='ml-4'>
                  <p className='text-sm font-medium text-gray-500'>
                    Organization
                  </p>
                  <p className='text-lg font-semibold text-gray-900'>
                    {organization?.isActive ? "Active" : "Inactive"}
                  </p>
                </div>
              </div>
            </div>

            <div className='bg-white rounded-lg shadow p-6'>
              <div className='flex items-center'>
                <div className='flex-shrink-0'>
                  <div className='h-8 w-8 bg-purple-100 rounded-md flex items-center justify-center'>
                    <svg
                      className='h-5 w-5 text-purple-600'
                      fill='none'
                      stroke='currentColor'
                      viewBox='0 0 24 24'
                    >
                      <path
                        strokeLinecap='round'
                        strokeLinejoin='round'
                        strokeWidth={2}
                        d='M9 12l2 2 4-4M7.835 4.697a3.42 3.42 0 001.946-.806 3.42 3.42 0 014.438 0 3.42 3.42 0 001.946.806 3.42 3.42 0 013.138 3.138 3.42 3.42 0 00.806 1.946 3.42 3.42 0 010 4.438 3.42 3.42 0 00-.806 1.946 3.42 3.42 0 01-3.138 3.138 3.42 3.42 0 00-1.946.806 3.42 3.42 0 01-4.438 0 3.42 3.42 0 00-1.946-.806 3.42 3.42 0 01-3.138-3.138 3.42 3.42 0 00-.806-1.946 3.42 3.42 0 010-4.438 3.42 3.42 0 00.806-1.946 3.42 3.42 0 013.138-3.138z'
                      />
                    </svg>
                  </div>
                </div>
                <div className='ml-4'>
                  <p className='text-sm font-medium text-gray-500'>
                    Subscription
                  </p>
                  <p className='text-lg font-semibold text-gray-900'>
                    {organization?.subscriptionPlan || "Free"}
                  </p>
                </div>
              </div>
            </div>

            <div className='bg-white rounded-lg shadow p-6'>
              <div className='flex items-center'>
                <div className='flex-shrink-0'>
                  <div className='h-8 w-8 bg-yellow-100 rounded-md flex items-center justify-center'>
                    <svg
                      className='h-5 w-5 text-yellow-600'
                      fill='none'
                      stroke='currentColor'
                      viewBox='0 0 24 24'
                    >
                      <path
                        strokeLinecap='round'
                        strokeLinejoin='round'
                        strokeWidth={2}
                        d='M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z'
                      />
                    </svg>
                  </div>
                </div>
                <div className='ml-4'>
                  <p className='text-sm font-medium text-gray-500'>
                    Token Expires
                  </p>
                  <p className='text-lg font-semibold text-gray-900'>
                    {systemStats
                      ? formatTokenExpiry(systemStats.tokenInfo.expiresIn)
                      : "Loading..."}
                  </p>
                </div>
              </div>
            </div>
          </div>

          {/* Main Cards */}
          <div className='grid grid-cols-1 lg:grid-cols-2 gap-8'>
            {/* User Information Card */}
            <div className='card'>
              <div className='card-header'>
                <h3 className='text-lg font-medium text-gray-900 flex items-center'>
                  <svg
                    className='w-5 h-5 mr-2 text-blue-600'
                    fill='none'
                    stroke='currentColor'
                    viewBox='0 0 24 24'
                  >
                    <path
                      strokeLinecap='round'
                      strokeLinejoin='round'
                      strokeWidth={2}
                      d='M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z'
                    />
                  </svg>
                  User Information
                </h3>
              </div>
              <div className='card-body'>
                <div className='space-y-4'>
                  <div className='grid grid-cols-2 gap-4'>
                    <div>
                      <p className='text-sm font-medium text-gray-500'>Email</p>
                      <p className='text-sm text-gray-900'>{user?.email}</p>
                    </div>
                    <div>
                      <p className='text-sm font-medium text-gray-500'>Name</p>
                      <p className='text-sm text-gray-900'>
                        {user?.firstName} {user?.lastName}
                      </p>
                    </div>
                    <div>
                      <p className='text-sm font-medium text-gray-500'>
                        Email Status
                      </p>
                      <div className='flex items-center'>
                        {user?.isEmailVerified ? (
                          <span className='inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-success-100 text-success-800'>
                            <svg
                              className='w-3 h-3 mr-1'
                              fill='currentColor'
                              viewBox='0 0 20 20'
                            >
                              <path
                                fillRule='evenodd'
                                d='M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z'
                                clipRule='evenodd'
                              />
                            </svg>
                            Verified
                          </span>
                        ) : (
                          <span className='inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-warning-100 text-warning-800'>
                            <svg
                              className='w-3 h-3 mr-1'
                              fill='currentColor'
                              viewBox='0 0 20 20'
                            >
                              <path
                                fillRule='evenodd'
                                d='M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z'
                                clipRule='evenodd'
                              />
                            </svg>
                            Pending
                          </span>
                        )}
                      </div>
                    </div>
                    <div>
                      <p className='text-sm font-medium text-gray-500'>
                        Member Since
                      </p>
                      <p className='text-sm text-gray-900'>
                        {user?.createdAt
                          ? formatUtils.formatDate(user.createdAt)
                          : "Unknown"}
                      </p>
                    </div>
                    <div>
                      <p className='text-sm font-medium text-gray-500'>
                        Time Zone
                      </p>
                      <p className='text-sm text-gray-900'>
                        {user?.timeZone || "UTC"}
                      </p>
                    </div>
                    <div>
                      <p className='text-sm font-medium text-gray-500'>
                        User ID
                      </p>
                      <p className='text-xs text-gray-500 font-mono'>
                        {user?.id}
                      </p>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Organization Information Card */}
            <div className='card'>
              <div className='card-header'>
                <h3 className='text-lg font-medium text-gray-900 flex items-center'>
                  <svg
                    className='w-5 h-5 mr-2 text-purple-600'
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
                  Organization Information
                </h3>
              </div>
              <div className='card-body'>
                {organization ? (
                  <div className='space-y-4'>
                    <div className='grid grid-cols-2 gap-4'>
                      <div>
                        <p className='text-sm font-medium text-gray-500'>
                          Name
                        </p>
                        <p className='text-sm text-gray-900'>
                          {organization.name}
                        </p>
                      </div>
                      <div>
                        <p className='text-sm font-medium text-gray-500'>
                          Plan
                        </p>
                        <span
                          className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${getPlanColor(organization.subscriptionPlan)}`}
                        >
                          {organization.subscriptionPlan}
                        </span>
                      </div>
                      <div>
                        <p className='text-sm font-medium text-gray-500'>
                          Max Users
                        </p>
                        <p className='text-sm text-gray-900'>
                          {organization.maxUsers}
                        </p>
                      </div>
                      <div>
                        <p className='text-sm font-medium text-gray-500'>
                          Max Workflows
                        </p>
                        <p className='text-sm text-gray-900'>
                          {organization.maxWorkflows}
                        </p>
                      </div>
                      <div>
                        <p className='text-sm font-medium text-gray-500'>
                          Status
                        </p>
                        <div className='flex items-center'>
                          {organization.isActive ? (
                            <span className='inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-success-100 text-success-800'>
                              ✓ Active
                            </span>
                          ) : (
                            <span className='inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-error-100 text-error-800'>
                              ✗ Inactive
                            </span>
                          )}
                        </div>
                      </div>
                      <div>
                        <p className='text-sm font-medium text-gray-500'>
                          Account Type
                        </p>
                        <div className='flex items-center'>
                          {organization.isTrialAccount ? (
                            <span className='inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-warning-100 text-warning-800'>
                              Trial
                              {organization.trialExpiresAt && (
                                <span className='ml-1'>
                                  (expires{" "}
                                  {formatUtils.formatDate(
                                    organization.trialExpiresAt
                                  )}
                                  )
                                </span>
                              )}
                            </span>
                          ) : (
                            <span className='inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-success-100 text-success-800'>
                              Full Account
                            </span>
                          )}
                        </div>
                      </div>
                    </div>

                    {organization.description && (
                      <div>
                        <p className='text-sm font-medium text-gray-500'>
                          Description
                        </p>
                        <p className='text-sm text-gray-900'>
                          {organization.description}
                        </p>
                      </div>
                    )}

                    <div>
                      <p className='text-sm font-medium text-gray-500'>
                        Organization ID
                      </p>
                      <p className='text-xs text-gray-500 font-mono'>
                        {organization.id}
                      </p>
                    </div>
                  </div>
                ) : (
                  <div className='text-center py-4'>
                    <p className='text-sm text-gray-500'>
                      No organization information available
                    </p>
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* Debug Information (Toggle) */}
          {showDebugInfo && systemStats && (
            <div className='mt-8'>
              <div className='card'>
                <div className='card-header'>
                  <h3 className='text-lg font-medium text-gray-900 flex items-center'>
                    <svg
                      className='w-5 h-5 mr-2 text-gray-600'
                      fill='none'
                      stroke='currentColor'
                      viewBox='0 0 24 24'
                    >
                      <path
                        strokeLinecap='round'
                        strokeLinejoin='round'
                        strokeWidth={2}
                        d='M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z'
                      />
                    </svg>
                    Debug Information
                    <span className='ml-2 text-xs bg-gray-100 text-gray-600 px-2 py-1 rounded'>
                      Development Only
                    </span>
                  </h3>
                </div>
                <div className='card-body'>
                  <div className='grid grid-cols-1 md:grid-cols-3 gap-6'>
                    <div>
                      <h4 className='font-medium text-gray-900 mb-2'>
                        Authentication State
                      </h4>
                      <div className='space-y-2 text-sm'>
                        <div className='flex justify-between'>
                          <span>Auth State:</span>
                          <span className='font-mono'>
                            {systemStats.authState}
                          </span>
                        </div>
                        <div className='flex justify-between'>
                          <span>Initialized:</span>
                          <span className='font-mono'>
                            {systemStats.isInitialized ? "Yes" : "No"}
                          </span>
                        </div>
                        <div className='flex justify-between'>
                          <span>Loading States:</span>
                          <span className='font-mono'>
                            {JSON.stringify(loading)}
                          </span>
                        </div>
                      </div>
                    </div>

                    <div>
                      <h4 className='font-medium text-gray-900 mb-2'>
                        Token Information
                      </h4>
                      <div className='space-y-2 text-sm'>
                        <div className='flex justify-between'>
                          <span>Valid:</span>
                          <span className='font-mono'>
                            {systemStats.tokenInfo.isValid ? "Yes" : "No"}
                          </span>
                        </div>
                        <div className='flex justify-between'>
                          <span>Expires At:</span>
                          <span className='font-mono text-xs'>
                            {systemStats.tokenInfo.expiresAt?.toLocaleTimeString() ||
                              "Unknown"}
                          </span>
                        </div>
                        <div className='flex justify-between'>
                          <span>Expires In:</span>
                          <span className='font-mono'>
                            {formatTokenExpiry(systemStats.tokenInfo.expiresIn)}
                          </span>
                        </div>
                      </div>
                    </div>

                    <div>
                      <h4 className='font-medium text-gray-900 mb-2'>
                        System Information
                      </h4>
                      <div className='space-y-2 text-sm'>
                        <div className='flex justify-between'>
                          <span>Storage Size:</span>
                          <span className='font-mono'>
                            {(systemStats.storageSize / 1024).toFixed(1)} KB
                          </span>
                        </div>
                        <div className='flex justify-between'>
                          <span>Environment:</span>
                          <span className='font-mono'>
                            {process.env.NODE_ENV}
                          </span>
                        </div>
                        <div className='flex justify-between'>
                          <span>API URL:</span>
                          <span className='font-mono text-xs'>
                            {process.env.REACT_APP_API_URL || "default"}
                          </span>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* Quick Actions */}
          <div className='mt-8'>
            <div className='card'>
              <div className='card-header'>
                <h3 className='text-lg font-medium text-gray-900'>
                  Quick Actions
                </h3>
              </div>
              <div className='card-body'>
                <div className='grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4'>
                  <Button variant='primary' className='w-full'>
                    <svg
                      className='w-4 h-4 mr-2'
                      fill='none'
                      stroke='currentColor'
                      viewBox='0 0 24 24'
                    >
                      <path
                        strokeLinecap='round'
                        strokeLinejoin='round'
                        strokeWidth={2}
                        d='M12 6v6m0 0v6m0-6h6m-6 0H6'
                      />
                    </svg>
                    Create Workflow
                  </Button>

                  <Button variant='secondary' className='w-full'>
                    <svg
                      className='w-4 h-4 mr-2'
                      fill='none'
                      stroke='currentColor'
                      viewBox='0 0 24 24'
                    >
                      <path
                        strokeLinecap='round'
                        strokeLinejoin='round'
                        strokeWidth={2}
                        d='M9 5H7a2 2 0 00-2 2v10a2 2 0 002 2h8a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2'
                      />
                    </svg>
                    View Workflows
                  </Button>

                  <Button variant='secondary' className='w-full'>
                    <svg
                      className='w-4 h-4 mr-2'
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
                    Manage Organization
                  </Button>

                  <Button variant='secondary' className='w-full'>
                    <svg
                      className='w-4 h-4 mr-2'
                      fill='none'
                      stroke='currentColor'
                      viewBox='0 0 24 24'
                    >
                      <path
                        strokeLinecap='round'
                        strokeLinejoin='round'
                        strokeWidth={2}
                        d='M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z'
                      />
                    </svg>
                    Edit Profile
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
};
