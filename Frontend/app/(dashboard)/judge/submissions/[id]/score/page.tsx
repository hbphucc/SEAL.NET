"use client";

import { FormEvent, use, useMemo, useState } from "react";
import Link from "next/link";
import { ArrowLeft, ExternalLink, Save } from "lucide-react";
import PageHeader from "@/components/shared/PageHeader";
import { useAuth } from "@/contexts/AuthContext";
import { useCriteria } from "@/hooks/useEvents";
import { useAssignedSubmissions, useSubmitBulkScores } from "@/hooks/useScores";
import { Criteria } from "@/types/event";

type ScoreFormValue = {
  scoreValue: string;
  comment: string;
};

export default function JudgeScorePage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const { hasRole } = useAuth();
  const isJudge = hasRole("Judge");
  const { data: submissions = [], isLoading: submissionsLoading } = useAssignedSubmissions(isJudge);
  const submission = submissions.find((item) => item.submissionId === id);
  const { data: criteria = [], isLoading: criteriaLoading } = useCriteria(submission?.round.roundId ?? "");
  const submitBulk = useSubmitBulkScores();
  const [scores, setScores] = useState<Record<string, ScoreFormValue>>({});
  const [formError, setFormError] = useState("");

  const totalWeight = useMemo(
    () => criteria.reduce((sum, item) => sum + Number(item.weight), 0),
    [criteria]
  );

  if (!isJudge) {
    return (
      <div className="flex h-64 items-center justify-center">
        <p className="text-slate-500">You do not have permission to access this page.</p>
      </div>
    );
  }

  if (submissionsLoading) {
    return <div className="h-64 animate-pulse rounded-xl bg-slate-200" />;
  }

  if (!submission) {
    return (
      <div className="flex flex-col items-center justify-center gap-3 rounded-xl border border-slate-200 bg-white px-4 py-16 text-center">
        <p className="text-sm text-slate-500">Submission not found or not assigned to you.</p>
        <Link href="/judge/submissions" className="text-sm font-medium text-blue-600 hover:underline">Back to submissions</Link>
      </div>
    );
  }

  function updateScore(criteriaId: string, patch: Partial<ScoreFormValue>) {
    setScores((prev) => ({
      ...prev,
      [criteriaId]: {
        scoreValue: prev[criteriaId]?.scoreValue ?? "",
        comment: prev[criteriaId]?.comment ?? "",
        ...patch,
      },
    }));
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setFormError("");

    if (!submission) return;

    const payloadScores = criteria.map((item) => {
      const value = scores[item.criteriaId]?.scoreValue ?? "";
      const numeric = Number(value);
      return {
        criteriaId: item.criteriaId,
        scoreValue: numeric,
        comment: scores[item.criteriaId]?.comment?.trim() || undefined,
        maxScore: Number(item.maxScore),
        rawValue: value,
      };
    });

    if (payloadScores.length === 0) {
      setFormError("This round has no criteria configured.");
      return;
    }

    const missing = payloadScores.some((item) => item.rawValue === "");
    if (missing) {
      setFormError("Enter a score for every criterion before submitting.");
      return;
    }

    const invalid = payloadScores.find((item) =>
      !Number.isFinite(item.scoreValue) || item.scoreValue < 0 || item.scoreValue > item.maxScore
    );
    if (invalid) {
      setFormError(`Scores must be between 0 and each criterion's max score.`);
      return;
    }

    await submitBulk.mutateAsync({
      submissionId: submission.submissionId,
      scores: payloadScores.map(({ criteriaId, scoreValue, comment }) => ({
        criteriaId,
        scoreValue,
        comment,
      })),
    });
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title={`Score ${submission.team.teamName}`}
        description={`${submission.round.roundName} / ${submission.team.category}`}
        icon={Save}
        actions={
          <Link href="/judge/submissions" className="inline-flex items-center gap-2 rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50">
            <ArrowLeft className="h-4 w-4" />
            Submissions
          </Link>
        }
      />

      <div className="grid gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm md:grid-cols-3">
        <ResourceLink href={submission.repositoryUrl} label="Repository" />
        <ResourceLink href={submission.demoUrl} label="Demo" />
        <ResourceLink href={submission.slideUrl} label="Slides" />
      </div>

      <form onSubmit={handleSubmit} className="rounded-xl border border-slate-200 bg-white shadow-sm">
        <div className="border-b border-slate-100 px-5 py-4">
          <h2 className="font-semibold text-slate-900">Criteria</h2>
          <p className="text-xs text-slate-400">Total configured weight: {totalWeight}/100</p>
        </div>

        <div className="divide-y divide-slate-100">
          {criteriaLoading ? (
            Array.from({ length: 4 }).map((_, index) => (
              <div key={index} className="px-5 py-4">
                <div className="h-6 animate-pulse rounded bg-slate-200" />
              </div>
            ))
          ) : criteria.length === 0 ? (
            <div className="px-5 py-10 text-center text-sm text-slate-400">No criteria found for this round.</div>
          ) : (
            criteria.map((item: Criteria) => (
              <div key={item.criteriaId} className="grid gap-4 px-5 py-4 lg:grid-cols-[1fr_160px]">
                <div>
                  <p className="font-medium text-slate-900">{item.criteriaName}</p>
                  <p className="text-xs text-slate-400">Max {item.maxScore} / Weight {item.weight}</p>
                  <textarea
                    value={scores[item.criteriaId]?.comment ?? ""}
                    onChange={(e) => updateScore(item.criteriaId, { comment: e.target.value })}
                    rows={2}
                    placeholder="Optional comment"
                    className="mt-3 w-full resize-none rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <label>
                  <span className="mb-1.5 block text-sm font-medium text-slate-700">Score</span>
                  <input
                    type="number"
                    min="0"
                    max={item.maxScore}
                    step="0.01"
                    value={scores[item.criteriaId]?.scoreValue ?? ""}
                    onChange={(e) => updateScore(item.criteriaId, { scoreValue: e.target.value })}
                    className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </label>
              </div>
            ))
          )}
        </div>

        {formError && <p className="px-5 pt-4 text-sm text-red-600">{formError}</p>}
        <div className="flex justify-end border-t border-slate-100 px-5 py-4">
          <button
            type="submit"
            disabled={criteriaLoading || submitBulk.isPending}
            className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
          >
            <Save className="h-4 w-4" />
            {submitBulk.isPending ? "Saving..." : "Submit Scores"}
          </button>
        </div>
      </form>
    </div>
  );
}

function ResourceLink({ href, label }: { href?: string | null; label: string }) {
  if (!href) {
    return <span className="text-sm text-slate-400">{label}: Not provided</span>;
  }

  return (
    <a href={href} target="_blank" rel="noreferrer" className="inline-flex items-center gap-2 text-sm font-medium text-blue-600 hover:underline">
      {label}
      <ExternalLink className="h-4 w-4" />
    </a>
  );
}
