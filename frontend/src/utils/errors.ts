import { AxiosError } from "axios";
import type { ApiResponse } from "../types";

export interface AppError {
  message: string;
  code?: string;
  field?: string;
}

export const errorUtils = {
  // Parse API error responses
  parseApiError: (error: unknown): AppError => {
    if (error instanceof AxiosError) {
      const response = error.response?.data as ApiResponse;

      if (response?.errors) {
        // Handle validation errors (field-specific)
        const firstField = Object.keys(response.errors)[0];
        const firstError = response.errors[firstField]?.[0];
        return {
          message: firstError || "Validation error occurred",
          field: firstField,
          code: "VALIDATION_ERROR",
        };
      }

      if (response?.message) {
        return {
          message: response.message,
          code: error.response?.status?.toString(),
        };
      }

      // Handle network errors
      if (error.code === "NETWORK_ERROR") {
        return {
          message: "Network error. Please check your connection.",
          code: "NETWORK_ERROR",
        };
      }

      // Handle timeout errors
      if (error.code === "ECONNABORTED") {
        return {
          message: "Request timeout. Please try again.",
          code: "TIMEOUT_ERROR",
        };
      }
    }

    // Fallback for unknown errors
    return {
      message:
        error instanceof Error ? error.message : "An unexpected error occurred",
      code: "UNKNOWN_ERROR",
    };
  },

  // Get user-friendly error message
  getErrorMessage: (error: unknown): string => {
    const parsedError = errorUtils.parseApiError(error);
    return parsedError.message;
  },

  // Check if error is a specific type
  isNetworkError: (error: unknown): boolean => {
    return error instanceof AxiosError && error.code === "NETWORK_ERROR";
  },

  isTimeoutError: (error: unknown): boolean => {
    return error instanceof AxiosError && error.code === "ECONNABORTED";
  },

  isValidationError: (error: unknown): boolean => {
    const parsedError = errorUtils.parseApiError(error);
    return parsedError.code === "VALIDATION_ERROR";
  },

  isUnauthorizedError: (error: unknown): boolean => {
    return error instanceof AxiosError && error.response?.status === 401;
  },

  isForbiddenError: (error: unknown): boolean => {
    return error instanceof AxiosError && error.response?.status === 403;
  },
};
