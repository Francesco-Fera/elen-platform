import React, { useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../contexts/AuthProvider";
import { Input, Button, Alert } from "../common";
import { ROUTES } from "../../constants";

interface LoginFormProps {
  onToggleForm: () => void;
}

interface LoginFormData {
  email: string;
  password: string;
}

interface FormErrors {
  email?: string;
  password?: string;
  general?: string;
}

export const LoginForm: React.FC<LoginFormProps> = ({ onToggleForm }) => {
  const { login, isLoading, error, clearError } = useAuth();

  const [formData, setFormData] = useState<LoginFormData>({
    email: "",
    password: "",
  });

  const [errors, setErrors] = useState<FormErrors>({});
  const [rememberMe, setRememberMe] = useState(false);

  const validateForm = (): boolean => {
    const newErrors: FormErrors = {};

    // Email validation
    if (!formData.email.trim()) {
      newErrors.email = "Email is required";
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      newErrors.email = "Please enter a valid email address";
    }

    // Password validation
    if (!formData.password) {
      newErrors.password = "Password is required";
    } else if (formData.password.length < 6) {
      newErrors.password = "Password must be at least 6 characters";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));

    // Clear field-specific error when user starts typing
    if (errors[name as keyof FormErrors]) {
      setErrors((prev) => ({ ...prev, [name]: undefined }));
    }

    // Clear general error
    if (error) {
      clearError();
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      return;
    }

    try {
      await login({
        email: formData.email.trim(),
        password: formData.password,
      });

      // Login successful - user will be redirected by ProtectedRoute logic
      console.log("Login successful");
    } catch (err) {
      // Error is handled by auth context and displayed via error state
      console.error("Login failed:", err);
    }
  };

  const isFormValid = formData.email && formData.password;

  return (
    <div className='bg-white rounded-xl shadow-lg p-8 w-full max-w-md'>
      {/* Header */}
      <div className='text-center mb-8'>
        <div className='w-16 h-16 bg-primary-100 rounded-full flex items-center justify-center mx-auto mb-4'>
          <svg
            className='w-8 h-8 text-primary-600'
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
        <h1 className='text-2xl font-bold text-gray-900'>Welcome back</h1>
        <p className='text-gray-600 mt-2'>Sign in to your account</p>
      </div>

      {/* Error Alert */}
      {error && (
        <Alert
          type='error'
          message={error}
          onClose={clearError}
          className='mb-6'
        />
      )}

      {/* Form */}
      <form onSubmit={handleSubmit} className='space-y-6'>
        <Input
          label='Email address'
          type='email'
          name='email'
          value={formData.email}
          onChange={handleInputChange}
          error={errors.email}
          placeholder='Enter your email'
          autoComplete='email'
          required
        />

        <Input
          label='Password'
          type='password'
          name='password'
          value={formData.password}
          onChange={handleInputChange}
          error={errors.password}
          placeholder='Enter your password'
          autoComplete='current-password'
          required
        />

        {/* Remember Me & Forgot Password */}
        <div className='flex items-center justify-between'>
          <label className='flex items-center'>
            <input
              type='checkbox'
              checked={rememberMe}
              onChange={(e) => setRememberMe(e.target.checked)}
              className='h-4 w-4 text-primary-600 focus:ring-primary-500 border-gray-300 rounded'
            />
            <span className='ml-2 text-sm text-gray-600'>Remember me</span>
          </label>

          <Link
            to={ROUTES.FORGOT_PASSWORD}
            className='text-sm text-primary-600 hover:text-primary-500 font-medium'
          >
            Forgot password?
          </Link>
        </div>

        {/* Submit Button */}
        <Button
          type='submit'
          variant='primary'
          loading={isLoading}
          disabled={!isFormValid || isLoading}
          className='w-full'
        >
          {isLoading ? "Signing in..." : "Sign in"}
        </Button>
      </form>

      {/* Social Login - Optional for future */}
      <div className='mt-6'>
        <div className='relative'>
          <div className='absolute inset-0 flex items-center'>
            <div className='w-full border-t border-gray-300' />
          </div>
          <div className='relative flex justify-center text-sm'>
            <span className='px-2 bg-white text-gray-500'>
              Or continue with
            </span>
          </div>
        </div>

        <div className='mt-6 grid grid-cols-2 gap-3'>
          <button
            type='button'
            disabled
            className='w-full inline-flex justify-center py-2 px-4 border border-gray-300 rounded-md shadow-sm bg-white text-sm font-medium text-gray-500 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed'
          >
            <svg className='w-5 h-5' viewBox='0 0 24 24'>
              <path
                fill='currentColor'
                d='M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z'
              />
              <path
                fill='currentColor'
                d='M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z'
              />
              <path
                fill='currentColor'
                d='M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z'
              />
              <path
                fill='currentColor'
                d='M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z'
              />
            </svg>
            <span className='ml-2'>Google</span>
          </button>

          <button
            type='button'
            disabled
            className='w-full inline-flex justify-center py-2 px-4 border border-gray-300 rounded-md shadow-sm bg-white text-sm font-medium text-gray-500 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed'
          >
            <svg className='w-5 h-5' viewBox='0 0 24 24'>
              <path
                fill='currentColor'
                d='M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z'
              />
            </svg>
            <span className='ml-2'>Facebook</span>
          </button>
        </div>
      </div>

      {/* Register Link */}
      <div className='mt-8 text-center'>
        <p className='text-sm text-gray-600'>
          Don't have an account?{" "}
          <button
            type='button'
            onClick={onToggleForm}
            className='font-medium text-primary-600 hover:text-primary-500 transition-colors'
          >
            Sign up
          </button>
        </p>
      </div>

      {/* Demo Credentials - For development only */}
      {process.env.NODE_ENV === "development" && (
        <div className='mt-6 p-4 bg-yellow-50 border border-yellow-200 rounded-lg'>
          <h4 className='text-sm font-medium text-yellow-800 mb-2'>
            Demo Credentials (Dev Only)
          </h4>
          <div className='text-xs text-yellow-700 space-y-1'>
            <div>
              <strong>Admin:</strong> admin@example.com / password123
            </div>
            <div>
              <strong>User:</strong> user@example.com / password123
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
