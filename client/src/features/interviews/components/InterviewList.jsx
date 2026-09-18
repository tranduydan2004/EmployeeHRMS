import { useState } from 'react';
import { usePaginationParams } from '@/hooks/usePaginationParams';
import { useInterviews, useInterviewMutations } from '../hooks/useInterviews';
import { usePermission } from '@/hooks/usePermission';
import InterviewScheduleModal from './InterviewScheduleModal';
import QuestionEvaluationCard from './QuestionEvaluationCard';
import { Card, Button, Badge, Skeleton, SearchBar, Pagination } from '@/components';
import { CalendarCheck2, Calendar, Plus, Trash2, Sparkles, UserCheck } from 'lucide-react';

export default function InterviewList() {
  const {
    search,
    setSearch,
    queryParams,
    setPage,
    setPageSize,
  } = usePaginationParams({ pageSize: 6, sortBy: 'scheduleddate', isDescending: true });

  const { data: pagedResult, isLoading } = useInterviews(queryParams);
  const interviews = pagedResult?.items || [];
  const { createInterview, deleteInterview, isCreating } = useInterviewMutations();
  const { isAdmin, isHR } = usePermission();

  const [isModalOpen, setIsModalOpen] = useState(false);

  const handleSave = async (payload) => {
    await createInterview(payload);
    setIsModalOpen(false);
  };

  const handleDelete = async (interview) => {
    if (window.confirm(`Bạn có chắc muốn hủy lịch phỏng vấn #${interview.id}?`)) {
      await deleteInterview(interview.id);
    }
  };

  return (
    <div>
      <div className="page-header">
        <div className="page-title-group">
          <h1>Lịch Phỏng Vấn & Đánh Giá AI</h1>
          <p>Điều phối các vòng phỏng vấn sơ loại và chấm điểm câu hỏi phỏng vấn tự động</p>
        </div>
        {(isAdmin || isHR) && (
          <Button variant="primary" icon={Plus} onClick={() => setIsModalOpen(true)}>
            Lên Lịch Phỏng Vấn
          </Button>
        )}
      </div>

      <div style={{ marginBottom: '1.25rem', maxWidth: '400px' }}>
        <SearchBar
          value={search}
          onChange={setSearch}
          placeholder="Tìm theo chức danh, ứng viên..."
        />
      </div>

      {isLoading ? (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          {Array.from({ length: 3 }).map((_, i) => (
            <Card key={i}>
              <Skeleton height="28px" width="40%" />
              <div style={{ margin: '0.75rem 0' }}>
                <Skeleton height="60px" />
              </div>
            </Card>
          ))}
        </div>
      ) : interviews.length === 0 ? (
        <Card style={{ textAlign: 'center', padding: '3.5rem 1rem' }}>
          <CalendarCheck2 size={48} color="var(--slate-400)" style={{ margin: '0 auto 1rem auto' }} />
          <h3>{search ? 'Không tìm thấy buổi phỏng vấn nào' : 'Chưa có buổi phỏng vấn nào'}</h3>
          <p style={{ color: 'var(--text-muted)', marginTop: '0.5rem' }}>
            {search ? 'Thử thay đổi từ khóa tìm kiếm.' : 'Lên lịch phỏng vấn mới cho các đơn ứng tuyển hợp lệ.'}
          </p>
        </Card>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
          {interviews.map((item) => (
            <Card key={item.id}>
              <div
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'flex-start',
                  flexWrap: 'wrap',
                  gap: '1rem',
                }}
              >
                <div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                    <h3 style={{ fontSize: '1.15rem', fontWeight: 600, color: 'var(--slate-900)' }}>
                      Buổi Phỏng Vấn #{item.id}
                    </h3>
                    <Badge variant={item.status === 'Completed' ? 'success' : item.status === 'Cancelled' ? 'danger' : 'info'}>
                      {item.status}
                    </Badge>
                  </div>

                  <div
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: '1.25rem',
                      marginTop: '0.5rem',
                      fontSize: '0.875rem',
                      color: 'var(--slate-600)',
                      flexWrap: 'wrap',
                    }}
                  >
                    <span style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                      <Calendar size={15} color="var(--primary-600)" />
                      {new Date(item.scheduledDate).toLocaleString('vi-VN')}
                    </span>
                    <span>Mã Đơn Ứng Tuyển: #{item.applicationId}</span>
                    {item.interviewerId && (
                      <span style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                        <UserCheck size={15} color="var(--info-main)" />
                        Người phỏng vấn: {item.interviewerName ? `${item.interviewerName} (#${item.interviewerId})` : `#${item.interviewerId}`}
                      </span>
                    )}
                  </div>
                </div>

                <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                  {item.aiOverallScore !== null && item.aiOverallScore !== undefined && (
                    <Badge variant="success" icon={Sparkles}>
                      Điểm Tổng AI: {item.aiOverallScore}/10
                    </Badge>
                  )}
                  {(isAdmin || isHR) && (
                    <button
                      type="button"
                      className="btn-icon"
                      onClick={() => handleDelete(item)}
                      title="Hủy buổi phỏng vấn"
                    >
                      <Trash2 size={16} color="var(--danger-main)" />
                    </button>
                  )}
                </div>
              </div>

              {item.aiOverallSummary && (
                <div
                  style={{
                    marginTop: '1rem',
                    padding: '0.75rem 1rem',
                    backgroundColor: 'var(--primary-50)',
                    borderRadius: 'var(--radius-sm)',
                    border: '1px solid var(--primary-200)',
                    fontSize: '0.85rem',
                    color: 'var(--primary-900)',
                  }}
                >
                  <strong>Nhận xét tổng thể từ AI:</strong> {item.aiOverallSummary}
                </div>
              )}

              <QuestionEvaluationCard interview={item} />
            </Card>
          ))}
        </div>
      )}

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

      {(isAdmin || isHR) && (
        <InterviewScheduleModal
          isOpen={isModalOpen}
          onClose={() => setIsModalOpen(false)}
          onSave={handleSave}
          isLoading={isCreating}
        />
      )}
    </div>
  );
}
