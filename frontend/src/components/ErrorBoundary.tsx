import React, { Component, ErrorInfo, ReactNode } from "react";
import { Button } from "./common/Button";

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
  onError?: (error: Error, errorInfo: ErrorInfo) => void;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
}

export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = {
      hasError: false,
      error: null,
      errorInfo: null,
    };
  }

  static getDerivedStateFromError(error: Error): State {
    return {
      hasError: true,
      error,
      errorInfo: null,
    };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error("ErrorBoundary caught an error:", error, errorInfo);

    this.setState({
      error,
      errorInfo,
    });

    // Call the onError callback if provided
    if (this.props.onError) {
      this.props.onError(error, errorInfo);
    }

    // Log to external service (e.g., Sentry, LogRocket)
    this.logErrorToService(error, errorInfo);
  }

  private logErrorToService(error: Error, errorInfo: ErrorInfo) {
    // Here you would integrate with your error tracking service
    // Example: Sentry.captureException(error, { extra: errorInfo });

    // For now, we'll just log to console in development
    if (process.env.NODE_ENV === "development") {
      console.group("🚨 Error Boundary");
      console.error("Error:", error);
      console.error("Error Info:", errorInfo);
      console.error("Component Stack:", errorInfo.componentStack);
      console.groupEnd();
    }

    // In production, you might want to send this to your monitoring service
    if (process.env.NODE_ENV === "production") {
      // Example API call to log error
      fetch("/api/errors", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          error: {
            message: error.message,
            stack: error.stack,
            name: error.name,
          },
          errorInfo,
          timestamp: new Date().toISOString(),
          userAgent: navigator.userAgent,
          url: window.location.href,
        }),
      }).catch(() => {
        // Silently fail if error logging fails
      });
    }
  }

  private handleRetry = () => {
    this.setState({
      hasError: false,
      error: null,
      errorInfo: null,
    });
  };

  private handleReload = () => {
    window.location.reload();
  };

  private handleGoHome = () => {
    window.location.href = "/";
  };

  render() {
    if (this.state.hasError) {
      // Custom fallback UI
      if (this.props.fallback) {
        return this.props.fallback;
      }

      // Default error UI
      return (
        <div className='min-h-screen bg-gray-50 flex flex-col justify-center py-12 sm:px-6 lg:px-8'>
          <div className='sm:mx-auto sm:w-full sm:max-w-md'>
            <div className='bg-white py-8 px-4 shadow sm:rounded-lg sm:px-10'>
              <div className='text-center'>
                <div className='mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-red-100'>
                  <svg
                    className='h-6 w-6 text-red-600'
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

                <h2 className='mt-4 text-lg font-medium text-gray-900'>
                  Something went wrong
                </h2>

                <p className='mt-2 text-sm text-gray-600'>
                  We're sorry, but something unexpected happened. The error has
                  been logged and we'll look into it.
                </p>

                {process.env.NODE_ENV === "development" && this.state.error && (
                  <div className='mt-4 p-4 bg-gray-50 rounded-md text-left'>
                    <h3 className='text-sm font-medium text-gray-900 mb-2'>
                      Error Details (Development Only):
                    </h3>
                    <pre className='text-xs text-red-600 whitespace-pre-wrap break-all'>
                      {this.state.error.message}
                    </pre>
                    {this.state.error.stack && (
                      <details className='mt-2'>
                        <summary className='text-xs text-gray-600 cursor-pointer'>
                          Stack Trace
                        </summary>
                        <pre className='text-xs text-gray-500 whitespace-pre-wrap mt-1'>
                          {this.state.error.stack}
                        </pre>
                      </details>
                    )}
                  </div>
                )}

                <div className='mt-6 flex flex-col sm:flex-row gap-3 justify-center'>
                  <Button
                    variant='primary'
                    onClick={this.handleRetry}
                    className='w-full sm:w-auto'
                  >
                    Try Again
                  </Button>

                  <Button
                    variant='secondary'
                    onClick={this.handleReload}
                    className='w-full sm:w-auto'
                  >
                    Reload Page
                  </Button>

                  <Button
                    variant='ghost'
                    onClick={this.handleGoHome}
                    className='w-full sm:w-auto'
                  >
                    Go Home
                  </Button>
                </div>

                <div className='mt-6 text-xs text-gray-500'>
                  <p>
                    If this problem persists, please contact support with the
                    error details above.
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}

// HOC version for easier usage
export const withErrorBoundary = <P extends object>(
  Component: React.ComponentType<P>,
  fallback?: ReactNode,
  onError?: (error: Error, errorInfo: ErrorInfo) => void
) => {
  return (props: P) => (
    <ErrorBoundary fallback={fallback} onError={onError}>
      <Component {...props} />
    </ErrorBoundary>
  );
};

// Specialized error boundary for auth-related errors
interface AuthErrorBoundaryProps {
  children: ReactNode;
  onAuthError?: () => void;
}

export const AuthErrorBoundary: React.FC<AuthErrorBoundaryProps> = ({
  children,
  onAuthError,
}) => {
  const handleError = (error: Error, errorInfo: ErrorInfo) => {
    // Check if this is an auth-related error
    const authErrorKeywords = [
      "token",
      "unauthorized",
      "authentication",
      "login",
      "session",
      "expired",
    ];

    const isAuthError = authErrorKeywords.some(
      (keyword) =>
        error.message.toLowerCase().includes(keyword) ||
        error.stack?.toLowerCase().includes(keyword)
    );

    if (isAuthError && onAuthError) {
      onAuthError();
    }
  };

  const authErrorFallback = (
    <div className='min-h-screen bg-gray-50 flex flex-col justify-center py-12 sm:px-6 lg:px-8'>
      <div className='sm:mx-auto sm:w-full sm:max-w-md'>
        <div className='bg-white py-8 px-4 shadow sm:rounded-lg sm:px-10'>
          <div className='text-center'>
            <div className='mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-yellow-100'>
              <svg
                className='h-6 w-6 text-yellow-600'
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

            <h2 className='mt-4 text-lg font-medium text-gray-900'>
              Authentication Error
            </h2>

            <p className='mt-2 text-sm text-gray-600'>
              There was an issue with your authentication. Please try logging in
              again.
            </p>

            <div className='mt-6'>
              <Button
                variant='primary'
                onClick={() => (window.location.href = "/login")}
                className='w-full'
              >
                Go to Login
              </Button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );

  return (
    <ErrorBoundary fallback={authErrorFallback} onError={handleError}>
      {children}
    </ErrorBoundary>
  );
};
