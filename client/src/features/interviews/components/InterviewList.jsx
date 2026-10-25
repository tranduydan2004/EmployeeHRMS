import { useState, useMemo } from 'react';
import { useSearchParams, Link } from 'react-router-dom';
import { usePaginationParams } from '@/hooks/usePaginationParams';
import {
  useInterviewDashboardSummary,
  useInterviewDashboardStats,
  useInterview,
  useInterviewMutations,
} from '../hooks/useInterviews';
import { usePermission } from '@/hooks/usePermission';
import InterviewScheduleModal from './InterviewScheduleModal';
import AssignInterviewerModal from './AssignInterviewerModal';
import QuickScreeningDecisionModal from './QuickScreeningDecisionModal';
import ScreeningDecisionHistoryModal from './ScreeningDecisionHistoryModal';
import QuestionEvaluationCard from './QuestionEvaluationCard';
import { Card, Button, Badge, Skeleton, SearchBar, Pagination, Select } from '@/components';
import {
  CalendarCheck2,
  Calendar,
  Plus,
  Trash2,
  Sparkles,
  UserCheck,
  UserPlus,
  CheckCircle2,
  AlertCircle,
  XCircle,
  HelpCircle,
  Gavel,
  History,
  Clock,
  Briefcase,
  ChevronDown,
  ChevronUp,
  Filter,
  Users,
  ShieldCheck,
  ArrowUpDown,
  FileCheck2,
  ExternalLink,
} from 'lucide-react';

/**
 * Component hiển thị chi tiết lazy-loaded khi người dùng bấm "Xem chi tiết & Phản biện"
 */
