import api from "@/lib/axios";
import { JudgeAssignment } from "@/types/judge";

export const judgeService = {
  async getAssignments(): Promise<JudgeAssignment[]> {
    const res = await api.get<JudgeAssignment[]>("/admin/judge-assignments");
    return res.data;
  },

  async createAssignment(data: {
    judgeId: string;
    roundId: string;
    categoryId: string;
  }): Promise<{ message: string; assignmentId: string }> {
    const res = await api.post<{ message: string; assignmentId: string }>("/admin/judge-assignments", data);
    return res.data;
  },

  async deleteAssignment(assignmentId: string): Promise<{ message: string }> {
    const res = await api.delete<{ message: string }>(`/admin/judge-assignments/${assignmentId}`);
    return res.data;
  },
};
