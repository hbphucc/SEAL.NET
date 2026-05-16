"use client";

import { useState } from "react";
import { AxiosError } from "axios";
import { AlertTriangle, Play, Trophy } from "lucide-react";
import ConfirmDialog from "@/components/shared/ConfirmDialog";
import PageHeader from "@/components/shared/PageHeader";
import RankingTable from "@/components/shared/RankingTable";
import { useAuth } from "@/contexts/AuthContext";
import { useEvents } from "@/hooks/useEvents";
import { useJudgeAssignments } from "@/hooks/useJudges";
import { useAdvanceRound, useAdminCategoryRoundRanking, useAdminRoundRanking } from "@/hooks/useRanking";
import { useAdminTeams } from "@/hooks/useTeams";
import { useRoundSubmissions } from "@/hooks/useSubmissions";
import { Category } from "@/types/event";
import { AdvanceRoundError, AdvanceRoundResponse } from "@/types/ranking";

export default function AdminRankingPage() {
  const { hasRole } = useAuth();
  const isAdmin = hasRole("Admin");
  const { data: events = [], isLoading: eventsLoading } = useEvents();
  const { data: teams = [] } = useAdminTeams(isAdmin);
  const { data: assignments = [] } = useJudgeAssignments(isAdmin);
  const advanceMutation = useAdvanceRound();

  const [eventId, setEventId] = useState("");
  const [roundId, setRoundId] = useState("");
  const [categoryId, setCategoryId] = useState("all");
  const [confirmAdvance, setConfirmAdvance] = useState(false);
  const [advanceResult, setAdvanceResult] = useState<AdvanceRoundResponse | null>(null);
  const [advanceError, setAdvanceError] = useState<AdvanceRoundError | null>(null);

  const selectedEvent = events.find((event) => event.eventId === eventId);
  const rounds = selectedEvent?.rounds ?? [];
  const categories = selectedEvent?.categories ?? [];
  const effectiveRoundId = roundId || rounds[0]?.roundId || "";
  const selectedRound = rounds.find((round) => round.roundId === effectiveRoundId);

  const roundRanking = useAdminRoundRanking(categoryId === "all" ? effectiveRoundId : "");
  const categoryRanking = useAdminCategoryRoundRanking(
    categoryId === "all" ? "" : categoryId,
    effectiveRoundId
  );
  const rankingQuery = categoryId === "all" ? roundRanking : categoryRanking;
  const { data: submissions = [] } = useRoundSubmissions(effectiveRoundId, isAdmin);

  const activeTeams = effectiveRoundId && selectedEvent
    ? teams.filter(
      (team) =>
        team.status === "Approved" &&
        team.currentRound?.roundId === effectiveRoundId &&
        selectedEvent.categories?.some((category) => category.categoryId === team.category?.categoryId)
    )
    : [];
  const submissionTeamIds = new Set(submissions.map((submission) => submission.team?.teamId));
  const missingSubmissions = activeTeams.filter((team) => !submissionTeamIds.has(team.teamId));
  const roundAssignments = assignments.filter((assignment) => assignment.round.roundId === effectiveRoundId);
  const assignedCategoryIds = new Set(roundAssignments.map((assignment) => assignment.category.categoryId));
  const activeCategoryIds = new Set(activeTeams.map((team) => team.category.categoryId));
  const categoriesWithoutJudges = [...activeCategoryIds].filter((id) => !assignedCategoryIds.has(id));
  const assignedJudgeCount = new Set(roundAssignments.map((assignment) => assignment.judge.judgeId)).size;

  if (!isAdmin) {
    return (
      <div className="flex items-center justify-center h-64">
        <p className="text-slate-500">You do not have permission to access this page.</p>
      </div>
    );
  }

  async function handleAdvance() {
    setAdvanceResult(null);
    setAdvanceError(null);
    try {
      const result = await advanceMutation.mutateAsync(effectiveRoundId);
      setAdvanceResult(result);
      setConfirmAdvance(false);
    } catch (err) {
      const data = (err as AxiosError<AdvanceRoundError>).response?.data;
      setAdvanceError(data ?? { message: "Round advancement failed." });
      setConfirmAdvance(false);
    }
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Ranking Preview"
        description="Review scores, diagnostics, and advance teams to the next round"
        icon={Trophy}
        actions={
          <button
            onClick={() => setConfirmAdvance(true)}
            disabled={!effectiveRoundId || advanceMutation.isPending}
            className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-3 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
          >
            <Play className="h-4 w-4" />
            Advance Round
          </button>
        }
      />

      <div className="grid gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm md:grid-cols-3">
        <select
          value={eventId}
          onChange={(e) => {
            setEventId(e.target.value);
            setRoundId("");
            setCategoryId("all");
            setAdvanceError(null);
            setAdvanceResult(null);
          }}
          className="rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          <option value="">Choose event</option>
          {events.map((event) => (
            <option key={event.eventId} value={event.eventId}>{event.eventName}</option>
          ))}
        </select>
        <select
          value={effectiveRoundId}
          onChange={(e) => setRoundId(e.target.value)}
          disabled={!selectedEvent}
          className="rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-slate-50"
        >
          {rounds.length === 0 && <option value="">No rounds</option>}
          {rounds.map((round) => (
            <option key={round.roundId} value={round.roundId}>{round.roundName}</option>
          ))}
        </select>
        <select
          value={categoryId}
          onChange={(e) => setCategoryId(e.target.value)}
          disabled={!selectedEvent}
          className="rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-slate-50"
        >
          <option value="all">All categories</option>
          {categories.map((category: Category) => (
            <option key={category.categoryId} value={category.categoryId}>{category.categoryName}</option>
          ))}
        </select>
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <DiagnosticCard label="Missing Submissions" value={missingSubmissions.length} tone={missingSubmissions.length ? "warning" : "ok"} />
        <DiagnosticCard label="Categories Without Judges" value={categoriesWithoutJudges.length} tone={categoriesWithoutJudges.length ? "warning" : "ok"} />
        <DiagnosticCard label="Assigned Judges" value={assignedJudgeCount} tone={assignedJudgeCount ? "ok" : "warning"} />
      </div>

      {(missingSubmissions.length > 0 || categoriesWithoutJudges.length > 0) && (
        <div className="rounded-xl border border-yellow-200 bg-yellow-50 p-4 text-sm text-yellow-900">
          <div className="mb-2 flex items-center gap-2 font-semibold">
            <AlertTriangle className="h-4 w-4" />
            Pre-advance diagnostics
          </div>
          {missingSubmissions.length > 0 && (
            <p>Missing submissions: {missingSubmissions.map((team) => team.teamName).join(", ")}</p>
          )}
          {categoriesWithoutJudges.length > 0 && (
            <p>Categories without judges: {categoriesWithoutJudges.join(", ")}</p>
          )}
        </div>
      )}

      {advanceError && <AdvanceDiagnostics title={advanceError.message} details={advanceError.details} tone="error" />}
      {advanceResult && <AdvanceDiagnostics title={advanceResult.message} details={advanceResult.details} tone="success" />}

      <RankingTable
        rows={rankingQuery.data ?? []}
        isLoading={eventsLoading || rankingQuery.isLoading}
        emptyMessage={selectedRound ? "No submissions have been scored for this round yet" : "Choose a round to preview ranking"}
      />

      <ConfirmDialog
        open={confirmAdvance}
        onClose={() => setConfirmAdvance(false)}
        onConfirm={handleAdvance}
        title="Advance Round"
        description={`Advance "${selectedRound?.roundName ?? "this round"}"? This publishes ranking and moves or eliminates teams.`}
        confirmLabel="Advance"
        variant="warning"
        isLoading={advanceMutation.isPending}
      />
    </div>
  );
}

