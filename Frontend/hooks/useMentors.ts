import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { mentorService } from "@/services/mentorService";
import { getErrorMessage } from "@/lib/utils";
import { toast } from "sonner";

export const MENTOR_KEYS = {
  teams: ["mentor", "teams"] as const,
  notes: (teamId: string) => ["mentor", "teams", teamId, "notes"] as const,
  submissions: (teamId: string) => ["mentor", "teams", teamId, "submissions"] as const,
};

export function useMentorTeams(enabled = true) {
  return useQuery({
    queryKey: MENTOR_KEYS.teams,
    queryFn: mentorService.getAssignedTeams,
    enabled,
  });
}

export function useMentorshipNotes(teamId: string) {
  return useQuery({
    queryKey: MENTOR_KEYS.notes(teamId),
    queryFn: () => mentorService.getNotes(teamId),
    enabled: !!teamId,
  });
}

export function useMentorTeamSubmissions(teamId: string) {
  return useQuery({
    queryKey: MENTOR_KEYS.submissions(teamId),
    queryFn: () => mentorService.getTeamSubmissions(teamId),
    enabled: !!teamId,
  });
}

export function useAddMentorshipNote(teamId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: string) => mentorService.addNote(teamId, body),
    onSuccess: (data) => {
      toast.success(data.message);
      qc.invalidateQueries({ queryKey: MENTOR_KEYS.notes(teamId) });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useAssignMentor() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ teamId, mentorId }: { teamId: string; mentorId: string }) => mentorService.assign(teamId, mentorId),
    onSuccess: (data) => {
      toast.success(data.message);
      qc.invalidateQueries({ queryKey: ["teams"] });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}
