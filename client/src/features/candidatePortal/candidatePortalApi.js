import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5167/api';

// Tạo axios instance riêng biệt cho portal ứng viên công khai
// KHÔNG gắn JWT Authorization header và KHÔNG redirect sang trang /login khi gặp lỗi
const candidateAxios = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 45000, // 45 giây cho upload file
});

/**
 * Lấy thông tin phiên phỏng vấn và danh sách câu hỏi
 * @param {string} inviteToken 
 * @returns {Promise<import('./types').CandidateInterviewSession>}
 */
export async function fetchCandidateSession(inviteToken) {
  const response = await candidateAxios.get(`/interviews/${inviteToken}`);
  return response.data;
}

/**
 * Xác nhận ứng viên đồng ý và bắt đầu buổi phỏng vấn (Idempotent)
 * @param {string} inviteToken 
 * @returns {Promise<{ inviteToken: string, status: string, consentedAt: string, message: string }>}
 */
export async function startCandidateInterview(inviteToken) {
  const response = await candidateAxios.post(`/interviews/${inviteToken}/start`);
  return response.data;
}

/**
 * Tải file ghi âm câu trả lời lên server kèm telemetry
 * @param {string} inviteToken 
 * @param {number} questionId 
 * @param {object} payload
 * @param {Blob} payload.audioBlob
 * @param {number} payload.durationSeconds
 * @param {number} payload.tabSwitchCount
 * @param {number} payload.outOfFocusDurationSeconds
 * @param {string[]} [payload.flags]
 * @param {(percent: number) => void} [onProgress]
 * @param {AbortSignal} [signal]
 */
export async function uploadAnswerAudio(
  inviteToken,
  questionId,
  { audioBlob, durationSeconds, tabSwitchCount = 0, outOfFocusDurationSeconds = 0, flags = [] },
  onProgress,
  signal
) {
  const formData = new FormData();
  
  // Xác định định dạng file từ blob type
  let fileName = 'answer.webm';
  if (audioBlob.type?.includes('mp4')) fileName = 'answer.mp4';
  else if (audioBlob.type?.includes('ogg')) fileName = 'answer.ogg';
  else if (audioBlob.type?.includes('wav')) fileName = 'answer.wav';

  formData.append('audioFile', audioBlob, fileName);
  formData.append('durationSeconds', Math.max(1, Math.round(durationSeconds)).toString());
  formData.append('tabSwitchCount', Math.round(tabSwitchCount).toString());
  formData.append('outOfFocusDurationSeconds', Number(outOfFocusDurationSeconds).toFixed(1));

  if (Array.isArray(flags) && flags.length > 0) {
    flags.forEach((flag) => {
      if (flag) formData.append('flags', flag);
    });
  }

  const response = await candidateAxios.post(
    `/interviews/${inviteToken}/answers/${questionId}`,
    formData,
    {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      signal,
      onUploadProgress: (progressEvent) => {
        if (progressEvent.total && onProgress) {
          const percent = Math.min(100, Math.round((progressEvent.loaded * 100) / progressEvent.total));
          onProgress(percent);
        }
      },
    }
  );

  return response.data;
}

/**
 * Polling kiểm tra trạng thái xử lý audio của câu hỏi
 * @param {string} inviteToken 
 * @param {number} questionId 
 */
export async function checkAnswerStatus(inviteToken, questionId) {
  const response = await candidateAxios.get(`/interviews/${inviteToken}/answers/${questionId}`);
  return response.data;
}
