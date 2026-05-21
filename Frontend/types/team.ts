export type TeamStatus =
  | "Pending"
  | "Approved"
  | "Active"
  | "Eliminated"
  | "Withdrawn"
  | "Champion"
  | "Archived";

export interface TeamMember {
  userId: string;
  fullName: string;
  email: string;
  role?: "Member" | "Leader";
  isLeader?: boolean;
}

export interface TeamCategory {
  categoryId: string;
  categoryName: string;
}

export interface TeamRound {
  roundId: string;
  roundName: string;
}

export interface Team {
  teamId: string;
  teamName: string;
  description?: string | null;
  status: TeamStatus;
  leaderId?: string;
  category: TeamCategory;
  currentRound?: TeamRound | null;
  members: TeamMember[];
  createdAt?: string;
  eliminationReason?: string | null;
  statusReason?: string | null;
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

export interface UpdateTeamRequest {
  teamName: string;
  description?: string;
  categoryId: string;
}

export interface InviteTeamMemberRequest {
  userId?: string;
  email?: string;
}

export interface TeamInvite {
  teamInviteId: string;
  createdAt: string;
  expiresAt: string;
  status: "Pending" | "Accepted" | "Rejected" | "Cancelled" | "Expired";
  team: {
    teamId: string;
    teamName: string;
    category: string;
  };
}

export interface TransferLeadershipRequest {
  newLeaderUserId: string;
}
