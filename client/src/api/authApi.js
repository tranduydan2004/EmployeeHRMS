import axios from 'axios';
import axiosClient from './axiosClient';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5167/api';

/**
 * POST /api/Auth/forgot-password
 * Dùng axiosClient (interceptor xử lý 429 rate-limit).
 * Backend luôn trả 200 OK (chống user enumeration).
 */
export const forgotPasswordApi = (email) =>
  axiosClient.post('/Auth/forgot-password', { email });

/**
 * POST /api/Auth/reset-password
 * Dùng raw axios (KHÔNG qua interceptor) để tránh:
 * - 401: interceptor logout + toast (nhưng ở đây chỉ là token invalid, KHÔNG phải auth session)
 * - 400: interceptor toast (nhưng user muốn INLINE error, không Toast)
 * ResetPasswordForm tự xử lý mọi error status.
 */
export const resetPasswordApi = ({ email, token, newPassword }) =>
  axios.post(`${API_BASE_URL}/Auth/reset-password`, { email, token, newPassword });

/**
 * POST /api/Auth/refresh-token
 */
export const refreshTokenApi = (refreshToken) =>
  axios.post(`${API_BASE_URL}/Auth/refresh-token`, { refreshToken });

/**
 * POST /api/Auth/revoke-token
 */
export const revokeTokenApi = (refreshToken) =>
  axiosClient.post('/Auth/revoke-token', { refreshToken });

/**
 * GET /api/Auth/verify-email?token=...
 * Dùng raw axios để tránh interceptor can thiệp
 */
export const verifyEmailApi = (token) =>
  axios.get(`${API_BASE_URL}/Auth/verify-email`, { params: { token } });

/**
 * POST /api/Auth/resend-verification
 */
export const resendVerificationApi = (email) =>
  axiosClient.post('/Auth/resend-verification', { email });
