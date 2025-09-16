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
import { EmailVerificationPage } from "./pages/auth/EmailVerificationPage";
import { ResetPasswordPage } from "./pages/auth/ResetPasswordPage";
import { ForgotPasswordPage } from "./pages/auth/ForgotPasswordPage";

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
                {/* Auth routes */}
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

                {/* Email verification routes */}
                <Route
                  path={ROUTES.VERIFY_EMAIL}
                  element={<EmailVerificationPage />}
                />

                {/* Password reset routes */}
                <Route
                  path={ROUTES.FORGOT_PASSWORD}
                  element={<ForgotPasswordPage />}
                />

                <Route
                  path={ROUTES.RESET_PASSWORD}
                  element={<ResetPasswordPage />}
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
