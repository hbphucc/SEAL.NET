import { UserRole, StudentType } from "./auth";

export interface User {
  id: string;
  fullName: string;
  email: string;
  isApproved: boolean;
  createdAt: string;
  roles: UserRole[];
  // Student fields
  studentType?: StudentType;
  studentCode?: string;
  schoolName?: string;
}

export interface UpdateUserRoleRequest {
  role: UserRole;
}

export type UserStatus = "approved" | "pending";
