export interface ScoreAuditLog {
  scoreAuditLogId: string;
  scoreId: string;
  submissionId: string;
  criteriaId: string;
  criteriaName?: string | null;
  judge?: {
    id: string;
    fullName: string;
    email: string;
  } | null;
  action: string;
  oldScoreValue?: number | null;
  newScoreValue: number;
  oldComment?: string | null;
  newComment?: string | null;
  createdAt: string;
}
