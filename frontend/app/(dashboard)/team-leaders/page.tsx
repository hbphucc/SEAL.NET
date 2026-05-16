"use client";

import { useUsers } from "@/hooks/useUsers";
import { useAdminTeams } from "@/hooks/useTeams";
import DataTable from "@/components/shared/DataTable";
import StatusBadge from "@/components/shared/StatusBadge";
import PageHeader from "@/components/shared/PageHeader";
import { UserCheck } from "lucide-react";
import { User } from "@/types/user";
import { formatDate } from "@/lib/utils";
import { useAuth } from "@/contexts/AuthContext";

export default function TeamLeadersPage() {
  const { hasRole } = useAuth();
  const isAdmin = hasRole("Admin");
  const { data: users = [], isLoading: usersLoading } = useUsers(isAdmin);
  const { data: teams = [], isLoading: teamsLoading } = useAdminTeams(isAdmin);

  const leaders = users.filter((u) => u.roles.includes("TeamLeader"));

  if (!isAdmin) {
    return (
      <div className="flex items-center justify-center h-64">
        <p className="text-slate-500">You do not have permission to access this page.</p>
      </div>
    );
  }

  const columns = [
    {
      key: "fullName",
      label: "Full Name",
      sortable: true,
      render: (_: unknown, row: User) => (
        <div>
          <p className="font-medium text-slate-800">{row.fullName}</p>
          <p className="text-xs text-slate-400">{row.email}</p>
        </div>
      ),
    },
    {
      key: "isApproved",
      label: "Account Status",
      render: (_: unknown, row: User) => (
        <StatusBadge type="user" value={row.isApproved ? "true" : "false"} />
      ),
    },
    {
      key: "teams",
      label: "Teams Led",
      render: (_: unknown, row: User) => {
        const ledTeams = teams.filter((t) => t.leaderId === row.id);
        return ledTeams.length > 0 ? (
          <div className="space-y-1">
            {ledTeams.map((t) => (
              <div key={t.teamId} className="flex items-center gap-2">
                <span className="text-sm text-slate-700">{t.teamName}</span>
                <StatusBadge type="team" value={t.status} />
              </div>
            ))}
          </div>
        ) : (
          <span className="text-sm text-slate-400">No teams</span>
        );
      },
    },
    {
      key: "createdAt",
      label: "Created At",
      sortable: true,
      render: (_: unknown, row: User) => (
        <span className="text-xs text-slate-500">{formatDate(row.createdAt)}</span>
      ),
    },
  ];

  return (
    <div>
      <PageHeader
        title="Team Leaders"
        description={`${leaders.length} team leaders in the system`}
        icon={UserCheck}
      />
      <DataTable
        data={leaders}
        columns={columns as never}
        searchKeys={["fullName", "email"] as never}
        searchPlaceholder="Search by name, email..."
        isLoading={usersLoading || teamsLoading}
        emptyMessage="No team leaders found"
      />
    </div>
  );
}
