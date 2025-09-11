import React, { useState } from "react";
import { useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import { useAuth } from "../../contexts/AuthContext";
import { Button } from "../common/Button";
import { Input } from "../common/Input";
import { Alert } from "../common/Alert";
import { Card } from "../common/Card";
import type { LoginRequest } from "../../types";
import { ROUTES } from "../../constants";

const loginSchema = yup.object({
  email: yup
    .string()
    .email("Please enter a valid email address")
    .required("Email is required"),
  password: yup.string().required("Password is required"),
});

interface LoginFormProps {
  onToggleForm?: () => void;
}

export const LoginForm: React.FC<LoginFormProps> = ({ onToggleForm }) => {
  const { login, isLoading, error, clearError } = useAuth();
  const [showPassword, setShowPassword] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginRequest>({
    resolver: yupResolver(loginSchema),
  });

  const onSubmit = async (data: LoginRequest) => {
    try {
      clearError();
      await login(data);
      // Navigation will be handled by ProtectedRoute after successful login
    } catch (error) {
      // Error will be displayed via the error state from useAuth
    }
  };

  return (
    <Card className='w-full max-w-md'>
      <Card.Header>
        <div className='text-center'>
          <h2 className='text-2xl font-bold text-gray-900'>Welcome back</h2>
          <p className='mt-2 text-sm text-gray-600'>
            Sign in to your WorkflowEngine account
          </p>
        </div>
      </Card.Header>

      <Card.Body>
        {error && (
          <Alert
            type='error'
            message={error}
            onClose={clearError}
            className='mb-4'
          />
        )}

        <form onSubmit={handleSubmit(onSubmit)} className='space-y-4'>
          <Input
            {...register("email")}
            type='email'
            label='Email address'
            placeholder='Enter your email'
            error={errors.email?.message}
            autoComplete='email'
          />

          <div className='relative'>
            <Input
              {...register("password")}
              type={showPassword ? "text" : "password"}
              label='Password'
              placeholder='Enter your password'
              error={errors.password?.message}
              autoComplete='current-password'
            />
            <button
              type='button'
              className='absolute right-3 top-8 text-gray-400 hover:text-gray-600'
              onClick={() => setShowPassword(!showPassword)}
            >
              {showPassword ? (
                <svg
                  className='w-5 h-5'
                  fill='none'
                  stroke='currentColor'
                  viewBox='0 0 24 24'
                >
                  <path
                    strokeLinecap='round'
                    strokeLinejoin='round'
                    strokeWidth={2}
                    d='M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.878 9.878L3 3m6.878 6.878L21 21'
                  />
                </svg>
              ) : (
                <svg
                  className='w-5 h-5'
                  fill='none'
                  stroke='currentColor'
                  viewBox='0 0 24 24'
                >
                  <path
                    strokeLinecap='round'
                    strokeLinejoin='round'
                    strokeWidth={2}
                    d='M15 12a3 3 0 11-6 0 3 3 0 016 0z'
                  />
                  <path
                    strokeLinecap='round'
                    strokeLinejoin='round'
                    strokeWidth={2}
                    d='M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z'
                  />
                </svg>
              )}
            </button>
          </div>

          <div className='flex items-center justify-between'>
            <label className='flex items-center'>
              <input
                type='checkbox'
                className='rounded border-gray-300 text-primary-600 focus:ring-primary-500'
              />
              <span className='ml-2 text-sm text-gray-600'>Remember me</span>
            </label>
            <a
              href={ROUTES.FORGOT_PASSWORD}
              className='text-sm text-primary-600 hover:text-primary-500'
            >
              Forgot password?
            </a>
          </div>

          <Button
            type='submit'
            variant='primary'
            className='w-full'
            loading={isLoading}
          >
            Sign in
          </Button>
        </form>
      </Card.Body>

      <Card.Footer>
        <div className='text-center text-sm text-gray-600'>
          Don't have an account?{" "}
          <button
            onClick={onToggleForm}
            className='text-primary-600 hover:text-primary-500 font-medium'
          >
            Sign up
          </button>
        </div>
      </Card.Footer>
    </Card>
  );
};
