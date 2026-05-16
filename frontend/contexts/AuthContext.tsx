"use client";

import React, {
  createContext,
  useContext,
  useCallback,
  useState,
  useEffect,
} from "react";
import { usePathname } from "next/navigation";
import { AuthUser, LoginRequest, RegisterRequest } from "@/types/auth";
import { authService } from "@/services/authService";

interface AuthContextValue {
  user: AuthUser | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (data: LoginRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
  hasRole: (role: string) => boolean;
  hasAnyRole: (roles: string[]) => boolean;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const isPublicAuthRoute =
    pathname === "/login" ||
    pathname === "/register" ||
    pathname === "/unauthorized" ||
    pathname.startsWith("/login/") ||
    pathname.startsWith("/register/") ||
    pathname.startsWith("/unauthorized/");
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(!isPublicAuthRoute);

  const fetchProfile = useCallback(async (showLoading = false) => {
    if (showLoading) {
      setIsLoading(true);
    }

    const profile = await authService.getProfile();
    setUser(profile);
    setIsLoading(false);
  }, []);

  useEffect(() => {
    if (isPublicAuthRoute) {
      return;
    }

    const initialProfileLoad = window.setTimeout(() => {
      void fetchProfile(true);
    }, 0);

    const handleAuthChanged = () => {
      void fetchProfile(true);
    };

    window.addEventListener("seal-auth-changed", handleAuthChanged);
    return () => {
      window.clearTimeout(initialProfileLoad);
      window.removeEventListener("seal-auth-changed", handleAuthChanged);
    };
  }, [fetchProfile, isPublicAuthRoute]);

  const login = useCallback(async (data: LoginRequest) => {
    await authService.login(data);
  }, []);

  const register = useCallback(async (data: RegisterRequest) => {
    await authService.register(data);
  }, []);

  const logout = useCallback(async () => {
    await authService.logout();
  }, []);

  const hasRole = useCallback(
    (role: string) => user?.roles.includes(role as never) ?? false,
    [user]
  );

  const hasAnyRole = useCallback(
    (roles: string[]) => roles.some((r) => user?.roles.includes(r as never)) ?? false,
    [user]
  );

  return (
    <AuthContext.Provider
      value={{
        user,
        isLoading,
        isAuthenticated: !!user,
        login,
        register,
        logout,
        hasRole,
        hasAnyRole,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside <AuthProvider>");
  return ctx;
}
