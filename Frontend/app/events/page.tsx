"use client";

import Link from "next/link";
import { ArrowRight, CalendarDays, Clock, Layers, LogIn, Trophy, UserPlus, Users } from "lucide-react";
import { usePublicEvents } from "@/hooks/useEvents";
import { Event } from "@/types/event";
import { formatDate } from "@/lib/utils";

function getEventTiming(event: Event) {
  const now = Date.now();
  const start = new Date(event.startDate).getTime();
  const end = new Date(event.endDate).getTime();

  if (Number.isNaN(start) || Number.isNaN(end)) {
    return {
      state: event.status,
      badgeClass: "border-slate-200 bg-slate-50 text-slate-700",
      eyebrow: "Schedule unavailable",
      countdown: "Date to be announced",
    };
  }

  if (now < start) {
    const minutes = Math.max(1, Math.round((start - now) / 60000));
    const hours = Math.round(minutes / 60);
    const days = Math.round(hours / 24);
    const countdown =
      minutes < 60
        ? `Starts in ${minutes} min`
        : hours < 48
          ? `Starts in ${hours} hour${hours === 1 ? "" : "s"}`
          : `Starts in ${days} day${days === 1 ? "" : "s"}`;

    return {
      state: "Upcoming",
      badgeClass: "border-blue-200 bg-blue-50 text-blue-700",
      eyebrow: `Starts ${formatDate(event.startDate)}`,
      countdown,
    };
  }

  if (now <= end) {
    return {
      state: "Live now",
      badgeClass: "border-emerald-200 bg-emerald-50 text-emerald-700",
      eyebrow: `Ends ${formatDate(event.endDate)}`,
      countdown: "Happening now",
    };
  }

  return {
    state: "Ended",
    badgeClass: "border-slate-200 bg-slate-50 text-slate-600",
    eyebrow: `Ended ${formatDate(event.endDate)}`,
    countdown: "Event ended",
  };
}

function getTeamCount(event: Event) {
  return event.totalTeams ?? event.categories?.reduce((sum, category) => sum + (category.teamCount ?? 0), 0) ?? 0;
}

