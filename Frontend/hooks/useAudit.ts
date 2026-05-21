import { useQuery } from "@tanstack/react-query";
import { auditService } from "@/services/auditService";

export const AUDIT_KEYS = {
  all: ["audit"] as const,
  submission: (submissionId: string) => ["audit", "submission", submissionId] as const,
};

export function useAuditLogs() {
  return useQuery({
    queryKey: AUDIT_KEYS.all,
    queryFn: auditService.getAuditLogs,
  });
}

export function useSubmissionAuditLogs(submissionId: string) {
  return useQuery({
    queryKey: AUDIT_KEYS.submission(submissionId),
    queryFn: () => auditService.getSubmissionLogs(submissionId),
    enabled: !!submissionId,
  });
}
