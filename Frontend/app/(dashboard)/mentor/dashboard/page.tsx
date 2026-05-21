"use client";

import { FormEvent, useMemo, useState } from "react";
import {
  AlertCircle,
  ExternalLink,
  FileText,
  Handshake,
  MessageSquareText,
  Send,
  Users2,
} from "lucide-react";
import EmptyState from "@/components/shared/EmptyState";
import PageHeader from "@/components/shared/PageHeader";
import StatCard from "@/components/shared/StatCard";
import StatusBadge from "@/components/shared/StatusBadge";
import { useAuth } from "@/contexts/AuthContext";
import {
  useAddMentorshipNote,
  useMentorTeams,
  useMentorTeamSubmissions,
  useMentorshipNotes,
} from "@/hooks/useMentors";
import { formatDate, getErrorMessage } from "@/lib/utils";
import { Team } from "@/types/team";

function LoadingRows() {
  return (
    <div className="space-y-3">
      {Array.from({ length: 4 }).map((_, index) => (
        <div key={index} className="rounded-lg border border-slate-100 bg-white p-4">
          <div className="h-4 w-44 animate-pulse rounded bg-slate-200" />
          <div className="mt-3 h-3 w-28 animate-pulse rounded bg-slate-100" />
        </div>
      ))}
    </div>
  );
}

function ErrorPanel({ message }: { message: string }) {
  return (
    <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
      <div className="flex items-center gap-2">
        <AlertCircle className="h-4 w-4" />
        <span>{message}</span>
      </div>
    </div>
  );
}

function TeamList({
  teams,
  selectedTeamId,
  onSelect,
}: {
  teams: Team[];
  selectedTeamId?: string;
  onSelect: (teamId: string) => void;
}) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white shadow-sm">
      <div className="border-b border-slate-100 px-5 py-4">
        <h2 className="font-semibold text-slate-900">Assigned Teams</h2>
        <p className="text-xs text-slate-400">Teams currently assigned to you</p>
      </div>
      <div className="divide-y divide-slate-100">
        {teams.map((team) => {
          const selected = team.teamId === selectedTeamId;
          const leader = team.members.find((member) => member.isLeader || member.userId === team.leaderId);

          return (
            <button
              key={team.teamId}
              type="button"
              onClick={() => onSelect(team.teamId)}
              className={`w-full px-5 py-4 text-left transition-colors ${
                selected ? "bg-blue-50" : "hover:bg-slate-50"
              }`}
            >
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <p className="truncate font-medium text-slate-900">{team.teamName}</p>
                  <p className="mt-1 text-xs text-slate-400">
                    {team.category?.categoryName ?? "Uncategorized"} / {team.members.length} members
                  </p>
                  {leader && (
                    <p className="mt-1 text-xs text-slate-500">Lead: {leader.fullName}</p>
                  )}
                </div>
                <StatusBadge type="team" value={team.status} />
              </div>
            </button>
          );
        })}
      </div>
    </div>
  );
}

