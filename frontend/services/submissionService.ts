import api from "@/lib/axios";
import { Submission, SubmissionPayload } from "@/types/submission";

export const submissionService = {
  async submit(data: SubmissionPayload): Promise<{ message: string; submissionId: string }> {
    const res = await api.post<{ message: string; submissionId: string }>("/submissions", data);
    return res.data;
  },

  async getByTeam(teamId: string): Promise<Submission[]> {
    const res = await api.get<Submission[]>(`/submissions/team/${teamId}`);
    return res.data;
  },

  async getByRound(roundId: string): Promise<Submission[]> {
    const res = await api.get<Submission[]>(`/submissions/round/${roundId}`);
    return res.data;
  },
};
