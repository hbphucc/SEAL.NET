export type TeamStatus =
  | "Pending"
  | "Approved"
  | "Active"
  | "Eliminated"
  | "Withdrawn"
  | "Champion";

export interface TeamMember {
  userId: string;
  fullName: string;
  email: string;
}

export interface TeamCategory {
  categoryId: string;
  categoryName: string;
}

export interface TeamRound {
  roundId: string;
  roundName: string;
  submissionDeadline?: string;
}

export interface Team {
  teamId: string;
  teamName: string;
  status: TeamStatus;
  leaderId?: string;
  category: TeamCategory;
  currentRound?: TeamRound | null;
  members: TeamMember[];
  createdAt?: string;
  eliminationReason?: string | null;
  eliminatedAt?: string | null;
}

export interface CreateTeamRequest {
  teamName: string;
  categoryId: string;
  memberIds: string[];
}

export interface EliminateTeamRequest {
  reason: string;
}

export interface AddTeamMemberRequest {
  userId: string;
}
