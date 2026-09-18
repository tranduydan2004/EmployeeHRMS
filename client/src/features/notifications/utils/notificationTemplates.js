/**
 * Notification Template Definitions & Formatter.
 * Renders user-friendly messages from templateKey and payload, falling back to message.
 */

export const notificationTemplates = {
  APPLICATION_SUBMITTED: (p = {}) =>
    `Ứng viên ${p.candidateName || 'Ứng viên'} vừa nộp hồ sơ cho vị trí ${p.jobTitle || 'công việc'}.`,

  APPLICATION_STATUS_CHANGED: (p = {}) =>
    `Hồ sơ ứng tuyển của bạn cho vị trí ${p.jobTitle || 'công việc'} đã được cập nhật sang trạng thái: ${p.status || ''}.`,

  INTERVIEW_SCHEDULED_CANDIDATE: (p = {}) =>
    `Bạn có lịch phỏng vấn mới vào lúc ${p.scheduledDate || ''}.`,

  INTERVIEW_ASSIGNED_INTERVIEWER: (p = {}) =>
    `Bạn được phân công phỏng vấn ứng viên ${p.candidateName || 'Ứng viên'} vào lúc ${p.scheduledDate || ''}.`,

  INTERVIEW_COMPLETED: (p = {}) =>
    `Buổi phỏng vấn #${p.interviewId || ''} của ứng viên ${p.candidateName || 'Ứng viên'} đã được hoàn tất.`,

  INTERVIEW_CANCELLED_CANDIDATE: (p = {}) =>
    `Lịch phỏng vấn cho vị trí ${p.jobTitle || 'buổi phỏng vấn'} vào lúc ${p.scheduledDate || ''} đã bị hủy.`,

  INTERVIEW_CANCELLED_INTERVIEWER: (p = {}) =>
    `Lịch phỏng vấn ứng viên ${p.candidateName || 'Ứng viên'} vào lúc ${p.scheduledDate || ''} đã bị hủy.`,

  EMPLOYEE_STATUS_CHANGED: (p = {}) =>
    `Trạng thái nhân sự của bạn đã được cập nhật thành: ${p.status || ''}.`,
};

/**
 * Format notification message using templateKey and payload.
 * Fallback to notification.message if template is not found or fails.
 *
 * @param {Object} notification - Notification item from API or SignalR
 * @returns {string} Formatted notification message string
 */
export function formatNotificationMessage(notification) {
  if (!notification) return '';

  const { templateKey, payload, message } = notification;

  if (templateKey && typeof notificationTemplates[templateKey] === 'function') {
    try {
      return notificationTemplates[templateKey](payload || {});
    } catch {
      return message || '';
    }
  }

  return message || '';
}
