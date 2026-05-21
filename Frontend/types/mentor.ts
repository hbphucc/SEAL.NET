import { Team } from "./team";

export interface MentorshipNote {
  mentorshipNoteId: string;
  body: string;
  createdAt: string;
  mentor: {
    mentorId: string;
    fullName: string;
    email: string;
  };
}

export type MentorTeam = Team;
