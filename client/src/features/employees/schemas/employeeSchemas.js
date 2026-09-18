import { z } from 'zod';

export const employeeSchema = z.object({
  fullName: z
    .string()
    .min(2, 'Họ tên phải có ít nhất 2 ký tự')
    .max(200, 'Họ tên không vượt quá 200 ký tự'),
  email: z
    .string()
    .min(1, 'Email không được để trống')
    .email('Định dạng email không hợp lệ')
    .max(200, 'Email không vượt quá 200 ký tự'),
  position: z
    .string()
    .min(2, 'Vị trí công việc phải có ít nhất 2 ký tự')
    .max(100, 'Vị trí không vượt quá 100 ký tự'),
  salary: z.coerce
    .number({ invalid_type_error: 'Mức lương phải là số hợp lệ' })
    .min(0, 'Mức lương không thể là số âm'),
  departmentId: z.coerce
    .number({ invalid_type_error: 'Vui lòng chọn phòng ban' })
    .int()
    .positive('Vui lòng chọn phòng ban hợp lệ'),
  joinDate: z.string().min(1, 'Vui lòng chọn ngày gia nhập'),
  status: z.enum(['Probation', 'Active', 'OnLeave', 'Terminated']).optional(),
});
