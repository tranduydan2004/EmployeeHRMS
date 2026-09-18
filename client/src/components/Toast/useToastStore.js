import { create } from 'zustand';

export const useToastStore = create((set, get) => ({
  toasts: [],
  
  addToast: (message, type = 'info', duration = 4000) => {
    // Chống hiển thị trùng lặp toast cùng nội dung và kiểu trong khi đang hiển thị
    const existing = get().toasts.some((t) => t.message === message && t.type === type);
    if (existing) {
      return;
    }

    const id = Date.now().toString() + Math.random().toString(36).substring(2, 5);
    set((state) => ({
      toasts: [...state.toasts, { id, message, type }],
    }));

    if (duration > 0) {
      setTimeout(() => {
        set((state) => ({
          toasts: state.toasts.filter((t) => t.id !== id),
        }));
      }, duration);
    }
  },

  removeToast: (id) => {
    set((state) => ({
      toasts: state.toasts.filter((t) => t.id !== id),
    }));
  },

  success: (msg, duration) => useToastStore.getState().addToast(msg, 'success', duration),
  error: (msg, duration) => useToastStore.getState().addToast(msg, 'error', duration),
  warning: (msg, duration) => useToastStore.getState().addToast(msg, 'warning', duration),
  info: (msg, duration) => useToastStore.getState().addToast(msg, 'info', duration),
}));

export const toast = {
  success: (msg, duration) => useToastStore.getState().success(msg, duration),
  error: (msg, duration) => useToastStore.getState().error(msg, duration),
  warning: (msg, duration) => useToastStore.getState().warning(msg, duration),
  info: (msg, duration) => useToastStore.getState().info(msg, duration),
};
