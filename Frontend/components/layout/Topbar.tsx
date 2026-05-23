"use client";

import { useAuth } from "@/contexts/AuthContext";
import { usePathname } from "next/navigation";
import { Bell, ChevronDown, LogOut, User, Check, Loader2 } from "lucide-react";
import { useState, useRef, useEffect } from "react";
import Link from "next/link";
import { useNotifications, useMarkNotificationRead } from "@/hooks/useNotifications";
import { cn, formatDate } from "@/lib/utils";

const BREADCRUMB_MAP: Record<string, string> = {
  "/dashboard": "Dashboard",
  "/profile": "Profile",
  "/admin/users": "Manage Users",
  "/judge/dashboard": "Judge Dashboard",
  "/judge/submissions": "Judge Submissions",
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
  const [notifDropdownOpen, setNotifDropdownOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const notifRef = useRef<HTMLDivElement>(null);

  const { data: notifications = [], isLoading: notifsLoading, isError: notifsError } = useNotifications(!!user);
  const markRead = useMarkNotificationRead();

  const unreadCount = notifications.filter((n) => n.status === "Unread").length;

  const pageTitle = BREADCRUMB_MAP[pathname] ?? "SEAL.NET";

  // Detect team detail route
  const isTeamDetail = pathname.startsWith("/teams/") && pathname !== "/teams";

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setDropdownOpen(false);
      }
      if (notifRef.current && !notifRef.current.contains(e.target as Node)) {
        setNotifDropdownOpen(false);
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
        {/* Notification bell */}
        <div className="relative" ref={notifRef}>
          <button
            onClick={() => setNotifDropdownOpen(!notifDropdownOpen)}
            className="relative p-2 rounded-lg text-slate-500 hover:bg-slate-100 hover:text-slate-700 transition-colors"
          >
            <Bell className="w-5 h-5" />
            {unreadCount > 0 && (
              <span className="absolute top-1.5 right-1.5 flex h-4 w-4 items-center justify-center rounded-full bg-red-500 text-[10px] font-bold text-white ring-2 ring-white animate-in zoom-in">
                {unreadCount}
              </span>
            )}
          </button>

          {notifDropdownOpen && (
            <div className="absolute right-0 top-full mt-1 w-80 bg-white border border-slate-200 rounded-xl shadow-lg py-1 z-50 flex flex-col animate-in fade-in slide-in-from-top-1 duration-200">
              <div className="px-4 py-2.5 border-b border-slate-100 flex items-center justify-between">
                <span className="text-xs font-semibold text-slate-900">Notifications</span>
                {unreadCount > 0 && (
                  <span className="rounded-full bg-blue-50 px-2 py-0.5 text-[10px] font-medium text-blue-700">
                    {unreadCount} unread
                  </span>
                )}
              </div>

              <div className="max-h-80 overflow-y-auto divide-y divide-slate-50">
                {notifsLoading ? (
                  <div className="flex flex-col items-center justify-center py-8 text-slate-400 gap-2">
                    <Loader2 className="h-5 w-5 animate-spin text-blue-600" />
                    <span className="text-xs">Loading notifications...</span>
                  </div>
                ) : notifsError ? (
                  <div className="px-4 py-8 text-center text-xs text-red-500">
                    Failed to load notifications.
                  </div>
                ) : notifications.length === 0 ? (
                  <div className="px-4 py-8 text-center text-xs text-slate-400">
                    No notifications yet.
                  </div>
                ) : (
                  notifications.map((notif) => {
                    const isUnread = notif.status === "Unread";
                    return (
                      <div
                        key={notif.notificationId}
                        className={cn(
                          "px-4 py-3 flex gap-3 text-left transition-colors relative",
                          isUnread ? "bg-blue-50/40 hover:bg-blue-50" : "hover:bg-slate-50"
                        )}
                      >
                        {/* Dot indicator */}
                        {isUnread && (
                          <span className="absolute left-2 top-[18px] h-1.5 w-1.5 rounded-full bg-blue-600" />
                        )}
                        <div className="flex-1 min-w-0 pl-1">
                          <p className="text-xs font-semibold text-slate-900 leading-tight">
                            {notif.title}
                          </p>
                          {notif.message && (
                            <p className="text-xs text-slate-500 mt-1 whitespace-pre-wrap leading-relaxed">
                              {notif.message}
                            </p>
                          )}
                          <p className="text-[10px] text-slate-400 mt-1.5">
                            {formatDate(notif.createdAt)}
                          </p>
                        </div>
                        {isUnread && (
                          <button
                            onClick={async (e) => {
                              e.stopPropagation();
                              await markRead.mutateAsync(notif.notificationId);
                            }}
                            disabled={markRead.isPending}
                            title="Mark as read"
                            className="p-1 rounded-md text-slate-400 hover:text-blue-600 hover:bg-slate-100 self-start transition-all disabled:opacity-50"
                          >
                            <Check className="h-3.5 w-3.5" />
                          </button>
                        )}
                      </div>
                    );
                  })
                )}
              </div>
            </div>
          )}
        </div>

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
