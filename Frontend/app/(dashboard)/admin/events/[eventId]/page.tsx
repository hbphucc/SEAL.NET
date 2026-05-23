"use client";

import { FormEvent, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { ArrowLeft, Edit, Layers, ListChecks, Plus, Trash2, UserCheck, Play, XCircle, Lock, RefreshCw, CheckSquare, ArrowRight } from "lucide-react";
import ConfirmDialog from "@/components/shared/ConfirmDialog";
import DataTable from "@/components/shared/DataTable";
import PageHeader from "@/components/shared/PageHeader";
import { useAuth } from "@/contexts/AuthContext";
import {
  useCategories,
  useCreateCategory,
  useCreateCriteria,
  useCreateRound,
  useCriteria,
  useDeleteCategory,
  useDeleteCriteria,
  useDeleteRound,
  useEvent,
  useRounds,
  useUpdateCategory,
  useUpdateCriteria,
  useUpdateRound,
  useOpenRound,
  useCloseRound,
  useLockSubmissions,
  usePublishRoundResult,
  useReopenRound,
  useAdvanceRound,
} from "@/hooks/useEvents";
import { useCreateJudgeAssignment, useDeleteJudgeAssignment, useJudgeAssignments } from "@/hooks/useJudges";
import { useUsers } from "@/hooks/useUsers";
import { formatDate } from "@/lib/utils";
import { Category, CategoryPayload, Criteria, CriteriaPayload, Round, RoundPayload, RoundStatus } from "@/types/event";

type CategoryFormState = {
  categoryName: string;
  description: string;
};

type RoundFormState = {
  roundName: string;
  submissionDeadline: string;
  roundOrder: string;
  maxTeamsAdvancing: string;
};

type CriteriaFormState = {
  criteriaName: string;
  maxScore: string;
  weight: string;
};

const EMPTY_CATEGORY: CategoryFormState = { categoryName: "", description: "" };
const EMPTY_ROUND: RoundFormState = {
  roundName: "",
  submissionDeadline: "",
  roundOrder: "1",
  maxTeamsAdvancing: "1",
};
const EMPTY_CRITERIA: CriteriaFormState = { criteriaName: "", maxScore: "10", weight: "0" };

function toDateTimeLocal(value?: string | null) {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
  return local.toISOString().slice(0, 16);
}

function categoryPayload(form: CategoryFormState): CategoryPayload {
  return {
    categoryName: form.categoryName.trim(),
    description: form.description.trim() || undefined,
  };
}

function roundPayload(form: RoundFormState): RoundPayload {
  return {
    roundName: form.roundName.trim(),
    submissionDeadline: new Date(form.submissionDeadline).toISOString(),
    roundOrder: Number(form.roundOrder),
    maxTeamsAdvancing: Number(form.maxTeamsAdvancing),
  };
}

function criteriaPayload(form: CriteriaFormState): CriteriaPayload {
  return {
    criteriaName: form.criteriaName.trim(),
    maxScore: Number(form.maxScore),
    weight: Number(form.weight),
  };
}

export default function AdminEventSetupPage() {
  const params = useParams<{ eventId: string }>();
  const eventId = params.eventId;
  const { hasRole } = useAuth();
  const isAdmin = hasRole("Admin");

  const { data: event, isLoading: eventLoading } = useEvent(eventId);
  const { data: categories = [], isLoading: categoriesLoading } = useCategories(eventId);
  const { data: rounds = [], isLoading: roundsLoading } = useRounds(eventId);

  const createCategory = useCreateCategory(eventId);
  const updateCategory = useUpdateCategory(eventId);
  const deleteCategory = useDeleteCategory(eventId);
  const createRound = useCreateRound(eventId);
  const updateRound = useUpdateRound(eventId);
  const deleteRound = useDeleteRound(eventId);

  const openRound = useOpenRound(eventId);
  const closeRound = useCloseRound(eventId);
  const lockSubmissions = useLockSubmissions(eventId);
  const publishRound = usePublishRoundResult(eventId);
  const reopenRound = useReopenRound(eventId);
  const advanceRound = useAdvanceRound(eventId);

  const [categoryFormOpen, setCategoryFormOpen] = useState(false);
  const [editingCategory, setEditingCategory] = useState<Category | null>(null);
  const [categoryForm, setCategoryForm] = useState<CategoryFormState>(EMPTY_CATEGORY);
  const [categoryError, setCategoryError] = useState("");
  const [deleteCategoryTarget, setDeleteCategoryTarget] = useState<Category | null>(null);

  const [roundFormOpen, setRoundFormOpen] = useState(false);
  const [editingRound, setEditingRound] = useState<Round | null>(null);
  const [roundForm, setRoundForm] = useState<RoundFormState>(EMPTY_ROUND);
  const [roundError, setRoundError] = useState("");
  const [deleteRoundTarget, setDeleteRoundTarget] = useState<Round | null>(null);

  const [confirmRoundAction, setConfirmRoundAction] = useState<{
    round: Round;
    type: "close" | "publish" | "advance";
  } | null>(null);

  function getRoundStatusBadgeColor(status?: RoundStatus, isLocked?: boolean) {
    if (isLocked) return "bg-red-50 text-red-700 border-red-200";
    switch (status) {
      case "Draft":
        return "bg-slate-50 text-slate-700 border-slate-200";
      case "Open":
        return "bg-green-50 text-green-700 border-green-200";
      case "Closed":
        return "bg-amber-50 text-amber-700 border-amber-200";
      case "Locked":
        return "bg-red-50 text-red-700 border-red-200";
      case "ResultsPublished":
        return "bg-purple-50 text-purple-700 border-purple-200";
      default:
        return "bg-slate-50 text-slate-700 border-slate-200";
    }
  }

  if (!isAdmin) {
    return (
      <div className="flex items-center justify-center h-64">
        <p className="text-slate-500">You do not have permission to access this page.</p>
      </div>
    );
  }

  function openCreateCategory() {
    setEditingCategory(null);
    setCategoryForm(EMPTY_CATEGORY);
    setCategoryError("");
    setCategoryFormOpen(true);
  }

  function openEditCategory(category: Category) {
    setEditingCategory(category);
    setCategoryForm({
      categoryName: category.categoryName,
      description: category.description ?? "",
    });
    setCategoryError("");
    setCategoryFormOpen(true);
  }

  async function handleCategorySubmit(e: FormEvent) {
    e.preventDefault();
    setCategoryError("");
    if (!categoryForm.categoryName.trim()) {
      setCategoryError("Category name is required.");
      return;
    }

    const payload = categoryPayload(categoryForm);
    if (editingCategory) {
      await updateCategory.mutateAsync({ categoryId: editingCategory.categoryId, data: payload });
    } else {
      await createCategory.mutateAsync(payload);
    }
    setCategoryFormOpen(false);
  }

  function openCreateRound() {
    setEditingRound(null);
    setRoundForm({
      ...EMPTY_ROUND,
      roundOrder: String((rounds.at(-1)?.roundOrder ?? 0) + 1),
    });
    setRoundError("");
    setRoundFormOpen(true);
  }

  function openEditRound(round: Round) {
    setEditingRound(round);
    setRoundForm({
      roundName: round.roundName,
      submissionDeadline: toDateTimeLocal(round.submissionDeadline),
      roundOrder: String(round.roundOrder),
      maxTeamsAdvancing: String(round.maxTeamsAdvancing),
    });
    setRoundError("");
    setRoundFormOpen(true);
  }

  async function handleRoundSubmit(e: FormEvent) {
    e.preventDefault();
    setRoundError("");
    const maxTeamsAdvancing = Number(roundForm.maxTeamsAdvancing);
    const roundOrder = Number(roundForm.roundOrder);

    if (!roundForm.roundName.trim()) {
      setRoundError("Round name is required.");
      return;
    }
    if (!roundForm.submissionDeadline) {
      setRoundError("Submission deadline is required.");
      return;
    }
    if (!Number.isInteger(roundOrder) || roundOrder < 1) {
      setRoundError("Round order must be a positive whole number.");
      return;
    }
    if (!Number.isInteger(maxTeamsAdvancing) || maxTeamsAdvancing < 1 || maxTeamsAdvancing > 100) {
      setRoundError("Max teams advancing must be between 1 and 100.");
      return;
    }

    const payload = roundPayload(roundForm);
    if (editingRound) {
      await updateRound.mutateAsync({ roundId: editingRound.roundId, data: payload });
    } else {
      await createRound.mutateAsync(payload);
    }
    setRoundFormOpen(false);
  }

  const categoryColumns = [
    {
      key: "categoryName",
      label: "Category",
      sortable: true,
      render: (_: unknown, row: Category) => (
        <div>
          <p className="font-medium text-slate-800">{row.categoryName}</p>
          <p className="text-xs text-slate-400">{row.description || "No description"}</p>
        </div>
      ),
    },
    {
      key: "teamCount",
      label: "Teams",
      render: (_: unknown, row: Category) => <span className="text-sm text-slate-600">{row.teamCount ?? 0}</span>,
    },
  ];

  const roundColumns = [
    {
      key: "roundOrder",
      label: "Order",
      sortable: true,
      render: (_: unknown, row: Round) => <span className="font-mono text-sm text-slate-600">#{row.roundOrder}</span>,
    },
    {
      key: "roundName",
      label: "Round",
      sortable: true,
      render: (_: unknown, row: Round) => (
        <div>
          <p className="font-medium text-slate-800">{row.roundName}</p>
          <p className="text-xs text-slate-400">{formatDate(row.submissionDeadline)}</p>
        </div>
      ),
    },
    {
      key: "maxTeamsAdvancing",
      label: "Advancing",
      sortable: true,
      render: (_: unknown, row: Round) => <span className="text-sm text-slate-600">{row.maxTeamsAdvancing} teams</span>,
    },
    {
      key: "status",
      label: "Status",
      sortable: true,
      render: (_: unknown, row: Round) => {
        const statusVal = row.status ?? "Draft";
        return (
          <div className="flex flex-col gap-1 items-start">
            <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium border ${getRoundStatusBadgeColor(row.status, row.isSubmissionLocked)}`}>
              {statusVal}
            </span>
          </div>
        );
      },
    },
  ];

  return (
    <div className="space-y-8">
      <PageHeader
        title={eventLoading ? "Competition Setup" : event?.eventName ?? "Competition Setup"}
        description={event ? `${formatDate(event.startDate)} to ${formatDate(event.endDate)}` : "Configure categories, rounds, and criteria"}
        icon={Layers}
        actions={
          <Link
            href="/admin/events"
            className="inline-flex items-center gap-2 rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50"
          >
            <ArrowLeft className="h-4 w-4" />
            Events
          </Link>
        }
      />

      <section className="space-y-4">
        <div className="flex items-center justify-between gap-3">
          <div>
            <h2 className="text-base font-semibold text-slate-900">Categories</h2>
            <p className="text-sm text-slate-500">Manage competition tracks for this event.</p>
          </div>
          <button onClick={openCreateCategory} className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-3 py-2 text-sm font-medium text-white hover:bg-blue-700">
            <Plus className="h-4 w-4" /> Category
          </button>
        </div>
        <DataTable
          data={categories}
          columns={categoryColumns as never}
          searchKeys={["categoryName"] as never}
          searchPlaceholder="Search categories..."
          isLoading={categoriesLoading}
          emptyMessage="No categories configured"
          actions={(row: Category) => (
            <>
              <button onClick={() => openEditCategory(row)} className="flex items-center gap-1 rounded-lg bg-blue-50 px-2 py-1.5 text-xs font-medium text-blue-700 hover:bg-blue-100">
                <Edit className="h-3.5 w-3.5" /> Edit
              </button>
              <button onClick={() => setDeleteCategoryTarget(row)} className="flex items-center gap-1 rounded-lg bg-red-50 px-2 py-1.5 text-xs font-medium text-red-600 hover:bg-red-100">
                <Trash2 className="h-3.5 w-3.5" /> Delete
              </button>
            </>
          )}
        />
      </section>

      <section className="space-y-4">
        <div className="flex items-center justify-between gap-3">
          <div>
            <h2 className="text-base font-semibold text-slate-900">Rounds</h2>
            <p className="text-sm text-slate-500">Configure each competition stage and its scoring criteria.</p>
          </div>
          <button onClick={openCreateRound} className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-3 py-2 text-sm font-medium text-white hover:bg-blue-700">
            <Plus className="h-4 w-4" /> Round
          </button>
        </div>
        <DataTable
          data={[...rounds].sort((a, b) => a.roundOrder - b.roundOrder)}
          columns={roundColumns as never}
          searchKeys={["roundName"] as never}
          searchPlaceholder="Search rounds..."
          isLoading={roundsLoading}
          emptyMessage="No rounds configured"
          actions={(row: Round) => {
            const status = row.status ?? "Draft";
            const isMutationPending =
              openRound.isPending ||
              closeRound.isPending ||
              lockSubmissions.isPending ||
              reopenRound.isPending ||
              publishRound.isPending ||
              advanceRound.isPending;

            return (
              <>
                {status === "Draft" && (
                  <button
                    disabled={isMutationPending}
                    onClick={() => openRound.mutate(row.roundId)}
                    className="inline-flex items-center gap-1 rounded-lg bg-green-50 px-2 py-1.5 text-xs font-medium text-green-700 hover:bg-green-100 disabled:opacity-50 transition-all animate-fade-in"
                  >
                    <Play className="h-3 w-3" /> Open
                  </button>
                )}

                {status === "Open" && (
                  <button
                    disabled={isMutationPending}
                    onClick={() => setConfirmRoundAction({ round: row, type: "close" })}
                    className="inline-flex items-center gap-1 rounded-lg bg-orange-50 px-2 py-1.5 text-xs font-medium text-orange-700 hover:bg-orange-100 disabled:opacity-50 transition-all animate-fade-in"
                  >
                    <XCircle className="h-3 w-3" /> Close
                  </button>
                )}

                {(status === "Closed" || status === "Locked") && (
                  <button
                    disabled={isMutationPending}
                    onClick={() => reopenRound.mutate(row.roundId)}
                    className="inline-flex items-center gap-1 rounded-lg bg-slate-100 px-2 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-200 disabled:opacity-50 transition-all animate-fade-in"
                  >
                    <RefreshCw className="h-3 w-3" /> Reopen
                  </button>
                )}

                {status === "Closed" && !row.isSubmissionLocked && (
                  <button
                    disabled={isMutationPending}
                    onClick={() => lockSubmissions.mutate(row.roundId)}
                    className="inline-flex items-center gap-1 rounded-lg bg-amber-50 px-2 py-1.5 text-xs font-medium text-amber-700 hover:bg-amber-100 disabled:opacity-50 transition-all animate-fade-in"
                  >
                    <Lock className="h-3 w-3" /> Lock Submissions
                  </button>
                )}

                {(status === "Closed" || status === "Locked") && !row.isRankingPublished && (
                  <button
                    disabled={isMutationPending}
                    onClick={() => setConfirmRoundAction({ round: row, type: "publish" })}
                    className="inline-flex items-center gap-1 rounded-lg bg-purple-50 px-2 py-1.5 text-xs font-medium text-purple-700 hover:bg-purple-100 disabled:opacity-50 transition-all animate-fade-in"
                  >
                    <CheckSquare className="h-3 w-3" /> Publish Results
                  </button>
                )}

                {status === "ResultsPublished" && (
                  <button
                    disabled={isMutationPending}
                    onClick={() => setConfirmRoundAction({ round: row, type: "advance" })}
                    className="inline-flex items-center gap-1 rounded-lg bg-indigo-50 px-2 py-1.5 text-xs font-medium text-indigo-700 hover:bg-indigo-100 disabled:opacity-50 transition-all animate-fade-in"
                  >
                    <ArrowRight className="h-3 w-3" /> Advance Teams
                  </button>
                )}

                <button onClick={() => openEditRound(row)} className="flex items-center gap-1 rounded-lg bg-blue-50 px-2 py-1.5 text-xs font-medium text-blue-700 hover:bg-blue-100 transition-all">
                  <Edit className="h-3.5 w-3.5" /> Edit
                </button>
                <button onClick={() => setDeleteRoundTarget(row)} className="flex items-center gap-1 rounded-lg bg-red-50 px-2 py-1.5 text-xs font-medium text-red-600 hover:bg-red-100 transition-all">
                  <Trash2 className="h-3.5 w-3.5" /> Delete
                </button>
              </>
            );
          }}
        />
      </section>

      <section className="space-y-4">
        <div>
          <h2 className="text-base font-semibold text-slate-900">Criteria</h2>
          <p className="text-sm text-slate-500">Weights are validated by the backend and cannot exceed 100 per round.</p>
        </div>
        {rounds.length === 0 && !roundsLoading ? (
          <div className="rounded-xl border border-dashed border-slate-300 bg-white px-4 py-10 text-center text-sm text-slate-500">
            Create a round before adding scoring criteria.
          </div>
        ) : (
          <div className="grid gap-4 xl:grid-cols-2">
            {[...rounds].sort((a, b) => a.roundOrder - b.roundOrder).map((round) => (
              <CriteriaPanel key={round.roundId} round={round} />
            ))}
          </div>
        )}
      </section>

      <JudgeAssignmentsPanel categories={categories} rounds={rounds} />

      {categoryFormOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
          <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" onClick={() => setCategoryFormOpen(false)} />
          <form onSubmit={handleCategorySubmit} className="relative w-full max-w-lg rounded-xl bg-white p-6 shadow-2xl">
            <h2 className="text-lg font-semibold text-slate-900">{editingCategory ? "Edit Category" : "Create Category"}</h2>
            <div className="mt-5 space-y-4">
              <label className="block">
                <span className="mb-1.5 block text-sm font-medium text-slate-700">Category Name</span>
                <input value={categoryForm.categoryName} onChange={(e) => setCategoryForm((prev) => ({ ...prev, categoryName: e.target.value }))} className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </label>
              <label className="block">
                <span className="mb-1.5 block text-sm font-medium text-slate-700">Description</span>
                <textarea value={categoryForm.description} onChange={(e) => setCategoryForm((prev) => ({ ...prev, description: e.target.value }))} rows={3} className="w-full resize-none rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </label>
            </div>
            {categoryError && <p className="mt-4 text-sm text-red-600">{categoryError}</p>}
            <DialogActions onCancel={() => setCategoryFormOpen(false)} isSaving={createCategory.isPending || updateCategory.isPending} />
          </form>
        </div>
      )}

      {roundFormOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
          <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" onClick={() => setRoundFormOpen(false)} />
          <form onSubmit={handleRoundSubmit} className="relative w-full max-w-2xl rounded-xl bg-white p-6 shadow-2xl">
            <h2 className="text-lg font-semibold text-slate-900">{editingRound ? "Edit Round" : "Create Round"}</h2>
            <div className="mt-5 grid gap-4 sm:grid-cols-2">
              <label className="sm:col-span-2">
                <span className="mb-1.5 block text-sm font-medium text-slate-700">Round Name</span>
                <input value={roundForm.roundName} onChange={(e) => setRoundForm((prev) => ({ ...prev, roundName: e.target.value }))} className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </label>
              <label>
                <span className="mb-1.5 block text-sm font-medium text-slate-700">Submission Deadline</span>
                <input type="datetime-local" value={roundForm.submissionDeadline} onChange={(e) => setRoundForm((prev) => ({ ...prev, submissionDeadline: e.target.value }))} className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </label>
              <label>
                <span className="mb-1.5 block text-sm font-medium text-slate-700">Round Order</span>
                <input type="number" min="1" value={roundForm.roundOrder} onChange={(e) => setRoundForm((prev) => ({ ...prev, roundOrder: e.target.value }))} className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </label>
              <label className="sm:col-span-2">
                <span className="mb-1.5 block text-sm font-medium text-slate-700">Max Teams Advancing</span>
                <input type="number" min="1" max="100" value={roundForm.maxTeamsAdvancing} onChange={(e) => setRoundForm((prev) => ({ ...prev, maxTeamsAdvancing: e.target.value }))} className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </label>
            </div>
            {roundError && <p className="mt-4 text-sm text-red-600">{roundError}</p>}
            <DialogActions onCancel={() => setRoundFormOpen(false)} isSaving={createRound.isPending || updateRound.isPending} />
          </form>
        </div>
      )}

      <ConfirmDialog
        open={!!deleteCategoryTarget}
        onClose={() => setDeleteCategoryTarget(null)}
        onConfirm={async () => {
          await deleteCategory.mutateAsync(deleteCategoryTarget!.categoryId);
          setDeleteCategoryTarget(null);
        }}
        title="Delete Category"
        description={`Delete "${deleteCategoryTarget?.categoryName}"? Categories with teams or judge assignments cannot be deleted.`}
        confirmLabel="Delete"
        variant="danger"
        isLoading={deleteCategory.isPending}
      />

      <ConfirmDialog
        open={!!deleteRoundTarget}
        onClose={() => setDeleteRoundTarget(null)}
        onConfirm={async () => {
          await deleteRound.mutateAsync(deleteRoundTarget!.roundId);
          setDeleteRoundTarget(null);
        }}
        title="Delete Round"
        description={`Delete "${deleteRoundTarget?.roundName}"? Rounds with criteria, submissions, or judge assignments cannot be deleted.`}
        confirmLabel="Delete"
        variant="danger"
        isLoading={deleteRound.isPending}
      />

      <ConfirmDialog
        open={!!confirmRoundAction}
        onClose={() => setConfirmRoundAction(null)}
        onConfirm={async () => {
          if (!confirmRoundAction) return;
          const { round, type } = confirmRoundAction;
          if (type === "close") {
            await closeRound.mutateAsync(round.roundId);
          } else if (type === "publish") {
            await publishRound.mutateAsync(round.roundId);
          } else if (type === "advance") {
            await advanceRound.mutateAsync(round.roundId);
          }
          setConfirmRoundAction(null);
        }}
        title={
          confirmRoundAction?.type === "close"
            ? "Close Round"
            : confirmRoundAction?.type === "publish"
            ? "Publish Results"
            : "Advance Teams"
        }
        description={
          confirmRoundAction?.type === "close"
            ? `Are you sure you want to close "${confirmRoundAction?.round?.roundName}"? This will stop teams from creating submissions.`
            : confirmRoundAction?.type === "publish"
            ? `Are you sure you want to publish results for "${confirmRoundAction?.round?.roundName}"? This will freeze the scores and make the results public.`
            : `Are you sure you want to advance teams for "${confirmRoundAction?.round?.roundName}"? This will calculate qualifications and advance the top teams to the next round.`
        }
        confirmLabel={
          confirmRoundAction?.type === "close"
            ? "Close"
            : confirmRoundAction?.type === "publish"
            ? "Publish"
            : "Advance"
        }
        variant={confirmRoundAction?.type === "advance" ? "default" : "danger"}
        isLoading={
          closeRound.isPending || publishRound.isPending || advanceRound.isPending
        }
      />
    </div>
  );
}

function JudgeAssignmentsPanel({
  categories,
  rounds,
}: {
  categories: Category[];
  rounds: Round[];
}) {
  const { data: users = [], isLoading: usersLoading } = useUsers();
  const { data: assignments = [], isLoading: assignmentsLoading } = useJudgeAssignments();
  const createAssignment = useCreateJudgeAssignment();
  const deleteAssignment = useDeleteJudgeAssignment();
  const [judgeId, setJudgeId] = useState("");
  const [roundId, setRoundId] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [formError, setFormError] = useState("");
  const judges = users.filter((user) => user.roles.includes("Judge") && user.isApproved);
  const roundIds = new Set(rounds.map((round) => round.roundId));
  const categoryIds = new Set(categories.map((category) => category.categoryId));
  const eventAssignments = assignments.filter(
    (assignment) => roundIds.has(assignment.round.roundId) && categoryIds.has(assignment.category.categoryId)
  );

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setFormError("");
    if (!judgeId || !roundId || !categoryId) {
      setFormError("Choose a judge, round, and category.");
      return;
    }

    await createAssignment.mutateAsync({ judgeId, roundId, categoryId });
    setJudgeId("");
    setRoundId("");
    setCategoryId("");
  }

  return (
    <section className="space-y-4">
      <div>
        <h2 className="text-base font-semibold text-slate-900">Judge Assignments</h2>
        <p className="text-sm text-slate-500">Assign approved judges to this event&apos;s rounds and categories.</p>
      </div>

      <form onSubmit={handleCreate} className="grid gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm lg:grid-cols-[1fr_1fr_1fr_auto]">
        <select value={judgeId} onChange={(e) => setJudgeId(e.target.value)} className="rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
          <option value="">Choose judge</option>
          {judges.map((judge) => (
            <option key={judge.id} value={judge.id}>{judge.fullName} ({judge.email})</option>
          ))}
        </select>
        <select value={roundId} onChange={(e) => setRoundId(e.target.value)} className="rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
          <option value="">Choose round</option>
          {rounds.map((round) => (
            <option key={round.roundId} value={round.roundId}>{round.roundName}</option>
          ))}
        </select>
        <select value={categoryId} onChange={(e) => setCategoryId(e.target.value)} className="rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
          <option value="">Choose category</option>
          {categories.map((category) => (
            <option key={category.categoryId} value={category.categoryId}>{category.categoryName}</option>
          ))}
        </select>
        <button type="submit" disabled={createAssignment.isPending} className="inline-flex items-center justify-center gap-2 rounded-lg bg-blue-600 px-3 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50">
          <UserCheck className="h-4 w-4" />
          Assign
        </button>
        {formError && <p className="text-sm text-red-600 lg:col-span-4">{formError}</p>}
      </form>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b border-slate-200 bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">
                <th className="px-4 py-3">Judge</th>
                <th className="px-4 py-3">Round</th>
                <th className="px-4 py-3">Category</th>
                <th className="px-4 py-3">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {usersLoading || assignmentsLoading ? (
                <tr><td colSpan={4} className="px-4 py-8 text-center text-sm text-slate-400">Loading assignments...</td></tr>
              ) : eventAssignments.length === 0 ? (
                <tr><td colSpan={4} className="px-4 py-8 text-center text-sm text-slate-400">No judge assignments yet</td></tr>
              ) : (
                eventAssignments.map((assignment) => (
                  <tr key={assignment.assignmentId} className="hover:bg-slate-50">
                    <td className="px-4 py-3">
                      <p className="text-sm font-medium text-slate-800">{assignment.judge.fullName}</p>
                      <p className="text-xs text-slate-400">{assignment.judge.email}</p>
                    </td>
                    <td className="px-4 py-3 text-sm text-slate-600">{assignment.round.roundName}</td>
                    <td className="px-4 py-3 text-sm text-slate-600">{assignment.category.categoryName}</td>
                    <td className="px-4 py-3">
                      <button
                        onClick={() => deleteAssignment.mutateAsync(assignment.assignmentId)}
                        disabled={deleteAssignment.isPending}
                        className="rounded-lg bg-red-50 px-2 py-1.5 text-xs font-medium text-red-600 hover:bg-red-100 disabled:opacity-50"
                      >
                        Delete
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </section>
  );
}

function DialogActions({ onCancel, isSaving }: { onCancel: () => void; isSaving: boolean }) {
  return (
    <div className="mt-6 flex justify-end gap-3">
      <button type="button" onClick={onCancel} className="rounded-lg border border-slate-200 px-4 py-2 text-sm text-slate-700 hover:bg-slate-50">
        Cancel
      </button>
      <button type="submit" disabled={isSaving} className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50">
        {isSaving ? "Saving..." : "Save"}
      </button>
    </div>
  );
}

function CriteriaPanel({ round }: { round: Round }) {
  const { data: criteria = [], isLoading } = useCriteria(round.roundId);
  const createCriteria = useCreateCriteria(round.roundId);
  const updateCriteria = useUpdateCriteria(round.roundId);
  const deleteCriteria = useDeleteCriteria(round.roundId);
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<Criteria | null>(null);
  const [form, setForm] = useState<CriteriaFormState>(EMPTY_CRITERIA);
  const [formError, setFormError] = useState("");
  const [deleteTarget, setDeleteTarget] = useState<Criteria | null>(null);

  const totalWeight = criteria.reduce((sum, item) => sum + Number(item.weight), 0);

  function openCreate() {
    setEditing(null);
    setForm(EMPTY_CRITERIA);
    setFormError("");
    setFormOpen(true);
  }

  function openEdit(item: Criteria) {
    setEditing(item);
    setForm({
      criteriaName: item.criteriaName,
      maxScore: String(item.maxScore),
      weight: String(item.weight),
    });
    setFormError("");
    setFormOpen(true);
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setFormError("");
    const maxScore = Number(form.maxScore);
    const weight = Number(form.weight);
    if (!form.criteriaName.trim()) {
      setFormError("Criteria name is required.");
      return;
    }
    if (!Number.isFinite(maxScore) || maxScore < 1 || maxScore > 100) {
      setFormError("Max score must be between 1 and 100.");
      return;
    }
    if (!Number.isFinite(weight) || weight < 0 || weight > 100) {
      setFormError("Weight must be between 0 and 100.");
      return;
    }

    const payload = criteriaPayload(form);
    if (editing) {
      await updateCriteria.mutateAsync({ criteriaId: editing.criteriaId, data: payload });
    } else {
      await createCriteria.mutateAsync(payload);
    }
    setFormOpen(false);
  }

  return (
    <div className="rounded-xl border border-slate-200 bg-white shadow-sm">
      <div className="flex items-center justify-between gap-3 border-b border-slate-100 px-4 py-3">
        <div>
          <div className="flex items-center gap-2">
            <ListChecks className="h-4 w-4 text-blue-600" />
            <h3 className="font-semibold text-slate-900">{round.roundName}</h3>
          </div>
          <p className="mt-1 text-xs text-slate-500">Total weight: {totalWeight}/100</p>
        </div>
        <button onClick={openCreate} className="inline-flex items-center gap-1.5 rounded-lg bg-blue-50 px-2.5 py-1.5 text-xs font-medium text-blue-700 hover:bg-blue-100">
          <Plus className="h-3.5 w-3.5" /> Criteria
        </button>
      </div>
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead>
            <tr className="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">
              <th className="px-4 py-3">Name</th>
              <th className="px-4 py-3">Max</th>
              <th className="px-4 py-3">Weight</th>
              <th className="px-4 py-3">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {isLoading ? (
              <tr><td colSpan={4} className="px-4 py-8 text-center text-sm text-slate-400">Loading...</td></tr>
            ) : criteria.length === 0 ? (
              <tr><td colSpan={4} className="px-4 py-8 text-center text-sm text-slate-400">No criteria configured</td></tr>
            ) : (
              criteria.map((item) => (
                <tr key={item.criteriaId} className="hover:bg-slate-50">
                  <td className="px-4 py-3 text-sm font-medium text-slate-800">{item.criteriaName}</td>
                  <td className="px-4 py-3 text-sm text-slate-600">{item.maxScore}</td>
                  <td className="px-4 py-3 text-sm text-slate-600">{item.weight}</td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-2">
                      <button onClick={() => openEdit(item)} className="rounded-lg bg-blue-50 px-2 py-1 text-xs font-medium text-blue-700 hover:bg-blue-100">Edit</button>
                      <button onClick={() => setDeleteTarget(item)} className="rounded-lg bg-red-50 px-2 py-1 text-xs font-medium text-red-600 hover:bg-red-100">Delete</button>
                    </div>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {formOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
          <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" onClick={() => setFormOpen(false)} />
          <form onSubmit={handleSubmit} className="relative w-full max-w-lg rounded-xl bg-white p-6 shadow-2xl">
            <h2 className="text-lg font-semibold text-slate-900">{editing ? "Edit Criteria" : "Create Criteria"}</h2>
            <p className="mt-1 text-sm text-slate-500">{round.roundName}</p>
            <div className="mt-5 grid gap-4 sm:grid-cols-2">
              <label className="sm:col-span-2">
                <span className="mb-1.5 block text-sm font-medium text-slate-700">Criteria Name</span>
                <input value={form.criteriaName} onChange={(e) => setForm((prev) => ({ ...prev, criteriaName: e.target.value }))} className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </label>
              <label>
                <span className="mb-1.5 block text-sm font-medium text-slate-700">Max Score</span>
                <input type="number" min="1" max="100" step="0.01" value={form.maxScore} onChange={(e) => setForm((prev) => ({ ...prev, maxScore: e.target.value }))} className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </label>
              <label>
                <span className="mb-1.5 block text-sm font-medium text-slate-700">Weight</span>
                <input type="number" min="0" max="100" step="0.01" value={form.weight} onChange={(e) => setForm((prev) => ({ ...prev, weight: e.target.value }))} className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </label>
            </div>
            {formError && <p className="mt-4 text-sm text-red-600">{formError}</p>}
            <DialogActions onCancel={() => setFormOpen(false)} isSaving={createCriteria.isPending || updateCriteria.isPending} />
          </form>
        </div>
      )}

      <ConfirmDialog
        open={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        onConfirm={async () => {
          await deleteCriteria.mutateAsync(deleteTarget!.criteriaId);
          setDeleteTarget(null);
        }}
        title="Delete Criteria"
        description={`Delete "${deleteTarget?.criteriaName}"? Criteria with scores cannot be deleted.`}
        confirmLabel="Delete"
        variant="danger"
        isLoading={deleteCriteria.isPending}
      />
    </div>
  );
}
