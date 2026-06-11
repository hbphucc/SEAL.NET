"use client";

import { useUsers } from "@/hooks/useUsers";
import DataTable from "@/components/shared/DataTable";
import PageHeader from "@/components/shared/PageHeader";
import { GraduationCap } from "lucide-react";
import { User } from "@/types/user";
import { formatDate } from "@/lib/utils";
import { STUDENT_TYPE_LABELS } from "@/lib/constants";
import { useState } from "react";
import { useAuth } from "@/contexts/AuthContext";

export default function StudentsPage() {
  const { hasRole } = useAuth();
  const isAdmin = hasRole("Admin");
  const { data: users = [], isLoading } = useUsers(isAdmin);
  const [typeFilter, setTypeFilter] = useState<"all" | "0" | "1">("all");

  // All users that have student info
  const students = users.filter((u) => {
    const hasInfo = u.studentCode || u.studentType !== undefined;
    if (!hasInfo) return false;
    if (typeFilter !== "all") {
      // Accept both the string ("FPT"/"External") and numeric (0/1) forms.
      const raw = String(u.studentType);
      const normalized = raw === "FPT" ? "0" : raw === "External" ? "1" : raw;
      if (normalized !== typeFilter) return false;
    }
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
      key: "studentCode",
      label: "Student ID",
      render: (_: unknown, row: User) => (
        <span className="font-mono text-sm text-slate-700 bg-slate-100 px-2 py-0.5 rounded">
          {row.studentCode || "-"}
        </span>
      ),
    },
    {
      key: "studentType",
      label: "Student Type",
      render: (_: unknown, row: User) => {
        const type = row.studentType as number | undefined;
        return (
          <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium border ${
            type === 0
              ? "bg-blue-100 text-blue-700 border-blue-200"
              : type === 1
              ? "bg-purple-100 text-purple-700 border-purple-200"
              : "bg-gray-100 text-gray-600 border-gray-200"
          }`}>
            {type !== undefined ? STUDENT_TYPE_LABELS[type] : "-"}
          </span>
        );
      },
    },
    {
      key: "schoolName",
      label: "University",
      render: (_: unknown, row: User) => (
        <span className="text-sm text-slate-600">{row.schoolName || "FPT University"}</span>
      ),
    },
    {
      key: "roles",
      label: "Role",
      render: (_: unknown, row: User) => (
        <div className="flex gap-1">
          {row.roles.map((role) => (
            <span key={role} className="text-xs px-1.5 py-0.5 bg-slate-100 text-slate-600 rounded">
              {role}
            </span>
          ))}
        </div>
      ),
    },
    {
      key: "createdAt",
      label: "Registration Date",
      sortable: true,
      render: (_: unknown, row: User) => (
        <span className="text-xs text-slate-500">{formatDate(row.createdAt)}</span>
      ),
    },
  ];

  return (
    <div>
      <PageHeader
        title="Student Information"
        description={`${students.length} students in the system`}
        icon={GraduationCap}
      />

      <DataTable
        data={students}
        columns={columns as never}
        searchKeys={["fullName", "email", "studentCode", "schoolName"] as never}
        searchPlaceholder="Search by name, email, student ID..."
        isLoading={isLoading}
        emptyMessage="No student information found"
        filterComponent={
          <select
            value={typeFilter}
            onChange={(e) => setTypeFilter(e.target.value as never)}
            className="px-3 py-2 text-sm border border-slate-200 rounded-lg bg-white focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="all">All Types</option>
            <option value="0">FPT</option>
            <option value="1">External</option>
          </select>
        }
      />
    </div>
  );
}
