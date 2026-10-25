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

  // --- Phase 2: Candidate Audio Interview & Link Sharing ---
  createInvite: (applicationId, data) =>
    axiosClient.post(`/Applications/${applicationId}/interview-invite`, data),
  resendInvite: (interviewId) =>
    axiosClient.post(`/Interviews/${interviewId}/resend-invite`),

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

  // --- Phase 5: HR/Interviewer Dashboard & Governance ---
  /**
   * GET /api/interviews/dashboard-summary
   * Danh sách phỏng vấn sơ loại tối ưu hóa cho Dashboard (không tải transcript/jsonb nặng)
   */
  getDashboardSummary: (params) => axiosClient.get('/Interviews/dashboard-summary', { params }),

  /**
   * GET /api/interviews/dashboard-stats
   * Thống kê nhanh các chỉ số KPI cho Dashboard (Executive Summary)
   */
  getDashboardStats: () => axiosClient.get('/Interviews/dashboard-stats'),

  /**
   * GET /api/interviews/available-interviewers
   * Danh sách người phỏng vấn khả dụng để phân công (Admin, HR)
   */
  getAvailableInterviewers: () => axiosClient.get('/Interviews/available-interviewers'),

  /**
   * PATCH /api/interviews/{id}/assign-interviewer
   * Phân công Interviewer phụ trách buổi phỏng vấn (Admin, HR)
   */
  assignInterviewer: (interviewId, interviewerId) =>
    axiosClient.patch(`/Interviews/${interviewId}/assign-interviewer`, { interviewerId }),

  /**
   * POST /api/interviews/{id}/screening-decision
   * Ghi nhận quyết định sơ loại bất biến kèm snapshot bằng chứng và lý do ghi đè (Admin, HR)
   */
  createScreeningDecision: (interviewId, data) =>
    axiosClient.post(`/Interviews/${interviewId}/screening-decision`, data),

  /**
   * GET /api/interviews/{id}/screening-decisions
   * Lịch sử các quyết định sơ loại của buổi phỏng vấn (Admin, HR, Interviewer)
   */
  getScreeningDecisions: (interviewId) =>
    axiosClient.get(`/Interviews/${interviewId}/screening-decisions`),

  /**
   * DELETE /api/interviews/{id}/personal-data
   * Xóa vĩnh viễn file audio, transcript, trích dẫn bằng chứng và phân tích AI (Admin, HR)
   */
  erasePersonalData: (interviewId, data) =>
    axiosClient.delete(`/Interviews/${interviewId}/personal-data`, { data }),
};

export default interviewApi;
