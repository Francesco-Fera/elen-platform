import React, { forwardRef } from "react";

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  helperText?: string;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label, error, helperText, className = "", id, ...props }, ref) => {
    const inputId = id || `input-${Math.random().toString(36).substr(2, 9)}`;

    return (
      <div className='mb-4'>
        {label && (
          <label htmlFor={inputId} className='form-label'>
            {label}
          </label>
        )}
        <input
          ref={ref}
          id={inputId}
          className={`form-input ${error ? "border-error-500 focus:border-error-500 focus:ring-error-500" : ""} ${className}`}
          {...props}
        />
        {error && <p className='form-error'>{error}</p>}
        {helperText && !error && (
          <p className='text-gray-500 text-xs mt-1'>{helperText}</p>
        )}
      </div>
    );
  }
);

Input.displayName = "Input";
