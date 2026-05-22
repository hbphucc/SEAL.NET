import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { teamService } from "@/services/teamService";
import { EliminateTeamRequest } from "@/types/team";
import { toast } from "sonner";
import { getErrorMessage } from "@/lib/utils";

export const TEAM_KEYS = {
  all: ["teams"] as const,
  myTeam: ["teams", "my-team"] as const,
  invites: ["teams", "invites"] as const,
};

export function useAdminTeams(enabled = true) {
  return useQuery({
    queryKey: TEAM_KEYS.all,
    queryFn: teamService.adminGetAll,
    enabled,
  });
}

export function useMyTeam(enabled = true) {
  return useQuery({
    queryKey: TEAM_KEYS.myTeam,
    queryFn: teamService.getMyTeam,
    enabled,
    retry: false, // Don't retry if user has no team
  });
}

export function useApproveTeam() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (teamId: string) => teamService.adminApprove(teamId),
    onSuccess: (data) => {
      toast.success(data.message);
      qc.invalidateQueries({ queryKey: TEAM_KEYS.all });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useCreateTeam() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: teamService.create,
    onSuccess: (data) => {
      toast.success(data.message);
      qc.invalidateQueries({ queryKey: TEAM_KEYS.myTeam });
      qc.invalidateQueries({ queryKey: TEAM_KEYS.all });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useAddTeamMember() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ teamId, data }: { teamId: string; data: { studentCode: string } }) =>
      teamService.addMember(teamId, data),
    onSuccess: (data) => {
      toast.success(data.message);
      qc.invalidateQueries({ queryKey: TEAM_KEYS.myTeam });
      qc.invalidateQueries({ queryKey: TEAM_KEYS.all });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useRejectTeam() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: string | { teamId: string; reason?: string }) => {
      const teamId = typeof input === "string" ? input : input.teamId;
      const reason = typeof input === "string" ? undefined : input.reason;
      return teamService.adminReject(teamId, reason);
    },
    onSuccess: (data) => {
      toast.success(data.message);
      qc.invalidateQueries({ queryKey: TEAM_KEYS.all });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useUpdateTeam() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ teamId, data }: { teamId: string; data: Parameters<typeof teamService.update>[1] }) =>
      teamService.update(teamId, data),
    onSuccess: (data) => {
      toast.success(data.message);
      qc.invalidateQueries({ queryKey: TEAM_KEYS.myTeam });
      qc.invalidateQueries({ queryKey: TEAM_KEYS.all });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function usePendingInvites(enabled = true) {
  return useQuery({
    queryKey: TEAM_KEYS.invites,
    queryFn: teamService.pendingInvites,
    enabled,
  });
}

export function useInviteTeamMember() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ teamId, data }: { teamId: string; data: Parameters<typeof teamService.invite>[1] }) =>
      teamService.invite(teamId, data),
    onSuccess: (data) => {
      toast.success(data.message);
      qc.invalidateQueries({ queryKey: TEAM_KEYS.invites });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useAcceptInvite() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: teamService.acceptInvite,
    onSuccess: (data) => {
      toast.success(data.message);
      qc.invalidateQueries({ queryKey: TEAM_KEYS.invites });
      qc.invalidateQueries({ queryKey: TEAM_KEYS.myTeam });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useRejectInvite() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: teamService.rejectInvite,
    onSuccess: (data) => {
      toast.success(data.message);
      qc.invalidateQueries({ queryKey: TEAM_KEYS.invites });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useCancelInvite() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ teamId, inviteId }: { teamId: string; inviteId: string }) =>
      teamService.cancelInvite(teamId, inviteId),
    onSuccess: (data) => {
      toast.success(data.message);
      qc.invalidateQueries({ queryKey: TEAM_KEYS.invites });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useLeaveTeam() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: teamService.leave,
    onSuccess: (data) => {
      toast.success(data.message);
      qc.invalidateQueries({ queryKey: TEAM_KEYS.myTeam });
      qc.invalidateQueries({ queryKey: TEAM_KEYS.all });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useDisbandTeam() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: teamService.disband,
    onSuccess: (data) => {
      toast.success(data.message);
      qc.invalidateQueries({ queryKey: TEAM_KEYS.myTeam });
      qc.invalidateQueries({ queryKey: TEAM_KEYS.all });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useEliminateTeam() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ teamId, data }: { teamId: string; data: EliminateTeamRequest }) =>
      teamService.adminEliminate(teamId, data),
    onSuccess: (data) => {
      toast.success(data.message);
      qc.invalidateQueries({ queryKey: TEAM_KEYS.all });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useDeleteTeam() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (teamId: string) => teamService.adminDelete(teamId),
    onSuccess: (data) => {
      toast.success(data.message);
      qc.invalidateQueries({ queryKey: TEAM_KEYS.all });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useRemoveTeamMember() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ teamId, studentCode }: { teamId: string; studentCode: string }) =>
      teamService.removeMember(teamId, studentCode),
    onSuccess: (data) => {
      toast.success(data.message);
      qc.invalidateQueries({ queryKey: TEAM_KEYS.all });
      qc.invalidateQueries({ queryKey: TEAM_KEYS.myTeam });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}
