import { create } from 'zustand';
import {
  getNotificationsApi,
  getUnreadCountApi,
  markAsReadApi,
  markAllAsReadApi,
} from '@/api/notificationApi';

export const useNotificationStore = create((set, get) => ({
  notifications: [],
  unreadCount: 0,
  isLoading: false,
  totalCount: 0,

  fetchNotifications: async (params = { page: 1, pageSize: 20 }) => {
    set({ isLoading: true });
    try {
      const response = await getNotificationsApi(params);
      const data = response.data;
      set({
        notifications: data.items || [],
        totalCount: data.totalCount || 0,
      });
    } catch (err) {
      console.error('Failed to fetch notifications:', err);
    } finally {
      set({ isLoading: false });
    }
  },

  fetchUnreadCount: async () => {
    try {
      const response = await getUnreadCountApi();
      set({ unreadCount: response.data.count || 0 });
    } catch (err) {
      console.error('Failed to fetch unread count:', err);
    }
  },

  addNotification: (newNotif) => {
    set((state) => {
      // Prevent duplicate if ID exists and is already in list
      if (newNotif.id && state.notifications.some((n) => n.id === newNotif.id)) {
        return state;
      }
      return {
        notifications: [newNotif, ...state.notifications],
        unreadCount: state.unreadCount + 1,
      };
    });
  },

  markAsRead: async (id) => {
    // Optimistic update
    set((state) => ({
      notifications: state.notifications.map((n) =>
        n.id === id ? { ...n, isRead: true, readAt: new Date().toISOString() } : n
      ),
      unreadCount: Math.max(0, state.unreadCount - 1),
    }));

    try {
      await markAsReadApi(id);
    } catch (err) {
      console.error('Failed to mark notification as read:', err);
      // Revert if needed
      get().fetchUnreadCount();
    }
  },

  markAllAsRead: async () => {
    // Optimistic update
    set((state) => ({
      notifications: state.notifications.map((n) => ({
        ...n,
        isRead: true,
        readAt: new Date().toISOString(),
      })),
      unreadCount: 0,
    }));

    try {
      await markAllAsReadApi();
    } catch (err) {
      console.error('Failed to mark all notifications as read:', err);
      get().fetchUnreadCount();
    }
  },
}));
