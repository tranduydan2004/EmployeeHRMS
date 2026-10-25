import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { interviewApi } from '@/api/interviewApi';
import axiosClient from '@/api/axiosClient';
import { toast } from '@/components/Toast/useToastStore';

export function useInterviews(queryParams = {}) {
  const { pageNumber = 1, pageSize = 10, sortBy, isDescending, search } = queryParams;

  return useQuery({
    queryKey: ['interviews', { pageNumber, pageSize, sortBy, isDescending, search }],
    queryFn: async () => {
      const params = new URLSearchParams();
      params.set('pageNumber', pageNumber.toString());
      params.set('pageSize', pageSize.toString());
      if (sortBy) params.set('sortBy', sortBy);
      if (isDescending !== undefined) params.set('isDescending', isDescending.toString());
      if (search) params.set('search', search);

      const res = await axiosClient.get(`/Interviews?${params.toString()}`);
      return res.data; // PagedResult<InterviewResponseDto>
    },
    placeholderData: (prev) => prev,
  });
}

export function useInterview(id) {
  return useQuery({
    queryKey: ['interviews', id],
    queryFn: async () => {
      const res = await interviewApi.getById(id);
      return res.data;
    },
    enabled: !!id,
  });
}

export function useQuestionEvaluations(interviewId, questionId) {
  return useQuery({
    queryKey: ['interviews', interviewId, 'questions', questionId, 'evaluations'],
    queryFn: async () => {
      const res = await interviewApi.getQuestionEvaluations(interviewId, questionId);
      return res.data;
    },
    enabled: !!interviewId && !!questionId,
  });
}

export function useInterviewEvaluations(interviewId) {
  return useQuery({
    queryKey: ['interviews', interviewId, 'evaluations'],
    queryFn: async () => {
      const res = await interviewApi.getInterviewEvaluations(interviewId);
      return res.data;
    },
    enabled: !!interviewId,
  });
}

// --- Phase 5: Dashboard List View & Governance Hooks ---

export function useInterviewDashboardSummary(filters = {}) {
  return useQuery({
    queryKey: ['interviews', 'dashboard-summary', filters],
    queryFn: async () => {
      const res = await interviewApi.getDashboardSummary(filters);
      return res.data; // PagedResult<InterviewDashboardSummaryDto>
    },
    placeholderData: (prev) => prev,
  });
}

export function useInterviewDashboardStats() {
  return useQuery({
    queryKey: ['interviews', 'dashboard-stats'],
    queryFn: async () => {
      const res = await interviewApi.getDashboardStats();
      return res.data; // InterviewDashboardStatsDto
    },
  });
}

export function useAvailableInterviewers() {
  return useQuery({
    queryKey: ['interviews', 'available-interviewers'],
    queryFn: async () => {
      const res = await interviewApi.getAvailableInterviewers();
      return res.data; // List<InterviewerOptionDto>
    },
  });
}

export function useScreeningDecisions(interviewId) {
  return useQuery({
    queryKey: ['interviews', interviewId, 'screening-decisions'],
    queryFn: async () => {
      const res = await interviewApi.getScreeningDecisions(interviewId);
      return res.data;
    },
    enabled: !!interviewId,
  });
}

