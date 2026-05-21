export interface Notification {
  notificationId: string;
  type: string;
  title: string;
  message?: string | null;
  link?: string | null;
  status: "Unread" | "Read";
  createdAt: string;
  readAt?: string | null;
}
