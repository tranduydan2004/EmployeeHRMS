import { z } from 'zod';

/**
 * Zod Schema cho form nhập tham số cấu trúc sinh JD bằng AI (Step 1)
 * Đồng bộ chính xác với DraftJdRequestDto backend
 */
export const draftJdSchema = z
  .object({
    title: z
      .string()
      .trim()
      .min(2, 'Tiêu đề vị trí phải từ 2 ký tự')
      .max(200, 'Tiêu đề không vượt quá 200 ký tự'),

    departmentId: z.coerce
      .number({ invalid_type_error: 'Vui lòng chọn phòng ban tiếp nhận' })
      .int()
      .positive('Vui lòng chọn phòng ban hợp lệ'),

    level: z
      .enum(['Intern', 'Fresher', 'Junior', 'Middle', 'Senior', 'Lead'], {
        errorMap: () => ({ message: 'Vui lòng chọn cấp bậc' }),
      })
      .default('Junior'),

    workMode: z
      .enum(['Onsite', 'Hybrid', 'Remote'], {
        errorMap: () => ({ message: 'Vui lòng chọn hình thức làm việc' }),
      })
      .default('Onsite'),

    coreSkills: z
      .array(z.string().trim().min(1, 'Kỹ năng không được rỗng'))
      .min(1, 'Vui lòng nhập ít nhất 1 kỹ năng chuyên môn cốt lõi'),

    yearsOfExperience: z
      .preprocess(
        (val) => (val === '' || val === null || val === undefined ? undefined : Number(val)),
        z.number({ invalid_type_error: 'Số năm kinh nghiệm phải là số' })
          .min(0, 'Kinh nghiệm không thể âm')
          .max(50, 'Kinh nghiệm tối đa 50 năm')
          .optional()
      ),

    salaryMin: z
      .preprocess(
        (val) => (val === '' || val === null || val === undefined ? undefined : Number(val)),
        z.number({ invalid_type_error: 'Mức lương phải là số' })
          .min(0, 'Lương không thể âm')
          .optional()
      ),

    salaryMax: z
      .preprocess(
        (val) => (val === '' || val === null || val === undefined ? undefined : Number(val)),
        z.number({ invalid_type_error: 'Mức lương phải là số' })
          .min(0, 'Lương không thể âm')
          .optional()
      ),

    currency: z
      .enum(['VND', 'USD', 'EUR'], {
        errorMap: () => ({ message: 'Vui lòng chọn loại tiền tệ' }),
      })
      .default('VND'),

    additionalNotes: z
      .string()
      .trim()
      .max(2000, 'Ghi chú thêm không vượt quá 2000 ký tự')
      .optional()
      .or(z.literal('')),

    certifications: z
      .array(z.string().trim().min(1, 'Chứng chỉ không được rỗng'))
      .optional()
      .default([])
      .transform((val) => (Array.isArray(val) ? val.filter(Boolean) : [])),
  })
  .refine(
    (data) => {
      if (data.salaryMin !== undefined && data.salaryMax !== undefined) {
        return data.salaryMin <= data.salaryMax;
      }
      return true;
    },
    {
      message: 'Lương tối thiểu không được lớn hơn lương tối đa',
      path: ['salaryMax'],
    }
  );

/**
 * Zod Schema cho việc tinh chỉnh nội dung JD đã sinh bởi AI (Step 2)
 * Đồng bộ chính xác với UpdateJdContentDto backend
 */
export const updateJdContentSchema = z.object({
  intro: z
    .string()
    .trim()
    .min(10, 'Đoạn giới thiệu vị trí phải từ 10 ký tự')
    .max(2000, 'Đoạn giới thiệu không vượt quá 2000 ký tự'),

  responsibilities: z
    .array(z.string().trim().min(1, 'Mục trách nhiệm không được để trống'))
    .min(1, 'Phải có ít nhất 1 trách nhiệm công việc chính'),

  mustHave: z
    .array(z.string().trim().min(1, 'Mục yêu cầu không được để trống'))
    .min(1, 'Phải có ít nhất 1 yêu cầu bắt buộc (Must-Have)'),

  niceToHave: z
    .array(z.string().trim().min(1, 'Mục yêu cầu không được để trống'))
    .default([]),

  benefits: z
    .array(z.string().trim().min(1, 'Mục quyền lợi không được để trống'))
    .default([]),
});
