import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
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
      const res = await axiosClient.get(`/Interviews/${id}`);
      return res.data;
    },
    enabled: !!id,
  });
}

export function useInterviewMutations() {
  const queryClient = useQueryClient();

  const createInterviewMutation = useMutation({
    mutationFn: async (payload) => {
      const res = await axiosClient.post('/Interviews', payload);
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['interviews'] });
      toast.success('Lên lịch phỏng vấn thành công!');
    },
  });

  const addQuestionMutation = useMutation({
    mutationFn: async ({ interviewId, payload }) => {
      const res = await axiosClient.post(`/Interviews/${interviewId}/questions`, payload);
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['interviews'] });
      toast.success('Thêm câu hỏi đánh giá thành công!');
    },
  });

  const deleteInterviewMutation = useMutation({
    mutationFn: async (id) => {
      await axiosClient.delete(`/Interviews/${id}`);
      return id;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['interviews'] });
      toast.success('Đã hủy lịch phỏng vấn!');
    },
  });

  const updateQuestionAnswerMutation = useMutation({
    mutationFn: async ({ interviewId, questionId, candidateAnswer }) => {
      const res = await axiosClient.patch(`/Interviews/${interviewId}/questions/${questionId}/answer`, {
        candidateAnswer,
      });
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

  return {
    createInterview: createInterviewMutation.mutateAsync,
    addQuestion: addQuestionMutation.mutateAsync,
    updateQuestionAnswer: updateQuestionAnswerMutation.mutateAsync,
    deleteInterview: deleteInterviewMutation.mutateAsync,
    isCreating: createInterviewMutation.isPending,
    isAddingQuestion: addQuestionMutation.isPending,
    isUpdatingAnswer: updateQuestionAnswerMutation.isPending,
    isDeleting: deleteInterviewMutation.isPending,
  };
}
