import { useMutation, useQueryClient } from '@tanstack/react-query';
import axiosClient from '@/api/axiosClient';
import { toast } from '@/components/Toast/useToastStore';

export function useCreateApplication(options = {}) {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: async (payload) => {
      const res = await axiosClient.post('/Applications', payload);
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['applications'] });
      queryClient.invalidateQueries({ queryKey: ['candidates'] });
      if (options.showToast !== false) {
        toast.success(options.successMessage || 'Nộp đơn ứng tuyển thành công!');
      }
    },
  });

  return {
    createApplication: mutation.mutateAsync,
    isCreating: mutation.isPending,
  };
}

export function useUpdateApplicationStatus() {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: async ({ id, status }) => {
      const res = await axiosClient.patch(`/Applications/${id}/status`, { status });
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['applications'] });
      toast.success('Chuyển đổi trạng thái đơn ứng tuyển thành công!');
    },
  });

  return {
    updateStatus: mutation.mutateAsync,
    isUpdatingStatus: mutation.isPending,
  };
}

export function useDeleteApplication() {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: async (id) => {
      await axiosClient.delete(`/Applications/${id}`);
      return id;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['applications'] });
      toast.success('Đã xóa đơn ứng tuyển!');
    },
  });

  return {
    deleteApplication: mutation.mutateAsync,
    isDeleting: mutation.isPending,
  };
}

export function useApplicationMutations(options = {}) {
  const { createApplication, isCreating } = useCreateApplication(options);
  const { updateStatus, isUpdatingStatus } = useUpdateApplicationStatus();
  const { deleteApplication, isDeleting } = useDeleteApplication();

  return {
    createApplication,
    updateStatus,
    deleteApplication,
    isCreating,
    isUpdatingStatus,
    isDeleting,
  };
}
