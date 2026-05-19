import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { notificationService } from "@/services/notificationService";
import { getErrorMessage } from "@/lib/utils";
import { toast } from "sonner";

export const NOTIFICATION_KEYS = {
  mine: ["notifications"] as const,
};

export function useNotifications(enabled = true) {
  return useQuery({
    queryKey: NOTIFICATION_KEYS.mine,
    queryFn: notificationService.getMine,
    enabled,
  });
}

export function useMarkNotificationRead() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: notificationService.markRead,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: NOTIFICATION_KEYS.mine });
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}
