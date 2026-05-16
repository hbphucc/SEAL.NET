import axios from "axios";

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
      const publicAuthRoutes = ["/login", "/register", "/unauthorized"];
      const isPublicAuthRoute = publicAuthRoutes.some(
        (route) => window.location.pathname === route || window.location.pathname.startsWith(`${route}/`)
      );

      if (!isPublicAuthRoute) {
        window.location.replace("/login");
      }
    }

    return Promise.reject(error);
  }
);

export default axiosInstance;
