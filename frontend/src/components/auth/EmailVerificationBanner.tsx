import React, { useState } from "react";
import { Alert, Button } from "../common";
import { useAuth } from "../../contexts/AuthProvider";
import { authService } from "../../services/api/authService";

export const EmailVerificationBanner: React.FC = () => {
  const { user } = useAuth();
  const [isResending, setIsResending] = useState(false);
  const [message, setMessage] = useState("");
  const [isVisible, setIsVisible] = useState(true);

  // Don't show banner if email is verified or user is not logged in
  if (!user || user.isEmailVerified || !isVisible) {
    return null;
  }

  const handleResendVerification = async () => {
    try {
      setIsResending(true);
      const response = await authService.sendEmailVerification();

      if (response.success) {
        setMessage("Verification email sent! Check your inbox.");
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

  return (
    <div className='bg-yellow-50 border-b border-yellow-200'>
      <div className='max-w-7xl mx-auto py-3 px-3 sm:px-6 lg:px-8'>
        <div className='flex items-center justify-between flex-wrap'>
          <div className='w-0 flex-1 flex items-center'>
            <span className='flex p-2 rounded-lg bg-yellow-100'>
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
                  d='M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.34 16.5c-.77.833.192 2.5 1.732 2.5z'
                />
              </svg>
            </span>
            <p className='ml-3 font-medium text-yellow-800 text-sm'>
              {message || (
                <>
                  Please verify your email address to access all features.{" "}
                  <span className='font-normal text-yellow-700'>
                    We sent a verification link to {user.email}
                  </span>
                </>
              )}
            </p>
          </div>

          <div className='order-3 mt-2 flex-shrink-0 w-full sm:order-2 sm:mt-0 sm:w-auto'>
            <div className='flex space-x-2'>
              <Button
                variant='ghost'
                size='sm'
                loading={isResending}
                onClick={handleResendVerification}
                className='text-yellow-800 hover:text-yellow-900 bg-yellow-100 hover:bg-yellow-200'
              >
                {isResending ? "Sending..." : "Resend Email"}
              </Button>

              <Button
                variant='ghost'
                size='sm'
                onClick={() => setIsVisible(false)}
                className='text-yellow-600 hover:text-yellow-700'
              >
                <svg
                  className='h-4 w-4'
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
              </Button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
