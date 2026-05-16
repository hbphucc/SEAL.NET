"use client";

import { FormEvent, useState } from "react";
import { History } from "lucide-react";
import DataTable from "@/components/shared/DataTable";
import PageHeader from "@/components/shared/PageHeader";
import { useAuth } from "@/contexts/AuthContext";
import { useSubmissionAuditLogs } from "@/hooks/useAudit";
import { formatDate } from "@/lib/utils";
import { ScoreAuditLog } from "@/types/audit";

export default function ScoreAuditPage() {
  const { hasRole } = useAuth();
  const isAdmin = hasRole("Admin");
  const [input, setInput] = useState("");
  const [submissionId, setSubmissionId] = useState("");
  const { data: logs = [], isLoading } = useSubmissionAuditLogs(submissionId);

  if (!isAdmin) {
    return (
      <div className="flex items-center justify-center h-64">
        <p className="text-slate-500">You do not have permission to access this page.</p>
      </div>
    );
  }

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmissionId(input.trim());
  }

  const columns = [
    {
      key: "createdAt",
      label: "When",
      sortable: true,
      render: (_: unknown, row: ScoreAuditLog) => <span className="text-xs text-slate-500">{formatDate(row.createdAt)}</span>,
    },
    {
      key: "action",
      label: "Action",
      sortable: true,
      render: (_: unknown, row: ScoreAuditLog) => (
        <span className="rounded-full bg-blue-50 px-2 py-0.5 text-xs font-medium text-blue-700">{row.action}</span>
      ),
    },
    {
      key: "criteriaName",
      label: "Criteria",
      render: (_: unknown, row: ScoreAuditLog) => <span className="text-sm text-slate-700">{row.criteriaName ?? row.criteriaId}</span>,
    },
    {
      key: "judge",
      label: "Judge",
      render: (_: unknown, row: ScoreAuditLog) => (
        <div>
          <p className="text-sm font-medium text-slate-800">{row.judge?.fullName ?? "Unknown"}</p>
          <p className="text-xs text-slate-400">{row.judge?.email}</p>
        </div>
      ),
    },
    {
      key: "oldScoreValue",
      label: "Old",
      render: (_: unknown, row: ScoreAuditLog) => <span className="text-sm text-slate-600">{row.oldScoreValue ?? "-"}</span>,
    },
    {
      key: "newScoreValue",
      label: "New",
      render: (_: unknown, row: ScoreAuditLog) => <span className="font-semibold text-blue-700">{row.newScoreValue}</span>,
    },
    {
      key: "newComment",
      label: "Comment",
      render: (_: unknown, row: ScoreAuditLog) => (
        <span className="block max-w-xs truncate text-sm text-slate-600">{row.newComment || row.oldComment || "-"}</span>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <PageHeader
        title="Score Audit Logs"
        description="Inspect score creation and update history by submission"
        icon={History}
      />

      <form onSubmit={handleSubmit} className="flex flex-col gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm sm:flex-row">
        <input
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder="Submission ID"
          className="flex-1 rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
        <button type="submit" className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700">
          Load Logs
        </button>
      </form>

      <DataTable
        data={logs}
        columns={columns as never}
        searchKeys={["action", "criteriaName"] as never}
        searchPlaceholder="Search logs..."
        isLoading={isLoading}
        emptyMessage={submissionId ? "No audit logs found for this submission" : "Enter a submission ID to load audit logs"}
      />
    </div>
  );
}
