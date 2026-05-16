"use client";

import { useState } from "react";
import { useAuth } from "@/contexts/AuthContext";
import { useAdminTeams, useApproveTeam, useRejectTeam, useEliminateTeam } from "@/hooks/useTeams";
import { Team, TeamStatus } from "@/types/team";
import DataTable from "@/components/shared/DataTable";
import StatusBadge from "@/components/shared/StatusBadge";
import ConfirmDialog from "@/components/shared/ConfirmDialog";
import PageHeader from "@/components/shared/PageHeader";
import { Trophy, CheckCircle, XCircle, AlertTriangle } from "lucide-react";
import { TEAM_STATUS_LABELS } from "@/lib/constants";
import Link from "next/link";

export default function TeamsPage() {
  const { hasRole } = useAuth();
  const isAdmin = hasRole("Admin");

  const { data: teams = [], isLoading } = useAdminTeams(isAdmin);
  const approveMutation = useApproveTeam();
  const rejectMutation = useRejectTeam();
  const eliminateMutation = useEliminateTeam();

  const [approveTarget, setApproveTarget] = useState<Team | null>(null);
  const [rejectTarget, setRejectTarget] = useState<Team | null>(null);
  const [eliminateTarget, setEliminateTarget] = useState<Team | null>(null);
  const [eliminationReason, setEliminationReason] = useState("");
  const [statusFilter, setStatusFilter] = useState<"all" | TeamStatus>("all");

  if (!isAdmin) {
    return (
      <div>
        <PageHeader
          title="Teams"
          description="Team registration and membership are managed from your team workspace"
          icon={Trophy}
          actions={
            <Link href="/my-team" className="rounded-lg bg-blue-600 px-3 py-2 text-sm font-medium text-white hover:bg-blue-700">
              Open My Team
            </Link>
          }
        />
        <div className="rounded-xl border border-slate-200 bg-white px-4 py-12 text-center text-sm text-slate-500">
          Admins manage all teams here. Members and team leaders use My Team for creation, members, and submissions.
        </div>
      </div>
    );
  }

  const filtered = teams.filter((t) =>
    statusFilter === "all" ? true : t.status === statusFilter
  );

  const columns = [
    {
      key: "teamName",
      label: "Team Name",
      sortable: true,
      render: (_: unknown, row: Team) => (
        <div>
          <Link href={`/teams/${row.teamId}`} className="font-medium text-blue-600 hover:underline">
            {row.teamName}
          </Link>
          <p className="text-xs text-slate-400">{row.members.length} members</p>
        </div>
      ),
    },
    {
      key: "status",
      label: "Status",
      render: (_: unknown, row: Team) => (
        <StatusBadge type="team" value={row.status} />
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
      key: "currentRound",
      label: "Current Round",
      render: (_: unknown, row: Team) => (
        <span className="text-sm text-slate-600">{row.currentRound?.roundName ?? "—"}</span>
      ),
    },
    {
      key: "members",
      label: "Members",
      render: (_: unknown, row: Team) => (
        <div className="flex -space-x-1.5">
          {row.members.slice(0, 3).map((m) => (
            <div
              key={m.userId}
              title={m.fullName}
              className="w-6 h-6 rounded-full bg-blue-100 border-2 border-white flex items-center justify-center text-[9px] font-semibold text-blue-700"
            >
              {m.fullName.charAt(0)}
            </div>
          ))}
          {row.members.length > 3 && (
            <div className="w-6 h-6 rounded-full bg-slate-100 border-2 border-white flex items-center justify-center text-[9px] font-semibold text-slate-500">
              +{row.members.length - 3}
            </div>
          )}
        </div>
      ),
    },
    {
      key: "eliminationReason",
      label: "Elimination Reason",
      render: (_: unknown, row: Team) => (
        <span className="text-xs text-slate-500 max-w-[120px] truncate block">
          {row.eliminationReason ?? "—"}
        </span>
      ),
    },
  ];

  return (
    <div>
      <PageHeader
        title="Manage Teams"
        description="View and manage all registered teams, approve, and eliminate"
        icon={Trophy}
      />

      <DataTable
        data={filtered}
        columns={columns as never}
        searchKeys={["teamName"] as never}
        searchPlaceholder="Search team name..."
        isLoading={isLoading}
        emptyMessage="No teams found"
        filterComponent={
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value as never)}
            className="px-3 py-2 text-sm border border-slate-200 rounded-lg bg-white focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="all">All Statuses</option>
            {Object.entries(TEAM_STATUS_LABELS).map(([k, v]) => (
              <option key={k} value={k}>{v}</option>
            ))}
          </select>
        }
        actions={isAdmin ? (row: Team) => (
          <>
            {row.status === "Pending" && (
              <button
                onClick={() => setApproveTarget(row)}
                className="flex items-center gap-1 px-2 py-1.5 text-xs rounded-lg bg-green-50 text-green-700 hover:bg-green-100 transition-colors font-medium"
              >
                <CheckCircle className="w-3.5 h-3.5" /> Approve
              </button>
            )}
            {row.status === "Pending" && (
              <button
                onClick={() => setRejectTarget(row)}
                className="flex items-center gap-1 px-2 py-1.5 text-xs rounded-lg bg-yellow-50 text-yellow-700 hover:bg-yellow-100 transition-colors font-medium"
              >
                <XCircle className="w-3.5 h-3.5" /> Reject
              </button>
            )}
            {row.status !== "Eliminated" && row.status !== "Pending" && (
              <button
                onClick={() => { setEliminateTarget(row); setEliminationReason(""); }}
                className="flex items-center gap-1 px-2 py-1.5 text-xs rounded-lg bg-red-50 text-red-600 hover:bg-red-100 transition-colors font-medium"
              >
                <AlertTriangle className="w-3.5 h-3.5" /> Eliminate
              </button>
            )}
            <Link
              href={`/teams/${row.teamId}`}
              className="flex items-center gap-1 px-2 py-1.5 text-xs rounded-lg bg-slate-50 text-slate-700 hover:bg-slate-100 transition-colors font-medium"
            >
              View
            </Link>
          </>
        ) : undefined}
      />

      {/* Approve */}
      <ConfirmDialog
        open={!!approveTarget}
        onClose={() => setApproveTarget(null)}
        onConfirm={async () => { await approveMutation.mutateAsync(approveTarget!.teamId); setApproveTarget(null); }}
        title="Approve Team"
        description={`Approve team "${approveTarget?.teamName}"?`}
        confirmLabel="Approve"
        variant="default"
        isLoading={approveMutation.isPending}
      />

      {/* Reject */}
      <ConfirmDialog
        open={!!rejectTarget}
        onClose={() => setRejectTarget(null)}
        onConfirm={async () => { await rejectMutation.mutateAsync(rejectTarget!.teamId); setRejectTarget(null); }}
        title="Reject Team"
        description={`Reject team "${rejectTarget?.teamName}"?`}
        confirmLabel="Reject"
        variant="warning"
        isLoading={rejectMutation.isPending}
      />

      {/* Eliminate */}
      {eliminateTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
          <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" onClick={() => setEliminateTarget(null)} />
          <div className="relative bg-white rounded-2xl shadow-2xl w-full max-w-md p-6">
            <div className="w-12 h-12 rounded-full bg-red-100 flex items-center justify-center mx-auto mb-4">
              <AlertTriangle className="w-6 h-6 text-red-600" />
            </div>
            <h2 className="text-lg font-semibold text-slate-900 text-center mb-2">Eliminate Team</h2>
            <p className="text-sm text-slate-500 text-center mb-4">Team: <strong>{eliminateTarget.teamName}</strong></p>
            <div className="mb-4">
              <label className="block text-sm font-medium text-slate-700 mb-1.5">
                Elimination Reason <span className="text-red-500">*</span>
              </label>
              <textarea
                value={eliminationReason}
                onChange={(e) => setEliminationReason(e.target.value)}
                placeholder="Enter reason for elimination..."
                rows={3}
                className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-red-500 resize-none"
              />
            </div>
            <div className="flex gap-3">
              <button onClick={() => setEliminateTarget(null)} className="flex-1 px-4 py-2.5 border border-slate-200 rounded-lg text-sm text-slate-700 hover:bg-slate-50 transition-colors">
                Cancel
              </button>
              <button
                onClick={async () => {
                  if (!eliminationReason.trim()) return;
                  await eliminateMutation.mutateAsync({ teamId: eliminateTarget.teamId, data: { reason: eliminationReason } });
                  setEliminateTarget(null);
                }}
                disabled={eliminateMutation.isPending || !eliminationReason.trim()}
                className="flex-1 px-4 py-2.5 bg-red-600 text-white rounded-lg text-sm font-medium hover:bg-red-700 transition-colors disabled:opacity-50"
              >
                {eliminateMutation.isPending ? "Processing..." : "Eliminate"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
