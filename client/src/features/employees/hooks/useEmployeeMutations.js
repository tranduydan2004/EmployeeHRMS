import { useMutation, useQueryClient } from '@tanstack/react-query';
import axiosClient from '@/api/axiosClient';
import { changeEmployeeStatusApi } from '@/api/employeeApi';
import { toast } from '@/components/Toast/useToastStore';

export function useChangeEmployeeStatus() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, status }) => {
      return await changeEmployeeStatusApi(id, status);
    },
    onSuccess: () => {
      // Dùng prefix matching mặc định của TanStack Query — KHÔNG truyền exact: true, KHÔNG params chi tiết
      queryClient.invalidateQueries({ queryKey: ['employees'] });
      toast.success('Cập nhật trạng thái nhân viên thành công!');
    },
  });
}

export function useEmployeeMutations() {
  const queryClient = useQueryClient();
  const changeStatusMutation = useChangeEmployeeStatus();

  const createMutation = useMutation({
    mutationFn: async (payload) => {
      const res = await axiosClient.post('/Employees', payload);
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['employees'] });
      toast.success('Thêm mới nhân viên thành công!');
    },
  });

  const updateMutation = useMutation({
    mutationFn: async ({ id, payload }) => {
      const res = await axiosClient.put(`/Employees/${id}`, payload);
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['employees'] });
      toast.success('Cập nhật nhân viên thành công!');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: async (id) => {
      await axiosClient.delete(`/Employees/${id}`);
      return id;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['employees'] });
      toast.success('Đã xóa nhân viên!');
    },
  });

  return {
    createEmployee: createMutation.mutateAsync,
    updateEmployee: updateMutation.mutateAsync,
    deleteEmployee: deleteMutation.mutateAsync,
    updateEmployeeStatus: changeStatusMutation.mutateAsync,
    changeEmployeeStatus: changeStatusMutation.mutateAsync,
    isCreating: createMutation.isPending,
    isUpdating: updateMutation.isPending,
    isDeleting: deleteMutation.isPending,
    isUpdatingStatus: changeStatusMutation.isPending,
  };
}
