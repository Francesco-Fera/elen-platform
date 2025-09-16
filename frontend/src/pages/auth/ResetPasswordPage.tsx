// frontend/src/pages/auth/ResetPasswordPage.tsx
import React, { useState, useEffect } from "react";
import { useSearchParams, useNavigate } from "react-router-dom";
import { Input, Button, Alert } from "../../components/common";
import { authService } from "../../services/api/authService";
import { ROUTES } from "../../constants";

export const ResetPasswordPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const [formData, setFormData] = useState({
    password: "",
    confirmPassword: "",
  });
  const [errors, setErrors] = useState<{
    password?: string;
    confirmPassword?: string;
    general?: string;
  }>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isValidToken, setIsValidToken] = useState<boolean | null>(null);
  const [isSuccess, setIsSuccess] = useState(false);

  const token = searchParams.get("token");

  useEffect(() => {
    if (token) {
      validateToken(token);
    } else {
      setIsValidToken(false);
    }
  }, [token]);

  const validateToken = async (resetToken: string) => {
    try {
      const response = await authService.validateResetToken(resetToken);
      setIsValidToken(response.data?.isValid || false);
    } catch (error) {
      console.error("Token validation error:", error);
      setIsValidToken(false);
    }
  };

  const validatePassword = (password: string): string[] => {
    const errors: string[] = [];

    if (password.length < 8) errors.push("At least 8 characters");
    if (!/[A-Z]/.test(password)) errors.push("One uppercase letter");
    if (!/[a-z]/.test(password)) errors.push("One lowercase letter");
    if (!/\d/.test(password)) errors.push("One number");
    if (!/[!@#$%^&*(),.?":{}|<>]/.test(password))
      errors.push("One special character");

    return errors;
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));

    // Clear field-specific errors
    if (errors[name as keyof typeof errors]) {
      setErrors((prev) => ({ ...prev, [name]: undefined }));
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const newErrors: typeof errors = {};

    // Password validation
    const passwordErrors = validatePassword(formData.password);
    if (passwordErrors.length > 0) {
      newErrors.password = `Password requirements: ${passwordErrors.join(", ")}`;
    }

    // Confirm password validation
    if (formData.password !== formData.confirmPassword) {
      newErrors.confirmPassword = "Passwords do not match";
    }

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    try {
      setIsSubmitting(true);
      setErrors({});

      const response = await authService.resetPassword({
        token: token!,
        password: formData.password,
      });

      if (response.success) {
        setIsSuccess(true);
        setTimeout(() => {
          navigate(ROUTES.LOGIN);
        }, 3000);
      } else {
        setErrors({ general: response.message || "Password reset failed" });
      }
    } catch (err) {
      setErrors({ general: "An error occurred while resetting password" });
    } finally {
      setIsSubmitting(false);
    }
  };

  // Loading state
  if (isValidToken === null) {
    return (
      <div className='min-h-screen bg-gradient-to-br from-primary-50 to-purple-50 flex items-center justify-center p-4'>
        <div className='bg-white rounded-xl shadow-lg p-8 w-full max-w-md text-center'>
          <div className='w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-4'>
            <div className='animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600'></div>
          </div>
          <h1 className='text-2xl font-bold text-gray-900 mb-4'>
            Validating Reset Link
          </h1>
          <p className='text-gray-600'>
            Please wait while we validate your reset link...
          </p>
        </div>
      </div>
    );
  }

  // Invalid token
  if (!isValidToken) {
    return (
      <div className='min-h-screen bg-gradient-to-br from-primary-50 to-purple-50 flex items-center justify-center p-4'>
        <div className='bg-white rounded-xl shadow-lg p-8 w-full max-w-md text-center'>
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
            Invalid Reset Link
          </h1>
          <p className='text-gray-600 mb-6'>
            This password reset link is invalid or has expired. Please request a
            new one.
          </p>
          <div className='space-y-3'>
            <Button
              variant='primary'
              onClick={() => navigate(ROUTES.FORGOT_PASSWORD)}
              className='w-full'
            >
              Request New Reset Link
            </Button>
            <Button
              variant='ghost'
              onClick={() => navigate(ROUTES.LOGIN)}
              className='w-full'
            >
              Back to Login
            </Button>
          </div>
        </div>
      </div>
    );
  }

  // Success state
  if (isSuccess) {
    return (
      <div className='min-h-screen bg-gradient-to-br from-primary-50 to-purple-50 flex items-center justify-center p-4'>
        <div className='bg-white rounded-xl shadow-lg p-8 w-full max-w-md text-center'>
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
            Password Reset Successful! 🎉
          </h1>
          <p className='text-gray-600 mb-6'>
            Your password has been updated successfully. You can now log in with
            your new password.
          </p>
          <div className='text-sm text-gray-500'>
            Redirecting to login page in a few seconds...
          </div>
        </div>
      </div>
    );
  }

  const isFormValid = formData.password && formData.confirmPassword;

  return (
    <div className='min-h-screen bg-gradient-to-br from-primary-50 to-purple-50 flex items-center justify-center p-4'>
      <div className='bg-white rounded-xl shadow-lg p-8 w-full max-w-md'>
        <div className='text-center mb-8'>
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
                d='M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z'
              />
            </svg>
          </div>
          <h1 className='text-2xl font-bold text-gray-900'>
            Reset Your Password
          </h1>
          <p className='text-gray-600 mt-2'>Enter your new password below.</p>
        </div>

        {errors.general && (
          <Alert
            type='error'
            message={errors.general}
            onClose={() =>
              setErrors((prev) => ({ ...prev, general: undefined }))
            }
            className='mb-6'
          />
        )}

        <form onSubmit={handleSubmit} className='space-y-6'>
          <Input
            label='New Password'
            type='password'
            name='password'
            value={formData.password}
            onChange={handleInputChange}
            error={errors.password}
            placeholder='Enter new password'
            autoComplete='new-password'
            required
          />

          <Input
            label='Confirm New Password'
            type='password'
            name='confirmPassword'
            value={formData.confirmPassword}
            onChange={handleInputChange}
            error={errors.confirmPassword}
            placeholder='Confirm new password'
            autoComplete='new-password'
            required
          />

          {/* Password Requirements */}
          {formData.password && (
            <div className='p-3 bg-gray-50 rounded-md border'>
              <h4 className='text-sm font-medium text-gray-700 mb-2'>
                Password Requirements:
              </h4>
              <ul className='space-y-1'>
                {[
                  {
                    test: (pwd: string) => pwd.length >= 8,
                    text: "At least 8 characters",
                  },
                  {
                    test: (pwd: string) => /[A-Z]/.test(pwd),
                    text: "One uppercase letter",
                  },
                  {
                    test: (pwd: string) => /[a-z]/.test(pwd),
                    text: "One lowercase letter",
                  },
                  { test: (pwd: string) => /\d/.test(pwd), text: "One number" },
                  {
                    test: (pwd: string) => /[!@#$%^&*(),.?":{}|<>]/.test(pwd),
                    text: "One special character",
                  },
                ].map((req, index) => {
                  const isMet = req.test(formData.password);
                  return (
                    <li
                      key={index}
                      className={`text-xs flex items-center ${isMet ? "text-green-600" : "text-gray-500"}`}
                    >
                      <svg
                        className={`w-3 h-3 mr-2 ${isMet ? "text-green-500" : "text-gray-300"}`}
                        fill='currentColor'
                        viewBox='0 0 20 20'
                      >
                        <path
                          fillRule='evenodd'
                          d='M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z'
                          clipRule='evenodd'
                        />
                      </svg>
                      {req.text}
                    </li>
                  );
                })}
              </ul>
            </div>
          )}

          <Button
            type='submit'
            variant='primary'
            loading={isSubmitting}
            disabled={!isFormValid || isSubmitting}
            className='w-full'
          >
            {isSubmitting ? "Updating Password..." : "Update Password"}
          </Button>
        </form>

        <div className='mt-8 text-center'>
          <p className='text-sm text-gray-600'>
            Remember your password?{" "}
            <button
              type='button'
              onClick={() => navigate(ROUTES.LOGIN)}
              className='font-medium text-primary-600 hover:text-primary-500 transition-colors'
            >
              Sign in
            </button>
          </p>
        </div>
      </div>
    </div>
  );
};
