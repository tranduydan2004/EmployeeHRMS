import { useState } from 'react';
import { usePaginationParams } from '@/hooks/usePaginationParams';
import { useDepartments, useDepartmentMutations } from '../hooks/useDepartments';
import DepartmentFormModal from './DepartmentFormModal';
import { Table, Button, Card, Badge, SearchBar, Pagination } from '@/components';
import { Building2, Plus, Edit, Trash2, Users } from 'lucide-react';

export default function DepartmentList() {
  const {
    pageNumber,
    pageSize,
    sortBy,
    isDescending,
    search,
    queryParams,
    setPage,
    setPageSize,
    setSort,
    setSearch,
  } = usePaginationParams({ pageSize: 10, sortBy: 'id', isDescending: false });

  const { data: pagedResult, isLoading } = useDepartments(queryParams);
  const {
    createDepartment,
    updateDepartment,
    deleteDepartment,
    isCreating,
    isUpdating,
  } = useDepartmentMutations();

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedDept, setSelectedDept] = useState(null);

  const handleOpenCreate = () => {
    setSelectedDept(null);
    setIsModalOpen(true);
  };

  const handleOpenEdit = (dept) => {
    setSelectedDept(dept);
    setIsModalOpen(true);
  };

  const handleDelete = async (dept) => {
    if (
      window.confirm(
        `Bạn có chắc chắn muốn xóa phòng ban "${dept.name}"? (Yêu cầu phòng ban không còn nhân viên nào).`
      )
    ) {
      await deleteDepartment(dept.id);
    }
  };

  const handleSave = async (data) => {
    if (selectedDept) {
      await updateDepartment({ id: selectedDept.id, payload: data });
    } else {
      await createDepartment(data);
    }
    setIsModalOpen(false);
  };

  const columns = [
    { key: 'id', title: 'ID', width: '80px', align: 'center', sortable: true, sortKey: 'id' },
    {
      key: 'name',
      title: 'Tên Phòng Ban',
      sortable: true,
      sortKey: 'name',
      render: (r) => (
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontWeight: 600 }}>
          <Building2 size={18} color="var(--primary-600)" />
          <span>{r.name}</span>
        </div>
      ),
    },
    {
      key: 'employeeCount',
      title: 'Số Lượng Nhân Viên',
      align: 'center',
      render: (r) => (
        <Badge variant={r.employeeCount > 0 ? 'info' : 'neutral'} icon={Users}>
          {r.employeeCount} nhân sự
        </Badge>
      ),
    },
    {
      key: 'employeeNames',
      title: 'Nhân Sự Tiêu Biểu',
      render: (r) =>
        r.employeeNames && r.employeeNames.length > 0 ? (
          <div style={{ fontSize: '0.825rem', color: 'var(--slate-600)' }}>
            {r.employeeNames.slice(0, 3).join(', ')}
            {r.employeeNames.length > 3 && ` (+${r.employeeNames.length - 3} người)`}
          </div>
        ) : (
          <span style={{ color: 'var(--text-muted)', fontSize: '0.8rem' }}>Chưa có</span>
        ),
    },
    {
      key: 'actions',
      title: 'Thao Tác',
      align: 'center',
      width: '120px',
      render: (r) => (
        <div style={{ display: 'flex', justifyContent: 'center', gap: '0.5rem' }}>
          <button
            type="button"
            className="btn-icon"
            onClick={() => handleOpenEdit(r)}
            title="Chỉnh sửa"
          >
            <Edit size={16} color="var(--primary-600)" />
          </button>
          <button
            type="button"
            className="btn-icon"
            onClick={() => handleDelete(r)}
            title="Xóa phòng ban"
          >
            <Trash2 size={16} color="var(--danger-main)" />
          </button>
        </div>
      ),
    },
  ];

  const departments = pagedResult?.items || (Array.isArray(pagedResult) ? pagedResult : []);

  return (
    <div>
      <div className="page-header">
        <div className="page-title-group">
          <h1>Quản Lý Phòng Ban</h1>
          <p>Cơ cấu tổ chức và phân bổ nguồn lực doanh nghiệp</p>
        </div>
        <Button variant="primary" icon={Plus} onClick={handleOpenCreate}>
          Thêm Phòng Ban
        </Button>
      </div>

      <Card>
        <div style={{ padding: '1rem', borderBottom: '1px solid var(--slate-100)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <SearchBar
            value={search}
            onChange={setSearch}
            placeholder="Tìm kiếm theo tên phòng ban..."
          />
        </div>

        <Table
          columns={columns}
          data={departments}
          isLoading={isLoading}
          sortBy={sortBy}
          isDescending={isDescending}
          onSort={setSort}
          emptyMessage="Chưa có phòng ban nào phù hợp."
          keyExtractor={(r) => r.id}
        />

        {pagedResult && pagedResult.totalCount > 0 && (
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

      <DepartmentFormModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSave={handleSave}
        selectedDepartment={selectedDept}
        isLoading={isCreating || isUpdating}
      />
    </div>
  );
}
