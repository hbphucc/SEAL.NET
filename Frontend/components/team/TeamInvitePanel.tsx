"use client";

import { FormEvent, useState } from "react";
import { MailPlus } from "lucide-react";
import { useInviteTeamMember } from "@/hooks/useTeams";
import { getErrorMessage } from "@/lib/utils";

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

interface TeamInvitePanelProps {
  teamId: string;
  disabled?: boolean;
}

export default function TeamInvitePanel({ teamId, disabled = false }: TeamInvitePanelProps) {
  const inviteMember = useInviteTeamMember();
  const [mode, setMode] = useState<"email" | "studentCode">("email");
  const [value, setValue] = useState("");
  const [error, setError] = useState("");

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");

    const trimmed = value.trim();
    if (!trimmed) {
      setError(mode === "email" ? "Email is required." : "Student Code is required.");
      return;
    }

    if (mode === "email" && !emailPattern.test(trimmed)) {
      setError("Enter a valid email address.");
      return;
    }

    try {
      await inviteMember.mutateAsync({
        teamId,
        data: mode === "email" ? { email: trimmed } : { studentCode: trimmed },
      });
      setValue("");
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  return (
    <form onSubmit={handleSubmit} className="border-t border-slate-200 p-5">
      <div className="mb-3 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h3 className="text-sm font-semibold text-slate-800">Invite Member</h3>
          <p className="text-xs text-slate-400">Send an invitation by email or Student Code.</p>
        </div>
        <div className="inline-flex rounded-lg border border-slate-200 bg-slate-50 p-1">
          {(["email", "studentCode"] as const).map((item) => (
            <button
              key={item}
              type="button"
              onClick={() => {
                setMode(item);
                setError("");
                setValue("");
              }}
              className={`rounded-md px-3 py-1.5 text-xs font-medium transition-colors ${
                mode === item
                  ? "bg-white text-blue-700 shadow-sm"
                  : "text-slate-500 hover:text-slate-700"
              }`}
            >
              {item === "email" ? "Email" : "Student Code"}
            </button>
          ))}
        </div>
      </div>

      <div className="flex flex-col gap-3 sm:flex-row">
        <input
          value={value}
          onChange={(event) => setValue(event.target.value)}
          placeholder={mode === "email" ? "teammate@example.com" : "Student Code"}
          disabled={disabled || inviteMember.isPending}
          className="flex-1 rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-slate-50"
        />
        <button
          type="submit"
          disabled={disabled || inviteMember.isPending}
          className="inline-flex items-center justify-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
        >
          <MailPlus className="h-4 w-4" />
          {inviteMember.isPending ? "Sending..." : "Send Invite"}
        </button>
      </div>

      {error && <p className="mt-3 text-sm text-red-600">{error}</p>}
      {disabled && (
        <p className="mt-3 text-xs text-slate-400">
          Invitations are unavailable when the team is full or membership changes are locked.
        </p>
      )}
    </form>
  );
}
