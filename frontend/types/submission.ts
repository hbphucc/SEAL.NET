export interface Submission {
  submissionId: string;
  repositoryUrl?: string | null;
  demoUrl?: string | null;
  slideUrl?: string | null;
  submittedAt: string;
  round?: {
    roundId: string;
    roundName: string;
  };
  team?: {
    teamId: string;
    teamName: string;
    category?: string;
    categoryName?: string;
  };
}

export interface SubmissionPayload {
  teamId: string;
  roundId: string;
  repositoryUrl?: string;
  demoUrl?: string;
  slideUrl?: string;
}
