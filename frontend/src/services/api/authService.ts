import { apiClient } from "../../utils/api";
import { API_ENDPOINTS } from "../../constants";
import type {
  LoginRequest,
  RegisterRequest,
  LoginResponse,
  User,
  Organization,
  ApiResponse,
} from "../../types";

interface ForgotPasswordRequest {
  email: string;
}

interface ResetPasswordRequest {
  token: string;
  password: string;
}

interface VerifyEmailRequest {
  token: string;
}

interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

interface UpdateProfileRequest {
  firstName: string;
  lastName: string;
  timeZone?: string;
}

interface ProfileResponse {
  user: User;
  organization: Organization;
}

export const authService = {
  async login(credentials: LoginRequest): Promise<ApiResponse<LoginResponse>> {
    console.log("🔧 AuthService: Calling login...");
    try {
      const response = await apiClient.post<LoginResponse>(
        API_ENDPOINTS.AUTH.LOGIN,
        credentials
      );
      console.log("✅ AuthService: Login successful");
      return response;
    } catch (error) {
      console.log("❌ AuthService: Login error:", error);
      throw error;
    }
  },

  async register(data: RegisterRequest): Promise<ApiResponse<LoginResponse>> {
    console.log("🔧 AuthService: Calling register...");
    try {
      const response = await apiClient.post<LoginResponse>(
        API_ENDPOINTS.AUTH.REGISTER,
        data
      );
      console.log("✅ AuthService: Registration successful");
      return response;
    } catch (error) {
      console.log("❌ AuthService: Registration error:", error);
      throw error;
    }
  },

  async logout(refreshToken: string): Promise<ApiResponse> {
    console.log("Logging out with token:", refreshToken);
    console.log("🔧 AuthService: Calling logout...");
    try {
      const response = await apiClient.post(API_ENDPOINTS.AUTH.LOGOUT, {
        refreshToken,
      });
      console.log("✅ AuthService: Logout successful");
      return response;
    } catch (error) {
      console.log("❌ AuthService: Logout error:", error);
      throw error;
    }
  },

  async refreshToken(
    refreshToken: string
  ): Promise<ApiResponse<{ accessToken: string; refreshToken: string }>> {
    console.log("🔧 AuthService: Calling refresh token...");
    try {
      const response = await apiClient.post<{
        accessToken: string;
        refreshToken: string;
      }>(API_ENDPOINTS.AUTH.REFRESH, { refreshToken });
      console.log("✅ AuthService: Token refresh successful");
      return response;
    } catch (error) {
      console.log("❌ AuthService: Token refresh error:", error);
      throw error;
    }
  },

  async verifyEmail(token: string): Promise<ApiResponse> {
    console.log("🔧 AuthService: Calling verify email...");
    try {
      const response = await apiClient.post(API_ENDPOINTS.AUTH.VERIFY_EMAIL, {
        token,
      });
      console.log("✅ AuthService: Email verification successful");
      return response;
    } catch (error) {
      console.log("❌ AuthService: Email verification error:", error);
      throw error;
    }
  },

  async sendEmailVerification(): Promise<ApiResponse> {
    console.log("🔧 AuthService: Calling send email verification...");
    try {
      const response = await apiClient.post("/auth/send-verification");
      console.log("✅ AuthService: Email verification sent");
      return response;
    } catch (error) {
      console.log("❌ AuthService: Send email verification error:", error);
      throw error;
    }
  },

  async forgotPassword(data: ForgotPasswordRequest): Promise<ApiResponse> {
    console.log("🔧 AuthService: Calling forgot password...");
    try {
      const response = await apiClient.post(
        API_ENDPOINTS.AUTH.FORGOT_PASSWORD,
        data
      );
      console.log("✅ AuthService: Forgot password successful");
      return response;
    } catch (error) {
      console.log("❌ AuthService: Forgot password error:", error);
      throw error;
    }
  },

  async resetPassword(data: ResetPasswordRequest): Promise<ApiResponse> {
    console.log("🔧 AuthService: Calling reset password...");
    try {
      const response = await apiClient.post(
        API_ENDPOINTS.AUTH.RESET_PASSWORD,
        data
      );
      console.log("✅ AuthService: Reset password successful");
      return response;
    } catch (error) {
      console.log("❌ AuthService: Reset password error:", error);
      throw error;
    }
  },

  async validateResetToken(
    token: string
  ): Promise<ApiResponse<{ isValid: boolean }>> {
    console.log("🔧 AuthService: Validating reset token...");
    try {
      const response = await apiClient.get<{ isValid: boolean }>(
        `/auth/reset-password/validate?token=${encodeURIComponent(token)}`
      );
      console.log("✅ AuthService: Reset token validation successful");
      return response;
    } catch (error) {
      console.log("❌ AuthService: Reset token validation error:", error);
      throw error;
    }
  },

  async validateEmailToken(
    token: string
  ): Promise<ApiResponse<{ isValid: boolean }>> {
    console.log("🔧 AuthService: Validating email token...");
    try {
      const response = await apiClient.get<{ isValid: boolean }>(
        `/auth/verify-email/validate?token=${encodeURIComponent(token)}`
      );
      return response;
    } catch (error) {
      console.log("❌ AuthService: Email token validation error:", error);
      throw error;
    }
  },

  // Updated to use correct endpoint based on backend implementation
  async getProfile(): Promise<ApiResponse<ProfileResponse>> {
    console.log("🔧 AuthService: Calling getProfile...");
    try {
      // Use the correct endpoint from backend - /user/profile
      const response = await apiClient.get<ProfileResponse>(
        API_ENDPOINTS.USER.PROFILE
      );
      console.log("✅ AuthService: Profile fetch successful");
      return response;
    } catch (error) {
      console.log("❌ AuthService: Profile fetch error:", error);
      throw error;
    }
  },

  async updateProfile(data: UpdateProfileRequest): Promise<ApiResponse<User>> {
    console.log("🔧 AuthService: Calling update profile...");
    try {
      const response = await apiClient.put<User>(
        API_ENDPOINTS.USER.UPDATE_PROFILE,
        data
      );
      console.log("✅ AuthService: Profile update successful");
      return response;
    } catch (error) {
      console.log("❌ AuthService: Profile update error:", error);
      throw error;
    }
  },

  async changePassword(data: ChangePasswordRequest): Promise<ApiResponse> {
    console.log("🔧 AuthService: Calling change password...");
    try {
      const response = await apiClient.post(
        API_ENDPOINTS.USER.CHANGE_PASSWORD,
        data
      );
      console.log("✅ AuthService: Password change successful");
      return response;
    } catch (error) {
      console.log("❌ AuthService: Password change error:", error);
      throw error;
    }
  },

  // Additional utility methods
  async checkEmailAvailability(
    email: string
  ): Promise<ApiResponse<{ available: boolean }>> {
    console.log("🔧 AuthService: Checking email availability...");
    try {
      const response = await apiClient.get<{ available: boolean }>(
        `/auth/check-email?email=${encodeURIComponent(email)}`
      );
      return response;
    } catch (error) {
      console.log("❌ AuthService: Email availability check error:", error);
      throw error;
    }
  },
};
