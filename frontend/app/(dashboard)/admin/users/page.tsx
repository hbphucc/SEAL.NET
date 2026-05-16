"use client";

import { useState } from "react";
import { useAuth } from "@/contexts/AuthContext";
import { useUsers, useApproveUser, useRejectUser, useUpdateUserRole, useDeleteUser } from "@/hooks/useUsers";
import { User } from "@/types/user";
import { UserRole } from "@/types/auth";
import { ALL_ROLES } from "@/lib/constants";
import DataTable from "@/components/shared/DataTable";
import StatusBadge from "@/components/shared/StatusBadge";
import ConfirmDialog from "@/components/shared/ConfirmDialog";
import PageHeader from "@/components/shared/PageHeader";
import { Shield, CheckCircle, XCircle, Trash2, Edit, Clock } from "lucide-react";
import { formatDate } from "@/lib/utils";
import { STUDENT_TYPE_LABELS } from "@/lib/constants";

export default function AdminUsersPage() {
  const { hasRole } = useAuth();
  const isAdmin = hasRole("Admin");

  const { data: users = [], isLoading } = useUsers(isAdmin);
  const approveMutation = useApproveUser();
  const rejectMutation = useRejectUser();
  const updateRoleMutation = useUpdateUserRole();
  const deleteMutation = useDeleteUser();

  // Dialogs state
  const [approveTarget, setApproveTarget] = useState<User | null>(null);
  const [rejectTarget, setRejectTarget] = useState<User | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<User | null>(null);
  const [roleTarget, setRoleTarget] = useState<User | null>(null);
  const [selectedRole, setSelectedRole] = useState<UserRole>("Member");

  // Filter
  const [statusFilter, setStatusFilter] = useState<"all" | "approved" | "pending">("all");

  const filtered = users.filter((u) => {
    if (statusFilter === "approved") return u.isApproved;
    if (statusFilter === "pending") return !u.isApproved;
    return true;
  });

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
      key: "roles",
      label: "Role",
      render: (_: unknown, row: User) => (
        <div className="flex flex-wrap gap-1">
          {row.roles.length > 0
            ? row.roles.map((role) => (
              <StatusBadge key={role} type="role" value={role} />
            ))
            : <span className="text-xs text-slate-400">—</span>
          }
        </div>
      ),
    },
    {
      key: "studentType",
      label: "Type",
      render: (_: unknown, row: User) => (
        <span className="text-sm text-slate-600">
          {row.studentType !== undefined && row.studentType !== null
            ? STUDENT_TYPE_LABELS[row.studentType as number]
            : "—"}
        </span>
      ),
    },
    {
      key: "studentCode",
      label: "Student ID",
      render: (_: unknown, row: User) => (
        <span className="text-sm text-slate-600 font-mono">{row.studentCode || "—"}</span>
      ),
    },
    {
      key: "isApproved",
      label: "Status",
      sortable: true,
      render: (_: unknown, row: User) => (
        <div className="flex items-center gap-1.5">
          {row.isApproved
            ? <CheckCircle className="w-4 h-4 text-green-500" />
            : <Clock className="w-4 h-4 text-yellow-500" />}
          <StatusBadge type="user" value={row.isApproved ? "true" : "false"} />
        </div>
      ),
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
        title="Manage Users"
        description="Approve, assign roles, and manage user accounts"
        icon={Shield}
      />

      <DataTable
        data={filtered}
        columns={columns as never}
        searchKeys={["fullName", "email", "studentCode"] as never}
        searchPlaceholder="Search by name, email, student ID..."
        isLoading={isLoading}
        emptyMessage="No users found"
        filterComponent={
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value as never)}
            className="px-3 py-2 text-sm border border-slate-200 rounded-lg bg-white focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="all">All</option>
            <option value="approved">Approved</option>
            <option value="pending">Pending</option>
          </select>
        }
        actions={(row: User) => (
          <>
            {!row.isApproved && (
              <button
                onClick={() => setApproveTarget(row)}
                className="flex items-center gap-1 px-2 py-1.5 text-xs rounded-lg bg-green-50 text-green-700 hover:bg-green-100 transition-colors font-medium"
              >
                <CheckCircle className="w-3.5 h-3.5" /> Approve
              </button>
            )}
            {row.isApproved && (
              <button
                onClick={() => setRejectTarget(row)}
                className="flex items-center gap-1 px-2 py-1.5 text-xs rounded-lg bg-yellow-50 text-yellow-700 hover:bg-yellow-100 transition-colors font-medium"
              >
                <XCircle className="w-3.5 h-3.5" /> Reject
              </button>
            )}
            <button
              onClick={() => { setRoleTarget(row); setSelectedRole(row.roles[0] as UserRole || "Member"); }}
              className="flex items-center gap-1 px-2 py-1.5 text-xs rounded-lg bg-blue-50 text-blue-700 hover:bg-blue-100 transition-colors font-medium"
            >
              <Edit className="w-3.5 h-3.5" /> Role
            </button>
            {!row.roles.includes("Admin") && (
              <button
                onClick={() => setDeleteTarget(row)}
                className="flex items-center gap-1 px-2 py-1.5 text-xs rounded-lg bg-red-50 text-red-600 hover:bg-red-100 transition-colors font-medium"
              >
                <Trash2 className="w-3.5 h-3.5" /> Delete
              </button>
            )}
          </>
        )}
      />

      {/* Approve dialog */}
      <ConfirmDialog
        open={!!approveTarget}
        onClose={() => setApproveTarget(null)}
        onConfirm={async () => { await approveMutation.mutateAsync(approveTarget!.id); setApproveTarget(null); }}
        title="Approve User"
        description={`Are you sure you want to approve "${approveTarget?.fullName}"?`}
        confirmLabel="Approve"
        variant="default"
        isLoading={approveMutation.isPending}
      />

      {/* Reject dialog */}
      <ConfirmDialog
        open={!!rejectTarget}
        onClose={() => setRejectTarget(null)}
        onConfirm={async () => { await rejectMutation.mutateAsync(rejectTarget!.id); setRejectTarget(null); }}
        title="Reject User"
        description={`"${rejectTarget?.fullName}" will not be able to log in after being rejected.`}
        confirmLabel="Reject"
        variant="warning"
        isLoading={rejectMutation.isPending}
      />

      {/* Delete dialog */}
      <ConfirmDialog
        open={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        onConfirm={async () => { await deleteMutation.mutateAsync(deleteTarget!.id); setDeleteTarget(null); }}
        title="Delete User"
        description={`Are you sure you want to permanently delete "${deleteTarget?.fullName}"? This action cannot be undone.`}
        confirmLabel="Delete Permanently"
        variant="danger"
        isLoading={deleteMutation.isPending}
      />

      {/* Change role dialog */}
      {roleTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
          <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" onClick={() => setRoleTarget(null)} />
          <div className="relative bg-white rounded-2xl shadow-2xl w-full max-w-sm p-6">
            <h2 className="text-lg font-semibold text-slate-900 mb-1">Change Role</h2>
            <p className="text-sm text-slate-500 mb-4">{roleTarget.fullName}</p>
            <select
              value={selectedRole}
              onChange={(e) => setSelectedRole(e.target.value as UserRole)}
              className="w-full px-3 py-2.5 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 mb-4"
            >
              {ALL_ROLES.map((role) => (
                <option key={role} value={role}>{role}</option>
              ))}
            </select>
            <div className="flex gap-3">
              <button onClick={() => setRoleTarget(null)} className="flex-1 px-4 py-2.5 border border-slate-200 rounded-lg text-sm text-slate-700 hover:bg-slate-50 transition-colors">
                Cancel
              </button>
              <button
                onClick={async () => {
                  await updateRoleMutation.mutateAsync({ userId: roleTarget.id, data: { role: selectedRole } });
                  setRoleTarget(null);
                }}
                disabled={updateRoleMutation.isPending}
                className="flex-1 px-4 py-2.5 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors disabled:opacity-50"
              >
                {updateRoleMutation.isPending ? "Saving..." : "Save Changes"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
