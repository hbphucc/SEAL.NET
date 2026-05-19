import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { categoryService, criteriaService, eventService, roundService } from "@/services/eventService";
import { CategoryPayload, CriteriaPayload, EventPayload, RoundPayload } from "@/types/event";
import { getErrorMessage } from "@/lib/utils";

export const EVENT_KEYS = {
  all: ["events"] as const,
  public: ["events", "public"] as const,
  mine: ["events", "mine"] as const,
  byId: (id: string) => ["events", id] as const,
  categories: (eventId: string) => ["events", eventId, "categories"] as const,
  categoryById: (eventId: string, categoryId: string) =>
    ["events", eventId, "categories", categoryId] as const,
  rounds: (eventId: string) => ["events", eventId, "rounds"] as const,
  roundById: (eventId: string, roundId: string) =>
    ["events", eventId, "rounds", roundId] as const,
  criteria: (roundId: string) => ["rounds", roundId, "criteria"] as const,
};

export function useEvents() {
  return useQuery({
    queryKey: EVENT_KEYS.all,
    queryFn: eventService.getAll,
  });
}

export function usePublicEvents() {
  return useQuery({
    queryKey: EVENT_KEYS.public,
    queryFn: eventService.getPublic,
  });
}

export function useMyEvents(enabled = true) {
  return useQuery({
    queryKey: EVENT_KEYS.mine,
    queryFn: eventService.getMine,
    enabled,
  });
}

export function useEvent(id: string) {
  return useQuery({
    queryKey: EVENT_KEYS.byId(id),
    queryFn: () => eventService.getById(id),
    enabled: !!id,
  });
}

