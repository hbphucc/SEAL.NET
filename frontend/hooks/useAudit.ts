import { useQuery } from "@tanstack/react-query";
import { auditService } from "@/services/auditService";

export const AUDIT_KEYS = {
  submission: (submissionId: string) => ["audit", "submission", submissionId] as const,
};

export function useSubmissionAuditLogs(submissionId: string) {
  return useQuery({
    queryKey: AUDIT_KEYS.submission(submissionId),
    queryFn: () => auditService.getSubmissionLogs(submissionId),
    enabled: !!submissionId,
  });
}
