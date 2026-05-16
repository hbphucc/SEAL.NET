import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { categoryService, criteriaService, eventService, roundService } from "@/services/eventService";
import { CategoryPayload, CriteriaPayload, EventPayload, RoundPayload } from "@/types/event";
import { getErrorMessage } from "@/lib/utils";

export const EVENT_KEYS = {
  all: ["events"] as const,
  byId: (id: string) => ["events", id] as const,
  categories: (eventId: string) => ["events", eventId, "categories"] as const,
  rounds: (eventId: string) => ["events", eventId, "rounds"] as const,
  criteria: (roundId: string) => ["rounds", roundId, "criteria"] as const,
};

export function useEvents() {
  return useQuery({
    queryKey: EVENT_KEYS.all,
    queryFn: eventService.getAll,
  });
}

export function useEvent(id: string) {
  return useQuery({
    queryKey: EVENT_KEYS.byId(id),
    queryFn: () => eventService.getById(id),
    enabled: !!id,
  });
}

export function useCategories(eventId: string) {
  return useQuery({
    queryKey: EVENT_KEYS.categories(eventId),
    queryFn: () => categoryService.getByEvent(eventId),
    enabled: !!eventId,
  });
}

export function useRounds(eventId: string) {
  return useQuery({
    queryKey: EVENT_KEYS.rounds(eventId),
    queryFn: () => roundService.getByEvent(eventId),
    enabled: !!eventId,
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
    onSuccess: () => {
      toast.success("Category updated successfully.");
      qc.invalidateQueries({ queryKey: EVENT_KEYS.categories(eventId) });
      qc.invalidateQueries({ queryKey: EVENT_KEYS.byId(eventId) });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useDeleteCategory(eventId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (categoryId: string) => categoryService.delete(eventId, categoryId),
    onSuccess: () => {
      toast.success("Category deleted successfully.");
      qc.invalidateQueries({ queryKey: EVENT_KEYS.categories(eventId) });
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
    onSuccess: () => {
      toast.success("Round updated successfully.");
      qc.invalidateQueries({ queryKey: EVENT_KEYS.rounds(eventId) });
      qc.invalidateQueries({ queryKey: EVENT_KEYS.byId(eventId) });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useDeleteRound(eventId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (roundId: string) => roundService.delete(eventId, roundId),
    onSuccess: () => {
      toast.success("Round deleted successfully.");
      qc.invalidateQueries({ queryKey: EVENT_KEYS.rounds(eventId) });
      qc.invalidateQueries({ queryKey: EVENT_KEYS.byId(eventId) });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
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