export default function MentorDashboardPage() {
  const { hasRole } = useAuth();
  const isMentor = hasRole("Mentor");
  const [chosenTeamId, setChosenTeamId] = useState<string>("");
  const [noteBody, setNoteBody] = useState("");

  const {
    data: teams = [],
    isLoading: teamsLoading,
    isError: teamsIsError,
    error: teamsError,
  } = useMentorTeams(isMentor);

  const selectedTeamId = chosenTeamId || teams[0]?.teamId || "";

  const selectedTeam = useMemo(
    () => teams.find((team) => team.teamId === selectedTeamId) ?? null,
    [selectedTeamId, teams]
  );

  const {
    data: submissions = [],
    isLoading: submissionsLoading,
    isError: submissionsIsError,
    error: submissionsError,
  } = useMentorTeamSubmissions(selectedTeamId);

  const {
    data: notes = [],
    isLoading: notesLoading,
    isError: notesIsError,
    error: notesError,
  } = useMentorshipNotes(selectedTeamId);

  const addNoteMutation = useAddMentorshipNote(selectedTeamId);

  if (!isMentor) {
    return (
      <div className="flex h-64 items-center justify-center">
        <p className="text-slate-500">You do not have permission to access this page.</p>
      </div>
    );
  }

  const handleSubmitNote = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const body = noteBody.trim();
    if (!body || !selectedTeamId) return;

    await addNoteMutation.mutateAsync(body);
    setNoteBody("");
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title="Mentor Dashboard"
        description="Review assigned teams, submissions, and mentorship notes"
        icon={Handshake}
      />

      <div className="grid gap-4 sm:grid-cols-3">
        <StatCard title="Assigned Teams" value={teams.length} icon={Users2} color="blue" isLoading={teamsLoading} />
        <StatCard title="Submissions" value={submissions.length} icon={FileText} color="green" isLoading={submissionsLoading} />
        <StatCard title="Notes" value={notes.length} icon={MessageSquareText} color="purple" isLoading={notesLoading} />
      </div>

      {teamsIsError && <ErrorPanel message={getErrorMessage(teamsError)} />}

      {teamsLoading ? (
        <LoadingRows />
      ) : teams.length === 0 ? (
        <div className="rounded-xl border border-slate-200 bg-white shadow-sm">
          <EmptyState
            icon={Handshake}
            title="No assigned teams"
            description="When an admin assigns you to mentor teams, they will appear here."
          />
        </div>
      ) : (
        <div className="grid gap-6 xl:grid-cols-[360px_minmax(0,1fr)]">
          <TeamList teams={teams} selectedTeamId={selectedTeamId} onSelect={setChosenTeamId} />

          <div className="space-y-6">
            <section className="rounded-xl border border-slate-200 bg-white shadow-sm">
              <div className="border-b border-slate-100 px-5 py-4">
                <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
                  <div>
                    <h2 className="font-semibold text-slate-900">{selectedTeam?.teamName ?? "Team Details"}</h2>
                    <p className="text-xs text-slate-400">{selectedTeam?.category?.categoryName ?? "No category"}</p>
                  </div>
                  {selectedTeam && <StatusBadge type="team" value={selectedTeam.status} />}
                </div>
              </div>
              <div className="grid gap-4 p-5 md:grid-cols-2">
                <div>
                  <p className="text-xs font-medium uppercase text-slate-400">Description</p>
                  <p className="mt-1 text-sm text-slate-600">
                    {selectedTeam?.description?.trim() || "No team description provided."}
                  </p>
                </div>
                <div>
                  <p className="text-xs font-medium uppercase text-slate-400">Members</p>
                  <div className="mt-2 flex flex-wrap gap-2">
                    {selectedTeam?.members.map((member) => (
                      <span
                        key={member.userId}
                        className="rounded-full border border-slate-200 bg-slate-50 px-2.5 py-1 text-xs text-slate-600"
                      >
                        {member.fullName}
                        {(member.isLeader || member.userId === selectedTeam.leaderId) && " (Lead)"}
                      </span>
                    ))}
                  </div>
                </div>
              </div>
            </section>

            <section className="rounded-xl border border-slate-200 bg-white shadow-sm">
              <div className="border-b border-slate-100 px-5 py-4">
                <h2 className="font-semibold text-slate-900">Submissions</h2>
                <p className="text-xs text-slate-400">Submitted project assets from this team</p>
              </div>
              <div className="p-5">
                {submissionsIsError ? (
                  <ErrorPanel message={getErrorMessage(submissionsError)} />
                ) : submissionsLoading ? (
                  <LoadingRows />
                ) : submissions.length === 0 ? (
                  <EmptyState
                    icon={FileText}
                    title="No submissions"
                    description="This team has not submitted project materials yet."
                  />
                ) : (
                  <div className="space-y-3">
                    {submissions.map((submission) => (
                      <div key={submission.submissionId} className="rounded-lg border border-slate-100 bg-slate-50 p-4">
                        <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                          <div>
                            <p className="font-medium text-slate-800">
                              {submission.round?.roundName ?? "Submission"}
                            </p>
                            <p className="mt-1 text-xs text-slate-400">
                              Submitted {formatDate(submission.submittedAt)}
                            </p>
                          </div>
                          <div className="flex flex-wrap gap-2">
                            {[
                              ["Repository", submission.repositoryUrl],
                              ["Demo", submission.demoUrl],
                              ["Slides", submission.slideUrl],
                            ].map(([label, url]) =>
                              url ? (
                                <a
                                  key={label}
                                  href={url}
                                  target="_blank"
                                  rel="noreferrer"
                                  className="inline-flex items-center gap-1 rounded-lg border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-100"
                                >
                                  {label}
                                  <ExternalLink className="h-3 w-3" />
                                </a>
                              ) : null
                            )}
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </section>

            <section className="rounded-xl border border-slate-200 bg-white shadow-sm">
              <div className="border-b border-slate-100 px-5 py-4">
                <h2 className="font-semibold text-slate-900">Mentorship Notes</h2>
                <p className="text-xs text-slate-400">Private guidance and follow-up notes for this team</p>
              </div>
              <div className="space-y-5 p-5">
                <form onSubmit={handleSubmitNote} className="space-y-3">
                  <textarea
                    value={noteBody}
                    onChange={(event) => setNoteBody(event.target.value)}
                    placeholder="Add a note for this team..."
                    rows={3}
                    className="w-full resize-none rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
                  />
                  <div className="flex justify-end">
                    <button
                      type="submit"
                      disabled={!noteBody.trim() || addNoteMutation.isPending}
                      className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-3 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-50"
                    >
                      <Send className="h-4 w-4" />
                      {addNoteMutation.isPending ? "Adding..." : "Add Note"}
                    </button>
                  </div>
                </form>

                {notesIsError ? (
                  <ErrorPanel message={getErrorMessage(notesError)} />
                ) : notesLoading ? (
                  <LoadingRows />
                ) : notes.length === 0 ? (
                  <EmptyState
                    icon={MessageSquareText}
                    title="No notes yet"
                    description="Add the first mentorship note for this team."
                  />
                ) : (
                  <div className="space-y-3">
                    {notes.map((note) => (
                      <article key={note.mentorshipNoteId} className="rounded-lg border border-slate-100 bg-slate-50 p-4">
                        <p className="whitespace-pre-wrap text-sm leading-6 text-slate-700">{note.body}</p>
                        <p className="mt-3 text-xs text-slate-400">
                          {note.mentor.fullName} / {formatDate(note.createdAt)}
                        </p>
                      </article>
                    ))}
                  </div>
                )}
              </div>
            </section>
          </div>
        </div>
      )}
    </div>
  );
}
