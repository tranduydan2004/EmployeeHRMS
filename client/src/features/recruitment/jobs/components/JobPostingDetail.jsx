import { useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useJobPosting } from '../hooks/useJobPostings';
import { useJobPostingDetail, useJobQuestions } from '../hooks/useSmartJd';
import { usePermission } from '@/hooks/usePermission';
import { useAuthStore } from '@/features/auth';
import { useSmartJdStore } from '../stores/useSmartJdStore';
import { Card, Button, Badge, Skeleton } from '@/components';
import {
  Building2,
  Calendar,
  Send,
  ArrowLeft,
  Sparkles,
  Briefcase,
  Layers,
  Award,
  DollarSign,
  CheckCircle2,
  FileText,
  HeartHandshake,
  HelpCircle,
  ChevronDown,
  ChevronUp,
} from 'lucide-react';
import { useCandidates } from '@/features/recruitment/candidates';
import { toast } from '@/components/Toast/useToastStore';
import ApplyModal from './ApplyModal';
import SmartJdModal from './SmartJd/SmartJdModal';

const CATEGORY_COLORS = {
  Technical: { bg: '#eff6ff', text: '#1e40af', border: '#bfdbfe' },
  Behavioral: { bg: '#faf5ff', text: '#6b21a8', border: '#e9d5ff' },
  Situational: { bg: '#fff7ed', text: '#9a3412', border: '#fed7aa' },
  CulturalFit: { bg: '#f0fdfa', text: '#115e59', border: '#99f6e4' },
};

