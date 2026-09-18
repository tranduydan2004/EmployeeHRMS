import { useQuery } from '@tanstack/react-query';
import axiosClient from '@/api/axiosClient';

export function useApplications(queryParams = {}) {
  const { pageNumber = 1, pageSize = 10, sortBy, isDescending, search } = queryParams;

  return useQuery({
    queryKey: ['applications', { pageNumber, pageSize, sortBy, isDescending, search }],
    queryFn: async () => {
      const params = new URLSearchParams();
      params.set('pageNumber', pageNumber.toString());
      params.set('pageSize', pageSize.toString());
      if (sortBy) params.set('sortBy', sortBy);
      if (isDescending !== undefined) params.set('isDescending', isDescending.toString());
      if (search) params.set('search', search);

      const res = await axiosClient.get(`/Applications?${params.toString()}`);
      return res.data; // PagedResult<ApplicationResponseDto>
    },
    placeholderData: (prev) => prev,
  });
}

export function useApplication(id) {
  return useQuery({
    queryKey: ['applications', id],
    queryFn: async () => {
      const res = await axiosClient.get(`/Applications/${id}`);
      return res.data;
    },
    enabled: !!id,
  });
}
