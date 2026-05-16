"use client";

import { FormEvent, useMemo, useState } from "react";
import Link from "next/link";
import { CalendarDays, Edit, Plus, Settings, Trash2 } from "lucide-react";
import ConfirmDialog from "@/components/shared/ConfirmDialog";
import DataTable from "@/components/shared/DataTable";
import PageHeader from "@/components/shared/PageHeader";
import { useAuth } from "@/contexts/AuthContext";
import { useCreateEvent, useDeleteEvent, useEvents, useUpdateEvent } from "@/hooks/useEvents";
import { formatDate } from "@/lib/utils";
import { Event, EventPayload, EventStatus } from "@/types/event";

const EVENT_STATUSES: EventStatus[] = ["Upcoming", "Ongoing", "Completed", "Cancelled"];

type EventFormState = {
  eventName: string;
  description: string;
  startDate: string;
  endDate: string;
  status: EventStatus;
};

const EMPTY_FORM: EventFormState = {
  eventName: "",
  description: "",
  startDate: "",
  endDate: "",
  status: "Upcoming",
};

function toDateTimeLocal(value?: string) {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
  return local.toISOString().slice(0, 16);
}

function toPayload(form: EventFormState): EventPayload {
  return {
    eventName: form.eventName.trim(),
    description: form.description.trim() || undefined,
    startDate: new Date(form.startDate).toISOString(),
    endDate: new Date(form.endDate).toISOString(),
    status: form.status,
  };
}

