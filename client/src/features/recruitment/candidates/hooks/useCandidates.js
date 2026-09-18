import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import axiosClient from '@/api/axiosClient';
import { toast } from '@/components/Toast/useToastStore';

export function useCandidates(queryParams = {}) {
  const { pageNumber = 1, pageSize = 10, sortBy, isDescending, search } = queryParams;

  return useQuery({
    queryKey: ['candidates', { pageNumber, pageSize, sortBy, isDescending, search }],
    queryFn: async () => {
      const params = new URLSearchParams();
      params.set('pageNumber', pageNumber.toString());
      params.set('pageSize', pageSize.toString());
      if (sortBy) params.set('sortBy', sortBy);
      if (isDescending !== undefined) params.set('isDescending', isDescending.toString());
      if (search) params.set('search', search);

      const res = await axiosClient.get(`/Candidates?${params.toString()}`);
      return res.data; // PagedResult<CandidateResponseDto>
    },
    placeholderData: (prev) => prev,
  });
}

export function useCandidate(id) {
  return useQuery({
    queryKey: ['candidates', id],
    queryFn: async () => {
      const res = await axiosClient.get(`/Candidates/${id}`);
      return res.data;
    },
    enabled: !!id,
  });
}

export function useCandidateMutations() {
  const queryClient = useQueryClient();

  const createMutation = useMutation({
    mutationFn: async (payload) => {
      const res = await axiosClient.post('/Candidates', payload);
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['candidates'] });
      toast.success('Tạo hồ sơ ứng viên thành công!');
    },
  });

  const updateMutation = useMutation({
    mutationFn: async ({ id, payload }) => {
      const res = await axiosClient.put(`/Candidates/${id}`, payload);
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['candidates'] });
      toast.success('Cập nhật hồ sơ ứng viên thành công!');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: async (id) => {
      await axiosClient.delete(`/Candidates/${id}`);
      return id;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['candidates'] });
      toast.success('Đã xóa hồ sơ ứng viên!');
    },
  });

  return {
    createCandidate: createMutation.mutateAsync,
    updateCandidate: updateMutation.mutateAsync,
    deleteCandidate: deleteMutation.mutateAsync,
    isCreating: createMutation.isPending,
    isUpdating: updateMutation.isPending,
    isDeleting: deleteMutation.isPending,
  };
}
