import { useQuery } from '@tanstack/react-query';
import axiosClient from '@/api/axiosClient';

export function useEmployees(queryParams = {}) {
  const { pageNumber = 1, pageSize = 10, sortBy, isDescending, search } = queryParams;

  return useQuery({
    queryKey: ['employees', { pageNumber, pageSize, sortBy, isDescending, search }],
    queryFn: async () => {
      const params = new URLSearchParams();
      params.set('pageNumber', pageNumber.toString());
      params.set('pageSize', pageSize.toString());
      if (sortBy) params.set('sortBy', sortBy);
      if (isDescending !== undefined) params.set('isDescending', isDescending.toString());
      if (search) params.set('search', search);

      const res = await axiosClient.get(`/Employees?${params.toString()}`);
      return res.data; // PagedResult<EmployeeResponseDto>
    },
    placeholderData: (previousData) => previousData,
  });
}

export function useEmployeeStatistics() {
  return useQuery({
    queryKey: ['employees', 'statistics'],
    queryFn: async () => {
      const res = await axiosClient.get('/Employees/statistics');
      return res.data;
    },
  });
}

export function useMyProfile() {
  return useQuery({
    queryKey: ['employees', 'me'],
    queryFn: async () => {
      const res = await axiosClient.get('/Employees/me');
      return res.data;
    },
  });
}

export function useMyDepartmentColleagues() {
  return useQuery({
    queryKey: ['employees', 'my-department'],
    queryFn: async () => {
      const res = await axiosClient.get('/Employees/my-department');
      return res.data;
    },
  });
}

export function useEmployeesByStatus(status) {
  return useQuery({
    queryKey: ['employees', 'status', status],
    queryFn: async () => {
      const res = await axiosClient.get(`/Employees/status/${status}`);
      return res.data;
    },
    enabled: !!status,
  });
}
