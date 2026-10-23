import axiosClient from './axiosClient';

/**
 * Interview API Service
 * Quản lý toàn bộ API phỏng vấn, bao gồm:
 * - CRUD lịch phỏng vấn và câu hỏi
 * - Phase 4: Chỉnh sửa transcript, Re-evaluate và Audit Trail lịch sử chấm điểm
 */
export const interviewApi = {
  // --- Existing CRUD ---
  getAll: (params) => axiosClient.get('/Interviews', { params }),
  getById: (id) => axiosClient.get(`/Interviews/${id}`),
  getByApplication: (applicationId) => axiosClient.get(`/Interviews/by-application/${applicationId}`),
  create: (data) => axiosClient.post('/Interviews', data),
  complete: (id, summary) => axiosClient.patch(`/Interviews/${id}/complete`, { summary }),
  addQuestion: (interviewId, data) => axiosClient.post(`/Interviews/${interviewId}/questions`, data),
  updateQuestionAnswer: (interviewId, questionId, candidateAnswer) =>
    axiosClient.patch(`/Interviews/${interviewId}/questions/${questionId}/answer`, { candidateAnswer }),
  delete: (id) => axiosClient.delete(`/Interviews/${id}`),

  // --- Phase 4: AI Rubric Grading, Re-evaluation & Audit Trail ---
  /**
   * PUT /api/interviews/{id}/answers/{questionId}/transcript
   * HR / Interviewer chỉnh sửa bản bóc băng trước khi chấm lại
   */
  updateTranscript: (interviewId, questionId, transcript) =>
    axiosClient.put(`/Interviews/${interviewId}/answers/${questionId}/transcript`, { transcript }),

  /**
   * POST /api/interviews/{id}/answers/{questionId}/re-evaluate
   * Kích hoạt chấm điểm lại câu trả lời theo barem rubric
   */
  reevaluateAnswer: (interviewId, questionId) =>
    axiosClient.post(`/Interviews/${interviewId}/answers/${questionId}/re-evaluate`),

  /**
   * GET /api/interviews/{id}/evaluations
   * Lấy toàn bộ lịch sử chấm điểm của buổi phỏng vấn
   */
  getInterviewEvaluations: (interviewId) =>
    axiosClient.get(`/Interviews/${interviewId}/evaluations`),

  /**
   * GET /api/interviews/{id}/answers/{questionId}/evaluations
   * Lấy lịch sử các lần đánh giá rubric của riêng một câu hỏi
   */
  getQuestionEvaluations: (interviewId, questionId) =>
    axiosClient.get(`/Interviews/${interviewId}/answers/${questionId}/evaluations`),

  /**
   * GET /api/interviews/audio/{*storageKey}
   * Tải audio stream có xác thực JWT (responseType: blob)
   */
  getAudioBlob: async (storageKey) => {
    const res = await axiosClient.get(`/Interviews/audio/${storageKey}`, {
      responseType: 'blob',
    });
    return res.data;
  },
};

export default interviewApi;
