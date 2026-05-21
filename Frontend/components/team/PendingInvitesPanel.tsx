"use client";

import { Check, Mail, X } from "lucide-react";
import EmptyState from "@/components/shared/EmptyState";
import {
  useAcceptInvite,
  usePendingInvites,
  useRejectInvite,
} from "@/hooks/useTeams";
import { formatDate, getErrorMessage } from "@/lib/utils";

interface PendingInvitesPanelProps {
  enabled?: boolean;
}

export default function PendingInvitesPanel({ enabled = true }: PendingInvitesPanelProps) {
  const { data: invites = [], isLoading, isError, error } = usePendingInvites(enabled);
  const acceptInvite = useAcceptInvite();
  const rejectInvite = useRejectInvite();

  return (
    <section className="rounded-xl border border-slate-200 bg-white shadow-sm">
      <div className="flex items-center justify-between gap-3 border-b border-slate-200 px-5 py-4">
        <div className="flex items-center gap-2">
          <Mail className="h-4 w-4 text-slate-500" />
          <h2 className="font-semibold text-slate-800">Pending Invites</h2>
        </div>
        <span className="rounded-full bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-600">
          {invites.length}
        </span>
      </div>

      <div className="p-5">
        {isError ? (
          <p className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
            {getErrorMessage(error)}
          </p>
        ) : isLoading ? (
          <div className="space-y-3">
            {Array.from({ length: 2 }).map((_, index) => (
              <div key={index} className="h-20 animate-pulse rounded-lg bg-slate-100" />
            ))}
          </div>
        ) : invites.length === 0 ? (
          <EmptyState
            icon={Mail}
            title="No pending invites"
            description="Team invitations sent to you will appear here."
          />
        ) : (
          <div className="space-y-3">
            {invites.map((invite) => (
              <div key={invite.teamInviteId} className="rounded-lg border border-slate-100 bg-slate-50 p-4">
                <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                  <div>
                    <p className="font-medium text-slate-900">{invite.team.teamName}</p>
                    <p className="mt-1 text-xs text-slate-500">{invite.team.category}</p>
                    <p className="mt-1 text-xs text-slate-400">Expires {formatDate(invite.expiresAt)}</p>
                  </div>
                  <div className="flex gap-2">
                    <button
                      type="button"
                      onClick={() => acceptInvite.mutate(invite.teamInviteId)}
                      disabled={acceptInvite.isPending || rejectInvite.isPending}
                      className="inline-flex items-center gap-1.5 rounded-lg bg-green-600 px-3 py-2 text-xs font-medium text-white hover:bg-green-700 disabled:opacity-50"
                    >
                      <Check className="h-3.5 w-3.5" />
                      Accept
                    </button>
                    <button
                      type="button"
                      onClick={() => rejectInvite.mutate(invite.teamInviteId)}
                      disabled={acceptInvite.isPending || rejectInvite.isPending}
                      className="inline-flex items-center gap-1.5 rounded-lg bg-slate-200 px-3 py-2 text-xs font-medium text-slate-700 hover:bg-slate-300 disabled:opacity-50"
                    >
                      <X className="h-3.5 w-3.5" />
                      Reject
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </section>
  );
}
