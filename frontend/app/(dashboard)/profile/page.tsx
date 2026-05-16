"use client";

import { useAuth } from "@/contexts/AuthContext";
import { useAdminTeams, useMyTeam } from "@/hooks/useTeams";
import { useUsers } from "@/hooks/useUsers";
import PageHeader from "@/components/shared/PageHeader";
import StatusBadge from "@/components/shared/StatusBadge";
import { User, Mail, Shield, GraduationCap, Building, Calendar, Trophy } from "lucide-react";
import { formatDate } from "@/lib/utils";
import { STUDENT_TYPE_LABELS } from "@/lib/constants";

export default function ProfilePage() {
  const { user } = useAuth();
  const isAdmin = user?.roles.includes("Admin") ?? false;
  const canHaveTeam =
    user?.roles.includes("Member") || user?.roles.includes("TeamLeader") || false;
  const { data: teams = [] } = useAdminTeams(isAdmin);
  const { data: ownTeam } = useMyTeam(!isAdmin && canHaveTeam);
  const { data: users = [] } = useUsers(isAdmin);

  if (!user) return null;

  const userDetails = users.find((u) => u.id === user.id);
  const ledTeam = teams.find((t) => t.leaderId === user.id);
  const memberOfTeam = teams.find((t) => t.members.some((m) => m.userId === user.id));

  const myTeam = isAdmin ? ledTeam || memberOfTeam : ownTeam;
  const studentCode = userDetails?.studentCode ?? user.studentCode;
  const studentType = userDetails?.studentType ?? user.studentType;
  const schoolName = userDetails?.schoolName ?? user.schoolName;
  const createdAt = userDetails?.createdAt ?? user.createdAt;
  const isApproved = userDetails?.isApproved ?? user.isApproved;

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <PageHeader
        title="Profile"
        description="Your account information"
        icon={User}
      />

      {/* Avatar + Basic info */}
      <div className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
        <div className="h-24 bg-gradient-to-r from-slate-900 to-blue-900" />
        <div className="px-6 pb-6">
          <div className="-mt-10 flex items-end justify-between mb-4">
            <div className="w-20 h-20 rounded-2xl bg-blue-600 border-4 border-white flex items-center justify-center text-white text-3xl font-bold shadow-md">
              {user.fullName.charAt(0)}
            </div>
            <div className="flex gap-1 mb-1">
              {user.roles.map((role) => (
                <StatusBadge key={role} type="role" value={role} />
              ))}
            </div>
          </div>
          <h2 className="text-xl font-bold text-slate-900">{user.fullName}</h2>
          <p className="text-slate-500 text-sm">{user.email}</p>
        </div>
      </div>

      {/* Details */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
        <h3 className="font-semibold text-slate-800 mb-4">Detailed Information</h3>
        <div className="space-y-4">
          <InfoRow icon={Mail} label="Email" value={user.email} />
          <InfoRow icon={Shield} label="Roles" value={user.roles.join(", ")} />
          {studentCode && (
            <InfoRow icon={GraduationCap} label="Student ID" value={studentCode} mono />
          )}
          {studentType !== undefined && studentType !== null && (
            <InfoRow
              icon={GraduationCap}
              label="Student Type"
              value={STUDENT_TYPE_LABELS[studentType as number]}
            />
          )}
          {schoolName && (
            <InfoRow icon={Building} label="University" value={schoolName} />
          )}
          {createdAt && (
            <InfoRow
              icon={Calendar}
              label="Account Created At"
              value={formatDate(createdAt)}
            />
          )}
          {isApproved !== undefined && (
            <div className="flex items-center gap-3 py-2 border-b border-slate-100 last:border-0">
              <div className="w-8 h-8 rounded-lg bg-slate-100 flex items-center justify-center flex-shrink-0">
                <Shield className="w-4 h-4 text-slate-500" />
              </div>
              <div className="flex-1">
                <p className="text-xs text-slate-400 mb-0.5">Account Status</p>
                <StatusBadge type="user" value={isApproved ? "true" : "false"} />
              </div>
            </div>
          )}
        </div>
      </div>

      {/* My Team */}
      {myTeam && (
        <div className="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
          <div className="flex items-center gap-2 mb-4">
            <Trophy className="w-4 h-4 text-slate-500" />
            <h3 className="font-semibold text-slate-800">My Team</h3>
          </div>
          <div className="flex items-center justify-between">
            <div>
              <p className="font-semibold text-slate-900">{myTeam.teamName}</p>
              <p className="text-sm text-slate-500">{myTeam.category?.categoryName}</p>
            </div>
            <StatusBadge type="team" value={myTeam.status} />
          </div>
          {myTeam.currentRound && (
            <div className="mt-3 px-3 py-2 bg-blue-50 border border-blue-100 rounded-lg text-sm">
              <span className="text-blue-600 font-medium">Current Round: </span>
              <span className="text-blue-800">{myTeam.currentRound.roundName}</span>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function InfoRow({
  icon: Icon,
  label,
  value,
  mono = false,
}: {
  icon: React.ElementType;
  label: string;
  value: string;
  mono?: boolean;
}) {
  return (
    <div className="flex items-center gap-3 py-2 border-b border-slate-100 last:border-0">
      <div className="w-8 h-8 rounded-lg bg-slate-100 flex items-center justify-center flex-shrink-0">
        <Icon className="w-4 h-4 text-slate-500" />
      </div>
      <div className="flex-1 min-w-0">
        <p className="text-xs text-slate-400 mb-0.5">{label}</p>
        <p className={`text-sm text-slate-800 truncate ${mono ? "font-mono" : "font-medium"}`}>{value}</p>
      </div>
    </div>
  );
}
