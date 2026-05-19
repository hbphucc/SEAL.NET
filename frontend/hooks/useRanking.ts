import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { rankingService } from "@/services/rankingService";
import { AdvanceRoundError } from "@/types/ranking";
import { getErrorMessage } from "@/lib/utils";

export const RANKING_KEYS = {
  publicRound: (roundId: string) => ["ranking", "public", "round", roundId] as const,
  publicCategoryRound: (categoryId: string, roundId: string) =>
    ["ranking", "public", "category", categoryId, "round", roundId] as const,
  adminRound: (roundId: string) => ["ranking", "admin", "round", roundId] as const,
  adminCategoryRound: (categoryId: string, roundId: string) =>
    ["ranking", "admin", "category", categoryId, "round", roundId] as const,
};

export function usePublicRoundRanking(roundId: string) {
  return useQuery({
    queryKey: RANKING_KEYS.publicRound(roundId),
    queryFn: () => rankingService.getPublicRound(roundId),
    enabled: !!roundId,
    retry: false,
  });
}

export function usePublicCategoryRoundRanking(categoryId: string, roundId: string) {
  return useQuery({
    queryKey: RANKING_KEYS.publicCategoryRound(categoryId, roundId),
    queryFn: () => rankingService.getPublicCategoryRound(categoryId, roundId),
    enabled: !!categoryId && !!roundId,
    retry: false,
  });
}

export function useAdminRoundRanking(roundId: string) {
  return useQuery({
    queryKey: RANKING_KEYS.adminRound(roundId),
    queryFn: () => rankingService.getAdminRound(roundId),
    enabled: !!roundId,
  });
}

export function useAdminCategoryRoundRanking(categoryId: string, roundId: string) {
  return useQuery({
    queryKey: RANKING_KEYS.adminCategoryRound(categoryId, roundId),
    queryFn: () => rankingService.getAdminCategoryRound(categoryId, roundId),
    enabled: !!categoryId && !!roundId,
  });
}

export function useAdvanceRound() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (roundId: string) => rankingService.advanceRound(roundId),
    onSuccess: (data, roundId) => {
      toast.success(data.message);
      qc.invalidateQueries({ queryKey: ["ranking", "admin"] });
      qc.invalidateQueries({ queryKey: RANKING_KEYS.adminRound(roundId) });
      qc.invalidateQueries({ queryKey: ["teams"] });
      qc.invalidateQueries({ queryKey: ["events"] });
    },
    onError: (err) => {
      const data = (err as { response?: { data?: AdvanceRoundError } }).response?.data;
      toast.error(data?.message ?? getErrorMessage(err));
    },
  });
}
