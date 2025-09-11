import { apiClient } from "../../utils/api";
import { API_ENDPOINTS } from "../../constants";
import type {
  Organization,
  OrganizationMember,
  ApiResponse,
  PaginatedResponse,
} from "../../types";

interface CreateOrganizationRequest {
  name: string;
  slug?: string;
}

interface UpdateOrganizationRequest {
  name: string;
  slug?: string;
}

interface InviteMemberRequest {
  email: string;
  role: "Admin" | "Member" | "Viewer";
}

interface UpdateMemberRoleRequest {
  role: "Owner" | "Admin" | "Member" | "Viewer";
}

interface OrganizationInvite {
  id: string;
  email: string;
  role: string;
  status: "Pending" | "Accepted" | "Declined" | "Expired";
  invitedBy: string;
  invitedAt: string;
  expiresAt: string;
}

export const organizationService = {
  async getCurrentOrganization(): Promise<ApiResponse<Organization>> {
    return apiClient.get(API_ENDPOINTS.ORGANIZATIONS.BASE + "/current");
  },

  async getUserOrganizations(): Promise<ApiResponse<Organization[]>> {
    return apiClient.get(API_ENDPOINTS.ORGANIZATIONS.BASE);
  },

  async createOrganization(
    data: CreateOrganizationRequest
  ): Promise<ApiResponse<Organization>> {
    return apiClient.post(API_ENDPOINTS.ORGANIZATIONS.BASE, data);
  },

  async updateOrganization(
    orgId: string,
    data: UpdateOrganizationRequest
  ): Promise<ApiResponse<Organization>> {
    return apiClient.put(`${API_ENDPOINTS.ORGANIZATIONS.BASE}/${orgId}`, data);
  },

  async deleteOrganization(orgId: string): Promise<ApiResponse> {
    return apiClient.delete(`${API_ENDPOINTS.ORGANIZATIONS.BASE}/${orgId}`);
  },

  async switchOrganization(
    orgId: string
  ): Promise<ApiResponse<{ user: any; organization: Organization }>> {
    return apiClient.post(API_ENDPOINTS.ORGANIZATIONS.SWITCH(orgId));
  },

  // Members management
  async getMembers(
    orgId: string,
    page = 1,
    pageSize = 20
  ): Promise<ApiResponse<PaginatedResponse<OrganizationMember>>> {
    return apiClient.get(API_ENDPOINTS.ORGANIZATIONS.MEMBERS(orgId), {
      params: { page, pageSize },
    });
  },

  async inviteMember(
    orgId: string,
    data: InviteMemberRequest
  ): Promise<ApiResponse> {
    return apiClient.post(API_ENDPOINTS.ORGANIZATIONS.INVITES(orgId), data);
  },

  async updateMemberRole(
    orgId: string,
    memberId: string,
    data: UpdateMemberRoleRequest
  ): Promise<ApiResponse<OrganizationMember>> {
    return apiClient.put(
      `${API_ENDPOINTS.ORGANIZATIONS.MEMBERS(orgId)}/${memberId}`,
      data
    );
  },

  async removeMember(orgId: string, memberId: string): Promise<ApiResponse> {
    return apiClient.delete(
      `${API_ENDPOINTS.ORGANIZATIONS.MEMBERS(orgId)}/${memberId}`
    );
  },

  // Invites management
  async getInvites(
    orgId: string,
    page = 1,
    pageSize = 20
  ): Promise<ApiResponse<PaginatedResponse<OrganizationInvite>>> {
    return apiClient.get(API_ENDPOINTS.ORGANIZATIONS.INVITES(orgId), {
      params: { page, pageSize },
    });
  },

  async cancelInvite(orgId: string, inviteId: string): Promise<ApiResponse> {
    return apiClient.delete(
      `${API_ENDPOINTS.ORGANIZATIONS.INVITES(orgId)}/${inviteId}`
    );
  },

  async resendInvite(orgId: string, inviteId: string): Promise<ApiResponse> {
    return apiClient.post(
      `${API_ENDPOINTS.ORGANIZATIONS.INVITES(orgId)}/${inviteId}/resend`
    );
  },

  async acceptInvite(
    token: string
  ): Promise<ApiResponse<{ user: any; organization: Organization }>> {
    return apiClient.post("/invites/accept", { token });
  },

  async declineInvite(token: string): Promise<ApiResponse> {
    return apiClient.post("/invites/decline", { token });
  },
};
