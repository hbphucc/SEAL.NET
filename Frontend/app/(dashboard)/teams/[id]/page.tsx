"use client";

import { use } from "react";
import { useAdminTeams } from "@/hooks/useTeams";
import { useAuth } from "@/contexts/AuthContext";
import { useEliminateTeam } from "@/hooks/useTeams";
import StatusBadge from "@/components/shared/StatusBadge";
import PageHeader from "@/components/shared/PageHeader";
import { Trophy, Users, ArrowLeft, AlertTriangle, Calendar } from "lucide-react";
import { formatDate } from "@/lib/utils";
import { TEAM_STATUS_LABELS } from "@/lib/constants";
import Link from "next/link";
import { useState } from "react";

export default function TeamDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const { hasRole } = useAuth();
  const isAdmin = hasRole("Admin");

  const { data: teams = [], isLoading } = useAdminTeams(isAdmin);
  const eliminateMutation = useEliminateTeam();
  const [showEliminate, setShowEliminate] = useState(false);
  const [eliminationReason, setEliminationReason] = useState("");

  const team = teams.find((t) => t.teamId === id);

  if (!isAdmin) {
    return (
      <div className="flex items-center justify-center h-64">
        <p className="text-slate-500">You do not have permission to access this page.</p>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="space-y-4 animate-pulse">
        <div className="h-8 w-48 bg-slate-200 rounded" />
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="h-32 bg-slate-200 rounded-xl" />
          ))}
        </div>
      </div>
    );
  }

  if (!isLoading && !team) {
    return (
      <div className="flex flex-col items-center justify-center h-64 gap-4">
        <Trophy className="w-12 h-12 text-slate-300" />
        <p className="text-slate-500">Team not found.</p>
        <Link href="/teams" className="text-sm text-blue-600 hover:underline flex items-center gap-1">
          <ArrowLeft className="w-4 h-4" /> Back to list
        </Link>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-2 mb-2">
        <Link href="/teams" className="flex items-center gap-1 text-sm text-slate-500 hover:text-slate-700 transition-colors">
          <ArrowLeft className="w-4 h-4" /> Teams List
        </Link>
      </div>

      <PageHeader
        title={team!.teamName}
        description={`Category: ${team!.category?.categoryName}`}
        icon={Trophy}
        actions={
          isAdmin && team!.status !== "Eliminated" ? (
            <button
              onClick={() => setShowEliminate(true)}
              className="flex items-center gap-2 px-4 py-2 bg-red-600 text-white rounded-lg text-sm font-medium hover:bg-red-700 transition-colors"
            >
              <AlertTriangle className="w-4 h-4" /> Eliminate Team
            </button>
          ) : null
        }
      />

      {/* Team info cards */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="bg-white rounded-xl border border-slate-200 p-5 shadow-sm">
          <p className="text-xs font-medium text-slate-500 uppercase tracking-wide mb-2">Status</p>
          <StatusBadge type="team" value={team!.status} />
          <p className="text-xs text-slate-400 mt-2">{TEAM_STATUS_LABELS[team!.status] ?? team!.status}</p>
        </div>
        <div className="bg-white rounded-xl border border-slate-200 p-5 shadow-sm">
          <p className="text-xs font-medium text-slate-500 uppercase tracking-wide mb-2">Current Round</p>
          <p className="font-semibold text-slate-900">
            {team!.currentRound?.roundName ?? "No round assigned"}
          </p>
        </div>
        <div className="bg-white rounded-xl border border-slate-200 p-5 shadow-sm">
          <p className="text-xs font-medium text-slate-500 uppercase tracking-wide mb-2">Members</p>
          <p className="font-semibold text-slate-900">{team!.members.length} / 5</p>
        </div>
      </div>

      {/* Members */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-sm">
        <div className="px-5 py-4 border-b border-slate-200 flex items-center gap-2">
          <Users className="w-4 h-4 text-slate-500" />
          <h3 className="font-semibold text-slate-800">Team Members</h3>
        </div>
        <div className="divide-y divide-slate-100">
          {team!.members.map((member) => {
            const isLeader = member.userId === team!.leaderId;
            return (
              <div key={member.userId} className="px-5 py-3 flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <div className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-semibold ${isLeader ? "bg-blue-600 text-white" : "bg-slate-100 text-slate-700"}`}>
                    {member.fullName.charAt(0)}
                  </div>
                  <div>
                    <p className="text-sm font-medium text-slate-800">{member.fullName}</p>
                    <p className="text-xs text-slate-400">{member.email}</p>
                  </div>
                </div>
                {isLeader && (
                  <span className="text-xs px-2 py-0.5 bg-blue-100 text-blue-700 rounded-full font-medium">
                    Team Leader
                  </span>
                )}
              </div>
            );
          })}
        </div>
      </div>

      {/* Elimination info */}
      {team!.status === "Eliminated" && team!.eliminationReason && (
        <div className="bg-red-50 border border-red-200 rounded-xl p-5">
          <div className="flex items-center gap-2 mb-2">
            <AlertTriangle className="w-4 h-4 text-red-600" />
            <h3 className="font-semibold text-red-800">Elimination Info</h3>
          </div>
          <p className="text-sm text-red-700 mb-2">{team!.eliminationReason}</p>
          {team!.eliminatedAt && (
            <div className="flex items-center gap-1 text-xs text-red-500">
              <Calendar className="w-3.5 h-3.5" />
              <span>{formatDate(team!.eliminatedAt)}</span>
            </div>
          )}
        </div>
      )}

      {/* Eliminate modal */}
      {showEliminate && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
          <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" onClick={() => setShowEliminate(false)} />
          <div className="relative bg-white rounded-2xl shadow-2xl w-full max-w-md p-6">
            <div className="w-12 h-12 rounded-full bg-red-100 flex items-center justify-center mx-auto mb-4">
              <AlertTriangle className="w-6 h-6 text-red-600" />
            </div>
            <h2 className="text-lg font-semibold text-slate-900 text-center mb-1">Eliminate Team</h2>
            <p className="text-sm text-slate-500 text-center mb-4">{team!.teamName}</p>
            <div className="mb-4">
              <label className="block text-sm font-medium text-slate-700 mb-1.5">
                Elimination Reason <span className="text-red-500">*</span>
              </label>
              <textarea
                value={eliminationReason}
                onChange={(e) => setEliminationReason(e.target.value)}
                placeholder="Enter reason..."
                rows={3}
                className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-red-500 resize-none"
              />
            </div>
            <div className="flex gap-3">
              <button onClick={() => setShowEliminate(false)} className="flex-1 px-4 py-2.5 border border-slate-200 rounded-lg text-sm text-slate-700 hover:bg-slate-50">
                Cancel
              </button>
              <button
                onClick={async () => {
                  if (!eliminationReason.trim()) return;
                  await eliminateMutation.mutateAsync({ teamId: team!.teamId, data: { reason: eliminationReason } });
                  setShowEliminate(false);
                }}
                disabled={eliminateMutation.isPending || !eliminationReason.trim()}
                className="flex-1 px-4 py-2.5 bg-red-600 text-white rounded-lg text-sm font-medium hover:bg-red-700 disabled:opacity-50"
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
