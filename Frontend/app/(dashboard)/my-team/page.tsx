"use client";

import { FormEvent, useState } from "react";
import Link from "next/link";
import { Plus, Send, Trash2, Trophy, Users } from "lucide-react";
import PageHeader from "@/components/shared/PageHeader";
import StatusBadge from "@/components/shared/StatusBadge";
import ConfirmDialog from "@/components/shared/ConfirmDialog";
import PendingInvitesPanel from "@/components/team/PendingInvitesPanel";
import TeamInvitePanel from "@/components/team/TeamInvitePanel";
import { useAuth } from "@/contexts/AuthContext";
import { useCreateTeam, useMyTeam, useRemoveTeamMember } from "@/hooks/useTeams";
import { useEvents } from "@/hooks/useEvents";
import { formatDate, getErrorMessage } from "@/lib/utils";
import { Category } from "@/types/event";
import { TeamMember } from "@/types/team";

export default function MyTeamPage() {
  const { user, hasRole } = useAuth();
  const canHaveTeam = hasRole("Member") || hasRole("TeamLeader");
  const { data: myTeam, isLoading } = useMyTeam(canHaveTeam);
  const { data: events = [], isLoading: eventsLoading } = useEvents();
  const createTeam = useCreateTeam();
  const removeMember = useRemoveTeamMember();

  const [teamName, setTeamName] = useState("");
  const [selectedEventId, setSelectedEventId] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [memberStudentCodesText, setMemberStudentCodesText] = useState("");
  const [createError, setCreateError] = useState("");
  const [removeTarget, setRemoveTarget] = useState<TeamMember | null>(null);

  const selectedEvent = events.find((event) => event.eventId === selectedEventId);
  const availableCategories = selectedEvent?.categories ?? [];

  const currentRoundId = myTeam?.currentRound?.roundId;
  const currentRoundDeadline = currentRoundId
    ? events
      .flatMap((event) => event.rounds ?? [])
      .find((round) => round.roundId === currentRoundId)?.submissionDeadline ?? null
    : null;

  const isLeader = !!user && myTeam?.leaderId === user.id;
  const canEditMembers = isLeader && myTeam?.status === "Pending";
  const canInviteMembers =
    isLeader &&
    !!myTeam &&
    myTeam.members.length < 5 &&
    myTeam.status !== "Eliminated" &&
    myTeam.status !== "Archived";
  const canSubmit = isLeader && myTeam?.status === "Approved" && !!myTeam.currentRound;

  if (!canHaveTeam) {
    return (
      <div className="flex items-center justify-center h-64">
        <p className="text-slate-500">Your current role cannot create or manage a team.</p>
      </div>
    );
  }

  async function handleCreateTeam(e: FormEvent) {
    e.preventDefault();
    setCreateError("");

    const memberStudentCodes = memberStudentCodesText
      .split(/[\s,]+/)
      .map((value) => value.trim())
      .filter(Boolean);

    if (!teamName.trim()) {
      setCreateError("Team name is required.");
      return;
    }

    if (!categoryId) {
      setCreateError("Choose an event category.");
      return;
    }

    if (memberStudentCodes.length < 2 || memberStudentCodes.length > 4) {
      setCreateError("Enter 2 to 4 teammate student codes. You are added as leader automatically.");
      return;
    }

    // Prevent duplicates case-insensitively
    const normalizedCodes = memberStudentCodes.map((code) => code.toUpperCase());
    const uniqueCodes = new Set(normalizedCodes);
    if (uniqueCodes.size !== memberStudentCodes.length) {
      setCreateError("Duplicate student codes are not allowed.");
      return;
    }

    // Prevent user from entering their own student code as a teammate
    if (user?.studentCode && normalizedCodes.includes(user.studentCode.toUpperCase())) {
      setCreateError("You cannot include your own student code as a team member.");
      return;
    }

    try {
      await createTeam.mutateAsync({
        teamName: teamName.trim(),
        categoryId,
        memberStudentCodes,
      });
    } catch (err) {
      setCreateError(getErrorMessage(err));
    }
  }

  if (isLoading || eventsLoading) {
    return (
      <div className="space-y-4 animate-pulse">
        <div className="h-10 w-56 rounded bg-slate-200" />
        <div className="h-64 rounded-xl bg-slate-200" />
      </div>
    );
  }

  if (!myTeam) {
    return (
      <div className="space-y-6">
        <PageHeader title="My Team" description="Create a team and invite teammates by Student Code" icon={Trophy} />
        <PendingInvitesPanel enabled={canHaveTeam} />
        <form onSubmit={handleCreateTeam} className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
          <div className="grid gap-4 lg:grid-cols-2">
            <label className="lg:col-span-2">
              <span className="mb-1.5 block text-sm font-medium text-slate-700">Team Name</span>
              <input
                value={teamName}
                onChange={(e) => setTeamName(e.target.value)}
                className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </label>
            <label>
              <span className="mb-1.5 block text-sm font-medium text-slate-700">Event</span>
              <select
                value={selectedEventId}
                onChange={(e) => {
                  setSelectedEventId(e.target.value);
                  setCategoryId("");
                }}
                className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value="">Choose event</option>
                {events.map((event) => (
                  <option key={event.eventId} value={event.eventId}>{event.eventName}</option>
                ))}
              </select>
            </label>
            <label>
              <span className="mb-1.5 block text-sm font-medium text-slate-700">Category</span>
              <select
                value={categoryId}
                onChange={(e) => setCategoryId(e.target.value)}
                disabled={!selectedEventId}
                className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-slate-50"
              >
                <option value="">Choose category</option>
                {availableCategories.map((category: Category) => (
                  <option key={category.categoryId} value={category.categoryId}>{category.categoryName}</option>
                ))}
              </select>
            </label>
            <label className="lg:col-span-2">
              <span className="mb-1.5 block text-sm font-medium text-slate-700">Teammate Student Codes</span>
              <textarea
                value={memberStudentCodesText}
                onChange={(e) => setMemberStudentCodesText(e.target.value)}
                rows={4}
                placeholder="Enter student codes separated by commas or new lines"
                className="w-full resize-none rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </label>
          </div>
          {createError && <p className="mt-4 text-sm text-red-600">{createError}</p>}
          <div className="mt-6 flex justify-end">
            <button
              type="submit"
              disabled={createTeam.isPending}
              className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
            >
              <Plus className="h-4 w-4" />
              {createTeam.isPending ? "Creating..." : "Create Team"}
            </button>
          </div>
        </form>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title={myTeam.teamName}
        description={myTeam.category?.categoryName}
        icon={Trophy}
        actions={
          canSubmit ? (
            <Link href="/submit" className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-3 py-2 text-sm font-medium text-white hover:bg-blue-700">
              <Send className="h-4 w-4" />
              Submit Project
            </Link>
          ) : null
        }
      />

      <PendingInvitesPanel enabled={canHaveTeam} />

      <div className="grid gap-4 lg:grid-cols-3">
        <div className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
          <p className="mb-2 text-xs font-medium uppercase tracking-wide text-slate-500">Status</p>
          <StatusBadge type="team" value={myTeam.status} />
        </div>
        <div className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
          <p className="mb-2 text-xs font-medium uppercase tracking-wide text-slate-500">Current Round</p>
          <p className="font-semibold text-slate-900">{myTeam.currentRound?.roundName ?? "Not assigned"}</p>
        </div>
        <div className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
          <p className="mb-2 text-xs font-medium uppercase tracking-wide text-slate-500">Deadline</p>
          <p className="font-semibold text-slate-900">{currentRoundDeadline ? formatDate(currentRoundDeadline) : "Not configured"}</p>
        </div>
      </div>

      <div className="rounded-xl border border-slate-200 bg-white shadow-sm">
        <div className="flex items-center justify-between gap-3 border-b border-slate-200 px-5 py-4">
          <div className="flex items-center gap-2">
            <Users className="h-4 w-4 text-slate-500" />
            <h2 className="font-semibold text-slate-800">Members</h2>
          </div>
          <span className="text-xs text-slate-500">{myTeam.members.length} / 5</span>
        </div>
        <div className="divide-y divide-slate-100">
          {myTeam.members.map((member) => {
            const memberIsLeader = member.userId === myTeam.leaderId;
            return (
              <div key={member.userId} className="flex items-center justify-between gap-3 px-5 py-3">
                <div className="flex items-center gap-3">
                  <div className={`flex h-8 w-8 items-center justify-center rounded-full text-sm font-semibold ${memberIsLeader ? "bg-blue-600 text-white" : "bg-slate-100 text-slate-700"}`}>
                    {member.fullName.charAt(0)}
                  </div>
                  <div>
                    <div className="flex items-center gap-2">
                      <p className="text-sm font-medium text-slate-800">{member.fullName}</p>
                      <span className="text-xs font-mono text-slate-400">({member.studentCode})</span>
                    </div>
                    <p className="text-xs text-slate-400">{member.email}</p>
                  </div>
                </div>
                {memberIsLeader ? (
                  <span className="rounded-full bg-blue-50 px-2 py-0.5 text-xs font-medium text-blue-700">Leader</span>
                ) : canEditMembers ? (
                  <button onClick={() => setRemoveTarget(member)} className="inline-flex items-center gap-1 rounded-lg bg-red-50 px-2 py-1.5 text-xs font-medium text-red-600 hover:bg-red-100">
                    <Trash2 className="h-3.5 w-3.5" /> Remove
                  </button>
                ) : null}
              </div>
            );
          })}
        </div>
        {isLeader && (
          <TeamInvitePanel teamId={myTeam.teamId} disabled={!canInviteMembers} />
        )}
      </div>

      <ConfirmDialog
        open={!!removeTarget}
        onClose={() => setRemoveTarget(null)}
        onConfirm={async () => {
          await removeMember.mutateAsync({ teamId: myTeam.teamId, studentCode: removeTarget!.studentCode });
          setRemoveTarget(null);
        }}
        title="Remove Member"
        description={`Remove "${removeTarget?.fullName}" from this team?`}
        confirmLabel="Remove"
        variant="danger"
        isLoading={removeMember.isPending}
      />
    </div>
  );
}
