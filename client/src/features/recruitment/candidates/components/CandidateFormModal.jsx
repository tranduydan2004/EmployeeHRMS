import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { candidateSchema } from '../schemas/candidateSchemas';
import { Modal, Input, Button } from '@/components';

export default function CandidateFormModal({
  isOpen,
  onClose,
  onSave,
  selectedCandidate,
  isLoading,
}) {
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(candidateSchema),
    defaultValues: {
      fullName: '',
      email: '',
      phone: '',
      resumeUrl: '',
    },
  });

  useEffect(() => {
    if (selectedCandidate) {
      reset({
        fullName: selectedCandidate.fullName || '',
        email: selectedCandidate.email || '',
        phone: selectedCandidate.phone || '',
        resumeUrl: selectedCandidate.resumeUrl || '',
      });
    } else {
      reset({
        fullName: '',
        email: '',
        phone: '',
        resumeUrl: '',
      });
    }
  }, [selectedCandidate, reset, isOpen]);

  const onSubmit = (data) => {
    onSave(data);
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={selectedCandidate ? 'Cập Nhật Hồ Sơ Ứng Viên' : 'Tạo Hồ Sơ Ứng Viên'}
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
            {selectedCandidate ? 'Lưu Thay Đổi' : 'Tạo Hồ Sơ'}
          </Button>
        </>
      }
    >
      <form onSubmit={handleSubmit(onSubmit)}>
        <Input
          label="Họ và tên ứng viên"
          placeholder="Trần Thị B"
          error={errors.fullName?.message}
          required
          {...register('fullName')}
        />

        <Input
          label="Email liên hệ"
          type="email"
          placeholder="candidate@example.com"
          error={errors.email?.message}
          required
          {...register('email')}
        />

        <Input
          label="Số điện thoại"
          placeholder="0912345678"
          error={errors.phone?.message}
          required
          {...register('phone')}
        />
      </form>
    </Modal>
  );
}