export default function AdminEventsPage() {
  const { hasRole } = useAuth();
  const isAdmin = hasRole("Admin");
  const { data: events = [], isLoading } = useEvents();
  const createMutation = useCreateEvent();
  const updateMutation = useUpdateEvent();
  const deleteMutation = useDeleteEvent();

  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<Event | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<Event | null>(null);
  const [form, setForm] = useState<EventFormState>(EMPTY_FORM);
  const [formError, setFormError] = useState("");

  const sortedEvents = useMemo(
    () => [...events].sort((a, b) => new Date(b.startDate).getTime() - new Date(a.startDate).getTime()),
    [events]
  );

  if (!isAdmin) {
    return (
      <div className="flex items-center justify-center h-64">
        <p className="text-slate-500">You do not have permission to access this page.</p>
      </div>
    );
  }

  function openCreate() {
    setEditing(null);
    setForm(EMPTY_FORM);
    setFormError("");
    setFormOpen(true);
  }

  function openEdit(event: Event) {
    setEditing(event);
    setForm({
      eventName: event.eventName,
      description: event.description ?? "",
      startDate: toDateTimeLocal(event.startDate),
      endDate: toDateTimeLocal(event.endDate),
      status: event.status,
    });
    setFormError("");
    setFormOpen(true);
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setFormError("");

    if (!form.eventName.trim()) {
      setFormError("Event name is required.");
      return;
    }

    if (!form.startDate || !form.endDate) {
      setFormError("Start date and end date are required.");
      return;
    }

    if (new Date(form.endDate) <= new Date(form.startDate)) {
      setFormError("End date must be after start date.");
      return;
    }

    const payload = toPayload(form);
    if (editing) {
      await updateMutation.mutateAsync({ eventId: editing.eventId, data: payload });
    } else {
      await createMutation.mutateAsync(payload);
    }

    setFormOpen(false);
  }

  const columns = [
    {
      key: "eventName",
      label: "Event",
      sortable: true,
      render: (_: unknown, row: Event) => (
        <div>
          <Link href={`/admin/events/${row.eventId}`} className="font-medium text-blue-600 hover:underline">
            {row.eventName}
          </Link>
          <p className="text-xs text-slate-400 max-w-md truncate">{row.description || "No description"}</p>
        </div>
      ),
    },
    {
      key: "status",
      label: "Status",
      sortable: true,
      render: (_: unknown, row: Event) => (
        <span className="inline-flex rounded-full border border-blue-200 bg-blue-50 px-2 py-0.5 text-xs font-medium text-blue-700">
          {row.status}
        </span>
      ),
    },
    {
      key: "startDate",
      label: "Dates",
      sortable: true,
      render: (_: unknown, row: Event) => (
        <span className="text-xs text-slate-500">
          {formatDate(row.startDate)} to {formatDate(row.endDate)}
        </span>
      ),
    },
    {
      key: "categories",
      label: "Setup",
      render: (_: unknown, row: Event) => (
        <span className="text-sm text-slate-600">
          {row.categories?.length ?? 0} categories / {row.rounds?.length ?? 0} rounds
        </span>
      ),
    },
  ];

  return (
    <div>
      <PageHeader
        title="Competition Events"
        description="Create events and configure the competition structure"
        icon={CalendarDays}
        actions={
          <button
            onClick={openCreate}
            className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-3 py-2 text-sm font-medium text-white hover:bg-blue-700"
          >
            <Plus className="h-4 w-4" />
            New Event
          </button>
        }
      />

      <DataTable
        data={sortedEvents}
        columns={columns as never}
        searchKeys={["eventName", "status"] as never}
        searchPlaceholder="Search events..."
        isLoading={isLoading}
        emptyMessage="No events found"
        actions={(row: Event) => (
          <>
            <Link
              href={`/admin/events/${row.eventId}`}
              className="flex items-center gap-1 rounded-lg bg-slate-50 px-2 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-100"
            >
              <Settings className="h-3.5 w-3.5" /> Setup
            </Link>
            <button
              onClick={() => openEdit(row)}
              className="flex items-center gap-1 rounded-lg bg-blue-50 px-2 py-1.5 text-xs font-medium text-blue-700 hover:bg-blue-100"
            >
              <Edit className="h-3.5 w-3.5" /> Edit
            </button>
            <button
              onClick={() => setDeleteTarget(row)}
              className="flex items-center gap-1 rounded-lg bg-red-50 px-2 py-1.5 text-xs font-medium text-red-600 hover:bg-red-100"
            >
              <Trash2 className="h-3.5 w-3.5" /> Delete
            </button>
          </>
        )}
      />

      {formOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
          <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" onClick={() => setFormOpen(false)} />
          <form onSubmit={handleSubmit} className="relative w-full max-w-2xl rounded-xl bg-white p-6 shadow-2xl">
            <h2 className="text-lg font-semibold text-slate-900">{editing ? "Edit Event" : "Create Event"}</h2>
            <div className="mt-5 grid gap-4 sm:grid-cols-2">
              <label className="sm:col-span-2">
                <span className="mb-1.5 block text-sm font-medium text-slate-700">Event Name</span>
                <input
                  value={form.eventName}
                  onChange={(e) => setForm((prev) => ({ ...prev, eventName: e.target.value }))}
                  className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </label>
              <label className="sm:col-span-2">
                <span className="mb-1.5 block text-sm font-medium text-slate-700">Description</span>
                <textarea
                  value={form.description}
                  onChange={(e) => setForm((prev) => ({ ...prev, description: e.target.value }))}
                  rows={3}
                  className="w-full resize-none rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </label>
              <label>
                <span className="mb-1.5 block text-sm font-medium text-slate-700">Start Date</span>
                <input
                  type="datetime-local"
                  value={form.startDate}
                  onChange={(e) => setForm((prev) => ({ ...prev, startDate: e.target.value }))}
                  className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </label>
              <label>
                <span className="mb-1.5 block text-sm font-medium text-slate-700">End Date</span>
                <input
                  type="datetime-local"
                  value={form.endDate}
                  onChange={(e) => setForm((prev) => ({ ...prev, endDate: e.target.value }))}
                  className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </label>
              <label className="sm:col-span-2">
                <span className="mb-1.5 block text-sm font-medium text-slate-700">Status</span>
                <select
                  value={form.status}
                  onChange={(e) => setForm((prev) => ({ ...prev, status: e.target.value as EventStatus }))}
                  className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  {EVENT_STATUSES.map((status) => (
                    <option key={status} value={status}>{status}</option>
                  ))}
                </select>
              </label>
            </div>
            {formError && <p className="mt-4 text-sm text-red-600">{formError}</p>}
            <div className="mt-6 flex justify-end gap-3">
              <button type="button" onClick={() => setFormOpen(false)} className="rounded-lg border border-slate-200 px-4 py-2 text-sm text-slate-700 hover:bg-slate-50">
                Cancel
              </button>
              <button
                type="submit"
                disabled={createMutation.isPending || updateMutation.isPending}
                className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
              >
                {createMutation.isPending || updateMutation.isPending ? "Saving..." : "Save Event"}
              </button>
            </div>
          </form>
        </div>
      )}

      <ConfirmDialog
        open={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        onConfirm={async () => {
          await deleteMutation.mutateAsync(deleteTarget!.eventId);
          setDeleteTarget(null);
        }}
        title="Delete Event"
        description={`Delete "${deleteTarget?.eventName}"? Events with categories or rounds cannot be deleted.`}
        confirmLabel="Delete"
        variant="danger"
        isLoading={deleteMutation.isPending}
      />
    </div>
  );
}
