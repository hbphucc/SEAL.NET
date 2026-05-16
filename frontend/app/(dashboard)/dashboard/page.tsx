"use client";

import { useAuth } from "@/contexts/AuthContext";
import { useUsers } from "@/hooks/useUsers";
import { useAdminTeams } from "@/hooks/useTeams";
import { useMyTeam } from "@/hooks/useTeams";
import { useEvents } from "@/hooks/useEvents";
import { useTeamSubmissions } from "@/hooks/useSubmissions";
import StatCard from "@/components/shared/StatCard";
import StatusBadge from "@/components/shared/StatusBadge";
import { CalendarDays, Send, Users, Trophy, UserCheck, XCircle, Clock, CheckCircle } from "lucide-react";
import { getTeamStatusColor } from "@/lib/utils";
import { TEAM_STATUS_LABELS } from "@/lib/constants";
import { cn } from "@/lib/utils";

export default function DashboardPage() {
  const { user, hasRole } = useAuth();
  const isAdmin = hasRole("Admin");
  const canHaveTeam = hasRole("Member") || hasRole("TeamLeader");

  const { data: users, isLoading: usersLoading } = useUsers(isAdmin);
  const { data: teams, isLoading: teamsLoading } = useAdminTeams(isAdmin);
  const { data: myTeam } = useMyTeam(!isAdmin && canHaveTeam);
  const { data: events, isLoading: eventsLoading } = useEvents();
  const { data: mySubmissions } = useTeamSubmissions(myTeam?.teamId);

  // Stats
  const totalUsers = users?.length ?? 0;
  const approvedUsers = users?.filter((u) => u.isApproved).length ?? 0;
  const pendingUsers = users?.filter((u) => !u.isApproved).length ?? 0;
  const totalTeams = teams?.length ?? 0;
  const activeTeams = teams?.filter((t) => t.status === "Active" || t.status === "Approved").length ?? 0;
  const eliminatedTeams = teams?.filter((t) => t.status === "Eliminated").length ?? 0;
  const pendingTeams = teams?.filter((t) => t.status === "Pending").length ?? 0;
  const totalEvents = events?.length ?? 0;
  const activeEvents = events?.filter((event) => event.status === "Ongoing").length ?? 0;
  const totalRounds = events?.reduce((sum, event) => sum + (event.rounds?.length ?? 0), 0) ?? 0;
  const totalCategories = events?.reduce((sum, event) => sum + (event.categories?.length ?? 0), 0) ?? 0;
  const approvedRatio = totalTeams > 0 ? Math.round((activeTeams / totalTeams) * 100) : 0;

  // Recent teams
  const recentTeams = teams?.slice(0, 5) ?? [];

  return (
    <div className="space-y-6">
      {/* Welcome */}
      <div className="bg-gradient-to-r from-slate-900 to-blue-900 rounded-2xl p-6 text-white shadow-lg">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-xl font-bold">
              Hello, {user?.fullName} 👋
            </h2>
            <p className="text-slate-300 text-sm mt-1">
              {new Date().toLocaleDateString("en-US", {
                weekday: "long",
                day: "numeric",
                month: "long",
                year: "numeric",
              })}
            </p>
          </div>
          <div className="hidden sm:flex flex-col items-end gap-1">
            {user?.roles.map((role) => (
              <span
                key={role}
                className="text-xs px-2 py-1 bg-white/10 rounded-full text-white border border-white/20"
              >
                {role}
              </span>
            ))}
          </div>
        </div>
      </div>

      {/* Stats grid — Admin only */}
      {isAdmin && (
        <>
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            <StatCard
              title="Total Users"
              value={totalUsers}
              icon={Users}
              color="blue"
              isLoading={usersLoading}
            />
            <StatCard
              title="Approved Users"
              value={approvedUsers}
              icon={CheckCircle}
              color="green"
              isLoading={usersLoading}
            />
            <StatCard
              title="Pending Users"
              value={pendingUsers}
              icon={Clock}
              color="yellow"
              isLoading={usersLoading}
            />
            <StatCard
              title="Total Teams"
              value={totalTeams}
              icon={Trophy}
              color="purple"
              isLoading={teamsLoading}
            />
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
            <StatCard
              title="Active Teams"
              value={activeTeams}
              icon={UserCheck}
              color="green"
              isLoading={teamsLoading}
            />
            <StatCard
              title="Pending Teams"
              value={pendingTeams}
              icon={Clock}
              color="yellow"
              isLoading={teamsLoading}
            />
            <StatCard
              title="Eliminated Teams"
              value={eliminatedTeams}
              icon={XCircle}
              color="red"
              isLoading={teamsLoading}
            />
          </div>

          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            <StatCard
              title="Events"
              value={totalEvents}
              icon={CalendarDays}
              color="blue"
              isLoading={eventsLoading}
            />
            <StatCard
              title="Ongoing Events"
              value={activeEvents}
              icon={Clock}
              color="green"
              isLoading={eventsLoading}
            />
            <StatCard
              title="Rounds"
              value={totalRounds}
              icon={Trophy}
              color="purple"
              isLoading={eventsLoading}
            />
            <StatCard
              title="Categories"
              value={totalCategories}
              icon={Users}
              color="yellow"
              isLoading={eventsLoading}
            />
          </div>

          <div className="bg-white rounded-xl border border-slate-200 shadow-sm p-5">
            <div className="flex items-center justify-between mb-3">
              <div>
                <h3 className="font-semibold text-slate-800">Team Approval Rate</h3>
                <p className="text-xs text-slate-400">Approved or active teams across all registrations</p>
              </div>
              <span className="text-sm font-semibold text-blue-700">{approvedRatio}%</span>
            </div>
            <div className="h-2 rounded-full bg-slate-100 overflow-hidden">
              <div className="h-full rounded-full bg-blue-600" style={{ width: `${approvedRatio}%` }} />
            </div>
          </div>

          {/* Recent teams */}
          <div className="bg-white rounded-xl border border-slate-200 shadow-sm">
            <div className="px-5 py-4 border-b border-slate-200">
              <h3 className="font-semibold text-slate-800">Recent Teams</h3>
              <p className="text-xs text-slate-400 mt-0.5">5 recently registered teams</p>
            </div>
            <div className="divide-y divide-slate-100">
              {teamsLoading
                ? Array.from({ length: 5 }).map((_, i) => (
                  <div key={i} className="px-5 py-3 flex items-center justify-between">
                    <div className="h-4 w-40 bg-slate-200 rounded animate-pulse" />
                    <div className="h-5 w-20 bg-slate-100 rounded-full animate-pulse" />
                  </div>
                ))
                : recentTeams.map((team) => (
                  <div key={team.teamId} className="px-5 py-3 flex items-center justify-between hover:bg-slate-50 transition-colors">
                    <div>
                      <p className="text-sm font-medium text-slate-800">{team.teamName}</p>
                      <p className="text-xs text-slate-400">{team.category?.categoryName}</p>
                    </div>
                    <div className="flex items-center gap-2">
                      <span className={cn(
                        "inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium border",
                        getTeamStatusColor(team.status)
                      )}>
                        {TEAM_STATUS_LABELS[team.status] ?? team.status}
                      </span>
                    </div>
                  </div>
                ))
              }
            </div>
          </div>
        </>
      )}

      {/* My Team — Member/TeamLeader */}
      {!isAdmin && (
        <div className="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
          <h3 className="font-semibold text-slate-800 mb-4">My Team</h3>
          {myTeam ? (
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <div>
                  <p className="font-semibold text-slate-900">{myTeam.teamName}</p>
                  <p className="text-sm text-slate-500">{myTeam.category?.categoryName}</p>
                </div>
                <StatusBadge type="team" value={myTeam.status} />
              </div>
              {myTeam.currentRound && (
                <div className="bg-blue-50 border border-blue-100 rounded-lg px-4 py-3">
                  <p className="text-xs text-blue-600 font-medium">Current Round</p>
                  <p className="text-sm font-semibold text-blue-800">{myTeam.currentRound.roundName}</p>
                </div>
              )}
              <div className="bg-slate-50 border border-slate-100 rounded-lg px-4 py-3">
                <div className="flex items-center gap-2 text-xs font-medium text-slate-500">
                  <Send className="w-3.5 h-3.5" />
                  Submissions
                </div>
                <p className="text-sm font-semibold text-slate-800 mt-1">
                  {mySubmissions?.length ?? 0} submitted round{(mySubmissions?.length ?? 0) === 1 ? "" : "s"}
                </p>
              </div>
              <div>
                <p className="text-xs font-medium text-slate-500 mb-2">Members ({myTeam.members.length})</p>
                <div className="space-y-1.5">
                  {myTeam.members.map((m) => (
                    <div key={m.userId} className="flex items-center gap-2 text-sm text-slate-700">
                      <div className="w-6 h-6 rounded-full bg-blue-100 flex items-center justify-center text-blue-700 text-xs font-semibold flex-shrink-0">
                        {m.fullName.charAt(0)}
                      </div>
                      <span>{m.fullName}</span>
                      {m.userId === myTeam.leaderId && (
                        <span className="text-xs text-blue-600 font-medium">(Leader)</span>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            </div>
          ) : (
            <div className="text-center py-8 text-slate-400">
              <Trophy className="w-10 h-10 mx-auto mb-2 opacity-30" />
              <p className="text-sm">You haven&apos;t joined a team yet</p>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
