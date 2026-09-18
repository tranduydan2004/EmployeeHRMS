import { useState } from 'react';
import { usePaginationParams } from '@/hooks/usePaginationParams';
import { useApplications } from '../hooks/useApplications';
import { useApplicationMutations } from '../hooks/useApplicationMutations';
import { usePermission } from '@/hooks/usePermission';
import ApplicationStatusBadge from './ApplicationStatusBadge';
import StatusTransitionModal from './StatusTransitionModal';
import { Table, Pagination, Card, Button, Badge, SearchBar } from '@/components';
import { Sparkles, RefreshCw, Trash2 } from 'lucide-react';

export default function ApplicationTable() {
  const {
    search,
    setSearch,
    sortBy,
    isDescending,
    queryParams,
    setPage,
    setPageSize,
    setSort,
  } = usePaginationParams({ pageSize: 10, sortBy: 'applieddate', isDescending: true });

  const { data: pagedResult, isLoading } = useApplications(queryParams);
  const { updateStatus, isUpdatingStatus, deleteApplication } = useApplicationMutations();
  const { isAdmin, isHR } = usePermission();

  const [selectedApp, setSelectedApp] = useState(null);
  const [isModalOpen, setIsModalOpen] = useState(false);

  const handleOpenTransition = (app) => {
    setSelectedApp(app);
    setIsModalOpen(true);
  };

  const handleTransition = async ({ id, status }) => {
    await updateStatus({ id, status });
    setIsModalOpen(false);
  };

  const handleDelete = async (app) => {
    if (window.confirm(`Bạn có chắc muốn xóa đơn ứng tuyển của "${app.candidateName}"?`)) {
      await deleteApplication(app.id);
    }
  };

  const columns = [
    { key: 'id', title: 'ID', width: '60px', align: 'center', sortable: true, sortKey: 'id' },
    {
      key: 'candidateName',
      title: 'Ứng Viên',
      sortable: true,
      sortKey: 'candidatename',
      render: (r) => <div style={{ fontWeight: 600 }}>{r.candidateName}</div>,
    },
    {
      key: 'jobTitle',
      title: 'Vị Trí Tuyển Dụng',
      sortable: true,
      sortKey: 'jobtitle',
      render: (r) => <Badge variant="neutral">{r.jobTitle}</Badge>,
    },
    {
      key: 'appliedDate',
      title: 'Ngày Nộp',
      sortable: true,
      sortKey: 'applieddate',
      align: 'center',
      render: (r) => (r.appliedDate ? new Date(r.appliedDate).toLocaleDateString('vi-VN') : '—'),
    },
    {
      key: 'aiMatchScore',
      title: 'Đánh Giá AI',
      align: 'center',
      render: (r) =>
        r.aiMatchScore ? (
          <Badge variant="success" icon={Sparkles}>
            {r.aiMatchScore}%
          </Badge>
        ) : (
          <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Chưa đánh giá</span>
        ),
    },
    {
      key: 'status',
      title: 'Trạng Thái',
      sortable: true,
      sortKey: 'status',
      align: 'center',
      render: (r) => <ApplicationStatusBadge status={r.status} />,
    },
    {
      key: 'actions',
      title: 'Thao Tác',
      align: 'center',
      width: '140px',
      render: (r) => (
        <div style={{ display: 'flex', justifyContent: 'center', gap: '0.35rem' }}>
          {(isAdmin || isHR) && (
            <>
              <Button
                variant="outline"
                size="sm"
                icon={RefreshCw}
                onClick={() => handleOpenTransition(r)}
                title="Chuyển trạng thái"
              >
                Đổi Status
              </Button>
              <button
                type="button"
                className="btn-icon"
                onClick={() => handleDelete(r)}
                title="Xóa đơn"
              >
                <Trash2 size={16} color="var(--danger-main)" />
              </button>
            </>
          )}
        </div>
      ),
    },
  ];

  return (
    <div>
      <div className="page-header">
        <div className="page-title-group">
          <h1>Quản Lý Đơn Ứng Tuyển</h1>
          <p>Theo dõi tiến trình xét duyệt hồ sơ và state machine chuyển trạng thái</p>
        </div>
      </div>

      <Card>
        <div style={{ marginBottom: '1.25rem', maxWidth: '400px' }}>
          <SearchBar
            value={search}
            onChange={setSearch}
            placeholder="Tìm kiếm ứng viên, chức danh công việc..."
          />
        </div>

        <Table
          columns={columns}
          data={pagedResult?.items || []}
          isLoading={isLoading}
          sortBy={sortBy}
          isDescending={isDescending}
          onSort={setSort}
          emptyMessage="Không có đơn ứng tuyển nào."
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

      <StatusTransitionModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onTransition={handleTransition}
        application={selectedApp}
        isLoading={isUpdatingStatus}
      />
    </div>
  );
}
