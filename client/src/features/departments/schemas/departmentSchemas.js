import { z } from 'zod';

export const departmentSchema = z.object({
  name: z
    .string()
    .min(2, 'Tên phòng ban phải từ 2 ký tự trở lên')
    .max(100, 'Tên phòng ban không vượt quá 100 ký tự'),
});
