"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useAuth } from "@/contexts/AuthContext";
import { cn } from "@/lib/utils";
import {
  LayoutDashboard,
  Users,
  Shield,
  Users2,
  Trophy,
  CalendarDays,
  Send,
  History,
  Medal,
  UserCheck,
  XCircle,
  FileText,
  GraduationCap,
  ChevronLeft,
  ChevronRight,
  LogOut,
  Menu,
  X,
} from "lucide-react";
import { useState } from "react";

interface NavItem {
  label: string;
  href: string;
  icon: React.ElementType;
  roles?: string[];
}

const NAV_ITEMS: NavItem[] = [
  { label: "Dashboard", href: "/dashboard", icon: LayoutDashboard },
  { label: "Manage Users", href: "/admin/users", icon: Shield, roles: ["Admin"] },
  { label: "Events", href: "/admin/events", icon: CalendarDays, roles: ["Admin"] },
  { label: "Rankings", href: "/admin/ranking", icon: Medal, roles: ["Admin"] },
  { label: "Score Audit", href: "/admin/audit", icon: History, roles: ["Admin"] },
  { label: "Manage Teams", href: "/teams", icon: Trophy, roles: ["Admin"] },
  { label: "My Team", href: "/my-team", icon: Users2, roles: ["Member", "TeamLeader"] },
  { label: "Submit Project", href: "/submit", icon: Send, roles: ["TeamLeader"] },
  { label: "Members", href: "/members", icon: Users, roles: ["Admin"] },
  { label: "Team Leaders", href: "/team-leaders", icon: UserCheck, roles: ["Admin"] },
  { label: "Eliminations", href: "/eliminations", icon: XCircle, roles: ["Admin"] },
  { label: "Elimination Reasons", href: "/elimination-reasons", icon: FileText, roles: ["Admin"] },
  { label: "Student Info", href: "/students", icon: GraduationCap, roles: ["Admin"] },
  { label: "Profile", href: "/profile", icon: Users2 },
];

interface SidebarContentProps {
  collapsed: boolean;
  pathname: string;
  visibleItems: NavItem[];
  user: { fullName: string; roles: string[] } | null;
  logout: () => void;
  onNavigate: () => void;
}

function SidebarContent({
  collapsed,
  pathname,
  visibleItems,
  user,
  logout,
  onNavigate,
}: SidebarContentProps) {
  return (
    <div className="flex flex-col h-full">
      <div className={cn(
        "flex items-center gap-3 px-4 py-5 border-b border-navy-700",
        collapsed && "justify-center px-2"
      )}>
        <div className="w-8 h-8 rounded-lg bg-blue-500 flex items-center justify-center flex-shrink-0">
          <Trophy className="w-4 h-4 text-white" />
        </div>
        {!collapsed && (
          <span className="font-bold text-lg text-white tracking-tight">SEAL.NET</span>
        )}
      </div>

      <nav className="flex-1 px-2 py-4 space-y-1 overflow-y-auto">
        {visibleItems.map((item) => {
          const Icon = item.icon;
          const isActive = pathname === item.href || pathname.startsWith(item.href + "/");
          return (
            <Link
              key={item.href}
              href={item.href}
              onClick={onNavigate}
              className={cn(
                "flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-all duration-150",
                collapsed && "justify-center px-2",
                isActive
                  ? "bg-blue-600 text-white shadow-sm"
                  : "text-slate-300 hover:bg-slate-700 hover:text-white"
              )}
              title={collapsed ? item.label : undefined}
            >
              <Icon className="w-5 h-5 flex-shrink-0" />
              {!collapsed && <span>{item.label}</span>}
            </Link>
          );
        })}
      </nav>

      <div className={cn(
        "px-2 py-4 border-t border-slate-700 space-y-2",
        collapsed && "px-2"
      )}>
        {!collapsed && user && (
          <div className="px-3 py-2 rounded-lg bg-slate-700/50">
            <p className="text-xs text-slate-400">Logged in as</p>
            <p className="text-sm font-medium text-white truncate">{user.fullName}</p>
            <div className="flex gap-1 mt-1 flex-wrap">
              {user.roles.map((role) => (
                <span
                  key={role}
                  className="inline-block text-[10px] px-1.5 py-0.5 rounded bg-blue-600/70 text-blue-100"
                >
                  {role}
                </span>
              ))}
            </div>
          </div>
        )}
        <button
          onClick={logout}
          className={cn(
            "flex items-center gap-3 w-full px-3 py-2.5 rounded-lg text-sm font-medium text-slate-300 hover:bg-red-600 hover:text-white transition-all duration-150",
            collapsed && "justify-center px-2"
          )}
          title={collapsed ? "Logout" : undefined}
        >
          <LogOut className="w-5 h-5 flex-shrink-0" />
          {!collapsed && <span>Logout</span>}
        </button>
      </div>
    </div>
  );
}

export default function Sidebar() {
  const pathname = usePathname();
  const { user, logout, hasAnyRole } = useAuth();
  const [collapsed, setCollapsed] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);

  const visibleItems = NAV_ITEMS.filter(
    (item) => !item.roles || hasAnyRole(item.roles)
  );

  return (
    <>
      {/* Desktop Sidebar */}
      <aside
        className={cn(
          "hidden lg:flex flex-col bg-slate-900 border-r border-slate-700 transition-all duration-300 flex-shrink-0",
          collapsed ? "w-16" : "w-64"
        )}
      >
        {/* Collapse toggle */}
        <div className="absolute top-4 -right-3 z-10 hidden lg:block">
          <button
            onClick={() => setCollapsed(!collapsed)}
            className="w-6 h-6 bg-slate-800 border border-slate-600 rounded-full flex items-center justify-center text-slate-400 hover:text-white hover:bg-slate-700 transition-colors shadow-sm"
          >
            {collapsed ? <ChevronRight className="w-3 h-3" /> : <ChevronLeft className="w-3 h-3" />}
          </button>
        </div>
        <SidebarContent
          collapsed={collapsed}
          pathname={pathname}
          visibleItems={visibleItems}
          user={user}
          logout={logout}
          onNavigate={() => setMobileOpen(false)}
        />
      </aside>

      {/* Mobile: hamburger button */}
      <button
        onClick={() => setMobileOpen(true)}
        className="lg:hidden fixed top-4 left-4 z-50 p-2 bg-slate-900 rounded-lg text-slate-300 hover:text-white shadow-md"
      >
        <Menu className="w-5 h-5" />
      </button>

      {/* Mobile overlay */}
      {mobileOpen && (
        <div className="lg:hidden fixed inset-0 z-50 flex">
          <div
            className="absolute inset-0 bg-black/50"
            onClick={() => setMobileOpen(false)}
          />
          <aside className="relative w-64 bg-slate-900 h-full shadow-xl">
            <button
              onClick={() => setMobileOpen(false)}
              className="absolute top-4 right-4 text-slate-400 hover:text-white"
            >
              <X className="w-5 h-5" />
            </button>
            <SidebarContent
              collapsed={false}
              pathname={pathname}
              visibleItems={visibleItems}
              user={user}
              logout={logout}
              onNavigate={() => setMobileOpen(false)}
            />
          </aside>
        </div>
      )}
    </>
  );
}
