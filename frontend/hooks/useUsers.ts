import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { userService } from "@/services/userService";
import { UpdateUserRoleRequest } from "@/types/user";
import { toast } from "sonner";
import { getErrorMessage } from "@/lib/utils";

export const USER_KEYS = {
  all: ["users"] as const,
  pending: ["users", "pending"] as const,
};

export function useUsers(enabled = true) {
  return useQuery({
    queryKey: USER_KEYS.all,
    queryFn: userService.getAll,
    enabled,
  });
}

export function usePendingUsers(enabled = true) {
  return useQuery({
    queryKey: USER_KEYS.pending,
    queryFn: userService.getPending,
    enabled,
  });
}

export function useApproveUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (userId: string) => userService.approve(userId),
    onSuccess: (data) => {
      toast.success(data.message);
      qc.invalidateQueries({ queryKey: USER_KEYS.all });
      qc.invalidateQueries({ queryKey: USER_KEYS.pending });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useRejectUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (userId: string) => userService.reject(userId),
    onSuccess: (data) => {
      toast.success(data.message);
      qc.invalidateQueries({ queryKey: USER_KEYS.all });
      qc.invalidateQueries({ queryKey: USER_KEYS.pending });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useUpdateUserRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ userId, data }: { userId: string; data: UpdateUserRoleRequest }) =>
      userService.updateRole(userId, data),
    onSuccess: (data) => {
      toast.success(data.message);
      qc.invalidateQueries({ queryKey: USER_KEYS.all });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useDeleteUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (userId: string) => userService.delete(userId),
    onSuccess: (data) => {
      toast.success(data.message);
      qc.invalidateQueries({ queryKey: USER_KEYS.all });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}