function DiagnosticCard({ label, value, tone }: { label: string; value: number; tone: "ok" | "warning" }) {
  return (
    <div className={`rounded-xl border p-5 shadow-sm ${tone === "ok" ? "border-green-200 bg-green-50" : "border-yellow-200 bg-yellow-50"}`}>
      <p className={`text-xs font-medium uppercase tracking-wide ${tone === "ok" ? "text-green-700" : "text-yellow-700"}`}>{label}</p>
      <p className="mt-2 text-2xl font-bold text-slate-900">{value}</p>
    </div>
  );
}

function AdvanceDiagnostics({
  title,
  details,
  tone,
}: {
  title: string;
  details?: AdvanceRoundError["details"];
  tone: "success" | "error";
}) {
  return (
    <div className={`rounded-xl border p-4 text-sm ${tone === "success" ? "border-green-200 bg-green-50 text-green-900" : "border-red-200 bg-red-50 text-red-800"}`}>
      <p className="font-semibold">{title}</p>
      {details?.missingScoreCount !== undefined && (
        <p className="mt-2">
          Missing scores: {details.missingScoreCount} of {details.expectedScoreCount} expected.
        </p>
      )}
      {details?.missingScores && details.missingScores.length > 0 && (
        <div className="mt-3 max-h-48 overflow-auto rounded-lg bg-white/70 p-3">
          {details.missingScores.map((item) => (
            <p key={`${item.submissionId}-${item.judgeId}-${item.criteriaId}`} className="text-xs">
              {item.teamName} - judge {item.judgeId} - criteria {item.criteriaId}
            </p>
          ))}
        </div>
      )}
      {details?.teams && details.teams.length > 0 && (
        <p className="mt-2">Teams: {details.teams.map((team) => team.teamName).join(", ")}</p>
      )}
      {details?.advancedTeams && <p className="mt-2">Advanced: {details.advancedTeams.map((team) => team.teamName).join(", ") || "None"}</p>}
      {details?.eliminatedTeams && <p className="mt-1">Eliminated: {details.eliminatedTeams.map((team) => team.teamName).join(", ") || "None"}</p>}
    </div>
  );
}
