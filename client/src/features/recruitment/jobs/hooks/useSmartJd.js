import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import jobPostingApi from '@/api/jobPostingApi';
import { toast } from '@/components/Toast/useToastStore';
import { useSmartJdStore } from '../stores/useSmartJdStore';

export function useDraftJdMutation() {
  const queryClient = useQueryClient();
  const setGeneratedJob = useSmartJdStore((state) => state.setGeneratedJob);

  return useMutation({
    mutationFn: async (payload) => {
      const res = await jobPostingApi.draftJd(payload);
      return res.data;
    },
    onSuccess: (data) => {
      setGeneratedJob(data);
      queryClient.invalidateQueries({ queryKey: ['jobPostings'] });
      toast.success('AI đã sinh bản thảo JD thành công! Hãy xem và tinh chỉnh nội dung.');
    },
    onError: (err) => {
      const msg = err?.response?.data?.error || err?.message || 'Có lỗi xảy ra khi gọi AI sinh JD';
      toast.error(msg);
    },
  });
}

export function useUpdateJdContentMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, payload }) => {
      const res = await jobPostingApi.updateJdContent(id, payload);
      return res.data;
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['jobPostings'] });
      queryClient.invalidateQueries({ queryKey: ['jobPostings', data.id] });
      queryClient.invalidateQueries({ queryKey: ['jobPostingDetail', data.id] });
      toast.success('Cập nhật nội dung JD thành công!');
    },
    onError: (err) => {
      const msg = err?.response?.data?.error || err?.message || 'Không thể cập nhật nội dung JD';
      toast.error(msg);
    },
  });
}

export function useApproveJdMutation() {
  const queryClient = useQueryClient();
  const setApprovedJob = useSmartJdStore((state) => state.setApprovedJob);

  return useMutation({
    mutationFn: async (id) => {
      // 1. Gọi Approve (sinh Question Bank ở backend)
      const approveRes = await jobPostingApi.approveJd(id);
      const approvedJob = approveRes.data;

      // 2. Lấy danh sách câu hỏi đã sinh
      const questionsRes = await jobPostingApi.getQuestions(id);
      const questions = questionsRes.data;

      return { approvedJob, questions };
    },
    onSuccess: ({ approvedJob, questions }) => {
      setApprovedJob(approvedJob, questions);
      queryClient.invalidateQueries({ queryKey: ['jobPostings'] });
      queryClient.invalidateQueries({ queryKey: ['jobPostings', approvedJob.id] });
      queryClient.invalidateQueries({ queryKey: ['jobPostingDetail', approvedJob.id] });
      queryClient.invalidateQueries({ queryKey: ['jobQuestions', approvedJob.id] });
      toast.success('Phê duyệt JD & sinh Ngân hàng câu hỏi phỏng vấn thành công!');
    },
    onError: (err) => {
      const msg = err?.response?.data?.error || err?.message || 'Không thể phê duyệt tin tuyển dụng';
      toast.error(msg);
    },
  });
}

export function useJobPostingDetail(id) {
  return useQuery({
    queryKey: ['jobPostingDetail', id],
    queryFn: async () => {
      const res = await jobPostingApi.getDetail(id);
      return res.data;
    },
    enabled: !!id,
  });
}

export function useJobQuestions(id) {
  return useQuery({
    queryKey: ['jobQuestions', id],
    queryFn: async () => {
      const res = await jobPostingApi.getQuestions(id);
      return res.data;
    },
    enabled: !!id,
  });
}
