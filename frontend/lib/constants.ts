import { UserRole } from "@/types/auth";

export const APP_NAME = "SEAL.NET";

export const ROLES = {
  ADMIN: "Admin" as UserRole,
  MEMBER: "Member" as UserRole,
  TEAM_LEADER: "TeamLeader" as UserRole,
  JUDGE: "Judge" as UserRole,
  MENTOR: "Mentor" as UserRole,
};

export const ALL_ROLES: UserRole[] = ["Admin", "Member", "TeamLeader", "Judge", "Mentor"];

export const ROUTES = {
  LOGIN: "/login",
  REGISTER: "/register",
  DASHBOARD: "/dashboard",
  PROFILE: "/profile",
  ADMIN_USERS: "/admin/users",
  TEAMS: "/teams",
  MEMBERS: "/members",
  TEAM_LEADERS: "/team-leaders",
  ELIMINATIONS: "/eliminations",
  ELIMINATION_REASONS: "/elimination-reasons",
  STUDENTS: "/students",
  UNAUTHORIZED: "/unauthorized",
};

export const TEAM_STATUS_LABELS: Record<string, string> = {
  Pending: "Pending",
  Approved: "Approved",
  Active: "Active",
  Eliminated: "Eliminated",
  Withdrawn: "Withdrawn",
  Champion: "Champion",
};

export const EVENT_STATUS_LABELS: Record<string, string> = {
  Upcoming: "Upcoming",
  Ongoing: "Ongoing",
  Completed: "Completed",
  Cancelled: "Cancelled",
};

export const STUDENT_TYPE_LABELS: Record<number, string> = {
  0: "FPT",
  1: "External",
};

export const DEFAULT_PAGE_SIZE = 10;
