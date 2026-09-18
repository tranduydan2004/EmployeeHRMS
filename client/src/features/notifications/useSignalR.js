import { useEffect, useRef } from 'react';
import { HubConnectionBuilder, LogLevel, HttpTransportType } from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { useAuthStore } from '../auth/useAuthStore';
import { useNotificationStore } from './useNotificationStore';
import { toast } from '@/components/Toast/useToastStore';
import { formatNotificationMessage } from './utils/notificationTemplates';

const rawBase =
  import.meta.env.VITE_SERVER_BASE_URL ||
  (import.meta.env.VITE_API_BASE_URL
    ? import.meta.env.VITE_API_BASE_URL.replace(/\/api\/?$/, '')
    : '');

const SERVER_BASE_URL = rawBase || 'http://localhost:5167';

// Bộ nhớ cache ngắn hạn chống nhận/hiển thị toast lặp lại (deduplication)
const processedNotificationIds = new Set();

export function useSignalR() {
  const accessToken = useAuthStore((state) => state.accessToken);
  const addNotification = useNotificationStore((state) => state.addNotification);
  const fetchUnreadCount = useNotificationStore((state) => state.fetchUnreadCount);
  const queryClient = useQueryClient();
  const connectionRef = useRef(null);

  useEffect(() => {
    if (!accessToken) {
      if (connectionRef.current) {
        connectionRef.current.stop().catch(() => {});
        connectionRef.current = null;
      }
      return;
    }

    // Tải số thông báo chưa đọc khi đăng nhập / kết nối
    fetchUnreadCount();

    const hubUrl = `${SERVER_BASE_URL}/hubs/notifications`;

    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => useAuthStore.getState().accessToken || '',
        transport: HttpTransportType.WebSockets | HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Information)
      .build();

    connectionRef.current = connection;

    connection.on('ReceiveNotification', (notification) => {
      // 1. Kiểm tra deduplication theo notification ID
      if (notification?.id) {
        if (processedNotificationIds.has(notification.id)) {
          return;
        }
        processedNotificationIds.add(notification.id);
        // Tự động giải phóng ID sau 30 giây
        setTimeout(() => processedNotificationIds.delete(notification.id), 30000);
      }

      // 2. Thêm vào Store thông báo
      addNotification(notification);

      // 3. Tự động đồng bộ / làm mới cache React Query tương ứng
      if (notification.relatedEntity === 'Interview') {
        queryClient.invalidateQueries({ queryKey: ['interviews'] });
      } else if (notification.relatedEntity === 'Application') {
        queryClient.invalidateQueries({ queryKey: ['applications'] });
        queryClient.invalidateQueries({ queryKey: ['my-applications'] });
      } else if (notification.relatedEntity === 'Employee') {
        queryClient.invalidateQueries({ queryKey: ['employees'] });
      }

      // 4. Hiển thị thông báo Toast theo loại (chỉ 1 lần duy nhất)
      const type = notification.type?.toLowerCase();
      const title = notification.title || 'Thông báo mới';
      const formatted = formatNotificationMessage(notification);
      const msg = formatted ? `${title}: ${formatted}` : title;

      if (type === 'success') {
        toast.success(msg);
      } else if (type === 'warning' || type === 'actionrequired') {
        toast.warning(msg);
      } else {
        toast.info(msg);
      }
    });

    let isDisposed = false;

    const start = async () => {
      try {
        await connection.start();
        if (isDisposed) {
          await connection.stop();
          return;
        }
        const transportName =
          connection.connection?.transport?.name ||
          connection.connection?.transport?.constructor?.name ||
          'WebSockets';
        console.info(`[SignalR] Connected successfully via transport: ${transportName}`);
      } catch (err) {
        if (!isDisposed) {
          console.error('[SignalR] Connection error:', err);
        }
      }
    };

    start();

    return () => {
      isDisposed = true;
      connection.stop().catch(() => {});
      if (connectionRef.current === connection) {
        connectionRef.current = null;
      }
    };
  }, [accessToken, addNotification, fetchUnreadCount, queryClient]);
}
