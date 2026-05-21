"use client";

import { useEffect } from "react";
import { usePathname, useRouter } from "next/navigation";
import { useAuth } from "@/contexts/AuthContext";
import Sidebar from "@/components/layout/Sidebar";
import Topbar from "@/components/layout/Topbar";

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading, hasAnyRole } = useAuth();
  const router = useRouter();
  const pathname = usePathname();

  const requiredRoles = getRequiredRoles(pathname);
  const isAuthorized = !requiredRoles || hasAnyRole(requiredRoles);

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.replace("/login");
      return;
    }

    if (!isLoading && isAuthenticated && !isAuthorized) {
      router.replace("/unauthorized");
    }
  }, [isAuthenticated, isAuthorized, isLoading, router]);

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-slate-50">
        <div className="flex flex-col items-center gap-3">
          <div className="w-10 h-10 border-4 border-blue-600 border-t-transparent rounded-full animate-spin" />
          <p className="text-sm text-slate-500">Loading...</p>
        </div>
      </div>
    );
  }

  if (!isAuthenticated || !isAuthorized) return null;

  return (
    <div className="flex h-screen bg-slate-50 overflow-hidden">
      <div className="relative flex">
        <Sidebar />
      </div>
      <div className="flex-1 flex flex-col overflow-hidden">
        <Topbar />
        <main className="flex-1 overflow-y-auto p-4 lg:p-6">
          <div className="animate-fade-in">{children}</div>
        </main>
      </div>
    </div>
  );
}

function getRequiredRoles(pathname: string): string[] | null {
  if (pathname.startsWith("/admin")) return ["Admin"];
  if (pathname === "/teams" || pathname.startsWith("/teams/")) return ["Admin"];
  if (pathname.startsWith("/judge")) return ["Judge"];
  if (pathname === "/submit") return ["Member", "TeamLeader"];
  if (pathname === "/my-team") return ["Member", "TeamLeader"];
  if (
    pathname === "/members" ||
    pathname === "/team-leaders" ||
    pathname === "/students" ||
    pathname === "/eliminations" ||
    pathname === "/elimination-reasons"
  ) {
    return ["Admin"];
  }
  return null;
}
