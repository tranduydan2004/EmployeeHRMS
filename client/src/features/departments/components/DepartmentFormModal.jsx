import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { departmentSchema } from '../schemas/departmentSchemas';
import { Modal, Input, Button } from '@/components';

export default function DepartmentFormModal({
  isOpen,
  onClose,
  onSave,
  selectedDepartment,
  isLoading,
}) {
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(departmentSchema),
    defaultValues: {
      name: '',
    },
  });

  useEffect(() => {
    if (selectedDepartment) {
      reset({ name: selectedDepartment.name });
    } else {
      reset({ name: '' });
    }
  }, [selectedDepartment, reset, isOpen]);

  const onSubmit = (data) => {
    onSave(data);
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={selectedDepartment ? 'Cập Nhật Phòng Ban' : 'Thêm Phòng Ban Mới'}
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
            {selectedDepartment ? 'Lưu Thay Đổi' : 'Tạo Mới'}
          </Button>
        </>
      }
    >
      <form onSubmit={handleSubmit(onSubmit)}>
        <Input
          label="Tên phòng ban"
          placeholder="Ví dụ: Kỹ thuật Công nghệ (IT), Nhân sự (HR)..."
          error={errors.name?.message}
          required
          {...register('name')}
        />
      </form>
    </Modal>
  );
}
