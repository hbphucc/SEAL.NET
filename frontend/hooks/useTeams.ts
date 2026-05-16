import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { teamService } from "@/services/teamService";
import { EliminateTeamRequest } from "@/types/team";
import { toast } from "sonner";
import { getErrorMessage } from "@/lib/utils";

export const TEAM_KEYS = {
  all: ["teams"] as const,
  myTeam: ["teams", "my-team"] as const,
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
    mutationFn: ({ teamId, data }: { teamId: string; data: { userId: string } }) =>
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
    mutationFn: (teamId: string) => teamService.adminReject(teamId),
    onSuccess: (data) => {
      toast.success(data.message);
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

export function useRemoveTeamMember() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ teamId, userId }: { teamId: string; userId: string }) =>
      teamService.removeMember(teamId, userId),
    onSuccess: (data) => {
      toast.success(data.message);
      qc.invalidateQueries({ queryKey: TEAM_KEYS.all });
      qc.invalidateQueries({ queryKey: TEAM_KEYS.myTeam });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}
