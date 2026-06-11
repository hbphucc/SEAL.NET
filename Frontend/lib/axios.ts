import axios from "axios";
import { isPublicRoute } from "@/lib/constants";

const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL || "https://localhost:5001/api";

const axiosInstance = axios.create({
  baseURL: API_BASE_URL,
  withCredentials: true,
  headers: {
    "Content-Type": "application/json",
    "X-SEAL-CSRF": "1",
  },
});

axiosInstance.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401 && typeof window !== "undefined") {
      if (!isPublicRoute(window.location.pathname)) {
        window.location.replace("/login");
      }
    }

    return Promise.reject(error);
  }
);

export default axiosInstance;
