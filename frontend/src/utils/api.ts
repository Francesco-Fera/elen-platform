import axios, {
  AxiosInstance,
  AxiosResponse,
  AxiosError,
  InternalAxiosRequestConfig,
} from "axios";
import { tokenManager } from "../services/tokenManager";
import type { ApiResponse } from "../types";

class ApiClient {
  private instance: AxiosInstance;
  private isRefreshing = false;
  private refreshSubscribers: Array<(token: string | null) => void> = [];

  constructor() {
    this.instance = axios.create({
      baseURL: process.env.REACT_APP_API_URL || "http://localhost:5000/api",
      timeout: 30000,
      headers: {
        "Content-Type": "application/json",
      },
    });

    this.setupInterceptors();
  }

  private setupInterceptors() {
    // Request interceptor - add auth token
    this.instance.interceptors.request.use(
      async (config: InternalAxiosRequestConfig) => {
        // Skip auth for auth endpoints
        if (this.isAuthEndpoint(config.url || "")) {
          return config;
        }

        const token = await tokenManager.getValidToken();
        if (token && config.headers) {
          config.headers.Authorization = `Bearer ${token}`;
        }

        return config;
      },
      (error) => {
        return Promise.reject(error);
      }
    );

    // Response interceptor - handle token refresh
    this.instance.interceptors.response.use(
      (response: AxiosResponse) => response,
      async (error: AxiosError) => {
        const originalRequest = error.config as InternalAxiosRequestConfig & {
          _retry?: boolean;
        };

        // Skip retry for auth endpoints or if already retried
        if (
          error.response?.status === 401 &&
          originalRequest &&
          !originalRequest._retry &&
          !this.isAuthEndpoint(originalRequest.url || "")
        ) {
          originalRequest._retry = true;

          if (this.isRefreshing) {
            // If already refreshing, queue this request
            return new Promise((resolve, reject) => {
              this.refreshSubscribers.push((token: string | null) => {
                if (token && originalRequest.headers) {
                  originalRequest.headers.Authorization = `Bearer ${token}`;
                  resolve(this.instance(originalRequest));
                } else {
                  reject(error);
                }
              });
            });
          }

          try {
            this.isRefreshing = true;
            const newToken = await tokenManager.getValidToken();

            if (newToken && originalRequest.headers) {
              // Notify all queued requests
              this.refreshSubscribers.forEach((callback) => callback(newToken));
              this.refreshSubscribers = [];

              // Retry original request
              originalRequest.headers.Authorization = `Bearer ${newToken}`;
              return this.instance(originalRequest);
            } else {
              // Refresh failed, notify queued requests
              this.refreshSubscribers.forEach((callback) => callback(null));
              this.refreshSubscribers = [];

              // Emit token refresh failed event
              window.dispatchEvent(new CustomEvent("token-refresh-failed"));
            }
          } catch (refreshError) {
            console.error("Token refresh failed:", refreshError);
            this.refreshSubscribers.forEach((callback) => callback(null));
            this.refreshSubscribers = [];
            window.dispatchEvent(new CustomEvent("token-refresh-failed"));
          } finally {
            this.isRefreshing = false;
          }
        }

        return Promise.reject(this.enhanceError(error));
      }
    );
  }

  private isAuthEndpoint(url: string): boolean {
    const authEndpoints = [
      "/auth/login",
      "/auth/register",
      "/auth/refresh",
      "/auth/logout",
    ];
    return authEndpoints.some((endpoint) => url.includes(endpoint));
  }

  private enhanceError(error: AxiosError): AxiosError {
    // Add more specific error information
    if (error.code === "ECONNABORTED") {
      error.message = "Request timeout. Please try again.";
    } else if (error.code === "ERR_NETWORK") {
      error.message = "Network error. Please check your connection.";
    } else if (error.response?.status === 429) {
      error.message = "Too many requests. Please try again later.";
    }

    return error;
  }

  async get<T>(url: string, config?: any): Promise<ApiResponse<T>> {
    try {
      const response = await this.instance.get(url, config);
      return response.data;
    } catch (error) {
      throw this.handleApiError(error);
    }
  }

  async post<T>(
    url: string,
    data?: any,
    config?: any
  ): Promise<ApiResponse<T>> {
    try {
      const response = await this.instance.post(url, data, config);
      return response.data;
    } catch (error) {
      throw this.handleApiError(error);
    }
  }

  async put<T>(url: string, data?: any, config?: any): Promise<ApiResponse<T>> {
    try {
      const response = await this.instance.put(url, data, config);
      return response.data;
    } catch (error) {
      throw this.handleApiError(error);
    }
  }

