export interface RankingRow {
  rank: number;
  submissionId: string;
  teamId: string;
  teamName: string;
  categoryName?: string;
  totalScore: number;
  submittedAt: string;
}

export interface AdvanceRoundDetails {
  fromRound?: {
    roundId: string;
    roundName: string;
  };
  toRound?: {
    roundId: string;
    roundName: string;
  };
  expectedScoreCount?: number;
  actualScoreCount?: number;
  missingScoreCount?: number;
  missingScores?: Array<{
    teamId: string;
    teamName: string;
    submissionId: string;
    judgeId: string;
    criteriaId: string;
  }>;
  teams?: Array<{
    teamId: string;
    teamName: string;
    categoryId: string;
  }>;
  categoryIds?: string[];
  advancedTeams?: Array<{
    teamId: string;
    teamName: string;
    categoryId: string;
    totalScore: number;
  }>;
  eliminatedTeams?: Array<{
    teamId: string;
    teamName: string;
    categoryId: string;
    totalScore: number;
  }>;
}

export interface AdvanceRoundResponse {
  message: string;
  details?: AdvanceRoundDetails;
}

export interface AdvanceRoundError {
  message: string;
  details?: AdvanceRoundDetails;
}
