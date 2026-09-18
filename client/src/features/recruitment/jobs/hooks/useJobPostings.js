import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import axiosClient from '@/api/axiosClient';
import { toast } from '@/components/Toast/useToastStore';

export function useJobPostings(queryParams = {}) {
  const {
    pageNumber = 1,
    pageSize = 10,
    sortBy,
    isDescending,
    search,
    departmentId,
    status,
  } = queryParams;

  return useQuery({
    queryKey: [
      'jobPostings',
      { pageNumber, pageSize, sortBy, isDescending, search, departmentId, status },
    ],
    queryFn: async () => {
      const params = new URLSearchParams();
      params.set('pageNumber', pageNumber.toString());
      params.set('pageSize', pageSize.toString());
      if (sortBy) params.set('sortBy', sortBy);
      if (isDescending !== undefined) params.set('isDescending', isDescending.toString());
      if (search) params.set('search', search);
      if (departmentId) params.set('departmentId', departmentId.toString());
      if (status) params.set('status', status);

      const res = await axiosClient.get(`/JobPostings?${params.toString()}`);
      return res.data; // PagedResult<JobPostingResponseDto>
    },
    placeholderData: (prev) => prev,
  });
}

export function useActiveJobPostings() {
  return useQuery({
    queryKey: ['jobPostings', 'active'],
    queryFn: async () => {
      const res = await axiosClient.get('/JobPostings/active');
      return res.data;
    },
  });
}

export function useJobPosting(id) {
  return useQuery({
    queryKey: ['jobPostings', id],
    queryFn: async () => {
      const res = await axiosClient.get(`/JobPostings/${id}`);
      return res.data;
    },
    enabled: !!id,
  });
}

export function useJobPostingMutations() {
  const queryClient = useQueryClient();

  const createMutation = useMutation({
    mutationFn: async (payload) => {
      const res = await axiosClient.post('/JobPostings', payload);
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['jobPostings'] });
      queryClient.invalidateQueries({ queryKey: ['jobpostings'] });
      toast.success('Đăng tin tuyển dụng thành công!');
    },
  });

  const updateMutation = useMutation({
    mutationFn: async ({ id, payload }) => {
      const res = await axiosClient.put(`/JobPostings/${id}`, payload);
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['jobPostings'] });
      queryClient.invalidateQueries({ queryKey: ['jobpostings'] });
      toast.success('Cập nhật tin tuyển dụng thành công!');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: async (id) => {
      await axiosClient.delete(`/JobPostings/${id}`);
      return id;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['jobPostings'] });
      queryClient.invalidateQueries({ queryKey: ['jobpostings'] });
      toast.success('Đã xóa tin tuyển dụng!');
    },
  });

  return {
    createJobPosting: createMutation.mutateAsync,
    updateJobPosting: updateMutation.mutateAsync,
    deleteJobPosting: deleteMutation.mutateAsync,
    isCreating: createMutation.isPending,
    isUpdating: updateMutation.isPending,
    isDeleting: deleteMutation.isPending,
  };
}
