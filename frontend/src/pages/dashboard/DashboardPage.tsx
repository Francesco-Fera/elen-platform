import React from "react";
import { useAuth } from "../../contexts/AuthProvider";
import { Card, Button } from "../../components/common";
import { DashboardLayout } from "../../components/dashboard/DashboardLayout";

export const DashboardPage: React.FC = () => {
  const { user, organization, logout, isLoading } = useAuth();

  const handleLogout = async () => {
    try {
      await logout();
    } catch (error) {
      console.error("Logout failed:", error);
    }
  };

  if (isLoading) {
    return (
      <DashboardLayout>
        <div className='min-h-screen flex items-center justify-center'>
          <div className='text-center'>
            <div className='animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600 mx-auto'></div>
            <p className='mt-2 text-gray-600'>Loading dashboard...</p>
          </div>
        </div>
      </DashboardLayout>
    );
  }

  return (
    <DashboardLayout>
      {/* Header */}
      <header className='bg-white shadow-sm border-b border-gray-200'>
        <div className='max-w-7xl mx-auto px-4 sm:px-6 lg:px-8'>
          <div className='flex justify-between items-center h-16'>
            <div className='flex items-center'>
              <div className='flex-shrink-0'>
                <div className='w-8 h-8 bg-primary-600 rounded-lg flex items-center justify-center'>
                  <svg
                    className='w-5 h-5 text-white'
                    fill='none'
                    stroke='currentColor'
                    viewBox='0 0 24 24'
                  >
                    <path
                      strokeLinecap='round'
                      strokeLinejoin='round'
                      strokeWidth={2}
                      d='M13 10V3L4 14h7v7l9-11h-7z'
                    />
                  </svg>
                </div>
              </div>
              <div className='ml-3'>
                <h1 className='text-lg font-semibold text-gray-900'>
                  WorkflowEngine
                </h1>
              </div>
            </div>

            <div className='flex items-center space-x-4'>
              <div className='text-sm text-gray-700'>
                Welcome, <span className='font-medium'>{user?.firstName}</span>
              </div>
              <Button
                variant='ghost'
                onClick={handleLogout}
                className='text-gray-500 hover:text-gray-700'
              >
                Sign out
              </Button>
            </div>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className='max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8'>
        {/* Welcome Section */}
        <div className='mb-8'>
          <h2 className='text-2xl font-bold text-gray-900 mb-2'>
            Welcome to your Dashboard
          </h2>
          <p className='text-gray-600'>
            Manage your workflows and automation from here.
          </p>
        </div>

        {/* Email Verification Status Card - Only show if not verified */}
        {user && !user.isEmailVerified && (
          <Card className='mb-8 border-yellow-200 bg-yellow-50'>
            <Card.Body>
              <div className='flex items-start'>
                <div className='flex-shrink-0'>
                  <svg
                    className='w-6 h-6 text-yellow-600'
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
                <div className='ml-3 flex-1'>
                  <h3 className='text-sm font-medium text-yellow-800'>
                    Email Verification Required
                  </h3>
                  <div className='mt-1 text-sm text-yellow-700'>
                    <p>
                      Some features may be limited until you verify your email
                      address. Check your inbox for a verification link, or
                      request a new one.
                    </p>
                  </div>
                </div>
              </div>
            </Card.Body>
          </Card>
        )}

        {/* Stats Grid */}
        <div className='grid grid-cols-1 md:grid-cols-3 gap-6 mb-8'>
          <Card className='p-6'>
            <div className='flex items-center'>
              <div className='w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center'>
                <svg
                  className='w-6 h-6 text-blue-600'
                  fill='none'
                  stroke='currentColor'
                  viewBox='0 0 24 24'
                >
                  <path
                    strokeLinecap='round'
                    strokeLinejoin='round'
                    strokeWidth={2}
                    d='M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v4a2 2 0 01-2 2h-2a2 2 0 00-2-2z'
                  />
                </svg>
              </div>
              <div className='ml-4'>
                <h3 className='text-lg font-semibold text-gray-900'>0</h3>
                <p className='text-sm text-gray-600'>Active Workflows</p>
              </div>
            </div>
          </Card>

          <Card className='p-6'>
            <div className='flex items-center'>
              <div className='w-12 h-12 bg-green-100 rounded-lg flex items-center justify-center'>
                <svg
                  className='w-6 h-6 text-green-600'
                  fill='none'
                  stroke='currentColor'
                  viewBox='0 0 24 24'
                >
                  <path
                    strokeLinecap='round'
                    strokeLinejoin='round'
                    strokeWidth={2}
                    d='M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z'
                  />
                </svg>
              </div>
              <div className='ml-4'>
                <h3 className='text-lg font-semibold text-gray-900'>0</h3>
                <p className='text-sm text-gray-600'>Successful Runs</p>
              </div>
            </div>
          </Card>

          <Card className='p-6'>
            <div className='flex items-center'>
              <div className='w-12 h-12 bg-purple-100 rounded-lg flex items-center justify-center'>
                <svg
                  className='w-6 h-6 text-purple-600'
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
              <div className='ml-4'>
                <h3 className='text-lg font-semibold text-gray-900'>0</h3>
                <p className='text-sm text-gray-600'>Hours Saved</p>
              </div>
            </div>
          </Card>
        </div>

        {/* User & Organization Info */}
        <div className='grid grid-cols-1 lg:grid-cols-2 gap-6 mb-8'>
          {/* User Info Card */}
          <Card>
            <Card.Header>
              <h3 className='text-lg font-medium text-gray-900'>
                User Information
              </h3>
            </Card.Header>
            <Card.Body>
              <div className='space-y-4'>
                <div className='flex items-center space-x-3'>
                  <div className='w-10 h-10 bg-primary-100 rounded-full flex items-center justify-center'>
                    <span className='text-primary-600 font-medium'>
                      {user?.firstName?.[0]}
                      {user?.lastName?.[0]}
                    </span>
                  </div>
                  <div>
                    <p className='font-medium text-gray-900'>
                      {user?.firstName} {user?.lastName}
                    </p>
                    <p className='text-sm text-gray-600'>{user?.email}</p>
                  </div>
                </div>

                <div className='grid grid-cols-2 gap-4 pt-4 border-t border-gray-200'>
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
                      Email Verified
                    </p>
                    <div className='flex items-center'>
                      <span
                        className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${
                          user?.isEmailVerified
                            ? "bg-green-100 text-green-800"
                            : "bg-red-100 text-red-800"
                        }`}
                      >
                        {user?.isEmailVerified ? "Verified" : "Not Verified"}
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            </Card.Body>
          </Card>

          {/* Organization Info Card */}
          <Card>
            <Card.Header>
              <h3 className='text-lg font-medium text-gray-900'>
                Organization
              </h3>
            </Card.Header>
            <Card.Body>
              {organization ? (
                <div className='space-y-4'>
                  <div>
                    <h4 className='font-medium text-gray-900'>
                      {organization.name}
                    </h4>
                    {organization.description && (
                      <p className='text-sm text-gray-600 mt-1'>
                        {organization.description}
                      </p>
                    )}
                  </div>

                  <div className='grid grid-cols-2 gap-4 pt-4 border-t border-gray-200'>
                    <div>
                      <p className='text-sm font-medium text-gray-500'>Plan</p>
                      <span
                        className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${
                          organization.subscriptionPlan === "Free"
                            ? "bg-gray-100 text-gray-800"
                            : organization.subscriptionPlan === "Pro"
                              ? "bg-blue-100 text-blue-800"
                              : "bg-purple-100 text-purple-800"
                        }`}
                      >
                        {organization.subscriptionPlan}
                      </span>
                    </div>
                    <div>
                      <p className='text-sm font-medium text-gray-500'>
                        Status
                      </p>
                      <span
                        className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${
                          organization.isActive
                            ? "bg-green-100 text-green-800"
                            : "bg-red-100 text-red-800"
                        }`}
                      >
                        {organization.isActive ? "Active" : "Inactive"}
                      </span>
                    </div>
                  </div>

                  <div className='grid grid-cols-2 gap-4 pt-2'>
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
                  </div>
                </div>
              ) : (
                <div className='text-center py-4'>
                  <p className='text-gray-500'>No organization found</p>
                </div>
              )}
            </Card.Body>
          </Card>
        </div>

        {/* Quick Actions */}
        <Card>
          <Card.Header>
            <h3 className='text-lg font-medium text-gray-900'>Quick Actions</h3>
          </Card.Header>
          <Card.Body>
            <div className='grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4'>
              <Button
                variant='secondary'
                className='h-20 flex flex-col items-center justify-center space-y-2'
                disabled
              >
                <svg
                  className='w-6 h-6'
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
                <span className='text-sm'>New Workflow</span>
              </Button>

              <Button
                variant='secondary'
                className='h-20 flex flex-col items-center justify-center space-y-2'
                disabled
              >
                <svg
                  className='w-6 h-6'
                  fill='none'
                  stroke='currentColor'
                  viewBox='0 0 24 24'
                >
                  <path
                    strokeLinecap='round'
                    strokeLinejoin='round'
                    strokeWidth={2}
                    d='M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v4a2 2 0 01-2 2h-2a2 2 0 00-2-2z'
                  />
                </svg>
                <span className='text-sm'>View Templates</span>
              </Button>

              <Button
                variant='secondary'
                className='h-20 flex flex-col items-center justify-center space-y-2'
                disabled
              >
                <svg
                  className='w-6 h-6'
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
                <span className='text-sm'>Documentation</span>
              </Button>

              <Button
                variant='secondary'
                className='h-20 flex flex-col items-center justify-center space-y-2'
                disabled
              >
                <svg
                  className='w-6 h-6'
                  fill='none'
                  stroke='currentColor'
                  viewBox='0 0 24 24'
                >
                  <path
                    strokeLinecap='round'
                    strokeLinejoin='round'
                    strokeWidth={2}
                    d='M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z'
                  />
                  <path
                    strokeLinecap='round'
                    strokeLinejoin='round'
                    strokeWidth={2}
                    d='M15 12a3 3 0 11-6 0 3 3 0 016 0z'
                  />
                </svg>
                <span className='text-sm'>Settings</span>
              </Button>
            </div>
          </Card.Body>
        </Card>

        {/* Coming Soon Notice */}
        <div className='mt-8 text-center'>
          <div className='inline-flex items-center px-4 py-2 bg-blue-50 border border-blue-200 rounded-lg'>
            <svg
              className='w-5 h-5 text-blue-600 mr-2'
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
            <span className='text-sm text-blue-700 font-medium'>
              This is a temporary dashboard. Full functionality coming soon!
            </span>
          </div>
        </div>
      </main>
    </DashboardLayout>
  );
};
