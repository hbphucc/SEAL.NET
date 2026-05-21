import api from "@/lib/axios";
import { Notification } from "@/types/notification";

export const notificationService = {
  async getMine(): Promise<Notification[]> {
    const res = await api.get<Notification[]>("/notifications");
    return res.data;
  },

  async markRead(notificationId: string): Promise<{ message: string }> {
    const res = await api.post<{ message: string }>(`/notifications/${notificationId}/read`);
    return res.data;
  },
};
