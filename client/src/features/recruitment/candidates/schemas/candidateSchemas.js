import { z } from 'zod';

export const candidateSchema = z.object({
  fullName: z
    .string()
    .min(2, 'Họ tên phải từ 2 ký tự')
    .max(200, 'Họ tên không vượt quá 200 ký tự'),
  email: z
    .string()
    .min(1, 'Email không được để trống')
    .email('Định dạng email không hợp lệ')
    .max(200, 'Email không vượt quá 200 ký tự'),
  phone: z
    .string()
    .min(1, 'Số điện thoại không được để trống')
    .max(20, 'Số điện thoại không vượt quá 20 ký tự'),
  resumeUrl: z.string().max(500).default(''),
});

// Client-side Resume Upload validation schema
const ALLOWED_MIME_TYPES = [
  'application/pdf',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
];
const MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB

export const resumeUploadSchema = z.object({
  file: z
    .custom((file) => file instanceof File, { message: 'Vui lòng chọn file CV' })
    .refine((file) => ALLOWED_MIME_TYPES.includes(file.type), {
      message: 'Chỉ chấp nhận file định dạng PDF (.pdf) hoặc Word (.docx)',
    })
    .refine((file) => file.size <= MAX_FILE_SIZE, {
      message: 'Dung lượng file CV không được vượt quá 5MB',
    }),
});
