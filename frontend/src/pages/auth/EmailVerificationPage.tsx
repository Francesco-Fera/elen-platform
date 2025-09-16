import React, { useState, useEffect } from "react";
import { useSearchParams, useNavigate } from "react-router-dom";
import { Button, Alert } from "../../components/common";
import { useAuth } from "../../contexts/AuthProvider";
import { authService } from "../../services/api/authService";
import { ROUTES } from "../../constants";

export const EmailVerificationPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { user, refreshAuth } = useAuth();

  const [status, setStatus] = useState<
    "loading" | "success" | "error" | "expired"
  >("loading");
  const [message, setMessage] = useState("");
  const [isResending, setIsResending] = useState(false);

  const token = searchParams.get("token");

  useEffect(() => {
    if (token) {
      verifyEmail(token);
    } else {
      setStatus("error");
      setMessage("No verification token provided");
    }
  }, [token]);

  const verifyEmail = async (verificationToken: string) => {
    try {
      const response = await authService.verifyEmail(verificationToken);

      if (response.success) {
        setStatus("success");
        setMessage("Your email has been verified successfully!");

        // Refresh user data to reflect email verification
        await refreshAuth();

        // Redirect to dashboard after 3 seconds
        setTimeout(() => {
          navigate(ROUTES.DASHBOARD);
        }, 3000);
      } else {
        setStatus("expired");
        setMessage(response.message || "Verification failed");
      }
    } catch (error) {
      console.error("Email verification error:", error);
      setStatus("error");
      setMessage("An error occurred during verification");
    }
  };

  const handleResendVerification = async () => {
    try {
      setIsResending(true);
      const response = await authService.sendEmailVerification();

      if (response.success) {
        setMessage("A new verification email has been sent to your inbox.");
      } else {
        setMessage(response.message || "Failed to send verification email");
      }
    } catch (error) {
      console.error("Resend verification error:", error);
      setMessage("An error occurred while sending verification email");
    } finally {
      setIsResending(false);
    }
  };

  const renderStatusContent = () => {
    switch (status) {
      case "loading":
        return (
          <>
            <div className='w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-4'>
              <div className='animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600'></div>
            </div>
            <h1 className='text-2xl font-bold text-gray-900 mb-4'>
              Verifying Your Email
            </h1>
            <p className='text-gray-600'>
              Please wait while we verify your email address...
            </p>
          </>
        );

      case "success":
        return (
          <>
            <div className='w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4'>
              <svg
                className='w-8 h-8 text-green-600'
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
            <h1 className='text-2xl font-bold text-gray-900 mb-4'>
              Email Verified! 🎉
            </h1>
            <p className='text-gray-600 mb-6'>{message}</p>
            <div className='text-sm text-gray-500'>
              Redirecting to dashboard in a few seconds...
            </div>
          </>
        );

      case "expired":
        return (
          <>
            <div className='w-16 h-16 bg-yellow-100 rounded-full flex items-center justify-center mx-auto mb-4'>
              <svg
                className='w-8 h-8 text-yellow-600'
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
            <h1 className='text-2xl font-bold text-gray-900 mb-4'>
              Verification Link Expired
            </h1>
            <p className='text-gray-600 mb-6'>{message}</p>
            {user && !user.isEmailVerified && (
              <div className='space-y-4'>
                <Button
                  variant='primary'
                  loading={isResending}
                  onClick={handleResendVerification}
                  className='w-full'
                >
                  Send New Verification Email
                </Button>
                <p className='text-sm text-gray-500'>
                  We'll send a new verification link to {user.email}
                </p>
              </div>
            )}
          </>
        );

      case "error":
        return (
          <>
            <div className='w-16 h-16 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4'>
              <svg
                className='w-8 h-8 text-red-600'
                fill='none'
                stroke='currentColor'
                viewBox='0 0 24 24'
              >
                <path
                  strokeLinecap='round'
                  strokeLinejoin='round'
                  strokeWidth={2}
                  d='M6 18L18 6M6 6l12 12'
                />
              </svg>
            </div>
            <h1 className='text-2xl font-bold text-gray-900 mb-4'>
              Verification Failed
            </h1>
            <p className='text-gray-600 mb-6'>{message}</p>
          </>
        );

      default:
        return null;
    }
  };

  return (
    <div className='min-h-screen bg-gradient-to-br from-primary-50 to-purple-50 flex items-center justify-center p-4'>
      <div className='bg-white rounded-xl shadow-lg p-8 w-full max-w-md text-center'>
        {renderStatusContent()}

        {status !== "loading" && status !== "success" && (
          <div className='mt-8 pt-6 border-t border-gray-200'>
            <Button
              variant='ghost'
              onClick={() => navigate(ROUTES.DASHBOARD)}
              className='w-full'
            >
              Continue to Dashboard
            </Button>
          </div>
        )}
      </div>
    </div>
  );
};
