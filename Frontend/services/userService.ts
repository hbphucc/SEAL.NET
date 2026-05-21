import api from "@/lib/axios";
import { User, UpdateUserRoleRequest } from "@/types/user";

export const userService = {
  async getAll(): Promise<User[]> {
    const res = await api.get<User[]>("/admin/users");
    return res.data;
  },

  async getPending(): Promise<User[]> {
    const res = await api.get<User[]>("/admin/users/pending");
    return res.data;
  },

  async approve(userId: string): Promise<{ message: string }> {
    const res = await api.put<{ message: string }>(`/admin/users/${userId}/approve`);
    return res.data;
  },

  async reject(userId: string): Promise<{ message: string }> {
    const res = await api.put<{ message: string }>(`/admin/users/${userId}/reject`);
    return res.data;
  },

  async updateRole(userId: string, data: UpdateUserRoleRequest): Promise<{ message: string }> {
    const res = await api.put<{ message: string }>(`/admin/users/${userId}/role`, data);
    return res.data;
  },

  async delete(userId: string): Promise<{ message: string }> {
    const res = await api.delete<{ message: string }>(`/admin/users/${userId}`);
    return res.data;
  },
};
