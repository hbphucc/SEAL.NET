import api from "@/lib/axios";
import { AuditLog, ScoreAuditLog } from "@/types/audit";

export const auditService = {
  async getSubmissionLogs(submissionId: string): Promise<ScoreAuditLog[]> {
    const res = await api.get<ScoreAuditLog[]>(`/admin/score-audit-logs/submission/${submissionId}`);
    return res.data;
  },

  async getAuditLogs(): Promise<AuditLog[]> {
    const res = await api.get<AuditLog[]>("/admin/audit-logs");
    return res.data;
  },
};
