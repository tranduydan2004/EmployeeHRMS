import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { employeeSchema } from '../schemas/employeeSchemas';
import { useDepartments } from '@/features/departments';
import { Modal, Input, Select, Button } from '@/components';

export default function EmployeeFormModal({
  isOpen,
  onClose,
  onSave,
  selectedEmployee,
  isLoading,
}) {
  const { data: departments, isLoading: isDeptsLoading } = useDepartments({ pageSize: 50 });
  const departmentList = departments?.items || (Array.isArray(departments) ? departments : []);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(employeeSchema),
    defaultValues: {
      fullName: '',
      email: '',
      position: '',
      salary: '',
      departmentId: '',
      joinDate: new Date().toISOString().split('T')[0],
      status: 'Active',
    },
  });

  useEffect(() => {
    if (selectedEmployee) {
      reset({
        fullName: selectedEmployee.fullName || '',
        email: selectedEmployee.email || '',
        position: selectedEmployee.position || '',
        salary: selectedEmployee.salary || '',
        departmentId: selectedEmployee.departmentId ? selectedEmployee.departmentId.toString() : '',
        joinDate: selectedEmployee.joinDate
          ? selectedEmployee.joinDate.split('T')[0]
          : new Date().toISOString().split('T')[0],
        status: selectedEmployee.status || 'Active',
      });
    } else {
      reset({
        fullName: '',
        email: '',
        position: '',
        salary: '',
        departmentId: departmentList && departmentList.length > 0 ? departmentList[0].id.toString() : '',
        joinDate: new Date().toISOString().split('T')[0],
        status: 'Active',
      });
    }
  }, [selectedEmployee, reset, isOpen, departments]);

  const onSubmit = (data) => {
    const payload = {
      ...data,
      salary: Number(data.salary),
      departmentId: parseInt(data.departmentId, 10),
      joinDate: new Date(data.joinDate).toISOString(),
    };
    onSave(payload);
  };

  const departmentOptions =
    departmentList.map((d) => ({
      value: d.id.toString(),
      label: d.name,
    })) || [];

  const statusOptions = [
    { value: 'Probation', label: 'Thử việc (Probation)' },
    { value: 'Active', label: 'Đang làm việc (Active)' },
    { value: 'OnLeave', label: 'Nghỉ phép (OnLeave)' },
    { value: 'Terminated', label: 'Đã thôi việc (Terminated)' },
  ];

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={selectedEmployee ? 'Cập Nhật Hồ Sơ Nhân Viên' : 'Thêm Nhân Viên Mới'}
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
            {selectedEmployee ? 'Lưu Thay Đổi' : 'Thêm Nhân Viên'}
          </Button>
        </>
      }
    >
      <form onSubmit={handleSubmit(onSubmit)}>
        <Input
          label="Họ và tên"
          placeholder="Nguyễn Văn A"
          error={errors.fullName?.message}
          required
          {...register('fullName')}
        />

        <Input
          label="Email doanh nghiệp"
          type="email"
          placeholder="nva@hrms.com"
          error={errors.email?.message}
          required
          {...register('email')}
        />

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
          <Input
            label="Vị trí công việc"
            placeholder="Senior Backend Developer"
            error={errors.position?.message}
            required
            {...register('position')}
          />

          <Select
            label="Phòng ban"
            placeholder={isDeptsLoading ? 'Đang tải...' : '-- Chọn phòng ban --'}
            options={departmentOptions}
            error={errors.departmentId?.message}
            required
            {...register('departmentId')}
          />
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: selectedEmployee ? '1fr 1fr 1fr' : '1fr 1fr', gap: '0.75rem' }}>
          <Input
            label="Mức lương cơ bản (VNĐ)"
            type="number"
            placeholder="25000000"
            error={errors.salary?.message}
            required
            {...register('salary')}
          />

          <Input
            label="Ngày gia nhập"
            type="date"
            error={errors.joinDate?.message}
            required
            {...register('joinDate')}
          />

          {selectedEmployee && (
            <Select
              label="Trạng thái"
              options={statusOptions}
              error={errors.status?.message}
              {...register('status')}
            />
          )}
        </div>
      </form>
    </Modal>
  );
}
