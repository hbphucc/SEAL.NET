"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { Trophy } from "lucide-react";
import RankingTable from "@/components/shared/RankingTable";
import { useEvents } from "@/hooks/useEvents";
import { usePublicCategoryRoundRanking, usePublicRoundRanking } from "@/hooks/useRanking";
import { Category } from "@/types/event";

export default function PublicLeaderboardPage() {
  const { data: events = [], isLoading: eventsLoading } = useEvents();
  const [eventId, setEventId] = useState("");
  const [roundId, setRoundId] = useState("");
  const [categoryId, setCategoryId] = useState("all");

  const selectedEvent = events.find((event) => event.eventId === eventId);
  const rounds = selectedEvent?.rounds ?? [];
  const categories = selectedEvent?.categories ?? [];

  const effectiveRoundId = roundId || rounds[0]?.roundId || "";
  const roundRanking = usePublicRoundRanking(categoryId === "all" ? effectiveRoundId : "");
  const categoryRanking = usePublicCategoryRoundRanking(
    categoryId === "all" ? "" : categoryId,
    effectiveRoundId
  );

  const rankingQuery = categoryId === "all" ? roundRanking : categoryRanking;
  const rows = rankingQuery.data ?? [];
  const selectedRound = rounds.find((round) => round.roundId === effectiveRoundId);

  const statusMessage = useMemo(() => {
    const status = (rankingQuery.error as { response?: { status?: number; data?: { message?: string } } } | null)?.response;
    if (status?.status === 403) return status.data?.message ?? "Ranking is not published for this round.";
    return null;
  }, [rankingQuery.error]);

  return (
    <main className="min-h-screen bg-slate-50 px-4 py-8">
      <div className="mx-auto max-w-6xl space-y-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-blue-600">
              <Trophy className="h-5 w-5 text-white" />
            </div>
            <div>
              <h1 className="text-xl font-bold text-slate-900">Public Leaderboard</h1>
              <p className="text-sm text-slate-500">Published rankings by round and category</p>
            </div>
          </div>
          <Link href="/login" className="text-sm font-medium text-blue-600 hover:underline">Sign in</Link>
        </div>

        <div className="grid gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm md:grid-cols-3">
          <select
            value={eventId}
            onChange={(e) => {
              setEventId(e.target.value);
              setRoundId("");
              setCategoryId("all");
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

        {selectedRound && (
          <div className="rounded-xl border border-blue-100 bg-blue-50 px-4 py-3 text-sm text-blue-800">
            Showing {selectedRound.roundName}
          </div>
        )}

        {statusMessage ? (
          <div className="rounded-xl border border-yellow-200 bg-yellow-50 px-4 py-12 text-center text-sm text-yellow-800">
            {statusMessage}
          </div>
        ) : (
          <RankingTable rows={rows} isLoading={eventsLoading || rankingQuery.isLoading} />
        )}
      </div>
    </main>
  );
}
