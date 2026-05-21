export interface JudgeAssignment {
  assignmentId: string;
  judge: {
    judgeId: string;
    fullName: string;
    email: string;
  };
  round: {
    roundId: string;
    roundName: string;
  };
  category: {
    categoryId: string;
    categoryName: string;
  };
}
