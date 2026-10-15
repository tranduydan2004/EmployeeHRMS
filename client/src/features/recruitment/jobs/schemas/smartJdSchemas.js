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

/**
 * Hằng số cấu hình thời gian suy nghĩ & trả lời câu hỏi phỏng vấn
 * Đồng bộ chính xác với AudioInterviewOptions ở backend
 */
export const QUESTION_TIME_LIMITS = {
  think: {
    min: 5,
    max: 40,
    default: 20,
    presets: [10, 15, 20, 30, 40],
  },
  answer: {
    min: 10,
    max: 120,
    default: 90,
    presets: [30, 45, 60, 90, 120],
  },
};

/**
 * Zod Schema cho việc tinh chỉnh câu hỏi & thời gian trong Question Bank
 * Đồng bộ chính xác với UpdateQuestionBankItemDto backend
 */
export const updateQuestionBankItemSchema = z.object({
  question: z
    .string()
    .trim()
    .min(5, 'Nội dung câu hỏi phải từ 5 ký tự')
    .max(1000, 'Nội dung câu hỏi không vượt quá 1000 ký tự')
    .optional(),

  scoringRubric: z
    .object({
      excellent: z.string().trim().default(''),
      good: z.string().trim().default(''),
      acceptable: z.string().trim().default(''),
      poor: z.string().trim().default(''),
    })
    .optional(),

  thinkSeconds: z.coerce
    .number({ invalid_type_error: 'Thời gian suy nghĩ phải là số' })
    .int('Thời gian suy nghĩ phải là số nguyên')
    .min(QUESTION_TIME_LIMITS.think.min, `Thời gian suy nghĩ tối thiểu ${QUESTION_TIME_LIMITS.think.min} giây`)
    .max(QUESTION_TIME_LIMITS.think.max, `Thời gian suy nghĩ tối đa ${QUESTION_TIME_LIMITS.think.max} giây`)
    .optional(),

  answerSeconds: z.coerce
    .number({ invalid_type_error: 'Thời gian trả lời phải là số' })
    .int('Thời gian trả lời phải là số nguyên')
    .min(QUESTION_TIME_LIMITS.answer.min, `Thời gian trả lời tối thiểu ${QUESTION_TIME_LIMITS.answer.min} giây`)
    .max(QUESTION_TIME_LIMITS.answer.max, `Thời gian trả lời tối đa ${QUESTION_TIME_LIMITS.answer.max} giây`)
    .optional(),
});

