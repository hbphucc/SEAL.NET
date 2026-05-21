import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { judgeService } from "@/services/judgeService";
import { getErrorMessage } from "@/lib/utils";

export const JUDGE_KEYS = {
  assignments: ["judge-assignments"] as const,
};

export function useJudgeAssignments(enabled = true) {
  return useQuery({
    queryKey: JUDGE_KEYS.assignments,
    queryFn: judgeService.getAssignments,
    enabled,
  });
}

export function useCreateJudgeAssignment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: judgeService.createAssignment,
    onSuccess: (data) => {
      toast.success(data.message);
      qc.invalidateQueries({ queryKey: JUDGE_KEYS.assignments });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useDeleteJudgeAssignment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: judgeService.deleteAssignment,
    onSuccess: (data) => {
      toast.success(data.message);
      qc.invalidateQueries({ queryKey: JUDGE_KEYS.assignments });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}