function LazyInterviewDetail({ interviewId }) {
  const { data: fullInterview, isLoading } = useInterview(interviewId);

  if (isLoading) {
    return (
      <div style={{ marginTop: '1.25rem', display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
        <Skeleton height="35px" width="50%" />
        <Skeleton height="120px" />
      </div>
    );
  }

  if (!fullInterview) {
    return (
      <div style={{ marginTop: '1rem', padding: '1rem', color: 'var(--slate-500)', fontStyle: 'italic' }}>
        Không thể tải thông tin chi tiết buổi phỏng vấn.
      </div>
    );
  }

  return (
    <div
      style={{
        marginTop: '1.25rem',
        paddingTop: '1.25rem',
        borderTop: '1px solid var(--border-color)',
      }}
    >
      {fullInterview.aiOverallSummary && (
        <div
          style={{
            marginBottom: '1rem',
            padding: '0.85rem 1rem',
            backgroundColor: 'var(--primary-50)',
            borderRadius: 'var(--radius-md)',
            border: '1px solid var(--primary-200)',
            fontSize: '0.85rem',
            color: 'var(--primary-900)',
          }}
        >
          <strong>Nhận xét tổng thể từ AI:</strong> {fullInterview.aiOverallSummary}
        </div>
      )}

      <QuestionEvaluationCard interview={fullInterview} />
    </div>
  );
}

export default function InterviewList() {
  const [searchParams, setSearchParams] = useSearchParams();

  const {
    search,
    setSearch,
    pageNumber,
    pageSize,
    setPage,
    setPageSize,
    setFilter,
  } = usePaginationParams({ pageSize: 6, sortBy: 'scheduleddate', isDescending: true });

  // Custom filters from URL search params
  const recommendation = searchParams.get('recommendation') || '';
  const status = searchParams.get('status') || '';
  const decisionStatus = searchParams.get('decisionStatus') || '';
  const sortBy = searchParams.get('sortBy') || 'scheduleddate';
  const isDescending = searchParams.get('isDescending') !== 'false';

  const filterParams = useMemo(
    () => ({
      pageNumber,
      pageSize,
      search: search || undefined,
      recommendation: recommendation || undefined,
      status: status || undefined,
      decisionStatus: decisionStatus || undefined,
      sortBy: sortBy || undefined,
      isDescending,
    }),
    [pageNumber, pageSize, search, recommendation, status, decisionStatus, sortBy, isDescending]
  );

  // Queries
  const { data: pagedResult, isLoading } = useInterviewDashboardSummary(filterParams);
  const { data: stats, isLoading: isLoadingStats } = useInterviewDashboardStats();
  const interviews = pagedResult?.items || [];

  // Mutations
  const {
    createInterview,
    deleteInterview,
    assignInterviewer,
    createScreeningDecision,
    isCreating,
    isDeleting,
    isAssigning,
    isDeciding,
  } = useInterviewMutations();

  const { isAdmin, isHR } = usePermission();

  // Modals state
  const [isScheduleModalOpen, setIsScheduleModalOpen] = useState(false);
  const [assignModalItem, setAssignModalItem] = useState(null);
  const [decisionModalItem, setDecisionModalItem] = useState(null);
  const [historyModalItem, setHistoryModalItem] = useState(null);

  // Expanded items for lazy loading question cards
  const [expandedIds, setExpandedIds] = useState(new Set());

  const toggleExpand = (id) => {
    setExpandedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  };

  const handleSortChange = (value) => {
    setSearchParams((prev) => {
      const next = new URLSearchParams(prev);
      if (value === 'score_desc') {
        next.set('sortBy', 'aioverallscore');
        next.set('isDescending', 'true');
      } else if (value === 'score_asc') {
        next.set('sortBy', 'aioverallscore');
        next.set('isDescending', 'false');
      } else if (value === 'candidate_asc') {
        next.set('sortBy', 'candidatename');
        next.set('isDescending', 'false');
      } else if (value === 'job_asc') {
        next.set('sortBy', 'jobtitle');
        next.set('isDescending', 'false');
      } else if (value === 'date_asc') {
        next.set('sortBy', 'scheduleddate');
        next.set('isDescending', 'false');
      } else {
        next.set('sortBy', 'scheduleddate');
        next.set('isDescending', 'true');
      }
      next.delete('pageNumber');
      return next;
    });
  };

  const getRecommendationBadge = (rec) => {
    switch (rec) {
      case 'Pass':
        return (
          <span
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '0.3rem',
              padding: '0.25rem 0.65rem',
              borderRadius: 'var(--radius-full)',
              fontSize: '0.75rem',
              fontWeight: 700,
              backgroundColor: 'var(--success-bg)',
              color: 'var(--success-text)',
              border: '1px solid var(--success-border)',
            }}
          >
            <CheckCircle2 size={13} />
            Gợi ý: Đạt (Pass)
          </span>
        );
      case 'Consider':
        return (
          <span
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '0.3rem',
              padding: '0.25rem 0.65rem',
              borderRadius: 'var(--radius-full)',
              fontSize: '0.75rem',
              fontWeight: 700,
              backgroundColor: 'var(--warning-bg)',
              color: 'var(--warning-text)',
              border: '1px solid var(--warning-border)',
            }}
          >
            <AlertCircle size={13} />
            Gợi ý: Cân nhắc (Consider)
          </span>
        );
      case 'Reject':
        return (
          <span
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '0.3rem',
              padding: '0.25rem 0.65rem',
              borderRadius: 'var(--radius-full)',
              fontSize: '0.75rem',
              fontWeight: 700,
              backgroundColor: 'var(--danger-bg)',
              color: 'var(--danger-text)',
              border: '1px solid var(--danger-border)',
            }}
          >
            <XCircle size={13} />
            Gợi ý: Chưa đạt (Reject)
          </span>
        );
      default:
        return (
          <span
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '0.3rem',
              padding: '0.25rem 0.65rem',
              borderRadius: 'var(--radius-full)',
              fontSize: '0.75rem',
              fontWeight: 600,
              backgroundColor: 'var(--slate-100)',
              color: 'var(--slate-600)',
              border: '1px solid var(--slate-200)',
            }}
          >
            <HelpCircle size={13} />
            Chưa có gợi ý
          </span>
        );
    }
  };

  const getHumanDecisionBadge = (item) => {
    if (!item.hasDecision) {
      if (item.status === 'Completed') {
        return (
          <span
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '0.35rem',
              padding: '0.25rem 0.65rem',
              borderRadius: 'var(--radius-full)',
              fontSize: '0.75rem',
              fontWeight: 700,
              backgroundColor: 'var(--warning-bg)',
              color: 'var(--warning-text)',
              border: '1px solid var(--warning-border)',
            }}
          >
            <Clock size={13} />
            Chờ HR ra quyết định
          </span>
        );
      }
      return (
        <span style={{ fontSize: '0.75rem', color: 'var(--slate-400)', fontStyle: 'italic' }}>
          Chưa kết thúc phỏng vấn
        </span>
      );
    }

    switch (item.latestDecisionType) {
      case 'AdvanceToInDepth':
        return (
          <span
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '0.35rem',
              padding: '0.25rem 0.65rem',
              borderRadius: 'var(--radius-full)',
              fontSize: '0.75rem',
              fontWeight: 700,
              backgroundColor: 'var(--success-bg)',
              color: 'var(--success-text)',
              border: '1px solid var(--success-border)',
            }}
          >
            <CheckCircle2 size={13} />
            Đã duyệt: Chuyên sâu
          </span>
        );
      case 'HoldForReview':
        return (
          <span
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '0.35rem',
              padding: '0.25rem 0.65rem',
              borderRadius: 'var(--radius-full)',
              fontSize: '0.75rem',
              fontWeight: 700,
              backgroundColor: 'var(--warning-bg)',
              color: 'var(--warning-text)',
              border: '1px solid var(--warning-border)',
            }}
          >
            <AlertCircle size={13} />
            Tạm hoãn thẩm định
          </span>
        );
      case 'Reject':
        return (
          <span
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '0.35rem',
              padding: '0.25rem 0.65rem',
              borderRadius: 'var(--radius-full)',
              fontSize: '0.75rem',
              fontWeight: 700,
              backgroundColor: 'var(--danger-bg)',
              color: 'var(--danger-text)',
              border: '1px solid var(--danger-border)',
            }}
          >
            <XCircle size={13} />
            Đã từ chối
          </span>
        );
      default:
        return <Badge variant="secondary">{item.latestDecisionType}</Badge>;
    }
  };

  const handleDelete = async (interview) => {
    if (window.confirm(`Bạn có chắc muốn hủy buổi phỏng vấn #${interview.id} của ứng viên ${interview.candidateName}?`)) {
      await deleteInterview(interview.id);
    }
  };

  const currentSortValue =
    sortBy === 'aioverallscore'
      ? isDescending
        ? 'score_desc'
        : 'score_asc'
      : sortBy === 'candidatename'
      ? 'candidate_asc'
      : sortBy === 'jobtitle'
      ? 'job_asc'
      : isDescending
      ? 'date_desc'
      : 'date_asc';

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
      {/* Header */}
      <div className="page-header" style={{ marginBottom: 0 }}>
        <div className="page-title-group">
          <h1>Màn Hình Danh Sách Ứng Viên Sơ Loại (Dashboard)</h1>
          <p>
            Theo dõi tiến trình phỏng vấn audio, tham vấn gợi ý sơ bộ từ AI và đưa ra quyết định nhân sự có trách nhiệm.
          </p>
        </div>
        {(isAdmin || isHR) && (
          <Button variant="primary" icon={Plus} onClick={() => setIsScheduleModalOpen(true)}>
            Lên Lịch Phỏng Vấn
          </Button>
        )}
      </div>

      {/* Executive Summary Stats Bar */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(210px, 1fr))',
          gap: '1rem',
        }}
      >
        <Card style={{ padding: '1rem', borderLeft: '4px solid var(--primary-600)' }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <span style={{ fontSize: '0.8rem', fontWeight: 600, color: 'var(--slate-600)' }}>
              TỔNG ỨNG VIÊN SƠ LOẠI
            </span>
            <Users size={18} color="var(--primary-600)" />
          </div>
          <div style={{ fontSize: '1.65rem', fontWeight: 700, color: 'var(--slate-900)', marginTop: '0.4rem' }}>
            {isLoadingStats ? <Skeleton height="32px" width="60px" /> : stats?.totalInterviews ?? 0}
          </div>
          <span style={{ fontSize: '0.75rem', color: 'var(--slate-500)' }}>
            Đang/Chờ PV: <strong>{stats?.pendingOrInProgress ?? 0}</strong>
          </span>
        </Card>

        <Card style={{ padding: '1rem', borderLeft: '4px solid var(--success-main)' }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <span style={{ fontSize: '0.8rem', fontWeight: 600, color: 'var(--slate-600)' }}>
              HOÀN THÀNH PHỎNG VẤN
            </span>
            <CalendarCheck2 size={18} color="var(--success-main)" />
          </div>
          <div style={{ fontSize: '1.65rem', fontWeight: 700, color: 'var(--slate-900)', marginTop: '0.4rem' }}>
            {isLoadingStats ? <Skeleton height="32px" width="60px" /> : stats?.completed ?? 0}
          </div>
          <span style={{ fontSize: '0.75rem', color: 'var(--slate-500)' }}>
            Đã có âm thanh & kết quả bóc băng
          </span>
        </Card>

        <Card style={{ padding: '1rem', borderLeft: '4px solid #6366f1' }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <span style={{ fontSize: '0.8rem', fontWeight: 600, color: 'var(--slate-600)' }}>
              GỢI Ý SƠ BỘ TỪ AI
            </span>
            <Sparkles size={18} color="#6366f1" />
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', marginTop: '0.65rem', flexWrap: 'wrap' }}>
            {isLoadingStats ? (
              <Skeleton height="26px" width="100%" />
            ) : (
              <>
                <span
                  style={{
                    fontSize: '0.75rem',
                    fontWeight: 700,
                    padding: '0.15rem 0.5rem',
                    borderRadius: 'var(--radius-sm)',
                    backgroundColor: 'var(--success-bg)',
                    color: 'var(--success-text)',
                  }}
                >
                  Đạt: {stats?.recommendedPass ?? 0}
                </span>
                <span
                  style={{
                    fontSize: '0.75rem',
                    fontWeight: 700,
                    padding: '0.15rem 0.5rem',
                    borderRadius: 'var(--radius-sm)',
                    backgroundColor: 'var(--warning-bg)',
                    color: 'var(--warning-text)',
                  }}
                >
                  Cân nhắc: {stats?.recommendedConsider ?? 0}
                </span>
                <span
                  style={{
                    fontSize: '0.75rem',
                    fontWeight: 700,
                    padding: '0.15rem 0.5rem',
                    borderRadius: 'var(--radius-sm)',
                    backgroundColor: 'var(--danger-bg)',
                    color: 'var(--danger-text)',
                  }}
                >
                  Loại: {stats?.recommendedReject ?? 0}
                </span>
              </>
            )}
          </div>
        </Card>

        <Card style={{ padding: '1rem', borderLeft: '4px solid var(--warning-main)' }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <span style={{ fontSize: '0.8rem', fontWeight: 600, color: 'var(--slate-600)' }}>
              QUẢN TRỊ NHÂN SỰ
            </span>
            <Gavel size={18} color="var(--warning-main)" />
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginTop: '0.65rem' }}>
            {isLoadingStats ? (
              <Skeleton height="26px" width="100%" />
            ) : (
              <>
                <span
                  style={{
                    fontSize: '0.75rem',
                    fontWeight: 700,
                    padding: '0.2rem 0.6rem',
                    borderRadius: 'var(--radius-full)',
                    backgroundColor: stats?.pendingDecisions > 0 ? 'var(--warning-bg)' : 'var(--slate-100)',
                    color: stats?.pendingDecisions > 0 ? 'var(--warning-text)' : 'var(--slate-600)',
                    border: '1px solid var(--border-color)',
                  }}
                >
                  Chờ duyệt: {stats?.pendingDecisions ?? 0}
                </span>
                <span
                  style={{
                    fontSize: '0.75rem',
                    fontWeight: 700,
                    padding: '0.2rem 0.6rem',
                    borderRadius: 'var(--radius-full)',
                    backgroundColor: 'var(--primary-50)',
                    color: 'var(--primary-800)',
                    border: '1px solid var(--primary-200)',
                  }}
                >
                  Đã duyệt: {stats?.decided ?? 0}
                </span>
              </>
            )}
          </div>
        </Card>
      </div>

      {/* Advanced Filters Toolbar */}
      <Card style={{ padding: '1rem 1.25rem' }}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap', alignItems: 'center' }}>
            <div style={{ flex: '1 1 300px', maxWidth: '450px' }}>
              <SearchBar
                value={search}
                onChange={setSearch}
                placeholder="Tìm theo tên ứng viên, email, chức danh..."
              />
            </div>

            <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap', alignItems: 'center', marginLeft: 'auto' }}>
              {/* Sort Dropdown */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', fontSize: '0.85rem' }}>
                <ArrowUpDown size={15} color="var(--slate-500)" />
                <Select
                  value={currentSortValue}
                  onChange={(e) => handleSortChange(e.target.value)}
                  options={[
                    { value: 'date_desc', label: 'Thời gian: Mới nhất' },
                    { value: 'date_asc', label: 'Thời gian: Cũ nhất' },
                    { value: 'score_desc', label: 'Điểm AI: Cao → Thấp (Nulls-Last)' },
                    { value: 'score_asc', label: 'Điểm AI: Thấp → Cao (Nulls-Last)' },
                    { value: 'candidate_asc', label: 'Tên ứng viên: A → Z' },
                    { value: 'job_asc', label: 'Vị trí ứng tuyển: A → Z' },
                  ]}
                />
              </div>
            </div>
          </div>

          {/* Filter Pills */}
          <div
            style={{
              display: 'flex',
              gap: '1rem',
              flexWrap: 'wrap',
              alignItems: 'center',
              paddingTop: '0.75rem',
              borderTop: '1px solid var(--border-color)',
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', fontSize: '0.825rem', color: 'var(--slate-600)' }}>
              <Filter size={14} />
              <span>Gợi ý AI:</span>
              <Select
                value={recommendation}
                onChange={(e) => setFilter('recommendation', e.target.value)}
                options={[
                  { value: '', label: 'Tất cả gợi ý' },
                  { value: 'Pass', label: 'Đạt (Pass)' },
                  { value: 'Consider', label: 'Cân nhắc (Consider)' },
                  { value: 'Reject', label: 'Chưa đạt (Reject)' },
                ]}
              />
            </div>

            <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', fontSize: '0.825rem', color: 'var(--slate-600)' }}>
              <span>Trạng thái PV:</span>
              <Select
                value={status}
                onChange={(e) => setFilter('status', e.target.value)}
                options={[
                  { value: '', label: 'Tất cả trạng thái' },
                  { value: 'Scheduled', label: 'Scheduled (Đã lên lịch)' },
                  { value: 'InProgress', label: 'InProgress (Đang diễn ra)' },
                  { value: 'Completed', label: 'Completed (Hoàn thành)' },
                  { value: 'Expired', label: 'Expired (Hết hạn)' },
                  { value: 'Cancelled', label: 'Cancelled (Đã hủy)' },
                ]}
              />
            </div>

            <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', fontSize: '0.825rem', color: 'var(--slate-600)' }}>
              <span>Quyết định HR:</span>
              <Select
                value={decisionStatus}
                onChange={(e) => setFilter('decisionStatus', e.target.value)}
                options={[
                  { value: '', label: 'Tất cả' },
                  { value: 'PendingDecision', label: 'Chờ HR quyết định' },
                  { value: 'Decided', label: 'Đã có quyết định' },
                ]}
              />
            </div>

            {(recommendation || status || decisionStatus || search) && (
              <button
                type="button"
                className="btn-link"
                style={{ fontSize: '0.8rem', color: 'var(--primary-600)', cursor: 'pointer' }}
                onClick={() => {
                  setSearchParams(new URLSearchParams());
                }}
              >
                Xóa tất cả bộ lọc
              </button>
            )}
          </div>
        </div>
      </Card>

      {/* Main List */}
      {isLoading ? (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          {Array.from({ length: 3 }).map((_, i) => (
            <Card key={i} style={{ padding: '1.25rem' }}>
              <Skeleton height="28px" width="35%" />
              <div style={{ margin: '0.75rem 0' }}>
                <Skeleton height="60px" />
              </div>
            </Card>
          ))}
        </div>
      ) : interviews.length === 0 ? (
        <Card style={{ textAlign: 'center', padding: '3.5rem 1rem' }}>
          <CalendarCheck2 size={48} color="var(--slate-400)" style={{ margin: '0 auto 1rem auto' }} />
          <h3>Không tìm thấy hồ sơ phỏng vấn nào phù hợp</h3>
          <p style={{ color: 'var(--text-muted)', marginTop: '0.5rem' }}>
            Thử thay đổi từ khóa tìm kiếm hoặc điều chỉnh các tiêu chí bộ lọc.
          </p>
        </Card>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
          {interviews.map((item) => {
            const isExpanded = expandedIds.has(item.id);

            return (
              <Card key={item.id} style={{ padding: '1.25rem' }}>
                {/* Top Row: Candidate info + Badges + Actions */}
                <div
                  style={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'flex-start',
                    flexWrap: 'wrap',
                    gap: '1rem',
                  }}
                >
                  {/* Left Column: Candidate & Job Details */}
                  <div>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.65rem', flexWrap: 'wrap' }}>
                      <h3 style={{ fontSize: '1.2rem', fontWeight: 600, color: 'var(--slate-900)', margin: 0 }}>
                        {item.candidateName}
                      </h3>
                      <Badge
                        variant={
                          item.status === 'Completed'
                            ? 'success'
                            : item.status === 'Cancelled'
                            ? 'danger'
                            : item.status === 'InProgress'
                            ? 'warning'
                            : 'info'
                        }
                      >
                        {item.status}
                      </Badge>
                      <Badge variant="secondary">
                        Đơn #{item.applicationId} &bull; {item.applicationStatus}
                      </Badge>
                    </div>

                    <div
                      style={{
                        display: 'flex',
                        alignItems: 'center',
                        gap: '1.25rem',
                        marginTop: '0.5rem',
                        fontSize: '0.85rem',
                        color: 'var(--slate-600)',
                        flexWrap: 'wrap',
                      }}
                    >
                      <span style={{ fontWeight: 600, color: 'var(--slate-800)' }}>
                        Vị trí: {item.jobTitle || 'Chưa cập nhật'}
                      </span>
                      <span>Email: {item.candidateEmail}</span>
                      <span style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                        <Calendar size={14} color="var(--primary-600)" />
                        {new Date(item.scheduledDate).toLocaleString('vi-VN')}
                      </span>
                    </div>

                    {/* Interviewer & Question progress tags */}
                    <div
                      style={{
                        display: 'flex',
                        alignItems: 'center',
                        gap: '1rem',
                        marginTop: '0.65rem',
                        fontSize: '0.825rem',
                        flexWrap: 'wrap',
                      }}
                    >
                      {/* Interviewer */}
                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
                        {item.interviewerId ? (
                          <>
                            <UserCheck size={15} color="var(--primary-600)" />
                            <span style={{ color: 'var(--slate-700)' }}>
                              Interviewer: <strong>{item.interviewerName}</strong>
                            </span>
                            {(isAdmin || isHR) && (
                              <button
                                type="button"
                                className="btn-link"
                                style={{ fontSize: '0.75rem', color: 'var(--primary-600)', cursor: 'pointer' }}
                                onClick={() => setAssignModalItem(item)}
                              >
                                [Đổi]
                              </button>
                            )}
                          </>
                        ) : (
                          <>
                            <UserPlus size={15} color="var(--slate-400)" />
                            <span style={{ color: 'var(--slate-500)', fontStyle: 'italic' }}>
                              Chưa có Interviewer
                            </span>
                            {(isAdmin || isHR) && (
                              <button
                                type="button"
                                className="btn-link"
                                style={{ fontSize: '0.75rem', color: 'var(--primary-600)', cursor: 'pointer' }}
                                onClick={() => setAssignModalItem(item)}
                              >
                                [Phân công]
                              </button>
                            )}
                          </>
                        )}
                      </div>

                      {/* Scored Questions Count */}
                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', color: 'var(--slate-600)' }}>
                        <FileCheck2 size={15} color="var(--slate-500)" />
                        <span>
                          Tiến độ chấm:{' '}
                          <strong>
                            {item.scoredQuestions}/{item.totalQuestions}
                          </strong>{' '}
                          câu
                        </span>
                      </div>
                    </div>
                  </div>

                  {/* Right Column: AI Score & Recommendation & Decision */}
                  <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end', gap: '0.5rem' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', flexWrap: 'wrap' }}>
                      {/* AI Overall Score */}
                      {item.aiOverallScore !== null && item.aiOverallScore !== undefined ? (
                        <Badge variant="success" icon={Sparkles}>
                          Điểm AI: {item.aiOverallScore}/10
                        </Badge>
                      ) : (
                        <span
                          style={{
                            display: 'inline-flex',
                            alignItems: 'center',
                            gap: '0.3rem',
                            padding: '0.2rem 0.6rem',
                            borderRadius: 'var(--radius-full)',
                            fontSize: '0.75rem',
                            fontWeight: 600,
                            backgroundColor: 'var(--slate-100)',
                            color: 'var(--slate-700)',
                            border: '1px solid var(--slate-200)',
                          }}
                        >
                          <Clock size={13} />
                          Chưa đủ câu / Đang xử lý
                        </span>
                      )}

                      {/* Recommendation */}
                      {getRecommendationBadge(item.recommendation)}
                    </div>

                    {/* Human Decision Badge */}
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', marginTop: '0.25rem' }}>
                      <span style={{ fontSize: '0.75rem', color: 'var(--slate-500)' }}>Quyết định:</span>
                      {getHumanDecisionBadge(item)}
                    </div>
                  </div>
                </div>

                {/* Bottom Action Toolbar */}
                <div
                  style={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                    marginTop: '1rem',
                    paddingTop: '0.75rem',
                    borderTop: '1px solid var(--border-color)',
                    flexWrap: 'wrap',
                    gap: '0.75rem',
                  }}
                >
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                    {/* Expand Detail & Rubrics */}
                    <Button
                      size="sm"
                      variant="secondary"
                      icon={isExpanded ? ChevronUp : ChevronDown}
                      onClick={() => toggleExpand(item.id)}
                    >
                      {isExpanded ? 'Đóng chi tiết' : 'Xem chi tiết & Phản biện AI'}
                    </Button>

                    {/* Audit History */}
                    <Button
                      size="sm"
                      variant="secondary"
                      icon={History}
                      onClick={() => setHistoryModalItem(item)}
                    >
                      Lịch sử quyết định {item.hasDecision && '•'}
                    </Button>

                    {/* Direct Page Link */}
                    <Link to={`/interviews/${item.id}`} style={{ textDecoration: 'none' }}>
                      <Button size="sm" variant="secondary" icon={ExternalLink}>
                        Mở trang thẩm định
                      </Button>
                    </Link>
                  </div>

                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                    {/* Make Screening Decision (Admin/HR) */}
                    {(isAdmin || isHR) && (
                      <Button
                        size="sm"
                        variant="primary"
                        icon={Gavel}
                        onClick={() => setDecisionModalItem(item)}
                      >
                        Ra Quyết Định
                      </Button>
                    )}

                    {/* Delete / Cancel (Admin/HR) */}
                    {(isAdmin || isHR) && item.status !== 'Cancelled' && (
                      <button
                        type="button"
                        className="btn-icon"
                        onClick={() => handleDelete(item)}
                        title="Hủy buổi phỏng vấn"
                        style={{ padding: '0.4rem' }}
                      >
                        <Trash2 size={16} color="var(--danger-main)" />
                      </button>
                    )}
                  </div>
                </div>

                {/* Lazy Loaded Detail View */}
                {isExpanded && <LazyInterviewDetail interviewId={item.id} />}
              </Card>
            );
          })}
        </div>
      )}

      {/* Pagination Controls */}
      {pagedResult && pagedResult.totalCount > 0 && (
        <div style={{ marginTop: '0.5rem' }}>
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

      {/* Schedule Modal */}
      {(isAdmin || isHR) && (
        <InterviewScheduleModal
          isOpen={isScheduleModalOpen}
          onClose={() => setIsScheduleModalOpen(false)}
          onSave={async (payload) => {
            await createInterview(payload);
            setIsScheduleModalOpen(false);
          }}
          isLoading={isCreating}
        />
      )}

      {/* Assign Interviewer Modal */}
      {(isAdmin || isHR) && assignModalItem && (
        <AssignInterviewerModal
          isOpen={!!assignModalItem}
          interview={assignModalItem}
          onClose={() => setAssignModalItem(null)}
          onAssign={assignInterviewer}
          isLoading={isAssigning}
        />
      )}

      {/* Quick Screening Decision Modal */}
      {(isAdmin || isHR) && decisionModalItem && (
        <QuickScreeningDecisionModal
          isOpen={!!decisionModalItem}
          interview={decisionModalItem}
          onClose={() => setDecisionModalItem(null)}
          onConfirm={createScreeningDecision}
          isLoading={isDeciding}
        />
      )}

      {/* Screening Decision History Modal */}
      {historyModalItem && (
        <ScreeningDecisionHistoryModal
          isOpen={!!historyModalItem}
          interview={historyModalItem}
          onClose={() => setHistoryModalItem(null)}
        />
      )}
    </div>
  );
}
