"use client";

import { useAuth } from "@/contexts/AuthContext";
import { usePathname } from "next/navigation";
import { Bell, ChevronDown, LogOut, User } from "lucide-react";
import { useState, useRef, useEffect } from "react";
import Link from "next/link";

const BREADCRUMB_MAP: Record<string, string> = {
  "/dashboard": "Dashboard",
  "/profile": "Profile",
  "/admin/users": "Manage Users",
  "/teams": "Manage Teams",
  "/members": "Members",
  "/team-leaders": "Team Leaders",
  "/eliminations": "Eliminations",
  "/elimination-reasons": "Elimination Reasons",
  "/students": "Student Info",
};

export default function Topbar() {
  const { user, logout } = useAuth();
  const pathname = usePathname();
  const [dropdownOpen, setDropdownOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  const pageTitle = BREADCRUMB_MAP[pathname] ?? "SEAL.NET";

  // Detect team detail route
  const isTeamDetail = pathname.startsWith("/teams/") && pathname !== "/teams";

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setDropdownOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  return (
    <header className="h-16 bg-white border-b border-slate-200 flex items-center justify-between px-4 lg:px-6 flex-shrink-0 shadow-sm z-10">
      {/* Left: breadcrumb */}
      <div className="flex items-center gap-2 ml-10 lg:ml-0">
        <span className="text-slate-400 text-sm hidden sm:block">SEAL.NET</span>
        <span className="text-slate-300 hidden sm:block">/</span>
        <h1 className="text-sm font-semibold text-slate-800">
          {isTeamDetail ? (
            <>
              <Link href="/teams" className="text-blue-600 hover:underline">Teams</Link>
              <span className="text-slate-400 mx-1">/</span>
              Detail
            </>
          ) : (
            pageTitle
          )}
        </h1>
      </div>

      {/* Right: actions + user */}
      <div className="flex items-center gap-3">
        {/* Notification bell - placeholder */}
        <button className="relative p-2 rounded-lg text-slate-500 hover:bg-slate-100 hover:text-slate-700 transition-colors">
          <Bell className="w-5 h-5" />
        </button>

        {/* User dropdown */}
        <div className="relative" ref={dropdownRef}>
          <button
            onClick={() => setDropdownOpen(!dropdownOpen)}
            className="flex items-center gap-2 px-3 py-2 rounded-lg hover:bg-slate-100 transition-colors"
          >
            <div className="w-8 h-8 rounded-full bg-blue-600 flex items-center justify-center text-white text-sm font-semibold flex-shrink-0">
              {user?.fullName?.charAt(0)?.toUpperCase() ?? "U"}
            </div>
            <div className="hidden sm:block text-left">
              <p className="text-sm font-medium text-slate-800 leading-tight max-w-[120px] truncate">
                {user?.fullName}
              </p>
              <p className="text-xs text-slate-500 leading-tight">
                {user?.roles?.[0]}
              </p>
            </div>
            <ChevronDown className="w-4 h-4 text-slate-400" />
          </button>

          {dropdownOpen && (
            <div className="absolute right-0 top-full mt-1 w-48 bg-white border border-slate-200 rounded-xl shadow-lg py-1 z-50">
              <div className="px-3 py-2 border-b border-slate-100">
                <p className="text-xs font-medium text-slate-800 truncate">{user?.fullName}</p>
                <p className="text-xs text-slate-500 truncate">{user?.email}</p>
              </div>
              <Link
                href="/profile"
                onClick={() => setDropdownOpen(false)}
                className="flex items-center gap-2 px-3 py-2 text-sm text-slate-700 hover:bg-slate-50 transition-colors"
              >
                <User className="w-4 h-4" />
                Profile
              </Link>
              <button
                onClick={() => { logout(); setDropdownOpen(false); }}
                className="flex items-center gap-2 w-full px-3 py-2 text-sm text-red-600 hover:bg-red-50 transition-colors"
              >
                <LogOut className="w-4 h-4" />
                Logout
              </button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
