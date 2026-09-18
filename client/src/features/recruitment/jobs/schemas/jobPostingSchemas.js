import { z } from 'zod';

export const jobPostingSchema = z.object({
  title: z
    .string()
    .min(2, 'Tiêu đề tuyển dụng phải từ 2 ký tự')
    .max(200, 'Tiêu đề không vượt quá 200 ký tự'),
  departmentId: z.coerce
    .number({ invalid_type_error: 'Vui lòng chọn phòng ban' })
    .int()
    .positive('Vui lòng chọn phòng ban hợp lệ'),
  description: z.string().max(5000, 'Mô tả không vượt quá 5000 ký tự').optional(),
  requirements: z.string().max(5000, 'Yêu cầu không vượt quá 5000 ký tự').optional(),
  status: z.enum(['Draft', 'Published', 'Closed']).optional(),
});
