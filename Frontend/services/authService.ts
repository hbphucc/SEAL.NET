import axios from "axios";
import api from "@/lib/axios";
import { AuthUser, LoginRequest, LoginResponse, RegisterRequest } from "@/types/auth";


function notifyAuthChanged() {
  if (typeof window === "undefined") return;
  window.dispatchEvent(new Event("seal-auth-changed"));
}

export const authService = {
  async login(data: LoginRequest): Promise<LoginResponse> {
    const res = await api.post<LoginResponse>("/auth/login", data);
    notifyAuthChanged();
    return res.data;
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
    } catch (err) {
      // Genuinely unauthenticated -> no user. Transient errors (network, 5xx)
      // are rethrown so the caller can keep the current session instead of
      // logging the user out on a blip.
      if (axios.isAxiosError(err) && err.response?.status === 401) {
        return null;
      }
      throw err;
    }
  }
};
