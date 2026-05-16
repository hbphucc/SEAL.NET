"use client";

import { useAdminTeams } from "@/hooks/useTeams";
import DataTable from "@/components/shared/DataTable";
import PageHeader from "@/components/shared/PageHeader";
import { XCircle, Calendar } from "lucide-react";
import { Team } from "@/types/team";
import { formatDate } from "@/lib/utils";
import Link from "next/link";

export default function EliminationsPage() {
  const { data: teams = [], isLoading } = useAdminTeams();

  const eliminated = teams.filter((t) => t.status === "Eliminated");

  const columns = [
    {
      key: "teamName",
      label: "Team Name",
      sortable: true,
      render: (_: unknown, row: Team) => (
        <Link href={`/teams/${row.teamId}`} className="font-medium text-blue-600 hover:underline">
          {row.teamName}
        </Link>
      ),
    },
    {
      key: "category",
      label: "Category",
      render: (_: unknown, row: Team) => (
        <span className="text-sm text-slate-600">{row.category?.categoryName ?? "—"}</span>
      ),
    },
    {
      key: "eliminationReason",
      label: "Elimination Reason",
      render: (_: unknown, row: Team) => (
        <p className="text-sm text-slate-700 max-w-[250px]">
          {row.eliminationReason || <span className="text-slate-400">No reason provided</span>}
        </p>
      ),
    },
    {
      key: "eliminatedAt",
      label: "Eliminated At",
      sortable: true,
      render: (_: unknown, row: Team) => (
        <div className="flex items-center gap-1 text-xs text-slate-500">
          <Calendar className="w-3.5 h-3.5" />
          <span>{formatDate(row.eliminatedAt)}</span>
        </div>
      ),
    },
    {
      key: "members",
      label: "Members Count",
      render: (_: unknown, row: Team) => (
        <span className="text-sm text-slate-600">{row.members.length}</span>
      ),
    },
  ];

  return (
    <div>
      <PageHeader
        title="Eliminations"
        description={`${eliminated.length} teams have been eliminated from the competition`}
        icon={XCircle}
      />
      <DataTable
        data={eliminated}
        columns={columns as never}
        searchKeys={["teamName"] as never}
        searchPlaceholder="Search team name..."
        isLoading={isLoading}
        emptyMessage="No teams have been eliminated yet"
      />
    </div>
  );
}
