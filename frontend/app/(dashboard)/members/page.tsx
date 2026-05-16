"use client";

import { useUsers } from "@/hooks/useUsers";
import DataTable from "@/components/shared/DataTable";
import StatusBadge from "@/components/shared/StatusBadge";
import PageHeader from "@/components/shared/PageHeader";
import { Users } from "lucide-react";
import { User } from "@/types/user";
import { formatDate } from "@/lib/utils";
import { STUDENT_TYPE_LABELS } from "@/lib/constants";
import { useAuth } from "@/contexts/AuthContext";

export default function MembersPage() {
  const { hasRole } = useAuth();
  const isAdmin = hasRole("Admin");
  const { data: users = [], isLoading } = useUsers(isAdmin);

  // Filter only members (role === "Member")
  const members = users.filter((u) => u.roles.includes("Member"));

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
      key: "studentCode",
      label: "Student ID",
      render: (_: unknown, row: User) => (
        <span className="font-mono text-sm text-slate-600">{row.studentCode || "—"}</span>
      ),
    },
    {
      key: "studentType",
      label: "Type",
      render: (_: unknown, row: User) => (
        <span className="text-sm text-slate-600">
          {row.studentType !== undefined ? STUDENT_TYPE_LABELS[row.studentType as number] : "—"}
        </span>
      ),
    },
    {
      key: "isApproved",
      label: "Status",
      render: (_: unknown, row: User) => (
        <StatusBadge type="user" value={row.isApproved ? "true" : "false"} />
      ),
    },
    {
      key: "createdAt",
      label: "Joined At",
      sortable: true,
      render: (_: unknown, row: User) => (
        <span className="text-xs text-slate-500">{formatDate(row.createdAt)}</span>
      ),
    },
  ];

  return (
    <div>
      <PageHeader
        title="Members List"
        description={`${members.length} members in the system`}
        icon={Users}
      />
      <DataTable
        data={members}
        columns={columns as never}
        searchKeys={["fullName", "email", "studentCode"] as never}
        searchPlaceholder="Search by name, email, student ID..."
        isLoading={isLoading}
        emptyMessage="No members found"
      />
    </div>
  );
}
