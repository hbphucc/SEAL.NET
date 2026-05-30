import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";
import { TeamStatus } from "@/types/team";
import { UserRole } from "@/types/auth";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function formatDate(dateStr?: string | null): string {
  if (!dateStr) return "—";
  return new Intl.DateTimeFormat("en-US", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(dateStr));
}

export function getTeamStatusColor(status: TeamStatus): string {
  const map: Record<TeamStatus, string> = {
    Pending: "bg-yellow-100 text-yellow-700 border-yellow-200",
    Approved: "bg-blue-100 text-blue-700 border-blue-200",
    Active: "bg-green-100 text-green-700 border-green-200",
    Eliminated: "bg-red-100 text-red-700 border-red-200",
    Withdrawn: "bg-gray-100 text-gray-600 border-gray-200",
    Champion: "bg-purple-100 text-purple-700 border-purple-200",
    Archived: "bg-slate-100 text-slate-600 border-slate-200",
    Rejected: "bg-orange-100 text-orange-700 border-orange-200",
  };
  return map[status] ?? "bg-gray-100 text-gray-600 border-gray-200";
}

export function getRoleColor(role: UserRole): string {
  const map: Record<UserRole, string> = {
    Admin: "bg-red-100 text-red-700 border-red-200",
    TeamLeader: "bg-blue-100 text-blue-700 border-blue-200",
    Member: "bg-green-100 text-green-700 border-green-200",
    Judge: "bg-purple-100 text-purple-700 border-purple-200",
    Mentor: "bg-orange-100 text-orange-700 border-orange-200",
  };
  return map[role] ?? "bg-gray-100 text-gray-600 border-gray-200";
}

export function getErrorMessage(error: unknown): string {
  if (error && typeof error === "object" && "response" in error) {
    const axiosError = error as {
      response?: {
        data?: {
          message?: string;
          title?: string;
          errors?: Record<string, string[]>;
        };
      };
    };
    const data = axiosError.response?.data;

    if (data?.message) return data.message;

    const validationMessages = data?.errors
      ? Object.values(data.errors).flat().filter(Boolean)
      : [];

    if (validationMessages.length > 0) return validationMessages[0];

    return data?.title ?? "An unknown error occurred.";
  }
  if (error instanceof Error) return error.message;
  return "An unknown error occurred.";
}
