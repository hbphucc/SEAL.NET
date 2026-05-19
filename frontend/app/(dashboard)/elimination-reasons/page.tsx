"use client";

import { useAdminTeams } from "@/hooks/useTeams";
import PageHeader from "@/components/shared/PageHeader";
import { FileText, Search } from "lucide-react";
import { Team } from "@/types/team";
import { formatDate } from "@/lib/utils";
import Link from "next/link";
import { useState, useMemo } from "react";

export default function EliminationReasonsPage() {
  const { data: teams = [], isLoading } = useAdminTeams();
  const [search, setSearch] = useState("");

  const eliminated = useMemo(() => {
    const base = teams.filter((t) => t.status === "Eliminated" && t.eliminationReason);
    if (!search.trim()) return base;
    const q = search.toLowerCase();
    return base.filter(
      (t) =>
        t.teamName.toLowerCase().includes(q) ||
        t.eliminationReason?.toLowerCase().includes(q)
    );
  }, [teams, search]);

  // Group by reason
  const reasonGroups = useMemo(() => {
    const groups: Record<string, Team[]> = {};
    for (const team of eliminated) {
      const reason = team.eliminationReason || "No reason provided";
      if (!groups[reason]) groups[reason] = [];
      groups[reason].push(team);
    }
    return groups;
  }, [eliminated]);

  return (
    <div>
      <PageHeader
        title="Elimination Reasons"
        description="Summary of elimination reasons and respective teams"
        icon={FileText}
      />

      {/* Search */}
      <div className="relative max-w-xs mb-6">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
        <input
          type="text"
          placeholder="Search reasons, team name..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="w-full pl-9 pr-4 py-2 text-sm border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
        />
      </div>

      {isLoading ? (
        <div className="space-y-4">
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="h-24 bg-slate-200 rounded-xl animate-pulse" />
          ))}
        </div>
      ) : Object.keys(reasonGroups).length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <FileText className="w-12 h-12 text-slate-300 mb-3" />
          <p className="text-slate-500 text-sm">No elimination reasons found</p>
        </div>
      ) : (
        <div className="space-y-4">
          {Object.entries(reasonGroups).map(([reason, reasonTeams]) => (
            <div key={reason} className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
              <div className="px-5 py-3 bg-red-50 border-b border-red-100 flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <div className="w-2 h-2 rounded-full bg-red-500" />
                  <span className="font-medium text-red-800 text-sm">{reason}</span>
                </div>
                <span className="text-xs bg-red-100 text-red-600 px-2 py-0.5 rounded-full font-medium">
                  {reasonTeams.length} teams
                </span>
              </div>
              <div className="divide-y divide-slate-100">
                {reasonTeams.map((team) => (
                  <div key={team.teamId} className="px-5 py-3 flex items-center justify-between hover:bg-slate-50 transition-colors">
                    <div>
                      <Link href={`/teams/${team.teamId}`} className="text-sm font-medium text-blue-600 hover:underline">
                        {team.teamName}
                      </Link>
                      <p className="text-xs text-slate-400">{team.category?.categoryName}</p>
                    </div>
                    <span className="text-xs text-slate-400">{formatDate(team.eliminatedAt)}</span>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Summary stats */}
      <div className="mt-6 grid grid-cols-2 gap-4">
        <div className="bg-white rounded-xl border border-slate-200 p-4 shadow-sm">
          <p className="text-xs text-slate-500 mb-1">Total Eliminated Teams</p>
          <p className="text-2xl font-bold text-slate-900">{teams.filter(t => t.status === "Eliminated").length}</p>
        </div>
        <div className="bg-white rounded-xl border border-slate-200 p-4 shadow-sm">
          <p className="text-xs text-slate-500 mb-1">Unique Reasons Count</p>
          <p className="text-2xl font-bold text-slate-900">{Object.keys(reasonGroups).length}</p>
        </div>
      </div>
    </div>
  );
}
