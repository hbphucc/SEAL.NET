"use client";

import Link from "next/link";
import { ExternalLink, FileText, Search } from "lucide-react";
import DataTable from "@/components/shared/DataTable";
import PageHeader from "@/components/shared/PageHeader";
import { useAuth } from "@/contexts/AuthContext";
import { useAssignedSubmissions } from "@/hooks/useScores";
import { JudgeSubmission } from "@/types/score";

export default function JudgeSubmissionsPage() {
  const { hasRole } = useAuth();
  const isJudge = hasRole("Judge");
  const { data: submissions = [], isLoading } = useAssignedSubmissions(isJudge);

  if (!isJudge) {
    return (
      <div className="flex h-64 items-center justify-center">
        <p className="text-slate-500">You do not have permission to access this page.</p>
      </div>
    );
  }

  const columns = [
    {
      key: "team",
      label: "Team",
      render: (_: unknown, row: JudgeSubmission) => (
        <div>
          <p className="font-medium text-slate-800">{row.team.teamName}</p>
          <p className="text-xs text-slate-400">{row.team.category}</p>
        </div>
      ),
    },
    {
      key: "round",
      label: "Round",
      render: (_: unknown, row: JudgeSubmission) => <span className="text-sm text-slate-600">{row.round.roundName}</span>,
    },
    {
      key: "links",
      label: "Resources",
      render: (_: unknown, row: JudgeSubmission) => (
        <div className="flex flex-wrap gap-2 text-xs">
          <ResourceLink href={row.repositoryUrl} label="Repo" />
          <ResourceLink href={row.demoUrl} label="Demo" />
          <ResourceLink href={row.slideUrl} label="Slides" />
        </div>
      ),
    },
  ];

  return (
    <div>
      <PageHeader
        title="Assigned Submissions"
        description="Score submissions assigned to your judging rounds and categories"
        icon={FileText}
      />

      <DataTable
        data={submissions}
        columns={columns as never}
        searchKeys={[]}
        searchPlaceholder="Search submissions..."
        isLoading={isLoading}
        emptyMessage="No assigned submissions found"
        filterComponent={<Search className="hidden" />}
        actions={(row: JudgeSubmission) => (
          <Link
            href={`/judge/submissions/${row.submissionId}/score`}
            className="rounded-lg bg-blue-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-blue-700"
          >
            Score
          </Link>
        )}
      />
    </div>
  );
}

function ResourceLink({ href, label }: { href?: string | null; label: string }) {
  if (!href) return null;
  return (
    <a href={href} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1 text-blue-600 hover:underline">
      {label}
      <ExternalLink className="h-3 w-3" />
    </a>
  );
}
