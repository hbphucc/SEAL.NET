import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { submissionService } from "@/services/submissionService";
import { SubmissionPayload } from "@/types/submission";
import { getErrorMessage } from "@/lib/utils";

export const SUBMISSION_KEYS = {
  byTeam: (teamId: string) => ["submissions", "team", teamId] as const,
  byRound: (roundId: string) => ["submissions", "round", roundId] as const,
};

export function useTeamSubmissions(teamId?: string) {
  return useQuery({
    queryKey: SUBMISSION_KEYS.byTeam(teamId ?? ""),
    queryFn: () => submissionService.getByTeam(teamId!),
    enabled: !!teamId,
  });
}

export function useSubmitProject(teamId?: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: SubmissionPayload) => submissionService.submit(data),
    onSuccess: (data) => {
      toast.success(data.message);
      if (teamId) {
        qc.invalidateQueries({ queryKey: SUBMISSION_KEYS.byTeam(teamId) });
      }
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useRoundSubmissions(roundId: string, enabled = true) {
  return useQuery({
    queryKey: SUBMISSION_KEYS.byRound(roundId),
    queryFn: () => submissionService.getByRound(roundId),
    enabled: enabled && !!roundId,
  });
}
