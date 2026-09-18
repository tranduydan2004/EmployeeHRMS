import { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { usePaginationParams } from '@/hooks/usePaginationParams';
import { useJobPostings, useJobPostingMutations } from '../hooks/useJobPostings';
import { useDepartments } from '@/features/departments';
import { usePermission } from '@/hooks/usePermission';
import JobPostingFormModal from './JobPostingFormModal';
import { Card, Button, Badge, Skeleton, SearchBar, Select, Pagination } from '@/components';
import { Briefcase, Building2, Calendar, Plus, Edit, Trash2, ArrowRight, Filter } from 'lucide-react';

export default function JobPostingList() {
  const location = useLocation();
  const isAdminRoute = location.pathname.startsWith('/admin/jobs');

  const {
    pageNumber,
    pageSize,
    sortBy,
    isDescending,
    search,
    departmentId,
    status,
    queryParams,
    setPage,
    setPageSize,
    setSearch,
    setFilter,
  } = usePaginationParams({ pageSize: 6, sortBy: 'createddate', isDescending: true });

  const { data: pagedResult, isLoading } = useJobPostings(queryParams);
  const { data: deptsData } = useDepartments({ pageSize: 50 });
  const departmentList = deptsData?.items || (Array.isArray(deptsData) ? deptsData : []);

  const { canManageJobPostings } = usePermission();
  const {
    createJobPosting,
    updateJobPosting,
    deleteJobPosting,
    isCreating,
    isUpdating,
  } = useJobPostingMutations();

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedJob, setSelectedJob] = useState(null);

  const handleOpenCreate = () => {
    setSelectedJob(null);
    setIsModalOpen(true);
  };

  const handleOpenEdit = (job) => {
    setSelectedJob(job);
    setIsModalOpen(true);
  };

  const handleDelete = async (job) => {
    if (window.confirm(`Bạn có chắc chắn muốn xóa tin tuyển dụng "${job.title}"?`)) {
      await deleteJobPosting(job.id);
    }
  };

  const handleSave = async (payload) => {
    if (selectedJob) {
      await updateJobPosting({ id: selectedJob.id, payload });
    } else {
      await createJobPosting(payload);
      setPage(1);
    }
    setIsModalOpen(false);
  };

  const departmentOptions = [
    { value: '', label: 'Tất cả phòng ban' },
    ...departmentList.map((d) => ({
      value: d.id.toString(),
      label: d.name,
    })),
  ];

  const statusOptions = [
    { value: '', label: 'Tất cả trạng thái' },
    { value: 'Draft', label: 'Bản nháp (Draft)' },
    { value: 'Published', label: 'Đang mở (Published)' },
    { value: 'Closed', label: 'Đã đóng (Closed)' },
  ];

  const jobs = pagedResult?.items || [];

  return (
    <div>
      <div className="page-header">
        <div className="page-title-group">
          <h1>{isAdminRoute ? 'Quản Lý Tin Tuyển Dụng' : 'Cơ Hội Nghề Nghiệp & Tuyển Dụng'}</h1>
          <p>
            {isAdminRoute
              ? 'Quản trị danh sách tin tuyển dụng, trạng thái xuất bản và phân bổ ứng viên'
              : 'Khám phá các vị trí tuyển dụng hấp dẫn tại doanh nghiệp'}
          </p>
        </div>
        {canManageJobPostings && (
          <Button variant="primary" icon={Plus} onClick={handleOpenCreate}>
            Đăng Tin Tuyển Dụng
          </Button>
        )}
      </div>

      {/* Filter Bar */}
      <Card style={{ marginBottom: '1.25rem', padding: '1rem' }}>
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '1rem',
            flexWrap: 'wrap',
          }}
        >
          <div style={{ flex: 1, minWidth: '240px' }}>
            <SearchBar
              value={search}
              onChange={setSearch}
              placeholder="Tìm kiếm chức danh công việc..."
            />
          </div>

          <div style={{ minWidth: '200px' }}>
            <Select
              options={departmentOptions}
              value={departmentId}
              onChange={(e) => setFilter('departmentId', e.target.value)}
              aria-label="Lọc theo phòng ban"
            />
          </div>

          {/* CHỈ hiển thị dropdown trạng thái trên route quản trị (/admin/jobs) */}
          {isAdminRoute && (
            <div style={{ minWidth: '180px' }}>
              <Select
                options={statusOptions}
                value={status}
                onChange={(e) => setFilter('status', e.target.value)}
                aria-label="Lọc theo trạng thái"
              />
            </div>
          )}
        </div>
      </Card>

      {isLoading ? (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(340px, 1fr))', gap: '1.25rem' }}>
          {Array.from({ length: 4 }).map((_, i) => (
            <Card key={i}>
              <Skeleton height="28px" width="70%" />
              <div style={{ margin: '1rem 0' }}>
                <Skeleton height="60px" />
              </div>
              <Skeleton height="36px" width="40%" />
            </Card>
          ))}
        </div>
      ) : jobs.length === 0 ? (
        <Card style={{ textAlign: 'center', padding: '3rem 1rem' }}>
          <Briefcase size={48} color="var(--slate-400)" style={{ margin: '0 auto 1rem auto' }} />
          <h3>Không tìm thấy tin tuyển dụng nào</h3>
          <p style={{ color: 'var(--text-muted)', marginTop: '0.5rem' }}>
            {search || departmentId || status
              ? 'Thử thay đổi bộ lọc hoặc từ khóa tìm kiếm.'
              : 'Hiện chưa có tin tuyển dụng nào được đăng.'}
          </p>
        </Card>
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(340px, 1fr))', gap: '1.25rem' }}>
          {jobs.map((job) => (
            <Card key={job.id} style={{ display: 'flex', flexDirection: 'column', justifyContent: 'space-between' }}>
              <div>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '0.5rem' }}>
                  <h3 style={{ fontSize: '1.15rem', fontWeight: 600, color: 'var(--slate-900)' }}>
                    {job.title}
                  </h3>
                  <Badge variant={job.status === 'Published' ? 'success' : job.status === 'Draft' ? 'warning' : 'neutral'}>
                    {job.status}
                  </Badge>
                </div>

                <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', color: 'var(--primary-700)', fontSize: '0.85rem', fontWeight: 500, margin: '0.5rem 0' }}>
                  <Building2 size={16} />
                  <span>{job.departmentName}</span>
                </div>

                <p
                  style={{
                    color: 'var(--slate-600)',
                    fontSize: '0.875rem',
                    lineHeight: 1.5,
                    display: '-webkit-box',
                    WebkitLineClamp: 3,
                    WebkitBoxOrient: 'vertical',
                    overflow: 'hidden',
                    marginBottom: '1rem',
                  }}
                >
                  {job.description || 'Chưa có mô tả chi tiết.'}
                </p>
              </div>

              <div style={{ borderTop: '1px solid var(--slate-100)', paddingTop: '0.875rem', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', color: 'var(--text-muted)', fontSize: '0.8rem' }}>
                  <Calendar size={14} />
                  {new Date(job.createdDate).toLocaleDateString('vi-VN')}
                </div>

                <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                  {canManageJobPostings && (
                    <>
                      <button
                        type="button"
                        className="btn-icon"
                        onClick={() => handleOpenEdit(job)}
                        title="Chỉnh sửa tin"
                      >
                        <Edit size={16} color="var(--primary-600)" />
                      </button>
                      <button
                        type="button"
                        className="btn-icon"
                        onClick={() => handleDelete(job)}
                        title="Xóa tin"
                      >
                        <Trash2 size={16} color="var(--danger-main)" />
                      </button>
                    </>
                  )}
                  <Link to={`/jobs/${job.id}`}>
                    <Button variant="outline" size="sm" icon={ArrowRight}>
                      Xem Chi Tiết
                    </Button>
                  </Link>
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}

      {/* Pagination */}
      {pagedResult && pagedResult.totalCount > 0 && (
        <div style={{ marginTop: '1.5rem' }}>
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
        </div>
      )}

      {canManageJobPostings && (
        <JobPostingFormModal
          isOpen={isModalOpen}
          onClose={() => setIsModalOpen(false)}
          onSave={handleSave}
          selectedJob={selectedJob}
          isLoading={isCreating || isUpdating}
        />
      )}
    </div>
  );
}
