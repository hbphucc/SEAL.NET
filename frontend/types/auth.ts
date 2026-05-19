export type UserRole = "Admin" | "Member" | "TeamLeader" | "Judge" | "Mentor";

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  studentType: StudentType;
  studentCode: string;
  schoolName?: string;
}

export interface AuthUser {
  id: string;
  fullName: string;
  email: string;
  studentType?: StudentType;
  studentCode?: string;
  schoolName?: string;
  isApproved?: boolean;
  createdAt?: string;
  roles: UserRole[];
}

export interface LoginResponse {
  message: string;
  user: AuthUser;
}

export type StudentType = 0 | 1;
