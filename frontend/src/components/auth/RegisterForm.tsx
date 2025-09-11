import React, { useState, useRef } from "react";
import { useAuth } from "../../contexts/AuthProvider";
import { Input, Button, Alert } from "../common";

interface RegisterFormProps {
  onToggleForm: () => void;
}

interface RegisterFormData {
  email: string;
  password: string;
  confirmPassword: string;
  firstName: string;
  lastName: string;
  organizationName: string;
  timeZone: string;
  acceptTerms: boolean;
}

interface FormErrors {
  email?: string;
  password?: string;
  confirmPassword?: string;
  firstName?: string;
  lastName?: string;
  organizationName?: string;
  acceptTerms?: string;
  general?: string;
}

const PASSWORD_REQUIREMENTS = [
  { test: (pwd: string) => pwd.length >= 8, text: "At least 8 characters" },
  { test: (pwd: string) => /[A-Z]/.test(pwd), text: "One uppercase letter" },
  { test: (pwd: string) => /[a-z]/.test(pwd), text: "One lowercase letter" },
  { test: (pwd: string) => /\d/.test(pwd), text: "One number" },
  {
    test: (pwd: string) => /[!@#$%^&*(),.?":{}|<>]/.test(pwd),
    text: "One special character",
  },
];

export const RegisterForm: React.FC<RegisterFormProps> = ({ onToggleForm }) => {
  const { register, isLoading, error, clearError } = useAuth();

  const [formData, setFormData] = useState<RegisterFormData>({
    email: "",
    password: "",
    confirmPassword: "",
    firstName: "",
    lastName: "",
    organizationName: "",
    timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone || "UTC",
    acceptTerms: false,
  });

  const [errors, setErrors] = useState<FormErrors>({});
  const [showPasswordRequirements, setShowPasswordRequirements] =
    useState(false);
  const [currentStep, setCurrentStep] = useState(1);
  const passwordInputRef = useRef<HTMLInputElement>(null);

  const validateStep1 = (): boolean => {
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
    } else {
      const failedRequirements = PASSWORD_REQUIREMENTS.filter(
        (req) => !req.test(formData.password)
      );
      if (failedRequirements.length > 0) {
        newErrors.password = "Password doesn't meet requirements";
      }
    }

    // Confirm password validation
    if (!formData.confirmPassword) {
      newErrors.confirmPassword = "Please confirm your password";
    } else if (formData.password !== formData.confirmPassword) {
      newErrors.confirmPassword = "Passwords don't match";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const validateStep2 = (): boolean => {
    const newErrors: FormErrors = {};

    // First name validation
    if (!formData.firstName.trim()) {
      newErrors.firstName = "First name is required";
    } else if (formData.firstName.trim().length < 2) {
      newErrors.firstName = "First name must be at least 2 characters";
    }

    // Last name validation
    if (!formData.lastName.trim()) {
      newErrors.lastName = "Last name is required";
    } else if (formData.lastName.trim().length < 2) {
      newErrors.lastName = "Last name must be at least 2 characters";
    }

    // Organization name validation
    if (!formData.organizationName.trim()) {
      newErrors.organizationName = "Organization name is required";
    } else if (formData.organizationName.trim().length < 2) {
      newErrors.organizationName =
        "Organization name must be at least 2 characters";
    }

    // Terms acceptance validation
    if (!formData.acceptTerms) {
      newErrors.acceptTerms = "You must accept the terms and conditions";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleInputChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>
  ) => {
    const { name, value, type } = e.target;
    const checked =
      type === "checkbox" ? (e.target as HTMLInputElement).checked : undefined;

    setFormData((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));

    // Clear field-specific error when user starts typing
    if (errors[name as keyof FormErrors]) {
      setErrors((prev) => ({ ...prev, [name]: undefined }));
    }

    // Clear general error
    if (error) {
      clearError();
    }
  };

  const handleNextStep = () => {
    if (validateStep1()) {
      setCurrentStep(2);
    }
  };

  const handlePrevStep = () => {
    setCurrentStep(1);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (currentStep === 1) {
      handleNextStep();
      return;
    }

    if (!validateStep2()) {
      return;
    }

    try {
      await register({
        email: formData.email.trim(),
        password: formData.password,
        firstName: formData.firstName.trim(),
        lastName: formData.lastName.trim(),
        organizationName: formData.organizationName.trim(),
        timeZone: formData.timeZone,
      });

      // Registration successful - user will be redirected by ProtectedRoute logic
      console.log("Registration successful");
    } catch (err) {
      // Error is handled by auth context and displayed via error state
      console.error("Registration failed:", err);
    }
  };

  const renderPasswordRequirements = () => {
    if (!showPasswordRequirements && !formData.password) return null;

    return (
      <div className='mt-2 p-3 bg-gray-50 rounded-md border'>
        <h4 className='text-sm font-medium text-gray-700 mb-2'>
          Password Requirements:
        </h4>
        <ul className='space-y-1'>
          {PASSWORD_REQUIREMENTS.map((req, index) => {
            const isMet = req.test(formData.password);
            return (
              <li
                key={index}
                className={`text-xs flex items-center ${
                  isMet ? "text-green-600" : "text-gray-500"
                }`}
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
    );
  };

  const isStep1Valid =
    formData.email && formData.password && formData.confirmPassword;
  const isStep2Valid =
    formData.firstName &&
    formData.lastName &&
    formData.organizationName &&
    formData.acceptTerms;

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
              d='M18 9v3m0 0v3m0-3h3m-3 0h-3m-2-5a4 4 0 11-8 0 4 4 0 018 0zM3 20a6 6 0 0112 0v1H3v-1z'
            />
          </svg>
        </div>
        <h1 className='text-2xl font-bold text-gray-900'>
          Create your account
        </h1>
        <p className='text-gray-600 mt-2'>
          Get started with your workflow automation platform
        </p>
      </div>

      {/* Progress Steps */}
      <div className='mb-8'>
        <div className='flex items-center'>
          <div
            className={`flex items-center justify-center w-8 h-8 rounded-full text-sm font-medium ${
              currentStep >= 1
                ? "bg-primary-600 text-white"
                : "bg-gray-200 text-gray-600"
            }`}
          >
            1
          </div>
          <div
            className={`flex-1 h-1 mx-2 ${
              currentStep >= 2 ? "bg-primary-600" : "bg-gray-200"
            }`}
          />
          <div
            className={`flex items-center justify-center w-8 h-8 rounded-full text-sm font-medium ${
              currentStep >= 2
                ? "bg-primary-600 text-white"
                : "bg-gray-200 text-gray-600"
            }`}
          >
            2
          </div>
        </div>
        <div className='flex justify-between mt-2'>
          <span className='text-xs text-gray-600'>Account</span>
          <span className='text-xs text-gray-600'>Details</span>
        </div>
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
        {currentStep === 1 && (
          <>
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
              ref={passwordInputRef}
              label='Password'
              type='password'
              name='password'
              value={formData.password}
              onChange={handleInputChange}
              onFocus={() => setShowPasswordRequirements(true)}
              onBlur={() => setShowPasswordRequirements(false)}
              error={errors.password}
              placeholder='Create a strong password'
              autoComplete='new-password'
              required
            />

            {renderPasswordRequirements()}

            <Input
              label='Confirm Password'
              type='password'
              name='confirmPassword'
              value={formData.confirmPassword}
              onChange={handleInputChange}
              error={errors.confirmPassword}
              placeholder='Confirm your password'
              autoComplete='new-password'
              required
            />
          </>
        )}

        {currentStep === 2 && (
          <>
            <div className='grid grid-cols-2 gap-4'>
              <Input
                label='First Name'
                type='text'
                name='firstName'
                value={formData.firstName}
                onChange={handleInputChange}
                error={errors.firstName}
                placeholder='John'
                autoComplete='given-name'
                required
              />

              <Input
                label='Last Name'
                type='text'
                name='lastName'
                value={formData.lastName}
                onChange={handleInputChange}
                error={errors.lastName}
                placeholder='Doe'
                autoComplete='family-name'
                required
              />
            </div>

            <Input
              label='Organization Name'
              type='text'
              name='organizationName'
              value={formData.organizationName}
              onChange={handleInputChange}
              error={errors.organizationName}
              placeholder='Acme Corp'
              autoComplete='organization'
              required
            />

            <div>
              <label className='form-label'>Time Zone</label>
              <select
                name='timeZone'
                value={formData.timeZone}
                onChange={handleInputChange}
                className='form-input'
              >
                <option value='UTC'>UTC</option>
                <option value='America/New_York'>Eastern Time</option>
                <option value='America/Chicago'>Central Time</option>
                <option value='America/Denver'>Mountain Time</option>
                <option value='America/Los_Angeles'>Pacific Time</option>
                <option value='Europe/London'>London</option>
                <option value='Europe/Paris'>Paris</option>
                <option value='Europe/Rome'>Rome</option>
                <option value='Asia/Tokyo'>Tokyo</option>
                <option value='Asia/Shanghai'>Shanghai</option>
                <option value='Australia/Sydney'>Sydney</option>
              </select>
            </div>

            <div className='space-y-4'>
              <div className='flex items-start'>
                <input
                  type='checkbox'
                  name='acceptTerms'
                  checked={formData.acceptTerms}
                  onChange={handleInputChange}
                  className='h-4 w-4 text-primary-600 focus:ring-primary-500 border-gray-300 rounded mt-1'
                />
                <div className='ml-3'>
                  <label className='text-sm text-gray-600'>
                    I agree to the{" "}
                    <a
                      href='/terms'
                      target='_blank'
                      rel='noopener noreferrer'
                      className='text-primary-600 hover:text-primary-500 font-medium'
                    >
                      Terms of Service
                    </a>{" "}
                    and{" "}
                    <a
                      href='/privacy'
                      target='_blank'
                      rel='noopener noreferrer'
                      className='text-primary-600 hover:text-primary-500 font-medium'
                    >
                      Privacy Policy
                    </a>
                  </label>
                  {errors.acceptTerms && (
                    <p className='text-sm text-error-600 mt-1'>
                      {errors.acceptTerms}
                    </p>
                  )}
                </div>
              </div>
            </div>
          </>
        )}

        {/* Form Navigation */}
        <div className='flex space-x-4'>
          {currentStep === 2 && (
            <Button
              type='button'
              variant='secondary'
              onClick={handlePrevStep}
              className='flex-1'
            >
              Back
            </Button>
          )}

          <Button
            type='submit'
            variant='primary'
            loading={isLoading}
            disabled={
              (currentStep === 1 && !isStep1Valid) ||
              (currentStep === 2 && (!isStep2Valid || isLoading))
            }
            className='flex-1'
          >
            {isLoading
              ? "Creating account..."
              : currentStep === 1
                ? "Next"
                : "Create account"}
          </Button>
        </div>
      </form>

      {/* Login Link */}
      <div className='mt-8 text-center'>
        <p className='text-sm text-gray-600'>
          Already have an account?{" "}
          <button
            type='button'
            onClick={onToggleForm}
            className='font-medium text-primary-600 hover:text-primary-500 transition-colors'
          >
            Sign in
          </button>
        </p>
      </div>

      {/* Features Preview - Only on step 1 */}
      {currentStep === 1 && (
        <div className='mt-8 p-4 bg-gray-50 rounded-lg'>
          <h4 className='text-sm font-medium text-gray-900 mb-3'>
            What you'll get:
          </h4>
          <ul className='space-y-2 text-sm text-gray-600'>
            <li className='flex items-center'>
              <svg
                className='w-4 h-4 text-green-500 mr-2'
                fill='currentColor'
                viewBox='0 0 20 20'
              >
                <path
                  fillRule='evenodd'
                  d='M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z'
                  clipRule='evenodd'
                />
              </svg>
              Team collaboration
            </li>
          </ul>
        </div>
      )}
    </div>
  );
};
