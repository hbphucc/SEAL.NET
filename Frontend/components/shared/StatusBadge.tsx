import { cn, getTeamStatusColor, getRoleColor } from "@/lib/utils";
import { TeamStatus } from "@/types/team";
import { UserRole } from "@/types/auth";
import { TEAM_STATUS_LABELS } from "@/lib/constants";

interface StatusBadgeProps {
  type: "team" | "user" | "role";
  value: string;
  className?: string;
}

export default function StatusBadge({ type, value, className }: StatusBadgeProps) {
  let colorClass = "";
  let label = value;

  if (type === "team") {
    colorClass = getTeamStatusColor(value as TeamStatus);
    label = TEAM_STATUS_LABELS[value] ?? value;
  } else if (type === "role") {
    colorClass = getRoleColor(value as UserRole);
  } else if (type === "user") {
    colorClass =
      value === "approved" || value === "true"
        ? "bg-green-100 text-green-700 border-green-200"
        : "bg-yellow-100 text-yellow-700 border-yellow-200";
    label = value === "approved" || value === "true" ? "Approved" : "Pending";
  }

  return (
    <span
      className={cn(
        "inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium border",
        colorClass,
        className
      )}
    >
      {label}
    </span>
  );
}