  async delete<T>(url: string, config?: any): Promise<ApiResponse<T>> {
    try {
      const response = await this.instance.delete(url, config);
      return response.data;
    } catch (error) {
      throw this.handleApiError(error);
    }
  }

  async patch<T>(
    url: string,
    data?: any,
    config?: any
  ): Promise<ApiResponse<T>> {
    try {
      const response = await this.instance.patch(url, data, config);
      return response.data;
    } catch (error) {
      throw this.handleApiError(error);
    }
  }

  private handleApiError(error: unknown): Error {
    if (error instanceof AxiosError) {
      const response = error.response?.data as ApiResponse;

      if (response?.errors) {
        // Handle validation errors
        const firstField = Object.keys(response.errors)[0];
        const firstError = response.errors[firstField]?.[0];
        return new Error(firstError || "Validation error occurred");
      }

      if (response?.message) {
        return new Error(response.message);
      }

      // Handle HTTP status errors
      if (error.response?.status) {
        switch (error.response.status) {
          case 400:
            return new Error("Bad request. Please check your input.");
          case 401:
            return new Error("You are not authorized to perform this action.");
          case 403:
            return new Error(
              "You do not have permission to perform this action."
            );
          case 404:
            return new Error("The requested resource was not found.");
          case 409:
            return new Error(
              "A conflict occurred. The resource already exists."
            );
          case 422:
            return new Error("The provided data is invalid.");
          case 429:
            return new Error("Too many requests. Please try again later.");
          case 500:
            return new Error("Internal server error. Please try again later.");
          case 502:
            return new Error(
              "Bad gateway. The server is temporarily unavailable."
            );
          case 503:
            return new Error("Service unavailable. Please try again later.");
          default:
            return new Error(
              `Request failed with status ${error.response.status}`
            );
        }
      }

      return new Error(error.message || "An unexpected error occurred");
    }

    return new Error("An unexpected error occurred");
  }

  // Utility methods for specific operations
  async uploadFile<T>(
    url: string,
    file: File,
    onProgress?: (progress: number) => void
  ): Promise<ApiResponse<T>> {
    const formData = new FormData();
    formData.append("file", file);

    return this.post<T>(url, formData, {
      headers: {
        "Content-Type": "multipart/form-data",
      },
      onUploadProgress: (progressEvent: any) => {
        if (onProgress && progressEvent.total) {
          const progress = Math.round(
            (progressEvent.loaded * 100) / progressEvent.total
          );
          onProgress(progress);
        }
      },
    });
  }

  // Download file with progress
  async downloadFile(
    url: string,
    filename: string,
    onProgress?: (progress: number) => void
  ): Promise<void> {
    try {
      const response = await this.instance.get(url, {
        responseType: "blob",
        onDownloadProgress: (progressEvent: any) => {
          if (onProgress && progressEvent.total) {
            const progress = Math.round(
              (progressEvent.loaded * 100) / progressEvent.total
            );
            onProgress(progress);
          }
        },
      });

      // Create download link
      const blob = new Blob([response.data]);
      const downloadUrl = window.URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = downloadUrl;
      link.download = filename;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(downloadUrl);
    } catch (error) {
      throw this.handleApiError(error);
    }
  }

  // Get request with retry logic
  async getWithRetry<T>(
    url: string,
    config?: any,
    maxRetries: number = 3,
    retryDelay: number = 1000
  ): Promise<ApiResponse<T>> {
    let lastError: Error;

    for (let attempt = 1; attempt <= maxRetries; attempt++) {
      try {
        return await this.get<T>(url, config);
      } catch (error) {
        lastError = error instanceof Error ? error : new Error("Unknown error");

        if (attempt === maxRetries) {
          break;
        }

        // Don't retry on certain errors
        if (error instanceof AxiosError) {
          const status = error.response?.status;
          if (status && [400, 401, 403, 404, 422].includes(status)) {
            break;
          }
        }

        // Wait before retry with exponential backoff
        await new Promise((resolve) =>
          setTimeout(resolve, retryDelay * attempt)
        );
      }
    }

    throw lastError!;
  }

  // Cancel all pending requests
  cancelAllRequests(): void {
    // Note: This would require implementing a request cancellation system
    // For now, we'll just log the intention
    console.log("Cancelling all pending requests...");
  }

  // Get instance for custom usage
  getInstance(): AxiosInstance {
    return this.instance;
  }
}

export const apiClient = new ApiClient();
