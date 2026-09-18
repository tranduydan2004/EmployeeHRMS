import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5167/api';

export const useAuthStore = create(
  persist(
    (set, get) => ({
      accessToken: null,
      refreshToken: null,
      token: null, // Alias cho accessToken để tương thích ngược
      role: null,
      userId: null,
      userEmail: null,
      accessTokenExpiresAt: null,

      isAuthenticated: () => {
        const { accessToken, refreshToken, accessTokenExpiresAt } = get();
        if (!accessToken && !refreshToken) return false;
        // Nếu access token hết hạn nhưng còn refresh token, interceptor sẽ tự refresh
        if (accessTokenExpiresAt && new Date(accessTokenExpiresAt) <= new Date() && !refreshToken) {
          get().logout();
          return false;
        }
        return true;
      },

      setTokens: ({ accessToken, refreshToken, accessTokenExpiresAt, role, userId }) => {
        set({
          accessToken,
          refreshToken,
          token: accessToken,
          accessTokenExpiresAt,
          ...(role ? { role } : {}),
          ...(userId !== undefined ? { userId } : {}),
        });
      },

      login: async (email, password) => {
        const response = await axios.post(`${API_BASE_URL}/Auth/login`, {
          email,
          password,
        });

        const { accessToken, refreshToken, role, accessTokenExpiresAt, userId } = response.data;
        set({
          accessToken,
          refreshToken,
          token: accessToken,
          role,
          userId,
          userEmail: email,
          accessTokenExpiresAt,
        });
        return response.data;
      },

      register: async (email, password) => {
        const response = await axios.post(`${API_BASE_URL}/Auth/register`, {
          email,
          password,
        });
        return response.data;
      },

      logout: async () => {
        const { accessToken, refreshToken } = get();
        if (refreshToken) {
          try {
            await axios.post(
              `${API_BASE_URL}/Auth/revoke-token`,
              { refreshToken },
              {
                headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : {},
              }
            );
          } catch {
            // Best-effort revoke: bỏ qua lỗi mạng/auth khi đăng xuất
          }
        }

        set({
          accessToken: null,
          refreshToken: null,
          token: null,
          role: null,
          userId: null,
          userEmail: null,
          accessTokenExpiresAt: null,
        });
      },
    }),
    {
      name: 'hrms_auth_storage',
    }
  )
);
