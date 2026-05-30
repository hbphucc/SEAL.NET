import api from "@/lib/axios";
import axios from "axios";
import {
  Team,
  CreateTeamRequest,
  EliminateTeamRequest,
  AddTeamMemberRequest,
  AddTeamMemberResponse,
  InviteTeamMemberRequest,
  TeamInvite,
  TransferLeadershipRequest,
  UpdateTeamRequest,
} from "@/types/team";

export const teamService = {
  // Admin endpoints
  async adminGetAll(): Promise<Team[]> {
    const res = await api.get<Team[]>("/admin/teams");
    return res.data;
  },

  async adminApprove(teamId: string): Promise<{ message: string }> {
    const res = await api.put<{ message: string }>(`/admin/teams/${teamId}/approve`);
    return res.data;
  },

  async adminReject(teamId: string, reason?: string): Promise<{ message: string }> {
    const res = await api.put<{ message: string }>(`/admin/teams/${teamId}/reject`, { reason });
    return res.data;
  },

  async adminEliminate(teamId: string, data: EliminateTeamRequest): Promise<{ message: string }> {
    const res = await api.put<{ message: string }>(`/admin/teams/${teamId}/eliminate`, data);
    return res.data;
  },

  async adminDelete(teamId: string): Promise<{ message: string }> {
    const res = await api.delete<{ message: string }>(`/admin/teams/${teamId}`);
    return res.data;
  },

  // Member/TeamLeader endpoints
  async create(data: CreateTeamRequest): Promise<{ message: string; teamId: string }> {
    const res = await api.post<{ message: string; teamId: string }>("/teams", data);
    return res.data;
  },

  async getMyTeam(): Promise<Team | null> {
    try {
      const res = await api.get<Team | null>("/teams/my-team");
      return res.data;
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 404) {
        return null;
      }

      throw err;
    }
  },

  async addMember(data: AddTeamMemberRequest): Promise<AddTeamMemberResponse> {
    const res = await api.post<AddTeamMemberResponse>("/teams/my-team/members", data);
    return res.data;
  },

  async removeMember(teamId: string, studentCode: string): Promise<{ message: string }> {
    const res = await api.delete<{ message: string }>(`/teams/${teamId}/members/${studentCode}`);
    return res.data;
  },

  async update(teamId: string, data: UpdateTeamRequest): Promise<{ message: string }> {
    const res = await api.put<{ message: string }>(`/teams/${teamId}`, data);
    return res.data;
  },

  async leave(teamId: string): Promise<{ message: string }> {
    const res = await api.post<{ message: string }>(`/teams/${teamId}/leave`);
    return res.data;
  },

  async disband(teamId: string): Promise<{ message: string }> {
    const res = await api.post<{ message: string }>(`/teams/${teamId}/disband`);
    return res.data;
  },

  async transferLeadership(teamId: string, data: TransferLeadershipRequest): Promise<{ message: string }> {
    const res = await api.post<{ message: string }>(`/teams/${teamId}/transfer-leadership`, data);
    return res.data;
  },

  async invite(teamId: string, data: InviteTeamMemberRequest): Promise<{ message: string; teamInviteId: string }> {
    const res = await api.post<{ message: string; teamInviteId: string }>(`/teams/${teamId}/invites`, data);
    return res.data;
  },

  async pendingInvites(): Promise<TeamInvite[]> {
    const res = await api.get<TeamInvite[]>("/teams/invites/pending");
    return res.data;
  },

  async acceptInvite(inviteId: string): Promise<{ message: string }> {
    const res = await api.post<{ message: string }>(`/teams/invites/${inviteId}/accept`);
    return res.data;
  },

  async rejectInvite(inviteId: string): Promise<{ message: string }> {
    const res = await api.post<{ message: string }>(`/teams/invites/${inviteId}/reject`);
    return res.data;
  },

  async cancelInvite(teamId: string, inviteId: string): Promise<{ message: string }> {
    const res = await api.delete<{ message: string }>(`/teams/${teamId}/invites/${inviteId}`);
    return res.data;
  },
};
