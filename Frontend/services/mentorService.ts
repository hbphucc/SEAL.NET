import api from "@/lib/axios";
import { MentorshipNote, MentorTeam } from "@/types/mentor";
import { Submission } from "@/types/submission";
import { Team } from "@/types/team";

export const mentorService = {
  async assign(teamId: string, mentorId: string): Promise<{ message: string }> {
    const res = await api.post<{ message: string }>(`/admin/teams/${teamId}/mentors`, { mentorId });
    return res.data;
  },

  async unassign(teamId: string, mentorId: string): Promise<{ message: string }> {
    const res = await api.delete<{ message: string }>(`/admin/teams/${teamId}/mentors/${mentorId}`);
    return res.data;
  },

  async getAssignedTeams(): Promise<MentorTeam[]> {
    const res = await api.get<Team[]>("/mentor/teams");
    return res.data;
  },

  async getTeamSubmissions(teamId: string): Promise<Submission[]> {
    const res = await api.get<Submission[]>(`/mentor/teams/${teamId}/submissions`);
    return res.data;
  },

  async getNotes(teamId: string): Promise<MentorshipNote[]> {
    const res = await api.get<MentorshipNote[]>(`/mentor/teams/${teamId}/notes`);
    return res.data;
  },

  async addNote(teamId: string, body: string): Promise<{ message: string; mentorshipNoteId: string }> {
    const res = await api.post<{ message: string; mentorshipNoteId: string }>(`/mentor/teams/${teamId}/notes`, { body });
    return res.data;
  },
};