export default function PublicEventsPage() {
  const { data: events = [], isLoading, isError } = usePublicEvents();

  const sortedEvents = [...events].sort(
    (a, b) => new Date(a.startDate).getTime() - new Date(b.startDate).getTime()
  );

  return (
    <main className="min-h-screen bg-slate-50 text-slate-900">
      <header className="border-b border-slate-200 bg-white/95 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between gap-3 px-4 py-4">
          <Link href="/events" className="flex items-center gap-3">
            <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-blue-600">
              <Trophy className="h-4 w-4 text-white" />
            </div>
            <span className="font-bold text-slate-900">SEAL.NET</span>
          </Link>
          <div className="flex items-center gap-4 text-sm">
            <Link href="/leaderboard" className="hidden font-medium text-slate-600 hover:text-blue-700 sm:inline">
              Leaderboard
            </Link>
            <Link href="/login" className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-3 py-2 font-medium text-white hover:bg-blue-700">
              <LogIn className="h-4 w-4" />
              Sign in
            </Link>
          </div>
        </div>
      </header>

      <section className="border-b border-slate-200 bg-white">
        <div className="mx-auto grid max-w-6xl gap-6 px-4 py-8 lg:grid-cols-[1fr_auto] lg:items-end">
          <div className="max-w-3xl">
            <div className="mb-3 inline-flex items-center gap-2 rounded-full border border-blue-100 bg-blue-50 px-3 py-1 text-xs font-medium text-blue-700">
              <CalendarDays className="h-3.5 w-3.5" />
              Public event board
            </div>
            <h1 className="text-3xl font-bold tracking-tight text-slate-950 sm:text-4xl">
              See what is happening at SEAL
            </h1>
            <p className="mt-3 text-base leading-7 text-slate-600">
              Explore published competitions, schedules, and team activity. Create an account when you are ready to register.
            </p>
          </div>
          <div className="flex flex-col gap-3 sm:flex-row lg:flex-col">
            <Link href="/register" className="inline-flex items-center justify-center gap-2 rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-blue-700">
              <UserPlus className="h-4 w-4" />
              Sign Up to Register
            </Link>
            <Link href="/login" className="inline-flex items-center justify-center gap-2 rounded-lg border border-slate-200 bg-white px-4 py-2.5 text-sm font-semibold text-slate-700 hover:bg-slate-50">
              Login
              <ArrowRight className="h-4 w-4" />
            </Link>
          </div>
        </div>
      </section>

      <section className="mx-auto max-w-6xl px-4 py-8">
        <div className="mb-6 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <h2 className="text-lg font-semibold text-slate-900">Published Events</h2>
          <div className="inline-flex w-fit items-center gap-2 rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-600">
            <Trophy className="h-4 w-4 text-blue-600" />
            {sortedEvents.length} event{sortedEvents.length === 1 ? "" : "s"} available
          </div>
        </div>

        {isLoading ? (
          <div className="grid gap-4 md:grid-cols-2">
            {Array.from({ length: 4 }).map((_, index) => (
              <div key={index} className="h-52 animate-pulse rounded-xl border border-slate-200 bg-white" />
            ))}
          </div>
        ) : isError ? (
          <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-12 text-center text-sm text-red-700">
            Could not load public events. Please try again later.
          </div>
        ) : sortedEvents.length === 0 ? (
          <div className="rounded-xl border border-slate-200 bg-white px-4 py-16 text-center">
            <CalendarDays className="mx-auto mb-3 h-10 w-10 text-slate-300" />
            <h2 className="font-semibold text-slate-800">No published events yet</h2>
            <p className="mt-1 text-sm text-slate-500">Events will appear here after an admin publishes them.</p>
          </div>
        ) : (
          <div className="grid gap-4 md:grid-cols-2">
            {sortedEvents.map((event) => {
              const timing = getEventTiming(event);
              const teamCount = getTeamCount(event);
              return (
                <article
                  key={event.eventId}
                  className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm transition-all hover:-translate-y-0.5 hover:shadow-md"
                >
                  <div className="border-b border-slate-100 p-5">
                    <div className="mb-4 flex items-start justify-between gap-3">
                      <div>
                        <h3 className="text-xl font-bold text-slate-950">{event.eventName}</h3>
                        <p className="mt-1 line-clamp-2 text-sm leading-6 text-slate-500">
                          {event.description || "No description provided."}
                        </p>
                      </div>
                      <span className={`shrink-0 rounded-full border px-2.5 py-1 text-xs font-medium ${timing.badgeClass}`}>
                        {timing.state}
                      </span>
                    </div>

                    <div className="rounded-lg border border-blue-100 bg-blue-50 p-4">
                      <div className="flex items-center gap-2 text-sm font-semibold text-blue-900">
                        <Clock className="h-4 w-4 text-blue-600" />
                        {timing.countdown}
                      </div>
                      <p className="mt-1 text-sm text-blue-700">{timing.eyebrow}</p>
                    </div>
                  </div>

                  <div className="space-y-4 p-5">
                    <div className="flex items-center gap-3 text-sm text-slate-700">
                      <CalendarDays className="h-4 w-4 text-slate-400" />
                      <span>{formatDate(event.startDate)} to {formatDate(event.endDate)}</span>
                    </div>

                    <div className="grid grid-cols-3 gap-3 text-sm">
                      <div className="rounded-lg border border-slate-100 bg-slate-50 px-3 py-2">
                        <div className="flex items-center gap-2 text-slate-500">
                          <Trophy className="h-4 w-4" />
                          Teams
                        </div>
                        <p className="mt-1 font-semibold text-slate-900">
                          {teamCount} Joined
                        </p>
                      </div>
                      <div className="rounded-lg border border-slate-100 bg-slate-50 px-3 py-2">
                        <div className="flex items-center gap-2 text-slate-500">
                          <Users className="h-4 w-4" />
                          Categories
                        </div>
                        <p className="mt-1 font-semibold text-slate-900">{event.categories?.length ?? 0}</p>
                      </div>
                      <div className="rounded-lg border border-slate-100 bg-slate-50 px-3 py-2">
                        <div className="flex items-center gap-2 text-slate-500">
                          <Layers className="h-4 w-4" />
                          Rounds
                        </div>
                        <p className="mt-1 font-semibold text-slate-900">{event.rounds?.length ?? 0}</p>
                      </div>
                    </div>

                    <Link
                      href="/register"
                      className="inline-flex w-full items-center justify-center gap-2 rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white hover:bg-blue-700"
                    >
                      Login / Sign Up to Register
                      <ArrowRight className="h-4 w-4" />
                    </Link>
                  </div>
                </article>
              );
            })}
          </div>
        )}
      </section>
    </main>
  );
}
