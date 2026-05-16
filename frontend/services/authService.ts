import api from "@/lib/axios";
import { AuthUser, LoginRequest, RegisterRequest } from "@/types/auth";


function notifyAuthChanged() {
  if (typeof window === "undefined") return;
  window.dispatchEvent(new Event("seal-auth-changed"));
}

export const authService = {
  async login(data: LoginRequest): Promise<void> {
    await api.post("/auth/login", data);
    notifyAuthChanged();
  },

  async register(data: RegisterRequest): Promise<{ message: string }> {
    const res = await api.post<{ message: string }>("/auth/register", data);
    return res.data;
  },

  async logout(): Promise<void> {
    try {
      await api.post("/auth/logout");
    } finally {
      notifyAuthChanged();
    }
  },

  async getProfile(): Promise<AuthUser | null> {
    try {
      const res = await api.get<AuthUser>("/auth/me");
      return res.data;
    } catch {
      return null;
    }
  }
};
