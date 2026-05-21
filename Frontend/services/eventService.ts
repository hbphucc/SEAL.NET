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

  async getPublic(): Promise<Event[]> {
    const res = await api.get<Event[]>("/events/public");
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

  async publish(id: string): Promise<{ message: string }> {
    const res = await api.post<{ message: string }>(`/events/${id}/publish`);
    return res.data;
  },

  async closeRegistration(id: string): Promise<{ message: string }> {
    const res = await api.post<{ message: string }>(`/events/${id}/close-registration`);
    return res.data;
  },

  async startJudging(id: string): Promise<{ message: string }> {
    const res = await api.post<{ message: string }>(`/events/${id}/start-judging`);
    return res.data;
  },

  async endJudging(id: string): Promise<{ message: string }> {
    const res = await api.post<{ message: string }>(`/events/${id}/end-judging`);
    return res.data;
  },

  async archive(id: string): Promise<{ message: string }> {
    const res = await api.post<{ message: string }>(`/events/${id}/archive`);
    return res.data;
  },

  async join(id: string): Promise<{ message: string }> {
    const res = await api.post<{ message: string }>(`/events/${id}/join`);
    return res.data;
  },

  async leave(id: string): Promise<{ message: string }> {
    const res = await api.delete<{ message: string }>(`/events/${id}/leave`);
    return res.data;
  },

  async getMine(): Promise<Event[]> {
    const res = await api.get<Event[]>("/events/mine");
    return res.data;
  },
};

export const categoryService = {
  async getByEvent(eventId: string): Promise<Category[]> {
    const res = await api.get<Category[]>(`/events/${eventId}/categories`);
    return res.data;
  },

  async getById(eventId: string, categoryId: string): Promise<Category> {
    const res = await api.get<Category>(`/events/${eventId}/categories/${categoryId}`);
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

  async getById(eventId: string, roundId: string): Promise<Round> {
    const res = await api.get<Round>(`/events/${eventId}/rounds/${roundId}`);
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

  async open(roundId: string): Promise<{ message: string }> {
    const res = await api.post<{ message: string }>(`/admin/rounds/${roundId}/open`);
    return res.data;
  },

  async close(roundId: string): Promise<{ message: string }> {
    const res = await api.post<{ message: string }>(`/admin/rounds/${roundId}/close`);
    return res.data;
  },

  async lockSubmissions(roundId: string): Promise<{ message: string }> {
    const res = await api.post<{ message: string }>(`/admin/rounds/${roundId}/lock-submissions`);
    return res.data;
  },

  async publishResult(roundId: string): Promise<{ message: string }> {
    const res = await api.post<{ message: string }>(`/admin/rounds/${roundId}/publish-result`);
    return res.data;
  },

  async reopen(roundId: string): Promise<{ message: string }> {
    const res = await api.post<{ message: string }>(`/admin/rounds/${roundId}/reopen`);
    return res.data;
  },

  async advance(roundId: string): Promise<{ message: string }> {
    const res = await api.post<{ message: string }>(`/admin/rounds/${roundId}/advance`);
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
