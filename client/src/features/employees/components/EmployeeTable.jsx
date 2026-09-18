import { useState } from 'react';
import { usePaginationParams } from '@/hooks/usePaginationParams';
import { useEmployees } from '../hooks/useEmployees';
import { useEmployeeMutations } from '../hooks/useEmployeeMutations';
import { useEmployeeStore } from '../useEmployeeStore';
import EmployeeStatCards from './EmployeeStatCards';
import EmployeeFormModal from './EmployeeFormModal';
import { Table, Pagination, Card, Button, Badge, SearchBar, Modal, Select } from '@/components';
import { Plus, Edit, Trash2 } from 'lucide-react';

export default function EmployeeTable() {
  const {
    sortBy,
    isDescending,
    search,
    queryParams,
    setPage,
    setPageSize,
    setSort,
    setSearch,
  } = usePaginationParams({ pageSize: 10, sortBy: 'employeeid', isDescending: false });

  const { data: pagedResult, isLoading } = useEmployees(queryParams);
  const {
    createEmployee,
    updateEmployee,
    deleteEmployee,
    changeEmployeeStatus,
    isCreating,
    isUpdating,
    isUpdatingStatus,
  } = useEmployeeMutations();

  const {
    selectedEmployeeIds,
    toggleSelectEmployee,
    selectAllEmployees,
    clearSelection,
  } = useEmployeeStore();

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedEmployee, setSelectedEmployee] = useState(null);
  const [statusModalEmployee, setStatusModalEmployee] = useState(null);
  const [newStatusSelection, setNewStatusSelection] = useState('');

  const handleOpenCreate = () => {
    setSelectedEmployee(null);
    setIsModalOpen(true);
  };

  const handleOpenEdit = (emp) => {
    setSelectedEmployee(emp);
    setIsModalOpen(true);
  };

  const handleDelete = async (emp) => {
    if (window.confirm(`Bạn có chắc muốn xóa nhân viên "${emp.fullName}" khỏi hệ thống?`)) {
      await deleteEmployee(emp.employeeId);
    }
  };

  const handleSave = async (payload) => {
    if (selectedEmployee) {
      await updateEmployee({ id: selectedEmployee.employeeId, payload });
      if (payload.status && payload.status !== selectedEmployee.status) {
        await changeEmployeeStatus({ id: selectedEmployee.employeeId, status: payload.status });
      }
    } else {
      await createEmployee(payload);
    }
    setIsModalOpen(false);
  };

  const handleConfirmChangeStatus = async () => {
    if (statusModalEmployee && newStatusSelection) {
      await changeEmployeeStatus({
        id: statusModalEmployee.employeeId,
        status: newStatusSelection,
      });
      setStatusModalEmployee(null);
    }
  };

  const allPageIds = pagedResult?.items?.map((e) => e.employeeId) || [];
  const isAllSelected =
    allPageIds.length > 0 && allPageIds.every((id) => selectedEmployeeIds.includes(id));

  const handleToggleSelectAll = () => {
    if (isAllSelected) {
      clearSelection();
    } else {
      selectAllEmployees(allPageIds);
    }
  };

  const columns = [
    {
      key: 'select',
      title: (
        <input
          type="checkbox"
          checked={isAllSelected}
          onChange={handleToggleSelectAll}
          aria-label="Chọn tất cả"
        />
      ),
      width: '40px',
      align: 'center',
      render: (r) => (
        <input
          type="checkbox"
          checked={selectedEmployeeIds.includes(r.employeeId)}
          onChange={() => toggleSelectEmployee(r.employeeId)}
          aria-label={`Chọn ${r.fullName}`}
        />
      ),
    },
    {
      key: 'employeeId',
      title: 'ID',
      width: '70px',
      align: 'center',
      sortable: true,
      sortKey: 'employeeid',
    },
    {
      key: 'fullName',
      title: 'Họ và Tên',
      sortable: true,
      sortKey: 'fullname',
      render: (r) => (
        <div>
          <div style={{ fontWeight: 600, color: 'var(--slate-900)' }}>{r.fullName}</div>
          <div style={{ fontSize: '0.775rem', color: 'var(--text-muted)' }}>{r.email}</div>
        </div>
      ),
    },
    {
      key: 'departmentName',
      title: 'Phòng Ban',
      render: (r) => <Badge variant="neutral">{r.departmentName || 'Chưa gán'}</Badge>,
    },
    {
      key: 'position',
      title: 'Vị Trí',
      sortable: true,
      sortKey: 'position',
    },
    {
      key: 'salary',
      title: 'Mức Lương',
      sortable: true,
      sortKey: 'salary',
      align: 'right',
      render: (r) => (
        <span style={{ fontWeight: 600, color: 'var(--slate-800)' }}>
          {r.salary?.toLocaleString('vi-VN')} ₫
        </span>
      ),
    },
    {
      key: 'joinDate',
      title: 'Ngày Gia Nhập',
      sortable: true,
      sortKey: 'joindate',
      align: 'center',
      render: (r) =>
        r.joinDate ? new Date(r.joinDate).toLocaleDateString('vi-VN') : '—',
    },
    {
      key: 'status',
      title: 'Trạng Thái',
      sortable: true,
      sortKey: 'status',
      align: 'center',
      render: (r) => {
        const getVariantAndLabel = (st) => {
          switch (st) {
            case 'Probation':
              return { variant: 'warning', label: 'Thử việc' };
            case 'Active':
              return { variant: 'success', label: 'Đang làm việc' };
            case 'OnLeave':
              return { variant: 'info', label: 'Nghỉ phép' };
            case 'Terminated':
              return { variant: 'danger', label: 'Đã thôi việc' };
            default:
              return { variant: 'neutral', label: st || 'N/A' };
          }
        };

        const { variant, label } = getVariantAndLabel(r.status);
        return (
          <div
            onClick={() => {
              setStatusModalEmployee(r);
              setNewStatusSelection(r.status);
            }}
            style={{ cursor: 'pointer', display: 'inline-block' }}
            title="Click để đổi nhanh trạng thái"
          >
            <Badge variant={variant}>
              {label} ({r.status})
            </Badge>
          </div>
        );
      },
    },
    {
      key: 'actions',
      title: 'Thao Tác',
      align: 'center',
      width: '100px',
      render: (r) => (
        <div style={{ display: 'flex', justifyContent: 'center', gap: '0.5rem' }}>
          <button
            type="button"
            className="btn-icon"
            onClick={() => handleOpenEdit(r)}
            title="Chỉnh sửa thông tin"
          >
            <Edit size={16} color="var(--primary-600)" />
          </button>
          <button
            type="button"
            className="btn-icon"
            onClick={() => handleDelete(r)}
            title="Xóa nhân viên"
          >
            <Trash2 size={16} color="var(--danger-main)" />
          </button>
        </div>
      ),
    },
  ];

  return (
    <div>
      <div className="page-header">
        <div className="page-title-group">
          <h1>Danh Sách Nhân Viên</h1>
          <p>Hồ sơ nhân sự toàn diện, phân trang URL và phân tích cơ cấu lương</p>
        </div>
        <Button variant="primary" icon={Plus} onClick={handleOpenCreate}>
          Thêm Nhân Viên
        </Button>
      </div>

      <EmployeeStatCards />

      {selectedEmployeeIds.length > 0 && (
        <div
          style={{
            marginBottom: '1rem',
            padding: '0.75rem 1rem',
            backgroundColor: 'var(--primary-50)',
            border: '1px solid var(--primary-200)',
            borderRadius: 'var(--radius-md)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
          }}
        >
          <span style={{ fontSize: '0.875rem', color: 'var(--primary-800)', fontWeight: 600 }}>
            Đã chọn {selectedEmployeeIds.length} nhân viên (Zustand Feature-Local State)
          </span>
          <Button variant="secondary" size="sm" onClick={clearSelection}>
            Bỏ chọn tất cả
          </Button>
        </div>
      )}

      <Card>
        <div style={{ padding: '1rem', borderBottom: '1px solid var(--slate-100)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <SearchBar
            value={search}
            onChange={setSearch}
            placeholder="Tìm kiếm nhân viên theo họ tên, email, vị trí..."
          />
        </div>

        <Table
          columns={columns}
          data={pagedResult?.items || []}
          isLoading={isLoading}
          sortBy={sortBy}
          isDescending={isDescending}
          onSort={setSort}
          emptyMessage="Không tìm thấy nhân viên nào phù hợp."
          keyExtractor={(r) => r.employeeId}
        />

        {pagedResult && (
          <Pagination
            pageNumber={pagedResult.pageNumber}
            pageSize={pagedResult.pageSize}
            totalPages={pagedResult.totalPages}
            totalCount={pagedResult.totalCount}
            hasPreviousPage={pagedResult.hasPreviousPage}
            hasNextPage={pagedResult.hasNextPage}
            onPageChange={setPage}
            onPageSizeChange={setPageSize}
          />
        )}
      </Card>

      <EmployeeFormModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSave={handleSave}
        selectedEmployee={selectedEmployee}
        isLoading={isCreating || isUpdating}
      />

      {statusModalEmployee && (
        <Modal
          isOpen={!!statusModalEmployee}
          onClose={() => setStatusModalEmployee(null)}
          title="Thay Đổi Trạng Thái Nhân Viên"
          footer={
            <>
              <Button
                variant="secondary"
                onClick={() => setStatusModalEmployee(null)}
                disabled={isUpdatingStatus}
              >
                Hủy
              </Button>
              <Button
                variant="primary"
                onClick={handleConfirmChangeStatus}
                isLoading={isUpdatingStatus}
              >
                Cập Nhật Trạng Thái
              </Button>
            </>
          }
        >
          <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
            <p>
              Nhân viên: <strong>{statusModalEmployee.fullName}</strong> ({statusModalEmployee.email})
            </p>
            <p style={{ fontSize: '0.875rem', color: 'var(--text-muted)' }}>
              Trạng thái hiện tại: <strong>{statusModalEmployee.status}</strong>
            </p>
            <Select
              label="Chọn trạng thái mới"
              value={newStatusSelection}
              onChange={(e) => setNewStatusSelection(e.target.value)}
              options={[
                { value: 'Probation', label: 'Probation — Thử việc' },
                { value: 'Active', label: 'Active — Đang làm việc' },
                { value: 'OnLeave', label: 'OnLeave — Nghỉ phép' },
                { value: 'Terminated', label: 'Terminated — Đã thôi việc' },
              ]}
            />
          </div>
        </Modal>
      )}
    </div>
  );
}
