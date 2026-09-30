import axiosClient from './axiosClient';

/**
 * JobPosting API Service
 * Bao gồm các endpoint CRUD chuẩn và 5 endpoint Phase 1 (Smart JD & Question Bank)
 */
export const jobPostingApi = {
  // --- Existing CRUD ---
  getAll: (params) => axiosClient.get('/JobPostings', { params }),
  getById: (id) => axiosClient.get(`/JobPostings/${id}`),
  getActive: () => axiosClient.get('/JobPostings/active'),
  getByDepartment: (departmentId) => axiosClient.get(`/JobPostings/by-department/${departmentId}`),
  create: (data) => axiosClient.post('/JobPostings', data),
  update: (id, data) => axiosClient.put(`/JobPostings/${id}`, data),
  delete: (id) => axiosClient.delete(`/JobPostings/${id}`),

  // --- Phase 1: Smart JD & Question Bank Generation ---
  /**
   * POST /api/JobPostings/draft-jd
   * Gửi tham số cấu trúc, gọi OpenAI sinh bản thảo JD và tạo JobPosting (Draft)
   */
  draftJd: (payload) => axiosClient.post('/JobPostings/draft-jd', payload),

  /**
   * PUT /api/JobPostings/{id}/jd-content
   * HR cập nhật nội dung JD khi job đang ở trạng thái Draft
   */
  updateJdContent: (id, payload) => axiosClient.put(`/JobPostings/${id}/jd-content`, payload),

  /**
   * POST /api/JobPostings/{id}/approve
   * HR duyệt JD: chuyển sang Approved và tự động gọi OpenAI sinh 3-5 câu hỏi tình huống kèm ScoringRubric
   */
  approveJd: (id) => axiosClient.post(`/JobPostings/${id}/approve`),

  /**
   * GET /api/JobPostings/{id}/detail
   * Lấy chi tiết đầy đủ của JobPosting gồm tham số cấu trúc, JdContent, QuestionCount...
   */
  getDetail: (id) => axiosClient.get(`/JobPostings/${id}/detail`),

  /**
   * GET /api/JobPostings/{id}/questions
   * Lấy danh sách câu hỏi phỏng vấn trong Question Bank kèm ScoringRubric 4 mức
   */
  getQuestions: (id) => axiosClient.get(`/JobPostings/${id}/questions`),
};

export default jobPostingApi;
