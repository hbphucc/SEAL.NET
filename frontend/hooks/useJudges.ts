import { useQuery } from "@tanstack/react-query";
import { judgeService } from "@/services/judgeService";

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
