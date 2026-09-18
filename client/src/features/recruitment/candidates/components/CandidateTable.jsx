import { useState } from 'react';
import { usePaginationParams } from '@/hooks/usePaginationParams';
import { useCandidates, useCandidateMutations } from '../hooks/useCandidates';
import ResumeUploadButton from './ResumeUploadButton';
import CandidateFormModal from './CandidateFormModal';
import { Table, Pagination, Card, Button, Badge, SearchBar } from '@/components';
import { Plus, Edit, Trash2 } from 'lucide-react';

export default function CandidateTable() {
  const {
    sortBy,
    isDescending,
    search,
    queryParams,
    setPage,
    setPageSize,
    setSort,
    setSearch,
  } = usePaginationParams({ pageSize: 10, sortBy: 'fullname', isDescending: false });

  const { data: pagedResult, isLoading } = useCandidates(queryParams);
  const {
    createCandidate,
    updateCandidate,
    deleteCandidate,
    isCreating,
    isUpdating,
  } = useCandidateMutations();

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedCandidate, setSelectedCandidate] = useState(null);

  const handleOpenCreate = () => {
    setSelectedCandidate(null);
    setIsModalOpen(true);
  };

  const handleOpenEdit = (cand) => {
    setSelectedCandidate(cand);
    setIsModalOpen(true);
  };

  const handleDelete = async (cand) => {
    if (window.confirm(`Bạn có chắc muốn xóa hồ sơ ứng viên "${cand.fullName}"?`)) {
      await deleteCandidate(cand.id);
    }
  };

  const handleSave = async (payload) => {
    if (selectedCandidate) {
      await updateCandidate({ id: selectedCandidate.id, payload });
    } else {
      await createCandidate(payload);
    }
    setIsModalOpen(false);
  };

  const columns = [
    { key: 'id', title: 'ID', width: '70px', align: 'center', sortable: true, sortKey: 'id' },
    {
      key: 'fullName',
      title: 'Ứng Viên',
      sortable: true,
      sortKey: 'fullname',
      render: (r) => (
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          <div className="avatar-circle" style={{ width: '32px', height: '32px', fontSize: '0.8rem' }}>
            {r.fullName ? r.fullName.charAt(0).toUpperCase() : 'U'}
          </div>
          <div>
            <div style={{ fontWeight: 600, color: 'var(--slate-900)' }}>{r.fullName}</div>
            <div style={{ fontSize: '0.775rem', color: 'var(--text-muted)' }}>{r.email}</div>
          </div>
        </div>
      ),
    },
    {
      key: 'phone',
      title: 'Số Điện Thoại',
      sortable: true,
      sortKey: 'phone',
    },
    {
      key: 'applicationCount',
      title: 'Số Đơn Đã Nộp',
      align: 'center',
      render: (r) => (
        <Badge variant={r.applicationCount > 0 ? 'info' : 'neutral'}>
          {r.applicationCount} đơn
        </Badge>
      ),
    },
    {
      key: 'resume',
      title: 'Hồ Sơ CV / Resume',
      render: (r) => <ResumeUploadButton candidate={r} />,
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
            title="Chỉnh sửa hồ sơ"
          >
            <Edit size={16} color="var(--primary-600)" />
          </button>
          <button
            type="button"
            className="btn-icon"
            onClick={() => handleDelete(r)}
            title="Xóa hồ sơ"
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
          <h1>Danh Sách Hồ Sơ Ứng Viên</h1>
          <p>Quản lý ứng viên, tải lên/tải xuống CV định dạng PDF/Word an toàn</p>
        </div>
        <Button variant="primary" icon={Plus} onClick={handleOpenCreate}>
          Tạo Hồ Sơ Ứng Viên
        </Button>
      </div>

      <Card>
        <div style={{ padding: '1rem', borderBottom: '1px solid var(--slate-100)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <SearchBar
            value={search}
            onChange={setSearch}
            placeholder="Tìm kiếm ứng viên theo họ tên, email..."
          />
        </div>

        <Table
          columns={columns}
          data={pagedResult?.items || []}
          isLoading={isLoading}
          sortBy={sortBy}
          isDescending={isDescending}
          onSort={setSort}
          emptyMessage="Chưa có dữ liệu ứng viên."
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

      <CandidateFormModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSave={handleSave}
        selectedCandidate={selectedCandidate}
        isLoading={isCreating || isUpdating}
      />
    </div>
  );
}
