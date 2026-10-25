import { useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useInterview, useInterviewMutations } from '../hooks/useInterviews';
import { usePermission } from '@/hooks/usePermission';
import RubricLevelDistribution from './RubricLevelDistribution';
import QuestionEvaluationCard from './QuestionEvaluationCard';
import AssignInterviewerModal from './AssignInterviewerModal';
import QuickScreeningDecisionModal from './QuickScreeningDecisionModal';
import ScreeningDecisionHistoryModal from './ScreeningDecisionHistoryModal';
import ErasePersonalDataModal from './ErasePersonalDataModal';
import { Card, Button, Badge, Skeleton } from '@/components';
import {
  ArrowLeft,
  Sparkles,
  Calendar,
  UserCheck,
  UserPlus,
  Gavel,
  History,
  CheckCircle2,
  AlertCircle,
  XCircle,
  Clock,
  Briefcase,
  Mail,
  ShieldAlert,
  Info,
  FileCheck2,
  Trash2,
} from 'lucide-react';

export default function InterviewDetailView() {
  const { id } = useParams();
  const navigate = useNavigate();
  const interviewId = parseInt(id, 10);

  const { data: interview, isLoading, error } = useInterview(interviewId);
  const {
    assignInterviewer,
    createScreeningDecision,
    erasePersonalData,
    isAssigning,
    isDeciding,
    isErasingPersonalData,
  } = useInterviewMutations();
  const { isAdmin, isHR } = usePermission();

  // Modals
  const [isAssignModalOpen, setIsAssignModalOpen] = useState(false);
  const [isDecisionModalOpen, setIsDecisionModalOpen] = useState(false);
  const [isHistoryModalOpen, setIsHistoryModalOpen] = useState(false);
  const [isEraseModalOpen, setIsEraseModalOpen] = useState(false);

  if (isLoading) {
    return (
      <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem', padding: '1rem 0' }}>
        <Skeleton height="40px" width="300px" />
        <Card style={{ padding: '1.5rem' }}>
          <Skeleton height="80px" />
        </Card>
        <Card style={{ padding: '1.5rem' }}>
          <Skeleton height="150px" />
        </Card>
      </div>
    );
  }

  if (error || !interview) {
    return (
      <div style={{ padding: '2rem 1rem', textAlign: 'center' }}>
        <h3 style={{ color: 'var(--danger-text)' }}>Không tìm thấy buổi phỏng vấn</h3>
        <p style={{ color: 'var(--text-muted)', marginTop: '0.5rem' }}>
          Buổi phỏng vấn #{id} không tồn tại hoặc bạn không có quyền truy cập.
        </p>
        <Button variant="secondary" icon={ArrowLeft} onClick={() => navigate('/interviews')} style={{ marginTop: '1rem' }}>
          Quay lại danh sách
        </Button>
      </div>
    );
  }

  const getRecommendationBadge = (rec) => {
    switch (rec) {
      case 'Pass':
        return (
          <span
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '0.35rem',
              padding: '0.3rem 0.75rem',
              borderRadius: 'var(--radius-full)',
              fontSize: '0.8rem',
              fontWeight: 700,
              backgroundColor: 'var(--success-bg)',
              color: 'var(--success-text)',
              border: '1px solid var(--success-border)',
            }}
          >
            <CheckCircle2 size={14} />
            Gợi ý sơ bộ: Đạt (Pass)
          </span>
        );
      case 'Consider':
        return (
          <span
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '0.35rem',
              padding: '0.3rem 0.75rem',
              borderRadius: 'var(--radius-full)',
              fontSize: '0.8rem',
              fontWeight: 700,
              backgroundColor: 'var(--warning-bg)',
              color: 'var(--warning-text)',
              border: '1px solid var(--warning-border)',
            }}
          >
            <AlertCircle size={14} />
            Gợi ý sơ bộ: Cân nhắc (Consider)
          </span>
        );
      case 'Reject':
        return (
          <span
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '0.35rem',
              padding: '0.3rem 0.75rem',
              borderRadius: 'var(--radius-full)',
              fontSize: '0.8rem',
              fontWeight: 700,
              backgroundColor: 'var(--danger-bg)',
              color: 'var(--danger-text)',
              border: '1px solid var(--danger-border)',
            }}
          >
            <XCircle size={14} />
            Gợi ý sơ bộ: Chưa đạt (Reject)
          </span>
        );
      default:
        return (
          <span
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '0.35rem',
              padding: '0.3rem 0.75rem',
              borderRadius: 'var(--radius-full)',
              fontSize: '0.8rem',
              fontWeight: 600,
              backgroundColor: 'var(--slate-100)',
              color: 'var(--slate-600)',
              border: '1px solid var(--slate-200)',
            }}
          >
            <Clock size={14} />
            Chưa có gợi ý từ hệ thống
          </span>
        );
    }
  };

  const scoredCount = interview.questions?.filter(
    (q) => q.answer?.evaluations?.some((e) => e.isActive && e.score !== null)
  ).length || 0;
  const totalCount = interview.questions?.length || 0;

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
      {/* Top Breadcrumb & Action Toolbar */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '1rem' }}>
        <Link
          to="/interviews"
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: '0.5rem',
            color: 'var(--slate-600)',
            textDecoration: 'none',
            fontSize: '0.875rem',
            fontWeight: 500,
          }}
        >
          <ArrowLeft size={16} />
          <span>Quay lại Danh sách phỏng vấn</span>
        </Link>

        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', flexWrap: 'wrap' }}>
          {/* View Decision History */}
          <Button
            variant="secondary"
            size="sm"
            icon={History}
            onClick={() => setIsHistoryModalOpen(true)}
          >
            Lịch Sử Quyết Định
          </Button>

          {/* Assign Interviewer (Admin/HR) */}
          {(isAdmin || isHR) && (
            <Button
              variant="secondary"
              size="sm"
              icon={UserCheck}
              onClick={() => setIsAssignModalOpen(true)}
            >
              {interview.interviewerId ? 'Đổi Interviewer' : 'Phân Công Interviewer'}
            </Button>
          )}

          {/* Make Screening Decision (Admin/HR) */}
          {(isAdmin || isHR) && (
            <Button
              variant="primary"
              size="sm"
              icon={Gavel}
              onClick={() => setIsDecisionModalOpen(true)}
            >
              Ra Quyết Định Tuyển Dụng
            </Button>
          )}

          {/* Right to Erasure / Personal Data Erasure (Admin/HR) */}
          {(isAdmin || isHR) && (
            interview.isPersonalDataErased ? (
              <span
                style={{
                  display: 'inline-flex',
                  alignItems: 'center',
                  gap: '0.35rem',
                  padding: '0.3rem 0.65rem',
                  borderRadius: 'var(--radius-full)',
                  fontSize: '0.775rem',
                  fontWeight: 600,
                  backgroundColor: 'var(--slate-100)',
                  color: 'var(--slate-600)',
                  border: '1px solid var(--slate-300)',
                }}
                title="Dữ liệu cá nhân (file audio và transcript) đã được xóa"
              >
                <ShieldAlert size={14} />
                Đã Xóa Dữ Liệu Cá Nhân
              </span>
            ) : (
              <Button
                variant="outline"
                size="sm"
                icon={Trash2}
                onClick={() => setIsEraseModalOpen(true)}
                title="Xóa dữ liệu cá nhân theo luật Bảo vệ dữ liệu cá nhân số 91/2025/QH15"
                style={{ color: 'var(--danger-text)', borderColor: 'var(--danger-border)' }}
              >
                Xóa Dữ Liệu Cá Nhân
              </Button>
            )
          )}
        </div>
      </div>

      {/* Privacy Notice Banner if personal data has been erased */}
      {interview.isPersonalDataErased && (
        <div
          style={{
            padding: '0.875rem 1.25rem',
            backgroundColor: 'var(--slate-50)',
            borderRadius: 'var(--radius-md)',
            border: '1px solid var(--slate-300)',
            display: 'flex',
            alignItems: 'flex-start',
            gap: '0.75rem',
            color: 'var(--slate-700)',
            fontSize: '0.875rem',
            lineHeight: 1.5,
          }}
        >
          <ShieldAlert size={20} color="var(--slate-500)" style={{ flexShrink: 0, marginTop: '2px' }} />
          <div>
            <strong>Chính sách quyền riêng tư dữ liệu cá nhân (Luật số 91/2025/QH15):</strong> Toàn bộ file ghi âm giọng nói, bản bóc băng và trích dẫn bằng chứng của buổi phỏng vấn này đã được làm sạch và xóa an toàn khỏi máy chủ lưu trữ. Điểm số rubric và lịch sử quyết định tuyển dụng vẫn được bảo lưu để phục vụ kiểm toán.
          </div>
        </div>
      )}

      {/* Main Executive Candidate Card (Evidence-First Layout) */}
      <Card style={{ padding: '1.5rem' }}>
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))',
            gap: '1.5rem',
            alignItems: 'flex-start',
          }}
        >
          {/* Left Block: Candidate & Application Details */}
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', flexWrap: 'wrap' }}>
              <h2 style={{ fontSize: '1.4rem', fontWeight: 700, color: 'var(--slate-900)', margin: 0 }}>
                {interview.candidateName || `Ứng viên (Hồ sơ #${interview.applicationId})`}
              </h2>
              <Badge
                variant={
                  interview.status === 'Completed'
                    ? 'success'
                    : interview.status === 'Cancelled'
                      ? 'danger'
                      : interview.status === 'InProgress'
                        ? 'warning'
                        : 'info'
                }
              >
                {interview.status}
              </Badge>
              {interview.applicationStatus && (
                <Badge variant="secondary">Vòng: {interview.applicationStatus}</Badge>
              )}
            </div>

            <div
              style={{
                display: 'flex',
                flexDirection: 'column',
                gap: '0.4rem',
                marginTop: '0.75rem',
                fontSize: '0.875rem',
                color: 'var(--slate-600)',
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                <Briefcase size={16} color="var(--primary-600)" />
                <span>
                  Vị trí: <strong>{interview.jobTitle || 'Chưa cập nhật'}</strong> &bull; Đơn ứng tuyển: #{interview.applicationId}
                </span>
              </div>
              {interview.candidateEmail && (
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                  <Mail size={16} color="var(--slate-500)" />
                  <span>Email: {interview.candidateEmail}</span>
                </div>
              )}
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                <Calendar size={16} color="var(--slate-500)" />
                <span>Ngày tạo/mời: {new Date(interview.scheduledDate).toLocaleString('vi-VN')}</span>
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                <UserCheck size={16} color="var(--primary-600)" />
                <span>
                  Interviewer phụ trách:{' '}
                  {interview.interviewerName ? (
                    <strong>{interview.interviewerName}</strong>
                  ) : (
                    <span style={{ color: 'var(--warning-text)', fontStyle: 'italic' }}>Chưa phân công</span>
                  )}
                </span>
              </div>
            </div>
          </div>

          {/* Right Block: AI Consultation & Evidence Status (Softened Framing) */}
          <div
            style={{
              padding: '1.25rem',
              backgroundColor: 'var(--slate-50)',
              borderRadius: 'var(--radius-md)',
              border: '1px solid var(--border-color)',
              display: 'flex',
              flexDirection: 'column',
              gap: '0.75rem',
            }}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '0.5rem' }}>
              <span style={{ fontSize: '0.8rem', fontWeight: 600, color: 'var(--slate-700)', display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                <Sparkles size={15} color="var(--primary-600)" />
                <span>Tham Vấn Sơ Bộ Từ AI (Decision Support)</span>
              </span>
              <span style={{ fontSize: '0.75rem', color: 'var(--slate-500)' }}>
                Tiến độ: <strong>{scoredCount}/{totalCount}</strong> câu đã chấm
              </span>
            </div>

            <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', flexWrap: 'wrap' }}>
              {/* Overall AI Score */}
              {interview.aiOverallScore !== null && interview.aiOverallScore !== undefined ? (
                <div
                  style={{
                    padding: '0.3rem 0.75rem',
                    borderRadius: 'var(--radius-full)',
                    backgroundColor: 'var(--success-bg)',
                    color: 'var(--success-text)',
                    border: '1px solid var(--success-border)',
                    fontSize: '0.85rem',
                    fontWeight: 700,
                    display: 'flex',
                    alignItems: 'center',
                    gap: '0.35rem',
                  }}
                >
                  <Sparkles size={14} />
                  <span>Điểm tổng AI: {interview.aiOverallScore}/10</span>
                </div>
              ) : (
                <div
                  style={{
                    padding: '0.3rem 0.75rem',
                    borderRadius: 'var(--radius-full)',
                    backgroundColor: 'var(--slate-100)',
                    color: 'var(--slate-700)',
                    border: '1px solid var(--slate-200)',
                    fontSize: '0.8rem',
                    fontWeight: 600,
                    display: 'flex',
                    alignItems: 'center',
                    gap: '0.35rem',
                  }}
                >
                  <Clock size={14} />
                  <span>Chưa đủ câu / Đang xử lý</span>
                </div>
              )}

              {/* Recommendation */}
              {getRecommendationBadge(interview.recommendation)}
            </div>

            <div
              style={{
                fontSize: '0.775rem',
                color: 'var(--slate-600)',
                lineHeight: 1.45,
                padding: '0.5rem 0.75rem',
                backgroundColor: '#ffffff',
                borderRadius: 'var(--radius-sm)',
                border: '1px solid var(--border-color)',
              }}
            >
              <Info size={13} style={{ display: 'inline', marginRight: '4px', verticalAlign: 'text-top' }} />
              <strong>Nguyên tắc thẩm định:</strong> Gợi ý sơ bộ không quyết định thay con người. Hãy đối chiếu các trích dẫn bằng chứng trong audio và bản bóc băng trước khi đưa ra quyết định cuối cùng.
            </div>
          </div>
        </div>
      </Card>

      {/* Visualizer: Segmented Rubric Distribution Across Questions */}
      {interview.questions && interview.questions.length > 0 && (
        <RubricLevelDistribution questions={interview.questions} />
      )}

      {/* Main Evidence & Question Evaluation Debate Panel */}
      <Card style={{ padding: '1.5rem' }}>
        <div style={{ marginBottom: '1.25rem' }}>
          <h3 style={{ fontSize: '1.15rem', fontWeight: 600, color: 'var(--slate-900)', margin: 0 }}>
            Chi Tiết Câu Trả Lời & Phản Biện Rubric AI (Evidence & Debate)
          </h3>
          <p style={{ fontSize: '0.825rem', color: 'var(--slate-600)', marginTop: '0.25rem' }}>
            Lắng nghe bản ghi âm thực tế, chỉnh sửa bản bóc băng nếu STT nhận diện sai, và kích hoạt chấm lại để AI cập nhật đánh giá.
          </p>
        </div>

        <QuestionEvaluationCard interview={interview} />
      </Card>

      {/* Sticky Bottom Call to Action for HR/Admin Decision */}
      {(isAdmin || isHR) && (
        <div
          style={{
            position: 'sticky',
            bottom: '1rem',
            padding: '1rem 1.5rem',
            backgroundColor: '#ffffff',
            borderRadius: 'var(--radius-md)',
            border: '2px solid var(--primary-500)',
            boxShadow: 'var(--shadow-lg)',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            flexWrap: 'wrap',
            gap: '1rem',
            zIndex: 10,
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
            <Gavel size={22} color="var(--primary-600)" />
            <div>
              <div style={{ fontWeight: 600, color: 'var(--slate-900)', fontSize: '0.95rem' }}>
                Đã hoàn tất thẩm định bằng chứng phỏng vấn?
              </div>
              <div style={{ fontSize: '0.8rem', color: 'var(--slate-600)' }}>
                Ghi nhận quyết định chuyển vòng chuyên sâu, tạm hoãn hoặc từ chối vào hồ sơ kiểm toán bất biến.
              </div>
            </div>
          </div>

          <Button
            variant="primary"
            icon={Gavel}
            onClick={() => setIsDecisionModalOpen(true)}
          >
            Ra Quyết Định Sơ Loại
          </Button>
        </div>
      )}

      {/* Modals */}
      {(isAdmin || isHR) && (
        <AssignInterviewerModal
          isOpen={isAssignModalOpen}
          interview={{
            id: interview.id,
            candidateName: interview.candidateName,
            jobTitle: interview.jobTitle,
            applicationId: interview.applicationId,
            interviewerId: interview.interviewerId,
            interviewerName: interview.interviewerName,
          }}
          onClose={() => setIsAssignModalOpen(false)}
          onAssign={assignInterviewer}
          isLoading={isAssigning}
        />
      )}

      {(isAdmin || isHR) && (
        <QuickScreeningDecisionModal
          isOpen={isDecisionModalOpen}
          interview={{
            id: interview.id,
            candidateName: interview.candidateName,
            candidateEmail: interview.candidateEmail,
            jobTitle: interview.jobTitle,
            applicationId: interview.applicationId,
            scoredQuestions: scoredCount,
            totalQuestions: totalCount,
            aiOverallScore: interview.aiOverallScore,
            recommendation: interview.recommendation,
          }}
          onClose={() => setIsDecisionModalOpen(false)}
          onConfirm={createScreeningDecision}
          isLoading={isDeciding}
        />
      )}

      <ScreeningDecisionHistoryModal
        isOpen={isHistoryModalOpen}
        interview={{
          id: interview.id,
          candidateName: interview.candidateName,
          jobTitle: interview.jobTitle,
        }}
        onClose={() => setIsHistoryModalOpen(false)}
      />

      {(isAdmin || isHR) && (
        <ErasePersonalDataModal
          isOpen={isEraseModalOpen}
          onClose={() => setIsEraseModalOpen(false)}
          interview={{
            id: interview.id,
            candidateName: interview.candidateName,
            jobTitle: interview.jobTitle,
            applicationId: interview.applicationId,
          }}
          onConfirm={erasePersonalData}
          isLoading={isErasingPersonalData}
        />
      )}
    </div>
  );
}
