export type EventStatus =
  | "Draft"
  | "Upcoming"
  | "RegistrationClosed"
  | "Judging"
  | "RankingPublished"
  | "Ongoing"
  | "Completed"
  | "Cancelled"
  | "Archived";

export type RoundStatus = "Draft" | "Open" | "Closed" | "Locked" | "ResultsPublished";

export interface Event {
  eventId: string;
  eventName: string;
  description?: string;
  status: EventStatus;
  startDate: string;
  endDate: string;
  isPublished?: boolean;
  isArchived?: boolean;
  registrationClosedAt?: string | null;
  judgingStartedAt?: string | null;
  judgingEndedAt?: string | null;
  categories?: Category[];
  rounds?: Round[];
}

export interface Category {
  categoryId: string;
  categoryName: string;
  description?: string;
  eventId?: string;
  teamCount?: number;
}

export interface Round {
  roundId: string;
  roundName: string;
  submissionDeadline?: string | null;
  roundOrder: number;
  maxTeamsAdvancing: number;
  eventId?: string;
  status?: RoundStatus;
  isRankingPublished?: boolean;
  isSubmissionLocked?: boolean;
}

export interface Criteria {
  criteriaId: string;
  criteriaName: string;
  maxScore: number;
  weight: number;
  roundId: string;
}

export interface EventPayload {
  eventName: string;
  description?: string;
  startDate: string;
  endDate: string;
  status: EventStatus;
}

export interface CategoryPayload {
  categoryName: string;
  description?: string;
}

export interface RoundPayload {
  roundName: string;
  submissionDeadline: string;
  roundOrder: number;
  maxTeamsAdvancing: number;
}

export interface CriteriaPayload {
  criteriaName: string;
  maxScore: number;
  weight: number;
}

export interface ApiResponse<T = void> {
  message: string;
  data?: T;
}

export interface PaginatedResponse<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}
