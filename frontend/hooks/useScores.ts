import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { scoreService } from "@/services/scoreService";
import { BulkScorePayload, ScorePayload } from "@/types/score";
import { getErrorMessage } from "@/lib/utils";

export const SCORE_KEYS = {
  assignedSubmissions: ["judge", "assigned-submissions"] as const,
};

export function useAssignedSubmissions(enabled = true) {
  return useQuery({
    queryKey: SCORE_KEYS.assignedSubmissions,
    queryFn: scoreService.getAssignedSubmissions,
    enabled,
  });
}

export function useSubmitScore() {
  return useMutation({
    mutationFn: (data: ScorePayload) => scoreService.submitScore(data),
    onSuccess: (data) => toast.success(data.message),
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useSubmitBulkScores() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: BulkScorePayload) => scoreService.submitBulkScores(data),
    onSuccess: (data) => {
      toast.success(data.message);
      qc.invalidateQueries({ queryKey: SCORE_KEYS.assignedSubmissions });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}
