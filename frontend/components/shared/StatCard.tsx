import { cn } from "@/lib/utils";
import { LucideIcon, TrendingUp, TrendingDown } from "lucide-react";

interface StatCardProps {
  title: string;
  value: string | number;
  icon: LucideIcon;
  color?: "blue" | "green" | "yellow" | "red" | "purple";
  trend?: { value: number; label: string };
  isLoading?: boolean;
}

const colorMap = {
  blue: {
    bg: "bg-blue-600",
    light: "bg-blue-50",
    text: "text-blue-600",
  },
  green: {
    bg: "bg-emerald-600",
    light: "bg-emerald-50",
    text: "text-emerald-600",
  },
  yellow: {
    bg: "bg-yellow-500",
    light: "bg-yellow-50",
    text: "text-yellow-600",
  },
  red: {
    bg: "bg-red-600",
    light: "bg-red-50",
    text: "text-red-600",
  },
  purple: {
    bg: "bg-purple-600",
    light: "bg-purple-50",
    text: "text-purple-600",
  },
};

export default function StatCard({
  title,
  value,
  icon: Icon,
  color = "blue",
  trend,
  isLoading = false,
}: StatCardProps) {
  const c = colorMap[color];

  return (
    <div className="bg-white rounded-xl border border-slate-200 p-5 shadow-sm hover:shadow-md transition-shadow">
      <div className="flex items-start justify-between">
        <div className={cn("w-10 h-10 rounded-lg flex items-center justify-center", c.light)}>
          <Icon className={cn("w-5 h-5", c.text)} />
        </div>
        {trend && (
          <div className={cn(
            "flex items-center gap-1 text-xs font-medium",
            trend.value >= 0 ? "text-emerald-600" : "text-red-500"
          )}>
            {trend.value >= 0
              ? <TrendingUp className="w-3.5 h-3.5" />
              : <TrendingDown className="w-3.5 h-3.5" />}
            <span>{Math.abs(trend.value)}%</span>
          </div>
        )}
      </div>

      <div className="mt-4">
        {isLoading ? (
          <>
            <div className="h-8 w-16 bg-slate-200 rounded animate-pulse mb-1" />
            <div className="h-4 w-24 bg-slate-100 rounded animate-pulse" />
          </>
        ) : (
          <>
            <p className="text-2xl font-bold text-slate-900">{value}</p>
            <p className="text-sm text-slate-500 mt-0.5">{title}</p>
            {trend && (
              <p className="text-xs text-slate-400 mt-0.5">{trend.label}</p>
            )}
          </>
        )}
      </div>
    </div>
  );
}
