import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { draftJdSchema } from '../../schemas/smartJdSchemas';
import { useDepartments } from '@/features/departments';
import { Input, Select, Button } from '@/components';
import { Sparkles, Briefcase, DollarSign, BrainCircuit, Award } from 'lucide-react';
import TagInput from './TagInput';

const LEVELS = [
  { value: 'Intern', label: 'Intern' },
  { value: 'Fresher', label: 'Fresher' },
  { value: 'Junior', label: 'Junior' },
  { value: 'Middle', label: 'Middle' },
  { value: 'Senior', label: 'Senior' },
  { value: 'Lead', label: 'Lead' },
];

const WORK_MODES = [
  { value: 'Onsite', label: 'Tại văn phòng (Onsite)' },
  { value: 'Hybrid', label: 'Linh hoạt (Hybrid)' },
  { value: 'Remote', label: 'Từ xa (Remote)' },
];

const CURRENCIES = [
  { value: 'VND', label: 'VND (₫)' },
  { value: 'USD', label: 'USD ($)' },
  { value: 'EUR', label: 'EUR (€)' },
];

const SKILL_SUGGESTIONS = [
  'C#',
  'ASP.NET Core',
  'React',
  'TypeScript',
  'PostgreSQL',
  'SQL Server',
  'Docker',
  'Kubernetes',
  'AWS',
  'Azure',
  'Microservices',
  'Git',
];

const CERT_SUGGESTIONS = [
  'AWS Certified Solutions Architect',
  'Microsoft Certified: Azure Developer',
  'Scrum Master (PSM / CSM)',
  'PMP',
  'TOEIC 750+',
  'IELTS 6.5+',
];

