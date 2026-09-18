import { z } from 'zod';

export const APPLICATION_STATUS_ENUM = [
  'Applied',
  'Screening',
  'Interviewing',
  'Offered',
  'Rejected',
];

export const applicationCreateSchema = z.object({
  candidateId: z.coerce.number().int().positive('Vui lòng chọn ứng viên hợp lệ'),
  jobPostingId: z.coerce.number().int().positive('Vui lòng chọn tin tuyển dụng hợp lệ'),
});

export const applicationStatusUpdateSchema = z.object({
  status: z.enum(['Applied', 'Screening', 'Interviewing', 'Offered', 'Rejected'], {
    errorMap: () => ({ message: 'Trạng thái ứng tuyển không hợp lệ' }),
  }),
});
