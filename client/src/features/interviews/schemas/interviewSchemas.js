import { z } from 'zod';

export const interviewCreateSchema = z.object({
  applicationId: z.coerce.number().int().positive('Vui lòng chọn đơn ứng tuyển'),
  scheduledDate: z.string().min(1, 'Vui lòng chọn ngày giờ phỏng vấn'),
  interviewerId: z.coerce.number().int().positive('Vui lòng chọn người phỏng vấn').optional().nullable(),
});

export const interviewQuestionCreateSchema = z.object({
  question: z
    .string()
    .min(5, 'Câu hỏi phải có ít nhất 5 ký tự')
    .max(2000, 'Câu hỏi không vượt quá 2000 ký tự'),
  orderIndex: z.coerce.number().int().min(0, 'Thứ tự câu hỏi phải từ 0 trở lên'),
});
