"use client";

import { FormEvent, useState } from "react";
import { UserPlus } from "lucide-react";
import { useAddTeamMember } from "@/hooks/useTeams";
import { getErrorMessage } from "@/lib/utils";

interface TeamAddMemberPanelProps {
  disabled?: boolean;
}

export default function TeamAddMemberPanel({ disabled = false }: TeamAddMemberPanelProps) {
  const addMember = useAddTeamMember();
  const [studentCode, setStudentCode] = useState("");
  const [error, setError] = useState("");

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");

    const trimmed = studentCode.trim();
    if (!trimmed) {
      setError("Student Code is required.");
      return;
    }

    try {
      await addMember.mutateAsync({ studentCode: trimmed });
      setStudentCode("");
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  return (
    <form onSubmit={handleSubmit} className="border-t border-slate-200 p-5">
      <div className="mb-3">
        <h3 className="text-sm font-semibold text-slate-800">Add Member</h3>
        <p className="text-xs text-slate-400">Add a teammate by Student Code.</p>
      </div>

      <div className="flex flex-col gap-3 sm:flex-row">
        <input
          value={studentCode}
          onChange={(event) => setStudentCode(event.target.value)}
          placeholder="Student Code"
          disabled={disabled || addMember.isPending}
          className="flex-1 rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-slate-50"
        />
        <button
          type="submit"
          disabled={disabled || addMember.isPending}
          className="inline-flex items-center justify-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
        >
          <UserPlus className="h-4 w-4" />
          {addMember.isPending ? "Adding..." : "Add Member"}
        </button>
      </div>

      {error && <p className="mt-3 text-sm text-red-600">{error}</p>}
      {disabled && (
        <p className="mt-3 text-xs text-slate-400">
          Adding members is unavailable when the team is full or membership changes are locked.
        </p>
      )}
    </form>
  );
}
