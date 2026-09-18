import { useMutation, useQueryClient } from '@tanstack/react-query';
import axiosClient from '@/api/axiosClient';
import { toast } from '@/components/Toast/useToastStore';
import { resumeUploadSchema } from '../schemas/candidateSchemas';

export function useResumeActions() {
  const queryClient = useQueryClient();

  const uploadMutation = useMutation({
    mutationFn: async ({ candidateId, file }) => {
      // Client-side validation with Zod
      const validationResult = resumeUploadSchema.safeParse({ file });
      if (!validationResult.success) {
        const errorMsg = validationResult.error.errors[0]?.message || 'File không hợp lệ';
        throw new Error(errorMsg);
      }

      const formData = new FormData();
      formData.append('file', file);

      const res = await axiosClient.post(`/Candidates/${candidateId}/resume`, formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      });
      return res.data;
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['candidates'] });
      queryClient.invalidateQueries({ queryKey: ['candidates', variables.candidateId] });
      toast.success('Upload CV / Resume thành công!');
    },
    onError: (error) => {
      toast.error(error.message || 'Lỗi khi upload CV.');
    },
  });

  const downloadResume = async (candidateId, candidateName = 'Candidate') => {
    try {
      const res = await axiosClient.get(`/Candidates/${candidateId}/resume`, {
        responseType: 'blob',
      });

      // Tạo Blob link an toàn
      const blob = new Blob([res.data], {
        type: res.headers['content-type'] || 'application/pdf',
      });
      const downloadUrl = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = downloadUrl;
      link.setAttribute('download', `CV_${candidateName.replace(/\s+/g, '_')}.pdf`);
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(downloadUrl);

      toast.success('Tải CV thành công!');
    } catch (err) {
      console.error('Download error:', err);
    }
  };

  return {
    uploadResume: uploadMutation.mutateAsync,
    isUploading: uploadMutation.isPending,
    downloadResume,
  };
}
