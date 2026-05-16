import api from "@/lib/axios";
import { JudgeAssignment } from "@/types/judge";

export const judgeService = {
  async getAssignments(): Promise<JudgeAssignment[]> {
    const res = await api.get<JudgeAssignment[]>("/admin/judge-assignments");
    return res.data;
  },
};
