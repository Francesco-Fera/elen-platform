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
    return apiClient.post(API_ENDPOINTS.AUTH.LOGIN, credentials);
  },

  async register(data: RegisterRequest): Promise<ApiResponse<LoginResponse>> {
    return apiClient.post(API_ENDPOINTS.AUTH.REGISTER, data);
  },

  async logout(): Promise<ApiResponse> {
    return apiClient.post(API_ENDPOINTS.AUTH.LOGOUT);
  },

  async refreshToken(
    refreshToken: string
  ): Promise<ApiResponse<{ accessToken: string }>> {
    return apiClient.post(API_ENDPOINTS.AUTH.REFRESH, { refreshToken });
  },

  async forgotPassword(data: ForgotPasswordRequest): Promise<ApiResponse> {
    return apiClient.post(API_ENDPOINTS.AUTH.FORGOT_PASSWORD, data);
  },

  async resetPassword(data: ResetPasswordRequest): Promise<ApiResponse> {
    return apiClient.post(API_ENDPOINTS.AUTH.RESET_PASSWORD, data);
  },

  async verifyEmail(token: string): Promise<ApiResponse> {
    return apiClient.post(API_ENDPOINTS.AUTH.VERIFY_EMAIL, { token });
  },

  // async getProfile(): Promise<ApiResponse<ProfileResponse>> {
  //   return apiClient.get(API_ENDPOINTS.USER.PROFILE);
  // },

  async getProfile(): Promise<ApiResponse<ProfileResponse>> {
    console.log("🔧 AuthService: Calling getProfile...");
    try {
      return apiClient.get(API_ENDPOINTS.USER.PROFILE);
    } catch (error) {
      console.log("❌ AuthService: getProfile error:", error);
      throw error;
    }
  },

  async updateProfile(data: UpdateProfileRequest): Promise<ApiResponse<User>> {
    return apiClient.put(API_ENDPOINTS.USER.UPDATE_PROFILE, data);
  },

  async changePassword(data: ChangePasswordRequest): Promise<ApiResponse> {
    return apiClient.post(API_ENDPOINTS.USER.CHANGE_PASSWORD, data);
  },
};
