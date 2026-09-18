import axios from 'axios';
import { useAuthStore } from '../features/auth/useAuthStore';
import { toast } from '../components/Toast/useToastStore';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5167/api';

const axiosClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Hàng đợi quản lý các request bị 401 chờ refresh hoàn thành (chống race condition)
let isRefreshing = false;
let failedQueue = [];

const processQueue = (error, token = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

// Request Interceptor: Gắn Bearer Token từ Zustand store
axiosClient.interceptors.request.use(
  (config) => {
    const accessToken = useAuthStore.getState().accessToken;
    if (accessToken) {
      config.headers.Authorization = `Bearer ${accessToken}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response Interceptor: Xử lý riêng biệt từng HTTP error code + Token Rotation
axiosClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    const status = error.response?.status;
    const backendErrorMessage = error.response?.data?.error;

    // Bỏ qua cơ chế auto-refresh nếu request là các auth endpoint cơ bản hoặc đã retry
    const isAuthEndpoint =
      originalRequest?.url?.includes('/Auth/login') ||
      originalRequest?.url?.includes('/Auth/register') ||
      originalRequest?.url?.includes('/Auth/verify-email') ||
      originalRequest?.url?.includes('/Auth/resend-verification') ||
      originalRequest?.url?.includes('/Auth/refresh-token');

    if (status === 401 && !originalRequest?._retry && !isAuthEndpoint) {
      if (isRefreshing) {
        // Nếu đang có 1 request thực hiện refresh, xếp các request sau vào queue chờ
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            originalRequest.headers.Authorization = `Bearer ${token}`;
            return axiosClient(originalRequest);
          })
          .catch((err) => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const currentRefreshToken = useAuthStore.getState().refreshToken;

      if (!currentRefreshToken) {
        processQueue(error, null);
        isRefreshing = false;
        useAuthStore.getState().logout();
        toast.error(backendErrorMessage || 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
        if (window.location.pathname !== '/login' && window.location.pathname !== '/register') {
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }

      try {
        // Dùng raw axios instance để tránh trigger response interceptor của axiosClient
        const response = await axios.post(`${API_BASE_URL}/Auth/refresh-token`, {
          refreshToken: currentRefreshToken,
        });

        const { accessToken, refreshToken: newRefreshToken, accessTokenExpiresAt, role } = response.data;

        useAuthStore.getState().setTokens({
          accessToken,
          refreshToken: newRefreshToken,
          accessTokenExpiresAt,
          role,
        });

        processQueue(null, accessToken);

        originalRequest.headers.Authorization = `Bearer ${accessToken}`;
        return axiosClient(originalRequest);
      } catch (refreshError) {
        processQueue(refreshError, null);
        useAuthStore.getState().logout();
        const refreshErrMsg =
          refreshError.response?.data?.error ||
          'Phiên đăng nhập đã hết hạn hoặc bị thu hồi. Vui lòng đăng nhập lại.';
        toast.error(refreshErrMsg);
        if (window.location.pathname !== '/login' && window.location.pathname !== '/register') {
          window.location.href = '/login';
        }
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    } else if (status === 401 && isAuthEndpoint) {
      // 401 từ login/register: hiển thị thông báo lỗi từ backend
      toast.error(backendErrorMessage || 'Thông tin xác thực không hợp lệ.');
    } else if (status === 403) {
      // Đúng token nhưng không có quyền trên resource
      toast.error(backendErrorMessage || 'Bạn không có quyền truy cập tài nguyên này (403 Forbidden).');
    } else if (status === 429) {
      // Rate limit quá số lần quy định
      toast.warning(backendErrorMessage || 'Quá nhiều yêu cầu. Vui lòng thử lại sau ít phút.');
    } else if (status === 400 || status === 409) {
      // Lỗi validation hoặc vi phạm business rule / state machine
      toast.error(backendErrorMessage || 'Dữ liệu hoặc thao tác không hợp lệ.');
    } else if (status >= 500) {
      toast.error(backendErrorMessage || 'Lỗi hệ thống máy chủ (500). Vui lòng thử lại sau.');
    } else if (!error.response) {
      toast.error('Không thể kết nối đến máy chủ API. Vui lòng kiểm tra mạng hoặc backend.');
    }

    return Promise.reject(error);
  }
);

export default axiosClient;
