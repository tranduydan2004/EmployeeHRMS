import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { interviewCreateSchema } from '../schemas/interviewSchemas';
import { useApplications } from '@/features/recruitment/applications';
import { Modal, Input, Select, Button } from '@/components';

function getInitialScheduleDate() {
  const tomorrow = new Date();
  tomorrow.setDate(tomorrow.getDate() + 1);
  return tomorrow.toISOString().slice(0, 16);
}

export default function InterviewScheduleModal({
  isOpen,
  onClose,
  onSave,
  isLoading,
}) {
  const { data: applicationsData } = useApplications({ pageNumber: 1, pageSize: 50 });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(interviewCreateSchema),
    defaultValues: {
      applicationId: '',
      scheduledDate: getInitialScheduleDate(),
      interviewerId: '',
    },
  });

  useEffect(() => {
    if (isOpen) {
      reset({
        applicationId: applicationsData?.items?.[0]?.id?.toString() || '',
        scheduledDate: getInitialScheduleDate(),
        interviewerId: '',
      });
    }
  }, [isOpen, applicationsData, reset]);

  const onSubmit = (data) => {
    const payload = {
      applicationId: parseInt(data.applicationId, 10),
      scheduledDate: new Date(data.scheduledDate).toISOString(),
      interviewerId: data.interviewerId ? parseInt(data.interviewerId, 10) : null,
    };
    onSave(payload);
  };

  const applicationOptions =
    applicationsData?.items?.map((app) => ({
      value: app.id.toString(),
      label: `#${app.id} - ${app.candidateName} (${app.jobTitle})`,
    })) || [];

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Lên Lịch Phỏng Vấn Mới"
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
            Tạo Lịch Phỏng Vấn
          </Button>
        </>
      }
    >
      <form onSubmit={handleSubmit(onSubmit)}>
        <Select
          label="Chọn Đơn Ứng Tuyển"
          placeholder="-- Chọn đơn ứng tuyển cần phỏng vấn --"
          options={applicationOptions}
          error={errors.applicationId?.message}
          required
          {...register('applicationId')}
        />

        <Input
          label="Thời Gian Phỏng Vấn"
          type="datetime-local"
          error={errors.scheduledDate?.message}
          required
          {...register('scheduledDate')}
        />

        <Input
          label="ID Người Phỏng Vấn (Tùy chọn)"
          type="number"
          placeholder="Nhập User ID người phụ trách phỏng vấn (nếu có)"
          error={errors.interviewerId?.message}
          {...register('interviewerId')}
        />
      </form>
    </Modal>
  );
}
