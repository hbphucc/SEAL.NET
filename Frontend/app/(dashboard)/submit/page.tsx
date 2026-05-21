"use client";

import { FormEvent, useMemo, useState } from "react";
import Link from "next/link";
import { ArrowLeft, ExternalLink, Send } from "lucide-react";
import PageHeader from "@/components/shared/PageHeader";
import { useAuth } from "@/contexts/AuthContext";
import { useEvents } from "@/hooks/useEvents";
import { useMyTeam } from "@/hooks/useTeams";
import { useSubmitProject, useTeamSubmissions } from "@/hooks/useSubmissions";
import { formatDate } from "@/lib/utils";

type SubmissionForm = {
  repositoryUrl: string;
  demoUrl: string;
  slideUrl: string;
};

const EMPTY_FORM: SubmissionForm = {
  repositoryUrl: "",
  demoUrl: "",
  slideUrl: "",
};

function normalizeUrl(value: string) {
  return value.trim() || undefined;
}

function isValidOptionalUrl(value: string) {
  if (!value.trim()) return true;
  try {
    const url = new URL(value);
    return url.protocol === "http:" || url.protocol === "https:";
  } catch {
    return false;
  }
}

export default function SubmitPage() {
  const { user, hasRole } = useAuth();
  const canLeadTeam = hasRole("TeamLeader") || hasRole("Member");
  const { data: myTeam, isLoading: teamLoading } = useMyTeam(canLeadTeam);
  const { data: events = [], isLoading: eventsLoading } = useEvents();
  const { data: submissions = [], isLoading: submissionsLoading } = useTeamSubmissions(myTeam?.teamId);
  const submitMutation = useSubmitProject(myTeam?.teamId);
  const [nowMs] = useState(() => Date.now());
  const [form, setForm] = useState<SubmissionForm>(EMPTY_FORM);
  const [isDirty, setIsDirty] = useState(false);
  const [formError, setFormError] = useState("");

  const isLeader = !!user && myTeam?.leaderId === user.id;
  const currentRound = myTeam?.currentRound ?? null;
  const currentSubmission = submissions.find((submission) => submission.round?.roundId === currentRound?.roundId);

  const currentRoundDeadline = useMemo(() => {
    if (!currentRound) return null;
    for (const event of events) {
      const round = event.rounds?.find((item) => item.roundId === currentRound.roundId);
      if (round?.submissionDeadline) return round.submissionDeadline;
    }
    return null;
  }, [currentRound, events]);

  const deadlineDate = currentRoundDeadline ? new Date(currentRoundDeadline) : null;
  const deadlinePassed = deadlineDate ? nowMs > deadlineDate.getTime() : false;
  const canSubmit =
    !!myTeam &&
    isLeader &&
    myTeam.status === "Approved" &&
    !!currentRound &&
    !!currentRoundDeadline &&
    !deadlinePassed;

  const fieldValues = isDirty
    ? form
    : {
      repositoryUrl: currentSubmission?.repositoryUrl ?? "",
      demoUrl: currentSubmission?.demoUrl ?? "",
      slideUrl: currentSubmission?.slideUrl ?? "",
    };

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setFormError("");

    if (!myTeam || !currentRound) return;

    if (!fieldValues.repositoryUrl.trim() && !fieldValues.demoUrl.trim() && !fieldValues.slideUrl.trim()) {
      setFormError("Add at least one project URL.");
      return;
    }

    if (![fieldValues.repositoryUrl, fieldValues.demoUrl, fieldValues.slideUrl].every(isValidOptionalUrl)) {
      setFormError("URLs must start with http:// or https://.");
      return;
    }

    await submitMutation.mutateAsync({
      teamId: myTeam.teamId,
      roundId: currentRound.roundId,
      repositoryUrl: normalizeUrl(fieldValues.repositoryUrl),
      demoUrl: normalizeUrl(fieldValues.demoUrl),
      slideUrl: normalizeUrl(fieldValues.slideUrl),
    });
    setIsDirty(false);
  }

  if (teamLoading || eventsLoading || submissionsLoading) {
    return (
      <div className="space-y-4 animate-pulse">
        <div className="h-10 w-56 rounded bg-slate-200" />
        <div className="h-72 rounded-xl bg-slate-200" />
      </div>
    );
  }

  if (!myTeam) {
    return (
      <div className="flex flex-col items-center justify-center gap-4 rounded-xl border border-dashed border-slate-300 bg-white px-4 py-16 text-center">
        <p className="text-sm text-slate-500">Create a team before submitting a project.</p>
        <Link href="/my-team" className="text-sm font-medium text-blue-600 hover:underline">Go to My Team</Link>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Project Submission"
        description={currentRound ? `${myTeam.teamName} - ${currentRound.roundName}` : myTeam.teamName}
        icon={Send}
        actions={
          <Link href="/my-team" className="inline-flex items-center gap-2 rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50">
            <ArrowLeft className="h-4 w-4" />
            My Team
          </Link>
        }
      />

      <div className="grid gap-4 lg:grid-cols-3">
        <div className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
          <p className="mb-2 text-xs font-medium uppercase tracking-wide text-slate-500">Team Status</p>
          <p className="font-semibold text-slate-900">{myTeam.status}</p>
        </div>
        <div className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
          <p className="mb-2 text-xs font-medium uppercase tracking-wide text-slate-500">Deadline</p>
          <p className="font-semibold text-slate-900">{currentRoundDeadline ? formatDate(currentRoundDeadline) : "Not configured"}</p>
        </div>
        <div className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
          <p className="mb-2 text-xs font-medium uppercase tracking-wide text-slate-500">Submission</p>
          <p className="font-semibold text-slate-900">{currentSubmission ? `Updated ${formatDate(currentSubmission.submittedAt)}` : "Not submitted"}</p>
        </div>
      </div>

      {!isLeader && (
        <div className="rounded-xl border border-yellow-200 bg-yellow-50 px-4 py-3 text-sm text-yellow-800">
          Only the team leader can submit or update project URLs.
        </div>
      )}
      {myTeam.status !== "Approved" && (
        <div className="rounded-xl border border-yellow-200 bg-yellow-50 px-4 py-3 text-sm text-yellow-800">
          Your team must be approved before submissions open.
        </div>
      )}
      {!currentRound && (
        <div className="rounded-xl border border-yellow-200 bg-yellow-50 px-4 py-3 text-sm text-yellow-800">
          No current round is assigned to this team.
        </div>
      )}
      {deadlinePassed && (
        <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          The submission deadline has passed. Updates are disabled.
        </div>
      )}

      <form onSubmit={handleSubmit} className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="grid gap-4">
          <UrlField
            label="Repository URL"
            value={fieldValues.repositoryUrl}
            onChange={(value) => {
              setIsDirty(true);
              setForm((prev) => ({ ...prev, ...fieldValues, repositoryUrl: value }));
            }}
            placeholder="https://github.com/team/project"
          />
          <UrlField
            label="Demo URL"
            value={fieldValues.demoUrl}
            onChange={(value) => {
              setIsDirty(true);
              setForm((prev) => ({ ...prev, ...fieldValues, demoUrl: value }));
            }}
            placeholder="https://demo.example.com"
          />
          <UrlField
            label="Slide URL"
            value={fieldValues.slideUrl}
            onChange={(value) => {
              setIsDirty(true);
              setForm((prev) => ({ ...prev, ...fieldValues, slideUrl: value }));
            }}
            placeholder="https://docs.google.com/presentation/..."
          />
        </div>
        {formError && <p className="mt-4 text-sm text-red-600">{formError}</p>}
        <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex flex-wrap gap-3 text-xs text-slate-500">
            {currentSubmission?.repositoryUrl && <ResourceLink href={currentSubmission.repositoryUrl} label="Repository" />}
            {currentSubmission?.demoUrl && <ResourceLink href={currentSubmission.demoUrl} label="Demo" />}
            {currentSubmission?.slideUrl && <ResourceLink href={currentSubmission.slideUrl} label="Slides" />}
          </div>
          <button
            type="submit"
            disabled={!canSubmit || submitMutation.isPending}
            className="inline-flex items-center justify-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            <Send className="h-4 w-4" />
            {submitMutation.isPending ? "Submitting..." : currentSubmission ? "Update Submission" : "Submit Project"}
          </button>
        </div>
      </form>
    </div>
  );
}

function UrlField({
  label,
  value,
  onChange,
  placeholder,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder: string;
}) {
  return (
    <label>
      <span className="mb-1.5 block text-sm font-medium text-slate-700">{label}</span>
      <input
        type="url"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
      />
    </label>
  );
}

function ResourceLink({ href, label }: { href: string; label: string }) {
  return (
    <a href={href} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1 text-blue-600 hover:underline">
      {label}
      <ExternalLink className="h-3 w-3" />
    </a>
  );
}
