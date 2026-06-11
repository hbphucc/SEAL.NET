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

/**
 * Single source of truth for route authorization. Consumed by the edge middleware,
 * the axios 401 interceptor, the AuthProvider, and the dashboard layout so the rules
 * can never drift between them.
 */
export const PUBLIC_ROUTES = [
  "/",
  "/login",
  "/register",
  "/unauthorized",
  "/leaderboard",
  "/events",
] as const;

export const ROLE_GUARDS: { path: string; roles: UserRole[] }[] = [
  { path: "/admin", roles: ["Admin"] },
  { path: "/teams", roles: ["Admin"] },
  { path: "/members", roles: ["Admin"] },
  { path: "/team-leaders", roles: ["Admin"] },
  { path: "/students", roles: ["Admin"] },
  { path: "/eliminations", roles: ["Admin"] },
  { path: "/elimination-reasons", roles: ["Admin"] },
  { path: "/judge", roles: ["Judge"] },
  { path: "/submit", roles: ["Member", "TeamLeader"] },
  { path: "/my-team", roles: ["Member", "TeamLeader"] },
];

export function isPublicRoute(pathname: string): boolean {
  return PUBLIC_ROUTES.some(
    (route) => pathname === route || pathname.startsWith(`${route}/`)
  );
}

export function getRequiredRoles(pathname: string): UserRole[] | null {
  const guard = ROLE_GUARDS.find(
    (item) => pathname === item.path || pathname.startsWith(`${item.path}/`)
  );
  return guard?.roles ?? null;
}

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
  Archived: "Archived",
};

// Must cover every value of the backend EventStatus enum.
export const EVENT_STATUS_LABELS: Record<string, string> = {
  Draft: "Draft",
  Upcoming: "Upcoming",
  RegistrationClosed: "Registration Closed",
  Judging: "Judging",
  RankingPublished: "Ranking Published",
  Ongoing: "Ongoing",
  Completed: "Completed",
  Cancelled: "Cancelled",
  Archived: "Archived",
};

// The API serializes the StudentType enum as its name ("FPT"/"External") via the global
// JsonStringEnumConverter, but older clients/data may still carry the numeric form. Keying
// on both keeps the label resolution correct regardless of representation.
export const STUDENT_TYPE_LABELS: Record<string | number, string> = {
  0: "FPT",
  1: "External",
  FPT: "FPT",
  External: "External",
};

export const DEFAULT_PAGE_SIZE = 10;