export default function Step1ParamForm({ onSubmit, isLoading, defaultValues }) {
  const { data: departments } = useDepartments({ pageSize: 50 });
  const departmentList = departments?.items || (Array.isArray(departments) ? departments : []);

  const {
    register,
    handleSubmit,
    control,
    watch,
    setValue,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(draftJdSchema),
    defaultValues: defaultValues || {
      title: '',
      departmentId: departmentList?.[0]?.id || '',
      level: 'Junior',
      workMode: 'Onsite',
      coreSkills: ['C#', 'ASP.NET Core'],
      yearsOfExperience: 1,
      salaryMin: undefined,
      salaryMax: undefined,
      currency: 'VND',
      additionalNotes: '',
      certifications: [],
    },
  });

  const currentLevel = watch('level');
  const currentWorkMode = watch('workMode');

  const departmentOptions = departmentList.map((d) => ({
    value: d.id.toString(),
    label: d.name,
  }));

  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
        {/* Banner Giới thiệu tính năng */}
        <div
          style={{
            background: 'linear-gradient(135deg, #eef2ff 0%, #ede9fe 100%)',
            border: '1px solid #c7d2fe',
            borderRadius: 'var(--radius-md)',
            padding: '1rem 1.25rem',
            display: 'flex',
            alignItems: 'center',
            gap: '0.85rem',
          }}
        >
          <div
            style={{
              width: '42px',
              height: '42px',
              borderRadius: 'var(--radius-full)',
              background: 'linear-gradient(135deg, var(--primary-600) 0%, #7c3aed 100%)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: '#ffffff',
              flexshrink: 0,
              boxShadow: '0 4px 8px rgba(99, 102, 241, 0.3)',
            }}
          >
            <Sparkles size={20} />
          </div>
          <div>
            <h4 style={{ margin: 0, fontSize: '0.95rem', fontWeight: 600, color: 'var(--primary-900)' }}>
              Trình Sinh Bản Thảo JD Thông Minh Bằng AI
            </h4>
            <p style={{ margin: '0.2rem 0 0 0', fontSize: '0.825rem', color: 'var(--primary-700)', lineHeight: 1.4 }}>
              Nhập các tham số cốt lõi bên dưới, AI sẽ chuẩn hóa cấu trúc JD chuẩn doanh nghiệp (Intro, Trách nhiệm, Must-Have, Nice-To-Have, Quyền lợi) và sẵn sàng sinh ngân hàng câu hỏi phỏng vấn tương ứng.
            </p>
          </div>
        </div>

        {/* Tiêu đề & Phòng ban */}
        <div style={{ display: 'grid', gridTemplateColumns: '1.5fr 1fr', gap: '1rem' }}>
          <Input
            label="Chức danh tuyển dụng (Job Title)"
            placeholder="VD: Senior .NET Backend Developer, Frontend Engineer..."
            error={errors.title?.message}
            required
            {...register('title')}
          />

          <Select
            label="Phòng ban tiếp nhận"
            placeholder="-- Chọn phòng ban --"
            options={departmentOptions}
            error={errors.departmentId?.message}
            required
            {...register('departmentId')}
          />
        </div>

        {/* Cấp bậc (Level) - Pill selection */}
        <div className="form-group">
          <label className="form-label" style={{ fontWeight: 600 }}>
            Cấp bậc chuyên môn (Level) <span style={{ color: 'var(--danger-main)' }}>*</span>
          </label>
          <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
            {LEVELS.map((lvl) => {
              const isSelected = currentLevel === lvl.value;
              return (
                <button
                  key={lvl.value}
                  type="button"
                  onClick={() => setValue('level', lvl.value)}
                  style={{
                    padding: '0.45rem 1rem',
                    borderRadius: 'var(--radius-full)',
                    fontSize: '0.85rem',
                    fontWeight: isSelected ? 600 : 500,
                    cursor: 'pointer',
                    border: isSelected ? '2px solid var(--primary-600)' : '1px solid var(--border-color)',
                    backgroundColor: isSelected ? 'var(--primary-50)' : 'var(--bg-card)',
                    color: isSelected ? 'var(--primary-700)' : 'var(--slate-700)',
                    transition: 'all var(--transition-fast)',
                  }}
                >
                  {lvl.label}
                </button>
              );
            })}
          </div>
          {errors.level && <div className="form-error">{errors.level.message}</div>}
        </div>

        {/* Hình thức làm việc (WorkMode) */}
        <div className="form-group">
          <label className="form-label" style={{ fontWeight: 600 }}>
            Hình thức làm việc (Work Mode) <span style={{ color: 'var(--danger-main)' }}>*</span>
          </label>
          <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
            {WORK_MODES.map((wm) => {
              const isSelected = currentWorkMode === wm.value;
              return (
                <button
                  key={wm.value}
                  type="button"
                  onClick={() => setValue('workMode', wm.value)}
                  style={{
                    padding: '0.45rem 0.9rem',
                    borderRadius: 'var(--radius-md)',
                    fontSize: '0.85rem',
                    fontWeight: isSelected ? 600 : 500,
                    cursor: 'pointer',
                    border: isSelected ? '2px solid var(--primary-600)' : '1px solid var(--border-color)',
                    backgroundColor: isSelected ? 'var(--primary-50)' : 'var(--bg-card)',
                    color: isSelected ? 'var(--primary-700)' : 'var(--slate-700)',
                    transition: 'all var(--transition-fast)',
                  }}
                >
                  {wm.label}
                </button>
              );
            })}
          </div>
          {errors.workMode && <div className="form-error">{errors.workMode.message}</div>}
        </div>

        {/* Kỹ năng cốt lõi (CoreSkills) */}
        <Controller
          name="coreSkills"
          control={control}
          render={({ field }) => (
            <TagInput
              label="Kỹ năng chuyên môn cốt lõi (Core Skills)"
              value={field.value}
              onChange={field.onChange}
              placeholder="Nhập tên kỹ năng (VD: C#, React, PostgreSQL) rồi nhấn Enter..."
              suggestions={SKILL_SUGGESTIONS}
              error={errors.coreSkills?.message}
              required
              helperText="Kỹ năng chính là dữ liệu then chốt để AI thiết kế trách nhiệm & sinh câu hỏi phỏng vấn chuẩn xác."
            />
          )}
        />

        {/* Kinh nghiệm & Dải lương */}
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 2fr', gap: '1rem' }}>
          <Input
            label="Số năm kinh nghiệm yêu cầu"
            type="number"
            min={0}
            max={50}
            placeholder="VD: 2"
            error={errors.yearsOfExperience?.message}
            {...register('yearsOfExperience')}
          />

          <div className="form-group">
            <label className="form-label" style={{ fontWeight: 600 }}>
              Dải lương dự kiến (Tùy chọn)
            </label>
            <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'flex-start' }}>
              <div style={{ flex: 1 }}>
                <Input
                  placeholder="Lương tối thiểu"
                  type="number"
                  min={0}
                  error={errors.salaryMin?.message}
                  {...register('salaryMin')}
                />
              </div>
              <span style={{ alignSelf: 'center', color: 'var(--text-muted)', marginBottom: '0.5rem' }}>-</span>
              <div style={{ flex: 1 }}>
                <Input
                  placeholder="Lương tối đa"
                  type="number"
                  min={0}
                  error={errors.salaryMax?.message}
                  {...register('salaryMax')}
                />
              </div>
              <div style={{ width: '110px' }}>
                <Select
                  options={CURRENCIES}
                  {...register('currency')}
                  aria-label="Loại tiền tệ"
                />
              </div>
            </div>
          </div>
        </div>

        {/* Chứng chỉ ưu tiên (Certifications) */}
        <Controller
          name="certifications"
          control={control}
          render={({ field }) => (
            <TagInput
              label="Chứng chỉ chuyên môn ưu tiên (Certifications)"
              value={field.value}
              onChange={field.onChange}
              placeholder="Nhập chứng chỉ (VD: AWS Certified, PMP...) rồi nhấn Enter..."
              suggestions={CERT_SUGGESTIONS}
              error={errors.certifications?.message}
              helperText="Tùy chọn: AI sẽ đưa các chứng chỉ này vào mục Yêu cầu ưu tiên (Nice-To-Have)."
            />
          )}
        />

        {/* Ghi chú thêm */}
        <div className="form-group">
          <label className="form-label" style={{ fontWeight: 600 }}>
            Ghi chú bổ sung cho AI (Additional Notes)
          </label>
          <textarea
            className="form-control"
            rows={2}
            placeholder="VD: Đội ngũ đang mở rộng quy mô, ưu tiên ứng viên có kinh nghiệm làm việc trong dự án fintech..."
            {...register('additionalNotes')}
          />
          {errors.additionalNotes && <div className="form-error">{errors.additionalNotes.message}</div>}
        </div>
      </div>

      {/* Action Footer */}
      <div
        style={{
          marginTop: '1.75rem',
          paddingTop: '1.25rem',
          borderTop: '1px solid var(--border-color)',
          display: 'flex',
          justifyContent: 'flex-end',
          alignItems: 'center',
          gap: '1rem',
        }}
      >
        <Button
          type="submit"
          variant="primary"
          size="lg"
          icon={Sparkles}
          isLoading={isLoading}
          style={{
            background: 'linear-gradient(135deg, var(--primary-600) 0%, #7c3aed 100%)',
            border: 'none',
            boxShadow: '0 4px 12px rgba(99, 102, 241, 0.35)',
            fontWeight: 600,
          }}
        >
          {isLoading ? 'AI đang phân tích & sinh bản thảo JD...' : '✨ Sinh Bản Thảo JD Bằng AI'}
        </Button>
      </div>
    </form>
  );
}
