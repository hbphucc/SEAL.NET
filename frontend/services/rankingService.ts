import api from "@/lib/axios";
import { AdvanceRoundResponse, RankingRow } from "@/types/ranking";

export const rankingService = {
  async getPublicRound(roundId: string): Promise<RankingRow[]> {
    const res = await api.get<RankingRow[]>(`/ranking/public/round/${roundId}`);
    return res.data;
  },

  async getPublicCategoryRound(categoryId: string, roundId: string): Promise<RankingRow[]> {
    const res = await api.get<RankingRow[]>(`/ranking/public/category/${categoryId}/round/${roundId}`);
    return res.data;
  },

  async getAdminRound(roundId: string): Promise<RankingRow[]> {
    const res = await api.get<RankingRow[]>(`/admin/ranking/round/${roundId}`);
    return res.data;
  },

  async getAdminCategoryRound(categoryId: string, roundId: string): Promise<RankingRow[]> {
    const res = await api.get<RankingRow[]>(`/admin/ranking/category/${categoryId}/round/${roundId}`);
    return res.data;
  },

  async advanceRound(roundId: string): Promise<AdvanceRoundResponse> {
    const res = await api.post<AdvanceRoundResponse>(`/admin/rounds/${roundId}/advance`);
    return res.data;
  },
};
