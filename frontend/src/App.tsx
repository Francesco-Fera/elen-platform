import React from "react";
import {
  BrowserRouter as Router,
  Routes,
  Route,
  Navigate,
} from "react-router-dom";
import { AuthProvider } from "./contexts/AuthProvider";
import { ErrorBoundary, AuthErrorBoundary } from "./components/ErrorBoundary";
import { ProtectedRoute } from "./components/auth/ProtectedRoute";
import { AuthPage } from "./pages/auth/AuthPage";
import { DashboardPage } from "./pages/dashboard/DashboardPage";
import { LoadingSpinner } from "./components/common";
import { ROUTES } from "./constants";
import "./App.css";

// Loading component for auth initialization
const AuthLoadingFallback: React.FC = () => (
  <div className='min-h-screen flex items-center justify-center bg-gray-50'>
    <div className='text-center'>
      <LoadingSpinner size='lg' />
      <p className='mt-4 text-sm text-gray-600'>Initializing...</p>
    </div>
  </div>
);

function App() {
  return (
    <ErrorBoundary>
      <Router>
        <AuthProvider>
          <AuthErrorBoundary
            onAuthError={() => {
              console.log("Auth error detected, redirecting to login...");
              window.location.href = "/login";
            }}
          >
            <div className='App'>
              <Routes>
                {/* Auth routes - both use the same AuthPage component */}
                <Route
                  path={ROUTES.LOGIN}
                  element={
                    <ProtectedRoute
                      requireAuth={false}
                      loadingComponent={<AuthLoadingFallback />}
                    >
                      <AuthPage />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path={ROUTES.REGISTER}
                  element={
                    <ProtectedRoute
                      requireAuth={false}
                      loadingComponent={<AuthLoadingFallback />}
                    >
                      <AuthPage />
                    </ProtectedRoute>
                  }
                />

                {/* Protected routes */}
                <Route
                  path={ROUTES.DASHBOARD}
                  element={
                    <ProtectedRoute
                      requireAuth={true}
                      requireEmailVerification={false} // Set to true if email verification is required
                      requireOrganization={true}
                      loadingComponent={<AuthLoadingFallback />}
                    >
                      <DashboardPage />
                    </ProtectedRoute>
                  }
                />

                {/* Organization management routes */}
                <Route
                  path={ROUTES.ORGANIZATION}
                  element={
                    <ProtectedRoute
                      requireAuth={true}
                      loadingComponent={<AuthLoadingFallback />}
                    >
                      <div className='min-h-screen flex items-center justify-center'>
                        <div className='text-center'>
                          <h1 className='text-2xl font-bold text-gray-900'>
                            Organization Management
                          </h1>
                          <p className='mt-2 text-gray-600'>
                            Organization management page coming soon...
                          </p>
                        </div>
                      </div>
                    </ProtectedRoute>
                  }
                />

                {/* Profile management routes */}
                <Route
                  path={ROUTES.PROFILE}
                  element={
                    <ProtectedRoute
                      requireAuth={true}
                      loadingComponent={<AuthLoadingFallback />}
                    >
                      <div className='min-h-screen flex items-center justify-center'>
                        <div className='text-center'>
                          <h1 className='text-2xl font-bold text-gray-900'>
                            User Profile
                          </h1>
                          <p className='mt-2 text-gray-600'>
                            Profile management page coming soon...
                          </p>
                        </div>
                      </div>
                    </ProtectedRoute>
                  }
                />

                {/* Workflows routes */}
                <Route
                  path={ROUTES.WORKFLOWS}
                  element={
                    <ProtectedRoute
                      requireAuth={true}
                      requireOrganization={true}
                      loadingComponent={<AuthLoadingFallback />}
                    >
                      <div className='min-h-screen flex items-center justify-center'>
                        <div className='text-center'>
                          <h1 className='text-2xl font-bold text-gray-900'>
                            Workflows
                          </h1>
                          <p className='mt-2 text-gray-600'>
                            Workflow management page coming soon...
                          </p>
                        </div>
                      </div>
                    </ProtectedRoute>
                  }
                />

                {/* Workflow editor routes */}
                <Route
                  path='/workflows/:id/edit'
                  element={
                    <ProtectedRoute
                      requireAuth={true}
                      requireOrganization={true}
                      minRole='Member' // Require at least Member role
                      loadingComponent={<AuthLoadingFallback />}
                    >
                      <div className='min-h-screen flex items-center justify-center'>
                        <div className='text-center'>
                          <h1 className='text-2xl font-bold text-gray-900'>
                            Workflow Editor
                          </h1>
                          <p className='mt-2 text-gray-600'>
                            Workflow editor coming soon...
                          </p>
                        </div>
                      </div>
                    </ProtectedRoute>
                  }
                />

                <Route
                  path='/workflows/new'
                  element={
                    <ProtectedRoute
                      requireAuth={true}
                      requireOrganization={true}
                      minRole='Member'
                      loadingComponent={<AuthLoadingFallback />}
                    >
                      <div className='min-h-screen flex items-center justify-center'>
                        <div className='text-center'>
                          <h1 className='text-2xl font-bold text-gray-900'>
                            Create New Workflow
                          </h1>
                          <p className='mt-2 text-gray-600'>
                            Workflow creation page coming soon...
                          </p>
                        </div>
                      </div>
                    </ProtectedRoute>
                  }
                />

                {/* Admin-only routes */}
                <Route
                  path='/admin/*'
                  element={
                    <ProtectedRoute
                      requireAuth={true}
                      requireOrganization={true}
                      minRole='Admin'
                      loadingComponent={<AuthLoadingFallback />}
                    >
                      <div className='min-h-screen flex items-center justify-center'>
                        <div className='text-center'>
                          <h1 className='text-2xl font-bold text-gray-900'>
                            Admin Panel
                          </h1>
                          <p className='mt-2 text-gray-600'>
                            Admin features coming soon...
                          </p>
                        </div>
                      </div>
                    </ProtectedRoute>
                  }
                />

                {/* Email verification route */}
                <Route
                  path='/verify-email'
                  element={
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
                                d='M3 8l7.89 4.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z'
                              />
                            </svg>
                          </div>
                          <h3 className='mt-2 text-lg font-medium text-gray-900'>
                            Email Verification
                          </h3>
                          <p className='mt-1 text-sm text-gray-500'>
                            Email verification functionality coming soon...
                          </p>
                        </div>
                      </div>
                    </div>
                  }
                />

                {/* Password reset routes */}
                <Route
                  path='/forgot-password'
                  element={
                    <div className='min-h-screen flex items-center justify-center bg-gray-50'>
                      <div className='max-w-md w-full bg-white rounded-lg shadow p-6'>
                        <div className='text-center'>
                          <h3 className='text-lg font-medium text-gray-900'>
                            Forgot Password
                          </h3>
                          <p className='mt-1 text-sm text-gray-500'>
                            Password reset functionality coming soon...
                          </p>
                        </div>
                      </div>
                    </div>
                  }
                />

                <Route
                  path='/reset-password'
                  element={
                    <div className='min-h-screen flex items-center justify-center bg-gray-50'>
                      <div className='max-w-md w-full bg-white rounded-lg shadow p-6'>
                        <div className='text-center'>
                          <h3 className='text-lg font-medium text-gray-900'>
                            Reset Password
                          </h3>
                          <p className='mt-1 text-sm text-gray-500'>
                            Password reset functionality coming soon...
                          </p>
                        </div>
                      </div>
                    </div>
                  }
                />

                {/* Redirect routes */}
                <Route
                  path={ROUTES.HOME}
                  element={<Navigate to={ROUTES.DASHBOARD} replace />}
                />

                {/* Catch-all route */}
                <Route
                  path='*'
                  element={
                    <div className='min-h-screen flex items-center justify-center bg-gray-50'>
                      <div className='text-center'>
                        <h1 className='text-4xl font-bold text-gray-900'>
                          404
                        </h1>
                        <p className='mt-2 text-gray-600'>Page not found</p>
                        <div className='mt-4'>
                          <button
                            onClick={() => window.history.back()}
                            className='btn btn-secondary mr-2'
                          >
                            Go Back
                          </button>
                          <button
                            onClick={() => (window.location.href = "/")}
                            className='btn btn-primary'
                          >
                            Go Home
                          </button>
                        </div>
                      </div>
                    </div>
                  }
                />
              </Routes>
            </div>
          </AuthErrorBoundary>
        </AuthProvider>
      </Router>
    </ErrorBoundary>
  );
}

export default App;