export function useInterviewMutations() {
  const queryClient = useQueryClient();

  const createInterviewMutation = useMutation({
    mutationFn: async (payload) => {
      const res = await interviewApi.create(payload);
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['interviews'] });
      toast.success('Lên lịch phỏng vấn thành công!');
    },
  });

  const addQuestionMutation = useMutation({
    mutationFn: async ({ interviewId, payload }) => {
      const res = await interviewApi.addQuestion(interviewId, payload);
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['interviews'] });
      toast.success('Thêm câu hỏi đánh giá thành công!');
    },
  });

  const deleteInterviewMutation = useMutation({
    mutationFn: async (id) => {
      await interviewApi.delete(id);
      return id;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['interviews'] });
      toast.success('Đã hủy lịch phỏng vấn!');
    },
  });

  const updateQuestionAnswerMutation = useMutation({
    mutationFn: async ({ interviewId, questionId, candidateAnswer }) => {
      const res = await interviewApi.updateQuestionAnswer(interviewId, questionId, candidateAnswer);
      return res.data;
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['interviews'] });
      if (variables?.interviewId) {
        queryClient.invalidateQueries({ queryKey: ['interviews', variables.interviewId] });
      }
      toast.success('Cập nhật câu trả lời thành công!');
    },
  });

  const completeInterviewMutation = useMutation({
    mutationFn: async ({ id, summary }) => {
      const res = await interviewApi.complete(id, summary);
      return res.data;
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['interviews'] });
      if (variables?.id) {
        queryClient.invalidateQueries({ queryKey: ['interviews', variables.id] });
      }
      toast.success('Đã kết thúc buổi phỏng vấn thành công!');
    },
  });

  // --- Phase 4: Chỉnh sửa transcript ---
  const updateTranscriptMutation = useMutation({
    mutationFn: async ({ interviewId, questionId, transcript }) => {
      const res = await interviewApi.updateTranscript(interviewId, questionId, transcript);
      return res.data;
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['interviews'] });
      if (variables?.interviewId) {
        queryClient.invalidateQueries({ queryKey: ['interviews', variables.interviewId] });
        queryClient.invalidateQueries({
          queryKey: ['interviews', variables.interviewId, 'questions', variables.questionId, 'evaluations'],
        });
      }
      toast.success('Đã lưu bản bóc băng thành công!');
    },
    onError: (err) => {
      const msg = err.response?.data?.error || 'Không thể cập nhật bản bóc băng.';
      toast.error(msg);
    },
  });

  // --- Phase 4: Re-evaluate chấm điểm lại ---
  const reevaluateMutation = useMutation({
    mutationFn: async ({ interviewId, questionId }) => {
      const res = await interviewApi.reevaluateAnswer(interviewId, questionId);
      return res.data;
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['interviews'] });
      if (variables?.interviewId) {
        queryClient.invalidateQueries({ queryKey: ['interviews', variables.interviewId] });
        queryClient.invalidateQueries({
          queryKey: ['interviews', variables.interviewId, 'questions', variables.questionId, 'evaluations'],
        });
      }
      toast.success('Tác vụ chấm lại AI đã được đưa vào hàng đợi!');
    },
    onError: (err) => {
      const msg = err.response?.data?.error || 'Không thể kích hoạt chấm lại AI.';
      toast.error(msg);
    },
  });

  // --- Phase 5: Phân công Interviewer ---
  const assignInterviewerMutation = useMutation({
    mutationFn: async ({ interviewId, interviewerId }) => {
      const res = await interviewApi.assignInterviewer(interviewId, interviewerId);
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['interviews'] });
      toast.success('Phân công người phỏng vấn thành công!');
    },
    onError: (err) => {
      const msg = err.response?.data?.error || 'Không thể phân công người phỏng vấn.';
      toast.error(msg);
    },
  });

  // --- Phase 5: Quyết định sơ loại bất biến ---
  const createScreeningDecisionMutation = useMutation({
    mutationFn: async ({ interviewId, payload }) => {
      const res = await interviewApi.createScreeningDecision(interviewId, payload);
      return res.data;
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['interviews'] });
      queryClient.invalidateQueries({ queryKey: ['applications'] });
      if (variables?.interviewId) {
        queryClient.invalidateQueries({ queryKey: ['interviews', variables.interviewId, 'screening-decisions'] });
      }
      toast.success('Đã ghi nhận quyết định sơ loại thành công!');
    },
    onError: (err) => {
      const msg = err.response?.data?.error || 'Không thể ghi nhận quyết định sơ loại.';
      toast.error(msg);
    },
  });

  // --- Phase 5 - Step 6: Xóa dữ liệu cá nhân theo quyền riêng tư (Right to Erasure) ---
  const erasePersonalDataMutation = useMutation({
    mutationFn: async ({ interviewId, reason }) => {
      const res = await interviewApi.erasePersonalData(interviewId, { reason });
      return res.data;
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['interviews'] });
      if (variables?.interviewId) {
        queryClient.invalidateQueries({ queryKey: ['interviews', variables.interviewId] });
      }
      toast.success('Đã xóa dữ liệu cá nhân an toàn theo chính sách bảo mật!');
    },
    onError: (err) => {
      const msg = err.response?.data?.error || 'Không thể xóa dữ liệu cá nhân.';
      toast.error(msg);
    },
  });

  // --- Phase 2: Mời phỏng vấn Audio sơ loại & Gửi lại lời mời ---
  const createInterviewInviteMutation = useMutation({
    mutationFn: async ({ applicationId, payload }) => {
      const res = await interviewApi.createInvite(applicationId, payload);
      return res.data;
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['interviews'] });
      queryClient.invalidateQueries({ queryKey: ['applications'] });
      if (data.emailSent) {
        toast.success('Đã tạo phiên phỏng vấn và gửi email mời thành công!');
      } else {
        toast.warning(data.emailMessage || 'Đã tạo phiên phỏng vấn (chưa gửi email do chưa cấu hình SMTP).');
      }
    },
    onError: (err) => {
      const msg = err.response?.data?.error || 'Không thể tạo lời mời phỏng vấn audio.';
      toast.error(msg);
    },
  });

  const resendInviteMutation = useMutation({
    mutationFn: async (interviewId) => {
      const res = await interviewApi.resendInvite(interviewId);
      return res.data;
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['interviews'] });
      if (data.emailSent) {
        toast.success('Đã gửi lại email lời mời phỏng vấn thành công!');
      } else {
        toast.warning(data.message || 'Không thể gửi email qua SMTP. Hãy sao chép liên kết trực tiếp.');
      }
    },
    onError: (err) => {
      const msg = err.response?.data?.error || 'Không thể gửi lại email lời mời.';
      toast.error(msg);
    },
  });

  return {
    createInterview: createInterviewMutation.mutateAsync,
    createInterviewInvite: createInterviewInviteMutation.mutateAsync,
    resendInvite: resendInviteMutation.mutateAsync,
    addQuestion: addQuestionMutation.mutateAsync,
    updateQuestionAnswer: updateQuestionAnswerMutation.mutateAsync,
    deleteInterview: deleteInterviewMutation.mutateAsync,
    completeInterview: completeInterviewMutation.mutateAsync,
    updateTranscript: updateTranscriptMutation.mutateAsync,
    reevaluateAnswer: reevaluateMutation.mutateAsync,
    assignInterviewer: assignInterviewerMutation.mutateAsync,
    createScreeningDecision: createScreeningDecisionMutation.mutateAsync,
    erasePersonalData: erasePersonalDataMutation.mutateAsync,
    isCreating: createInterviewMutation.isPending,
    isCreatingInvite: createInterviewInviteMutation.isPending,
    isResendingInvite: resendInviteMutation.isPending,
    isAddingQuestion: addQuestionMutation.isPending,
    isUpdatingAnswer: updateQuestionAnswerMutation.isPending,
    isDeleting: deleteInterviewMutation.isPending,
    isCompleting: completeInterviewMutation.isPending,
    isUpdatingTranscript: updateTranscriptMutation.isPending,
    isReevaluating: reevaluateMutation.isPending,
    isAssigning: assignInterviewerMutation.isPending,
    isDeciding: createScreeningDecisionMutation.isPending,
    isErasingPersonalData: erasePersonalDataMutation.isPending,
  };
}
