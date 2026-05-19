import { Medal } from "lucide-react";
import { formatDate } from "@/lib/utils";
import { RankingRow } from "@/types/ranking";

export default function RankingTable({
  rows,
  isLoading,
  emptyMessage = "No ranking rows found",
}: {
  rows: RankingRow[];
  isLoading?: boolean;
  emptyMessage?: string;
}) {
  return (
    <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">
              <th className="px-4 py-3">Rank</th>
              <th className="px-4 py-3">Team</th>
              <th className="px-4 py-3">Category</th>
              <th className="px-4 py-3">Score</th>
              <th className="px-4 py-3">Submitted</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {isLoading ? (
              Array.from({ length: 5 }).map((_, index) => (
                <tr key={index}>
                  <td colSpan={5} className="px-4 py-3">
                    <div className="h-4 rounded bg-slate-200 animate-pulse" />
                  </td>
                </tr>
              ))
            ) : rows.length === 0 ? (
              <tr>
                <td colSpan={5} className="px-4 py-12 text-center text-sm text-slate-400">
                  {emptyMessage}
                </td>
              </tr>
            ) : (
              rows.map((row) => (
                <tr key={row.submissionId} className="hover:bg-slate-50">
                  <td className="px-4 py-3">
                    <span className="inline-flex items-center gap-1 font-semibold text-slate-900">
                      {row.rank <= 3 && <Medal className="h-4 w-4 text-yellow-500" />}
                      #{row.rank}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <p className="font-medium text-slate-800">{row.teamName}</p>
                    <p className="text-xs text-slate-400">{row.teamId}</p>
                  </td>
                  <td className="px-4 py-3 text-sm text-slate-600">{row.categoryName ?? "All categories"}</td>
                  <td className="px-4 py-3 text-sm font-semibold text-blue-700">{Number(row.totalScore).toFixed(2)}</td>
                  <td className="px-4 py-3 text-xs text-slate-500">{formatDate(row.submittedAt)}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
