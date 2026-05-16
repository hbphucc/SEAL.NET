"use client";

import { FormEvent, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { ArrowLeft, Edit, Layers, ListChecks, Plus, Trash2 } from "lucide-react";
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
} from "@/hooks/useEvents";
import { formatDate } from "@/lib/utils";
import { Category, CategoryPayload, Criteria, CriteriaPayload, Round, RoundPayload } from "@/types/event";

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

function toDateTimeLocal(value?: string) {
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
          actions={(row: Round) => (
            <>
              <button onClick={() => openEditRound(row)} className="flex items-center gap-1 rounded-lg bg-blue-50 px-2 py-1.5 text-xs font-medium text-blue-700 hover:bg-blue-100">
                <Edit className="h-3.5 w-3.5" /> Edit
              </button>
              <button onClick={() => setDeleteRoundTarget(row)} className="flex items-center gap-1 rounded-lg bg-red-50 px-2 py-1.5 text-xs font-medium text-red-600 hover:bg-red-100">
                <Trash2 className="h-3.5 w-3.5" /> Delete
              </button>
            </>
          )}
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
    </div>
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
