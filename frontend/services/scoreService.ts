import api from "@/lib/axios";
import { BulkScorePayload, BulkScoreResponse, JudgeSubmission, ScorePayload } from "@/types/score";

export const scoreService = {
  async getAssignedSubmissions(): Promise<JudgeSubmission[]> {
    const res = await api.get<JudgeSubmission[]>("/judge/scores/my-assigned-submissions");
    return res.data;
  },

  async submitScore(data: ScorePayload): Promise<{ message: string; scoreId?: string }> {
    const res = await api.post<{ message: string; scoreId?: string }>("/judge/scores", data);
    return res.data;
  },

  async submitBulkScores(data: BulkScorePayload): Promise<BulkScoreResponse> {
    const res = await api.post<BulkScoreResponse>("/judge/scores/bulk", data);
    return res.data;
  },
};
