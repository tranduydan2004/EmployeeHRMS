import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import axiosClient from '@/api/axiosClient';
import { toast } from '@/components/Toast/useToastStore';

export function useDepartments(queryParams = {}, options = {}) {
  const { pageNumber = 1, pageSize = 50, sortBy, isDescending, search } = queryParams;

  return useQuery({
    queryKey: ['departments', { pageNumber, pageSize, sortBy, isDescending, search }],
    queryFn: async () => {
      const params = new URLSearchParams();
      params.set('pageNumber', pageNumber.toString());
      params.set('pageSize', pageSize.toString());
      if (sortBy) params.set('sortBy', sortBy);
      if (isDescending !== undefined) params.set('isDescending', isDescending.toString());
      if (search) params.set('search', search);

      const res = await axiosClient.get(`/Departments?${params.toString()}`);
      return res.data; // PagedResult<DepartmentResponseDto> hoặc PagedResult<DepartmentPublicDto>
    },
    placeholderData: (prev) => prev,
    ...options,
  });
}

export function useDepartmentMutations() {
  const queryClient = useQueryClient();

  const createMutation = useMutation({
    mutationFn: async (payload) => {
      const res = await axiosClient.post('/Departments', payload);
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['departments'] });
      toast.success('Thêm phòng ban thành công!');
    },
  });

  const updateMutation = useMutation({
    mutationFn: async ({ id, payload }) => {
      const res = await axiosClient.put(`/Departments/${id}`, payload);
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['departments'] });
      toast.success('Cập nhật thông tin phòng ban thành công!');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: async (id) => {
      await axiosClient.delete(`/Departments/${id}`);
      return id;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['departments'] });
      toast.success('Đã xóa phòng ban!');
    },
  });

  return {
    createDepartment: createMutation.mutateAsync,
    updateDepartment: updateMutation.mutateAsync,
    deleteDepartment: deleteMutation.mutateAsync,
    isCreating: createMutation.isPending,
    isUpdating: updateMutation.isPending,
    isDeleting: deleteMutation.isPending,
  };
}