export default function JobPostingDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { isAuthenticated, canManageJobPostings } = usePermission();
  const { role } = useAuthStore();

  // Nếu là Admin/HR -> gọi endpoint detail đầy đủ (Phase 1). Ngược lại gọi endpoint public chuẩn
  const { data: legacyJob, isLoading: isLegacyLoading } = useJobPosting(id);
  const { data: richJob, isLoading: isRichLoading } = useJobPostingDetail(canManageJobPostings ? id : null);
  const { data: questions, isLoading: isQuestionsLoading } = useJobQuestions(canManageJobPostings ? id : null);

  const job = richJob || legacyJob;
  const isLoading = canManageJobPostings ? isRichLoading : isLegacyLoading;

  const { data: candidateData } = useCandidates({ pageNumber: 1, pageSize: 1 });
  const myCandidate = candidateData?.items?.[0];

  const [isApplyModalOpen, setIsApplyModalOpen] = useState(false);
  const [activeTab, setActiveTab] = useState('jd'); // 'jd' | 'questions'
  const [expandedQuestionIdx, setExpandedQuestionIdx] = useState(0);

  const openSmartJdModal = useSmartJdStore((state) => state.openModal);

  const handleApply = () => {
    if (!isAuthenticated) {
      toast.info('Vui lòng đăng nhập tài khoản Ứng viên để nộp hồ sơ.');
      navigate('/login', { state: { from: { pathname: `/jobs/${id}` } } });
      return;
    }
    setIsApplyModalOpen(true);
  };

  const handleEditWithAi = () => {
    if (richJob) {
      if ((richJob.status === 'Approved' || richJob.status === 'Published') && questions && questions.length > 0) {
        useSmartJdStore.getState().setApprovedJob(richJob, questions);
      } else {
        openSmartJdModal(richJob, 2);
      }
    }
  };

  if (isLoading) {
    return (
      <div style={{ maxWidth: '900px', margin: '0 auto' }}>
        <Skeleton height="36px" width="60%" />
        <div style={{ marginTop: '1rem' }}>
          <Skeleton height="350px" />
        </div>
      </div>
    );
  }

  if (!job) {
    return (
      <div style={{ textAlign: 'center', padding: '3rem' }}>
        <h2>Không tìm thấy tin tuyển dụng</h2>
        <Link to="/jobs" style={{ marginTop: '1rem', display: 'inline-block' }}>
          <Button variant="secondary" icon={ArrowLeft}>
            Quay lại danh sách
          </Button>
        </Link>
      </div>
    );
  }

  const jdContent = job.jdContent;

  return (
    <div style={{ maxWidth: '920px', margin: '0 auto' }}>
      <div style={{ marginBottom: '1.25rem' }}>
        <Link
          to="/jobs"
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: '0.35rem',
            color: 'var(--text-muted)',
            fontSize: '0.9rem',
          }}
        >
          <ArrowLeft size={16} /> Quay lại danh sách việc làm
        </Link>
      </div>

      <Card>
        {/* Header vị trí công việc */}
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
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', flexWrap: 'wrap' }}>
              <h1 style={{ fontSize: '1.75rem', fontWeight: 700, color: 'var(--slate-900)', margin: 0 }}>
                {job.title}
              </h1>
              <Badge
                variant={
                  job.status === 'Published'
                    ? 'success'
                    : job.status === 'Approved'
                      ? 'info'
                      : job.status === 'Draft'
                        ? 'warning'
                        : 'neutral'
                }
              >
                {job.status}
              </Badge>
            </div>

            <div
              style={{
                display: 'flex',
                gap: '1rem',
                marginTop: '0.75rem',
                flexWrap: 'wrap',
                color: 'var(--slate-600)',
                fontSize: '0.9rem',
              }}
            >
              <span style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                <Building2 size={16} color="var(--primary-600)" />
                {job.departmentName}
              </span>
              <span style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                <Calendar size={16} color="var(--slate-400)" />
                Đăng ngày: {new Date(job.createdDate).toLocaleDateString('vi-VN')}
              </span>
              {job.level && (
                <span style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                  <Briefcase size={16} color="var(--slate-400)" />
                  {job.level} • {job.workMode}
                </span>
              )}
            </div>
          </div>

          <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
            {canManageJobPostings && (
              <Button
                variant="secondary"
                icon={Sparkles}
                onClick={handleEditWithAi}
                style={{ color: '#7c3aed', borderColor: '#c7d2fe' }}
              >
                {job.status === 'Draft' ? 'Chỉnh Sửa JD (AI)' : 'Xem JD & Barem Câu Hỏi (AI)'}
              </Button>
            )}

            {(!role || role === 'Candidate') && (
              <Button variant="primary" size="lg" icon={Send} onClick={handleApply}>
                Nộp Đơn Ứng Tuyển
              </Button>
            )}
          </div>
        </div>

        {/* Structured Parameter Badges (Level, Skills, Salary...) */}
        {job.coreSkills && job.coreSkills.length > 0 && (
          <div
            style={{
              marginTop: '1.25rem',
              padding: '0.85rem 1rem',
              borderRadius: 'var(--radius-md)',
              backgroundColor: 'var(--slate-50)',
              border: '1px solid var(--slate-200)',
              display: 'flex',
              flexWrap: 'wrap',
              alignItems: 'center',
              gap: '0.65rem',
            }}
          >
            <span style={{ fontSize: '0.825rem', fontWeight: 600, color: 'var(--slate-700)' }}>
              Kỹ năng cốt lõi:
            </span>
            {job.coreSkills.map((skill, sIdx) => (
              <span
                key={sIdx}
                style={{
                  fontSize: '0.8rem',
                  fontWeight: 500,
                  backgroundColor: 'var(--primary-50)',
                  color: 'var(--primary-700)',
                  border: '1px solid var(--primary-200)',
                  borderRadius: 'var(--radius-full)',
                  padding: '0.15rem 0.6rem',
                }}
              >
                {skill}
              </span>
            ))}

            {job.salaryRange && (job.salaryRange.salaryMin || job.salaryRange.salaryMax) && (
              <span
                style={{
                  marginLeft: 'auto',
                  fontSize: '0.825rem',
                  fontWeight: 600,
                  color: '#059669',
                  backgroundColor: '#ecfdf5',
                  padding: '0.2rem 0.65rem',
                  borderRadius: 'var(--radius-md)',
                  border: '1px solid #a7f3d0',
                }}
              >
                💰 {job.salaryRange.salaryMin ? Number(job.salaryRange.salaryMin).toLocaleString() : ''}
                {job.salaryRange.salaryMin && job.salaryRange.salaryMax ? ' - ' : ''}
                {job.salaryRange.salaryMax ? Number(job.salaryRange.salaryMax).toLocaleString() : ''} {job.salaryRange.currency}
              </span>
            )}
          </div>
        )}

        {/* Admin/HR Navigation Tabs */}
        {canManageJobPostings && questions && questions.length > 0 && (
          <div
            style={{
              display: 'flex',
              gap: '1rem',
              borderBottom: '2px solid var(--border-color)',
              marginTop: '1.5rem',
              marginBottom: '1.25rem',
            }}
          >
            <button
              type="button"
              onClick={() => setActiveTab('jd')}
              style={{
                background: 'transparent',
                border: 'none',
                borderBottom: activeTab === 'jd' ? '2px solid var(--primary-600)' : '2px solid transparent',
                marginBottom: '-2px',
                padding: '0.6rem 0.5rem',
                fontSize: '0.95rem',
                fontWeight: activeTab === 'jd' ? 600 : 500,
                color: activeTab === 'jd' ? 'var(--primary-600)' : 'var(--text-muted)',
                cursor: 'pointer',
                display: 'flex',
                alignItems: 'center',
                gap: '0.4rem',
              }}
            >
              <FileText size={16} /> Nội dung JD
            </button>

            <button
              type="button"
              onClick={() => setActiveTab('questions')}
              style={{
                background: 'transparent',
                border: 'none',
                borderBottom: activeTab === 'questions' ? '2px solid #7c3aed' : '2px solid transparent',
                marginBottom: '-2px',
                padding: '0.6rem 0.5rem',
                fontSize: '0.95rem',
                fontWeight: activeTab === 'questions' ? 600 : 500,
                color: activeTab === 'questions' ? '#7c3aed' : 'var(--text-muted)',
                cursor: 'pointer',
                display: 'flex',
                alignItems: 'center',
                gap: '0.4rem',
              }}
            >
              <Sparkles size={16} color="#7c3aed" /> Ngân hàng câu hỏi & Barem ({questions.length})
            </button>
          </div>
        )}

        <hr style={{ margin: canManageJobPostings && questions?.length > 0 ? '0 0 1.5rem 0' : '1.5rem 0', borderColor: 'var(--border-color)' }} />

        {/* Tab 1: Nội dung JD (Hiển thị JdContent cấu trúc mới nếu có, fallback về description/requirements) */}
        {activeTab === 'jd' && (
          <div>
            {jdContent ? (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '1.75rem' }}>
                {/* Intro */}
                {jdContent.intro && (
                  <div>
                    <h3 style={{ fontSize: '1.15rem', fontWeight: 600, color: 'var(--slate-900)', marginBottom: '0.65rem' }}>
                      Giới thiệu vị trí (Overview)
                    </h3>
                    <div
                      style={{
                        lineHeight: 1.7,
                        color: 'var(--slate-700)',
                        backgroundColor: 'var(--slate-50)',
                        padding: '1.25rem',
                        borderRadius: 'var(--radius-md)',
                        fontSize: '0.925rem',
                      }}
                    >
                      {jdContent.intro}
                    </div>
                  </div>
                )}

                {/* Responsibilities */}
                {jdContent.responsibilities?.length > 0 && (
                  <div>
                    <h3 style={{ fontSize: '1.15rem', fontWeight: 600, color: 'var(--slate-900)', marginBottom: '0.65rem' }}>
                      Trách nhiệm công việc chính (Responsibilities)
                    </h3>
                    <ul style={{ paddingLeft: '1.25rem', margin: 0, display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                      {jdContent.responsibilities.map((r, rIdx) => (
                        <li key={rIdx} style={{ color: 'var(--slate-700)', lineHeight: 1.6, fontSize: '0.925rem' }}>
                          {r}
                        </li>
                      ))}
                    </ul>
                  </div>
                )}

                {/* MustHave */}
                {jdContent.mustHave?.length > 0 && (
                  <div>
                    <h3 style={{ fontSize: '1.15rem', fontWeight: 600, color: 'var(--slate-900)', marginBottom: '0.65rem' }}>
                      Yêu cầu bắt buộc (Must-Have Requirements)
                    </h3>
                    <ul style={{ paddingLeft: '1.25rem', margin: 0, display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                      {jdContent.mustHave.map((m, mIdx) => (
                        <li key={mIdx} style={{ color: 'var(--slate-700)', lineHeight: 1.6, fontSize: '0.925rem' }}>
                          {m}
                        </li>
                      ))}
                    </ul>
                  </div>
                )}

                {/* NiceToHave */}
                {jdContent.niceToHave?.length > 0 && (
                  <div>
                    <h3 style={{ fontSize: '1.15rem', fontWeight: 600, color: 'var(--slate-900)', marginBottom: '0.65rem' }}>
                      Yêu cầu ưu tiên / Điểm cộng (Nice-To-Have)
                    </h3>
                    <ul style={{ paddingLeft: '1.25rem', margin: 0, display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                      {jdContent.niceToHave.map((n, nIdx) => (
                        <li key={nIdx} style={{ color: 'var(--slate-700)', lineHeight: 1.6, fontSize: '0.925rem' }}>
                          {n}
                        </li>
                      ))}
                    </ul>
                  </div>
                )}

                {/* Benefits */}
                {jdContent.benefits?.length > 0 && (
                  <div>
                    <h3 style={{ fontSize: '1.15rem', fontWeight: 600, color: 'var(--slate-900)', marginBottom: '0.65rem' }}>
                      Quyền lợi & Phúc lợi (Benefits)
                    </h3>
                    <ul style={{ paddingLeft: '1.25rem', margin: 0, display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                      {jdContent.benefits.map((b, bIdx) => (
                        <li key={bIdx} style={{ color: 'var(--slate-700)', lineHeight: 1.6, fontSize: '0.925rem' }}>
                          {b}
                        </li>
                      ))}
                    </ul>
                  </div>
                )}
              </div>
            ) : (
              /* Fallback cho tin tuyển dụng cũ */
              <>
                <div style={{ marginBottom: '2rem' }}>
                  <h3 style={{ fontSize: '1.15rem', fontWeight: 600, color: 'var(--slate-900)', marginBottom: '0.75rem' }}>
                    Mô tả công việc (Job Description)
                  </h3>
                  <div
                    style={{
                      whiteSpace: 'pre-line',
                      lineHeight: 1.7,
                      color: 'var(--slate-700)',
                      backgroundColor: 'var(--slate-50)',
                      padding: '1.25rem',
                      borderRadius: 'var(--radius-md)',
                    }}
                  >
                    {job.description || 'Chưa có thông tin mô tả chi tiết.'}
                  </div>
                </div>

                <div>
                  <h3 style={{ fontSize: '1.15rem', fontWeight: 600, color: 'var(--slate-900)', marginBottom: '0.75rem' }}>
                    Yêu cầu chuyên môn (Requirements)
                  </h3>
                  <div
                    style={{
                      whiteSpace: 'pre-line',
                      lineHeight: 1.7,
                      color: 'var(--slate-700)',
                      backgroundColor: 'var(--slate-50)',
                      padding: '1.25rem',
                      borderRadius: 'var(--radius-md)',
                    }}
                  >
                    {job.requirements || 'Chưa có thông tin yêu cầu chi tiết.'}
                  </div>
                </div>
              </>
            )}
          </div>
        )}

        {/* Tab 2: Ngân hàng câu hỏi phỏng vấn & Barem (Admin / HR) */}
        {activeTab === 'questions' && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
            {questions?.map((item, qIdx) => {
              const isExpanded = expandedQuestionIdx === qIdx;
              const catStyle = CATEGORY_COLORS[item.category] || CATEGORY_COLORS.Technical;
              const rubric = item.scoringRubric;

              return (
                <div
                  key={item.id || qIdx}
                  style={{
                    border: '1px solid var(--border-color)',
                    borderRadius: 'var(--radius-md)',
                    backgroundColor: 'var(--bg-card)',
                    overflow: 'hidden',
                  }}
                >
                  <div
                    onClick={() => setExpandedQuestionIdx(isExpanded ? -1 : qIdx)}
                    style={{
                      padding: '1rem 1.25rem',
                      display: 'flex',
                      justifyContent: 'space-between',
                      alignItems: 'flex-start',
                      gap: '1rem',
                      cursor: 'pointer',
                      backgroundColor: isExpanded ? 'var(--slate-50)' : 'transparent',
                    }}
                  >
                    <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'flex-start', flex: 1 }}>
                      <span
                        style={{
                          width: '26px',
                          height: '26px',
                          borderRadius: 'var(--radius-full)',
                          backgroundColor: '#f3e8ff',
                          color: '#7c3aed',
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                          fontWeight: 700,
                          fontSize: '0.8rem',
                          flexshrink: 0,
                        }}
                      >
                        {qIdx + 1}
                      </span>
                      <div>
                        <h4 style={{ margin: 0, fontSize: '0.95rem', fontWeight: 600, color: 'var(--slate-900)' }}>
                          {item.question}
                        </h4>
                        <div style={{ display: 'flex', gap: '0.5rem', marginTop: '0.45rem' }}>
                          <span
                            style={{
                              fontSize: '0.75rem',
                              fontWeight: 600,
                              padding: '0.15rem 0.55rem',
                              borderRadius: 'var(--radius-full)',
                              backgroundColor: catStyle.bg,
                              color: catStyle.text,
                              border: `1px solid ${catStyle.border}`,
                            }}
                          >
                            {item.category}
                          </span>
                          <span style={{ fontSize: '0.75rem', color: 'var(--slate-600)' }}>
                            Độ khó: {item.difficulty}
                          </span>
                        </div>
                      </div>
                    </div>

                    <button type="button" style={{ border: 'none', background: 'transparent', cursor: 'pointer' }}>
                      {isExpanded ? <ChevronUp size={18} /> : <ChevronDown size={18} />}
                    </button>
                  </div>

                  {isExpanded && rubric && (
                    <div
                      style={{
                        padding: '1.25rem',
                        borderTop: '1px solid var(--border-color)',
                        display: 'grid',
                        gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
                        gap: '0.75rem',
                      }}
                    >
                      <div style={{ padding: '0.75rem', borderRadius: 'var(--radius-md)', backgroundColor: '#ecfdf5', border: '1px solid #a7f3d0' }}>
                        <div style={{ fontWeight: 700, fontSize: '0.75rem', color: '#065f46', marginBottom: '0.25rem' }}>
                          🌟 Xuất Sắc (9-10đ)
                        </div>
                        <p style={{ margin: 0, fontSize: '0.78rem', color: '#047857', lineHeight: 1.4 }}>
                          {rubric.excellent}
                        </p>
                      </div>

                      <div style={{ padding: '0.75rem', borderRadius: 'var(--radius-md)', backgroundColor: '#eff6ff', border: '1px solid #bfdbfe' }}>
                        <div style={{ fontWeight: 700, fontSize: '0.75rem', color: '#1e40af', marginBottom: '0.25rem' }}>
                          👍 Tốt (7-8đ)
                        </div>
                        <p style={{ margin: 0, fontSize: '0.78rem', color: '#1d4ed8', lineHeight: 1.4 }}>
                          {rubric.good}
                        </p>
                      </div>

                      <div style={{ padding: '0.75rem', borderRadius: 'var(--radius-md)', backgroundColor: '#fffbeb', border: '1px solid #fde68a' }}>
                        <div style={{ fontWeight: 700, fontSize: '0.75rem', color: '#92400e', marginBottom: '0.25rem' }}>
                          👌 Chấp Nhận (5-6đ)
                        </div>
                        <p style={{ margin: 0, fontSize: '0.78rem', color: '#b45309', lineHeight: 1.4 }}>
                          {rubric.acceptable}
                        </p>
                      </div>

                      <div style={{ padding: '0.75rem', borderRadius: 'var(--radius-md)', backgroundColor: '#fef2f2', border: '1px solid #fecaca' }}>
                        <div style={{ fontWeight: 700, fontSize: '0.75rem', color: '#991b1b', marginBottom: '0.25rem' }}>
                          ⚠️ Kém (&lt;5đ)
                        </div>
                        <p style={{ margin: 0, fontSize: '0.78rem', color: '#b91c1c', lineHeight: 1.4 }}>
                          {rubric.poor}
                        </p>
                      </div>
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </Card>

      <ApplyModal
        isOpen={isApplyModalOpen}
        onClose={() => setIsApplyModalOpen(false)}
        job={job}
        candidate={myCandidate}
      />

      {canManageJobPostings && <SmartJdModal />}
    </div>
  );
}
