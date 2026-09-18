import axiosClient from './axiosClient';

/**
 * GET /api/Notifications?page=1&pageSize=20&unreadOnly=false
 */
export const getNotificationsApi = (params) =>
  axiosClient.get('/Notifications', { params });

/**
 * GET /api/Notifications/unread-count
 */
export const getUnreadCountApi = () =>
  axiosClient.get('/Notifications/unread-count');

/**
 * PATCH /api/Notifications/{id}/read
 */
export const markAsReadApi = (id) =>
  axiosClient.patch(`/Notifications/${id}/read`);

/**
 * PATCH /api/Notifications/read-all
 */
export const markAllAsReadApi = () =>
  axiosClient.patch('/Notifications/read-all');
