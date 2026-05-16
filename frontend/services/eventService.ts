import api from "@/lib/axios";
import {
  Category,
  CategoryPayload,
  Criteria,
  CriteriaPayload,
  Event,
  EventPayload,
  Round,
  RoundPayload,
} from "@/types/event";

export const eventService = {
  async getAll(): Promise<Event[]> {
    const res = await api.get<Event[]>("/events");
    return res.data;
  },

  async getById(id: string): Promise<Event> {
    const res = await api.get<Event>(`/events/${id}`);
    return res.data;
  },

  async create(data: EventPayload): Promise<{ id: string }> {
    const res = await api.post<{ id: string }>("/events", data);
    return res.data;
  },

  async update(id: string, data: EventPayload): Promise<{ message: string }> {
    const res = await api.put<{ message: string }>(`/events/${id}`, data);
    return res.data;
  },

  async delete(id: string): Promise<{ message: string }> {
    const res = await api.delete<{ message: string }>(`/events/${id}`);
    return res.data;
  },
};

export const categoryService = {
  async getByEvent(eventId: string): Promise<Category[]> {
    const res = await api.get<Category[]>(`/events/${eventId}/categories`);
    return res.data;
  },

  async create(eventId: string, data: CategoryPayload): Promise<{ categoryId: string; message: string }> {
    const res = await api.post<{ categoryId: string; message: string }>(`/events/${eventId}/categories`, data);
    return res.data;
  },

  async update(eventId: string, categoryId: string, data: CategoryPayload): Promise<{ message: string }> {
    const res = await api.put<{ message: string }>(`/events/${eventId}/categories/${categoryId}`, data);
    return res.data;
  },

  async delete(eventId: string, categoryId: string): Promise<{ message: string }> {
    const res = await api.delete<{ message: string }>(`/events/${eventId}/categories/${categoryId}`);
    return res.data;
  },
};

export const roundService = {
  async getByEvent(eventId: string): Promise<Round[]> {
    const res = await api.get<Round[]>(`/events/${eventId}/rounds`);
    return res.data;
  },

  async create(eventId: string, data: RoundPayload): Promise<{ roundId: string; message: string }> {
    const res = await api.post<{ roundId: string; message: string }>(`/events/${eventId}/rounds`, data);
    return res.data;
  },

  async update(eventId: string, roundId: string, data: RoundPayload): Promise<{ message: string }> {
    const res = await api.put<{ message: string }>(`/events/${eventId}/rounds/${roundId}`, data);
    return res.data;
  },

  async delete(eventId: string, roundId: string): Promise<{ message: string }> {
    const res = await api.delete<{ message: string }>(`/events/${eventId}/rounds/${roundId}`);
    return res.data;
  },
};

export const criteriaService = {
  async getByRound(roundId: string): Promise<Criteria[]> {
    const res = await api.get<Criteria[]>(`/rounds/${roundId}/criteria`);
    return res.data;
  },

  async create(roundId: string, data: CriteriaPayload): Promise<{ criteriaId: string; message: string }> {
    const res = await api.post<{ criteriaId: string; message: string }>(`/rounds/${roundId}/criteria`, data);
    return res.data;
  },

  async update(roundId: string, criteriaId: string, data: CriteriaPayload): Promise<{ message: string }> {
    const res = await api.put<{ message: string }>(`/rounds/${roundId}/criteria/${criteriaId}`, data);
    return res.data;
  },

  async delete(roundId: string, criteriaId: string): Promise<{ message: string }> {
    const res = await api.delete<{ message: string }>(`/rounds/${roundId}/criteria/${criteriaId}`);
    return res.data;
  },
};
