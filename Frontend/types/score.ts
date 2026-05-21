export interface JudgeSubmission {
  submissionId: string;
  repositoryUrl?: string | null;
  demoUrl?: string | null;
  slideUrl?: string | null;
  team: {
    teamId: string;
    teamName: string;
    category: string;
  };
  round: {
    roundId: string;
    roundName: string;
  };
}

export interface ScorePayload {
  submissionId: string;
  criteriaId: string;
  scoreValue: number;
  comment?: string;
  submitFinal?: boolean;
}

export interface BulkScoreItemPayload {
  criteriaId: string;
  scoreValue: number;
  comment?: string;
}

export interface BulkScorePayload {
  submissionId: string;
  scores: BulkScoreItemPayload[];
  submitFinal?: boolean;
}

export interface BulkScoreResponse {
  message: string;
  createdCount: number;
  updatedCount: number;
}
