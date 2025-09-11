import React from "react";
import { useAuth } from "../../contexts/AuthContext";
import { Button } from "../../components/common";

export const DashboardPage: React.FC = () => {
  const { user, organization, logout } = useAuth();
  return (
    <div className='min-h-screen bg-gray-50'>
      {/* Header */}
      <header className='bg-white shadow'>
        <div className='max-w-7xl mx-auto px-4 sm:px-6 lg:px-8'>
          <div className='flex justify-between items-center py-6'>
            <div className='flex items-center'>
              <h1 className='text-2xl font-bold text-gradient'>
                WorkflowEngine
              </h1>
            </div>
            <div className='flex items-center space-x-4'>
              <div className='text-sm text-gray-600'>
                <span className='font-medium'>
                  {user?.firstName} {user?.lastName}
                </span>
                <span className='mx-2'>•</span>
                <span>{organization?.name}</span>
              </div>
              <Button variant='secondary' size='sm' onClick={logout}>
                Logout
              </Button>
            </div>
          </div>
        </div>
      </header>

      {/* Main content */}
      <main className='max-w-7xl mx-auto py-6 sm:px-6 lg:px-8'>
        <div className='px-4 py-6 sm:px-0'>
          <div className='card'>
            <div className='card-header'>
              <h2 className='text-lg font-medium text-gray-900'>
                Welcome to WorkflowEngine
              </h2>
            </div>
            <div className='card-body'>
              <div className='space-y-4'>
                <div>
                  <h3 className='text-sm font-medium text-gray-900 mb-2'>
                    User Information
                  </h3>
                  <div className='bg-gray-50 rounded-md p-3'>
                    <div className='grid grid-cols-2 gap-4 text-sm'>
                      <div>
                        <span className='text-gray-500'>Email:</span>
                        <div className='font-medium'>{user?.email}</div>
                      </div>
                      <div>
                        <span className='text-gray-500'>Name:</span>
                        <div className='font-medium'>
                          {user?.firstName} {user?.lastName}
                        </div>
                      </div>
                      <div>
                        <span className='text-gray-500'>Email Verified:</span>
                        <div className='font-medium'>
                          {user?.isEmailVerified ? (
                            <span className='text-success-600'>✓ Verified</span>
                          ) : (
                            <span className='text-warning-600'>⚠ Pending</span>
                          )}
                        </div>
                      </div>
                      <div>
                        <span className='text-gray-500'>Member Since:</span>
                        <div className='font-medium'>
                          {new Date(user?.createdAt || "").toLocaleDateString()}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <div>
                  <h3 className='text-sm font-medium text-gray-900 mb-2'>
                    Organization Information
                  </h3>
                  <div className='bg-gray-50 rounded-md p-3'>
                    <div className='grid grid-cols-2 gap-4 text-sm'>
                      <div>
                        <span className='text-gray-500'>Name:</span>
                        <div className='font-medium'>{organization?.name}</div>
                      </div>
                      <div>
                        <span className='text-gray-500'>Plan:</span>
                        <div className='font-medium'>
                          <span
                            className={`px-2 py-1 rounded-full text-xs ${
                              organization?.subscriptionPlan === "Free"
                                ? "bg-gray-100 text-gray-800"
                                : organization?.subscriptionPlan === "Pro"
                                  ? "bg-primary-100 text-primary-800"
                                  : "bg-purple-100 text-purple-800"
                            }`}
                          >
                            {organization?.subscriptionPlan}
                          </span>
                        </div>
                      </div>
                      <div>
                        <span className='text-gray-500'>Max Users:</span>
                        <div className='font-medium'>
                          {organization?.maxUsers}
                        </div>
                      </div>
                      <div>
                        <span className='text-gray-500'>Max Workflows:</span>
                        <div className='font-medium'>
                          {organization?.maxWorkflows}
                        </div>
                      </div>
                      <div>
                        <span className='text-gray-500'>Trial Account:</span>
                        <div className='font-medium'>
                          {organization?.isTrialAccount ? (
                            <span className='text-warning-600'>
                              Trial{" "}
                              {organization.trialExpiresAt
                                ? `(expires ${new Date(organization.trialExpiresAt).toLocaleDateString()})`
                                : ""}
                            </span>
                          ) : (
                            <span className='text-success-600'>
                              Full Account
                            </span>
                          )}
                        </div>
                      </div>
                      <div>
                        <span className='text-gray-500'>Status:</span>
                        <div className='font-medium'>
                          {organization?.isActive ? (
                            <span className='text-success-600'>✓ Active</span>
                          ) : (
                            <span className='text-error-600'>✗ Inactive</span>
                          )}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <div className='flex space-x-4'>
                  <Button variant='primary'>Create Workflow</Button>
                  <Button variant='secondary'>View Workflows</Button>
                  <Button variant='secondary'>Manage Organization</Button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
};