function useEventAction(action: (eventId: string) => Promise<{ message: string }>) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: action,
    onSuccess: (data, eventId) => {
      toast.success(data.message);
      qc.invalidateQueries({ queryKey: EVENT_KEYS.all });
      qc.invalidateQueries({ queryKey: EVENT_KEYS.public });
      qc.invalidateQueries({ queryKey: EVENT_KEYS.byId(eventId) });
      qc.invalidateQueries({ queryKey: EVENT_KEYS.mine });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function usePublishEvent() {
  return useEventAction(eventService.publish);
}

export function useCloseRegistration() {
  return useEventAction(eventService.closeRegistration);
}

export function useStartJudging() {
  return useEventAction(eventService.startJudging);
}

export function useEndJudging() {
  return useEventAction(eventService.endJudging);
}

export function useArchiveEvent() {
  return useEventAction(eventService.archive);
}

export function useJoinEvent() {
  return useEventAction(eventService.join);
}

export function useLeaveEvent() {
  return useEventAction(eventService.leave);
}

export function useCategories(eventId: string) {
  return useQuery({
    queryKey: EVENT_KEYS.categories(eventId),
    queryFn: () => categoryService.getByEvent(eventId),
    enabled: !!eventId,
  });
}

export function useCategory(eventId: string, categoryId: string) {
  return useQuery({
    queryKey: EVENT_KEYS.categoryById(eventId, categoryId),
    queryFn: () => categoryService.getById(eventId, categoryId),
    enabled: !!eventId && !!categoryId,
  });
}

export function useRounds(eventId: string) {
  return useQuery({
    queryKey: EVENT_KEYS.rounds(eventId),
    queryFn: () => roundService.getByEvent(eventId),
    enabled: !!eventId,
  });
}

export function useRound(eventId: string, roundId: string) {
  return useQuery({
    queryKey: EVENT_KEYS.roundById(eventId, roundId),
    queryFn: () => roundService.getById(eventId, roundId),
    enabled: !!eventId && !!roundId,
  });
}

export function useCriteria(roundId: string) {
  return useQuery({
    queryKey: EVENT_KEYS.criteria(roundId),
    queryFn: () => criteriaService.getByRound(roundId),
    enabled: !!roundId,
  });
}

export function useCreateEvent() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: EventPayload) => eventService.create(data),
    onSuccess: () => {
      toast.success("Event created successfully.");
      qc.invalidateQueries({ queryKey: EVENT_KEYS.all });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useUpdateEvent() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ eventId, data }: { eventId: string; data: EventPayload }) =>
      eventService.update(eventId, data),
    onSuccess: (_, vars) => {
      toast.success("Event updated successfully.");
      qc.invalidateQueries({ queryKey: EVENT_KEYS.all });
      qc.invalidateQueries({ queryKey: EVENT_KEYS.byId(vars.eventId) });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useDeleteEvent() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (eventId: string) => eventService.delete(eventId),
    onSuccess: () => {
      toast.success("Event deleted successfully.");
      qc.invalidateQueries({ queryKey: EVENT_KEYS.all });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useCreateCategory(eventId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CategoryPayload) => categoryService.create(eventId, data),
    onSuccess: () => {
      toast.success("Category created successfully.");
      qc.invalidateQueries({ queryKey: EVENT_KEYS.categories(eventId) });
      qc.invalidateQueries({ queryKey: EVENT_KEYS.byId(eventId) });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useUpdateCategory(eventId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ categoryId, data }: { categoryId: string; data: CategoryPayload }) =>
      categoryService.update(eventId, categoryId, data),
    onSuccess: (_, vars) => {
      toast.success("Category updated successfully.");
      qc.invalidateQueries({ queryKey: EVENT_KEYS.categories(eventId) });
      qc.invalidateQueries({ queryKey: EVENT_KEYS.categoryById(eventId, vars.categoryId) });
      qc.invalidateQueries({ queryKey: EVENT_KEYS.byId(eventId) });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useDeleteCategory(eventId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (categoryId: string) => categoryService.delete(eventId, categoryId),
    onSuccess: (_, categoryId) => {
      toast.success("Category deleted successfully.");
      qc.invalidateQueries({ queryKey: EVENT_KEYS.categories(eventId) });
      qc.invalidateQueries({ queryKey: EVENT_KEYS.categoryById(eventId, categoryId) });
      qc.invalidateQueries({ queryKey: EVENT_KEYS.byId(eventId) });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useCreateRound(eventId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: RoundPayload) => roundService.create(eventId, data),
    onSuccess: () => {
      toast.success("Round created successfully.");
      qc.invalidateQueries({ queryKey: EVENT_KEYS.rounds(eventId) });
      qc.invalidateQueries({ queryKey: EVENT_KEYS.byId(eventId) });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useUpdateRound(eventId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ roundId, data }: { roundId: string; data: RoundPayload }) =>
      roundService.update(eventId, roundId, data),
    onSuccess: (_, vars) => {
      toast.success("Round updated successfully.");
      qc.invalidateQueries({ queryKey: EVENT_KEYS.rounds(eventId) });
      qc.invalidateQueries({ queryKey: EVENT_KEYS.roundById(eventId, vars.roundId) });
      qc.invalidateQueries({ queryKey: EVENT_KEYS.byId(eventId) });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useDeleteRound(eventId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (roundId: string) => roundService.delete(eventId, roundId),
    onSuccess: (_, roundId) => {
      toast.success("Round deleted successfully.");
      qc.invalidateQueries({ queryKey: EVENT_KEYS.rounds(eventId) });
      qc.invalidateQueries({ queryKey: EVENT_KEYS.roundById(eventId, roundId) });
      qc.invalidateQueries({ queryKey: EVENT_KEYS.byId(eventId) });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

function useRoundAction(action: (roundId: string) => Promise<{ message: string }>, eventId?: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: action,
    onSuccess: (data) => {
      toast.success(data.message);
      if (eventId) {
        qc.invalidateQueries({ queryKey: EVENT_KEYS.rounds(eventId) });
        qc.invalidateQueries({ queryKey: EVENT_KEYS.byId(eventId) });
      }
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useOpenRound(eventId?: string) {
  return useRoundAction(roundService.open, eventId);
}

export function useCloseRound(eventId?: string) {
  return useRoundAction(roundService.close, eventId);
}

export function useLockSubmissions(eventId?: string) {
  return useRoundAction(roundService.lockSubmissions, eventId);
}

export function usePublishRoundResult(eventId?: string) {
  return useRoundAction(roundService.publishResult, eventId);
}

export function useReopenRound(eventId?: string) {
  return useRoundAction(roundService.reopen, eventId);
}

export function useAdvanceRound(eventId?: string) {
  return useRoundAction(roundService.advance, eventId);
}

export function useCreateCriteria(roundId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CriteriaPayload) => criteriaService.create(roundId, data),
    onSuccess: () => {
      toast.success("Criteria created successfully.");
      qc.invalidateQueries({ queryKey: EVENT_KEYS.criteria(roundId) });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useUpdateCriteria(roundId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ criteriaId, data }: { criteriaId: string; data: CriteriaPayload }) =>
      criteriaService.update(roundId, criteriaId, data),
    onSuccess: () => {
      toast.success("Criteria updated successfully.");
      qc.invalidateQueries({ queryKey: EVENT_KEYS.criteria(roundId) });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useDeleteCriteria(roundId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (criteriaId: string) => criteriaService.delete(roundId, criteriaId),
    onSuccess: () => {
      toast.success("Criteria deleted successfully.");
      qc.invalidateQueries({ queryKey: EVENT_KEYS.criteria(roundId) });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}
