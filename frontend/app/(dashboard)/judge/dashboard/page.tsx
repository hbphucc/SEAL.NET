"use client";

import Link from "next/link";
import { ClipboardCheck, FileText, Trophy } from "lucide-react";
import PageHeader from "@/components/shared/PageHeader";
import StatCard from "@/components/shared/StatCard";
import { useAuth } from "@/contexts/AuthContext";
import { useAssignedSubmissions } from "@/hooks/useScores";

export default function JudgeDashboardPage() {
  const { hasRole } = useAuth();
  const isJudge = hasRole("Judge");
  const { data: submissions = [], isLoading } = useAssignedSubmissions(isJudge);
  const roundCount = new Set(submissions.map((submission) => submission.round.roundId)).size;
  const categoryCount = new Set(submissions.map((submission) => submission.team.category)).size;

  if (!isJudge) {
    return (
      <div className="flex h-64 items-center justify-center">
        <p className="text-slate-500">You do not have permission to access this page.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Judge Dashboard"
        description="Review assigned submissions and score projects"
        icon={ClipboardCheck}
        actions={
          <Link href="/judge/submissions" className="rounded-lg bg-blue-600 px-3 py-2 text-sm font-medium text-white hover:bg-blue-700">
            Open Submissions
          </Link>
        }
      />

      <div className="grid gap-4 sm:grid-cols-3">
        <StatCard title="Assigned Submissions" value={submissions.length} icon={FileText} color="blue" isLoading={isLoading} />
        <StatCard title="Rounds" value={roundCount} icon={Trophy} color="purple" isLoading={isLoading} />
        <StatCard title="Categories" value={categoryCount} icon={ClipboardCheck} color="green" isLoading={isLoading} />
      </div>

      <div className="rounded-xl border border-slate-200 bg-white shadow-sm">
        <div className="border-b border-slate-100 px-5 py-4">
          <h2 className="font-semibold text-slate-900">Recent Assignments</h2>
          <p className="text-xs text-slate-400">The latest submissions available for scoring</p>
        </div>
        <div className="divide-y divide-slate-100">
          {isLoading ? (
            Array.from({ length: 4 }).map((_, index) => (
              <div key={index} className="px-5 py-4">
                <div className="h-4 w-48 animate-pulse rounded bg-slate-200" />
              </div>
            ))
          ) : submissions.length === 0 ? (
            <div className="px-5 py-10 text-center text-sm text-slate-400">No submissions assigned yet.</div>
          ) : (
            submissions.slice(0, 6).map((submission) => (
              <Link
                key={submission.submissionId}
                href={`/judge/submissions/${submission.submissionId}/score`}
                className="block px-5 py-4 hover:bg-slate-50"
              >
                <p className="font-medium text-slate-800">{submission.team.teamName}</p>
                <p className="text-xs text-slate-400">{submission.round.roundName} / {submission.team.category}</p>
              </Link>
            ))
          )}
        </div>
      </div>
    </div>
  );
}
