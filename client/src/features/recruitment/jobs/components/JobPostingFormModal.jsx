import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { jobPostingSchema } from '../schemas/jobPostingSchemas';
import { useDepartments } from '@/features/departments';
import { Modal, Input, Select, Button } from '@/components';

export default function JobPostingFormModal({
  isOpen,
  onClose,
  onSave,
  selectedJob,
  isLoading,
}) {
  const { data: departments } = useDepartments({ pageSize: 50 });
  const departmentList = departments?.items || (Array.isArray(departments) ? departments : []);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(jobPostingSchema),
    defaultValues: {
      title: '',
      departmentId: '',
      description: '',
      requirements: '',
      status: 'Published',
    },
  });

  useEffect(() => {
    if (selectedJob) {
      reset({
        title: selectedJob.title || '',
        departmentId: selectedJob.departmentId ? selectedJob.departmentId.toString() : '',
        description: selectedJob.description || '',
        requirements: selectedJob.requirements || '',
        status: selectedJob.status || 'Published',
      });
    } else {
      reset({
        title: '',
        departmentId: departmentList && departmentList.length > 0 ? departmentList[0].id.toString() : '',
        description: '',
        requirements: '',
        status: 'Published',
      });
    }
  }, [selectedJob, reset, isOpen, departments]);

  const onSubmit = (data) => {
    const payload = {
      ...data,
      departmentId: parseInt(data.departmentId, 10),
    };
    onSave(payload);
  };

  const departmentOptions =
    departmentList.map((d) => ({
      value: d.id.toString(),
      label: d.name,
    })) || [];

  const isApprovedOrPublished = selectedJob?.status === 'Approved' || selectedJob?.status === 'Published';
  const statusOptions = isApprovedOrPublished
    ? [
        { value: 'Published', label: 'Đang mở tuyển (Published)' },
        { value: 'Closed', label: 'Đã đóng tuyển (Closed)' },
      ]
    : [
        { value: 'Draft', label: 'Bản nháp (Draft)' },
        { value: 'Published', label: 'Đang mở tuyển (Published)' },
        { value: 'Closed', label: 'Đã đóng tuyển (Closed)' },
      ];

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={selectedJob ? 'Cập Nhật Tin Tuyển Dụng' : 'Tạo Tin Tuyển Dụng Mới'}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={isLoading}>
            Hủy
          </Button>
          <Button
            variant="primary"
            onClick={handleSubmit(onSubmit)}
            isLoading={isLoading}
          >
            {selectedJob ? 'Lưu Thay Đổi' : 'Đăng Tuyển'}
          </Button>
        </>
      }
    >
      <form onSubmit={handleSubmit(onSubmit)}>
        {selectedJob?.jdContent && (
          <div
            style={{
              padding: '0.75rem 1rem',
              backgroundColor: '#f5f3ff',
              border: '1px solid #ddd6fe',
              borderRadius: 'var(--radius-md)',
              marginBottom: '1rem',
              fontSize: '0.85rem',
              color: '#5b21b6',
              display: 'flex',
              alignItems: 'center',
              gap: '0.5rem',
            }}
          >
            <span>✨</span>
            <span>
              Tin tuyển dụng này có nội dung <strong>Smart JD (AI)</strong>. Barem câu hỏi & nội dung chi tiết được đồng bộ tự động với Smart JD.
            </span>
          </div>
        )}

        <Input
          label="Tiêu đề vị trí tuyển dụng"
          placeholder="Ví dụ: Senior .NET Core Developer, HR Specialist..."
          error={errors.title?.message}
          required
          {...register('title')}
        />

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
          <Select
            label="Phòng ban tiếp nhận"
            placeholder="-- Chọn phòng ban --"
            options={departmentOptions}
            error={errors.departmentId?.message}
            required
            {...register('departmentId')}
          />

          <Select
            label="Trạng thái hiển thị"
            options={statusOptions}
            error={errors.status?.message}
            {...register('status')}
          />
        </div>

        <div className="form-group">
          <label className="form-label">Mô tả công việc (Job Description)</label>
          <textarea
            className="form-control"
            rows={4}
            placeholder="Mô tả trách nhiệm công việc, môi trường làm việc..."
            {...register('description')}
          />
          {errors.description && <div className="form-error">{errors.description.message}</div>}
        </div>

        <div className="form-group">
          <label className="form-label">Yêu cầu ứng viên (Requirements)</label>
          <textarea
            className="form-control"
            rows={4}
            placeholder="Kỹ năng bắt buộc, số năm kinh nghiệm, bằng cấp..."
            {...register('requirements')}
          />
          {errors.requirements && <div className="form-error">{errors.requirements.message}</div>}
        </div>
      </form>
    </Modal>
  );
}
