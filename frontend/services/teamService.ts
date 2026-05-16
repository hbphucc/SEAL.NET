import api from "@/lib/axios";
import {
  Team,
  CreateTeamRequest,
  EliminateTeamRequest,
  AddTeamMemberRequest,
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

  async adminReject(teamId: string): Promise<{ message: string }> {
    const res = await api.put<{ message: string }>(`/admin/teams/${teamId}/reject`);
    return res.data;
  },

  async adminEliminate(teamId: string, data: EliminateTeamRequest): Promise<{ message: string }> {
    const res = await api.put<{ message: string }>(`/admin/teams/${teamId}/eliminate`, data);
    return res.data;
  },

  // Member/TeamLeader endpoints
  async create(data: CreateTeamRequest): Promise<{ message: string; teamId: string }> {
    const res = await api.post<{ message: string; teamId: string }>("/teams", data);
    return res.data;
  },

  async getMyTeam(): Promise<Team> {
    const res = await api.get<Team>("/teams/my-team");
    return res.data;
  },

  async addMember(teamId: string, data: AddTeamMemberRequest): Promise<{ message: string }> {
    const res = await api.post<{ message: string }>(`/teams/${teamId}/members`, data);
    return res.data;
  },

  async removeMember(teamId: string, userId: string): Promise<{ message: string }> {
    const res = await api.delete<{ message: string }>(`/teams/${teamId}/members/${userId}`);
    return res.data;
  },
};
