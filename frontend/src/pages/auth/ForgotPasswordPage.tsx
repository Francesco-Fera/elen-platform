import React, { useState } from "react";
import { Link } from "react-router-dom";
import { Input, Button, Alert } from "../../components/common";
import { authService } from "../../services/api/authService";
import { ROUTES } from "../../constants";

export const ForgotPasswordPage: React.FC = () => {
  const [email, setEmail] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [error, setError] = useState("");

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!email.trim()) {
      setError("Email address is required");
      return;
    }

    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      setError("Please enter a valid email address");
      return;
    }

    try {
      setIsSubmitting(true);
      setError("");

      const response = await authService.forgotPassword({
        email: email.trim(),
      });

      if (response.success) {
        setIsSubmitted(true);
      } else {
        setError(response.message || "Failed to send reset email");
      }
    } catch (err) {
      setError("An error occurred while sending reset email");
    } finally {
      setIsSubmitting(false);
    }
  };

  if (isSubmitted) {
    return (
      <div className='min-h-screen bg-gradient-to-br from-primary-50 to-purple-50 flex items-center justify-center p-4'>
        <div className='bg-white rounded-xl shadow-lg p-8 w-full max-w-md'>
          <div className='text-center'>
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
                  d='M3 8l7.89 4.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z'
                />
              </svg>
            </div>
            <h1 className='text-2xl font-bold text-gray-900 mb-4'>
              Check Your Email
            </h1>
            <p className='text-gray-600 mb-6'>
              If an account with <strong>{email}</strong> exists, we've sent you
              a password reset link.
            </p>
            <div className='space-y-4'>
              <p className='text-sm text-gray-500'>
                Didn't receive the email? Check your spam folder or try again in
                a few minutes.
              </p>
              <Button
                variant='ghost'
                onClick={() => setIsSubmitted(false)}
                className='w-full'
              >
                Try Different Email
              </Button>
              <Link to={ROUTES.LOGIN}>
                <Button variant='primary' className='w-full'>
                  Back to Login
                </Button>
              </Link>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className='min-h-screen bg-gradient-to-br from-primary-50 to-purple-50 flex items-center justify-center p-4'>
      <div className='bg-white rounded-xl shadow-lg p-8 w-full max-w-md'>
        <div className='text-center mb-8'>
          <div className='w-16 h-16 bg-orange-100 rounded-full flex items-center justify-center mx-auto mb-4'>
            <svg
              className='w-8 h-8 text-orange-600'
              fill='none'
              stroke='currentColor'
              viewBox='0 0 24 24'
            >
              <path
                strokeLinecap='round'
                strokeLinejoin='round'
                strokeWidth={2}
                d='M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z'
              />
            </svg>
          </div>
          <h1 className='text-2xl font-bold text-gray-900'>Forgot Password?</h1>
          <p className='text-gray-600 mt-2'>
            Enter your email address and we'll send you a reset link.
          </p>
        </div>

        {error && (
          <Alert
            type='error'
            message={error}
            onClose={() => setError("")}
            className='mb-6'
          />
        )}

        <form onSubmit={handleSubmit} className='space-y-6'>
          <Input
            label='Email Address'
            type='email'
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder='Enter your email'
            autoComplete='email'
            required
          />

          <Button
            type='submit'
            variant='primary'
            loading={isSubmitting}
            disabled={!email.trim() || isSubmitting}
            className='w-full'
          >
            {isSubmitting ? "Sending Reset Link..." : "Send Reset Link"}
          </Button>
        </form>

        <div className='mt-8 text-center'>
          <p className='text-sm text-gray-600'>
            Remember your password?{" "}
            <Link
              to={ROUTES.LOGIN}
              className='font-medium text-primary-600 hover:text-primary-500 transition-colors'
            >
              Sign in
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
};
