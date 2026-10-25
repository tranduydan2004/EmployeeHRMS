import { Modal, Button, Badge, Skeleton } from '@/components';
import { useScreeningDecisions } from '../hooks/useInterviews';
import {
  History,
  CheckCircle2,
  XCircle,
  AlertCircle,
  User,
  Calendar,
  Sparkles,
  ShieldAlert,
  FileText,
} from 'lucide-react';

export default function ScreeningDecisionHistoryModal({ isOpen, onClose, interview }) {
  const { data: decisions = [], isLoading } = useScreeningDecisions(interview?.id);

  if (!interview) return null;

  const getDecisionBadge = (type) => {
    switch (type) {
      case 'AdvanceToInDepth':
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
            Chuyển vòng chuyên sâu
          </span>
        );
      case 'HoldForReview':
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
            Tạm hoãn thẩm định
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
            Từ chối hồ sơ
          </span>
        );
      default:
        return <Badge variant="secondary">{type}</Badge>;
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Lịch Sử Quyết Định Sơ Loại (Audit Trail)"
      maxWidth="650px"
      footer={
        <div style={{ display: 'flex', justifyContent: 'flex-end', width: '100%' }}>
          <Button variant="secondary" onClick={onClose}>
            Đóng
          </Button>
        </div>
      }
    >
      <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
        <div
          style={{
            backgroundColor: 'var(--slate-50)',
            padding: '0.75rem 1rem',
            borderRadius: 'var(--radius-md)',
            border: '1px solid var(--border-color)',
            fontSize: '0.85rem',
          }}
        >
          Ứng viên: <strong>{interview.candidateName}</strong> &bull; Vị trí:{' '}
          <strong>{interview.jobTitle}</strong> &bull; Phỏng vấn #{interview.id}
        </div>

        {isLoading ? (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
            <Skeleton height="70px" />
            <Skeleton height="70px" />
          </div>
        ) : decisions.length === 0 ? (
          <div
            style={{
              textAlign: 'center',
              padding: '2.5rem 1rem',
              color: 'var(--slate-500)',
            }}
          >
            <History size={40} color="var(--slate-400)" style={{ margin: '0 auto 0.75rem auto' }} />
            <p style={{ fontWeight: 600, margin: 0 }}>Chưa có quyết định sơ loại nào</p>
            <span style={{ fontSize: '0.8rem' }}>
              Quyết định từ HR/Interviewer sẽ được lưu vết bất biến tại đây.
            </span>
          </div>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
            {decisions.map((dec, idx) => (
              <div
                key={dec.id || idx}
                style={{
                  padding: '1rem',
                  borderRadius: 'var(--radius-md)',
                  border: '1px solid var(--border-color)',
                  backgroundColor: 'var(--bg-card)',
                  display: 'flex',
                  flexDirection: 'column',
                  gap: '0.65rem',
                }}
              >
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '0.5rem' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                    {getDecisionBadge(dec.decisionType)}
                    <span style={{ fontSize: '0.8rem', color: 'var(--slate-500)' }}>
                      #{dec.id}
                    </span>
                  </div>

                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', fontSize: '0.8rem', color: 'var(--slate-500)' }}>
                    <Calendar size={13} />
                    <span>{new Date(dec.decidedAt).toLocaleString('vi-VN')}</span>
                  </div>
                </div>

                <div style={{ display: 'flex', alignItems: 'center', gap: '1.25rem', fontSize: '0.825rem', color: 'var(--slate-700)', flexWrap: 'wrap' }}>
                  <span style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                    <User size={14} color="var(--primary-600)" />
                    Người quyết định: <strong>{dec.decidedByUserName || `User #${dec.decidedByUserId}`}</strong> ({dec.decidedByRole})
                  </span>

                  <span style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                    <Sparkles size={14} color="var(--warning-main)" />
                    Gợi ý AI lúc đó: <strong>{dec.systemRecommendation || 'N/A'}</strong> (Điểm: {dec.systemOverallScore !== null ? `${dec.systemOverallScore}/10` : 'N/A'})
                  </span>
                </div>

                {dec.overrideReason && (
                  <div
                    style={{
                      padding: '0.5rem 0.75rem',
                      backgroundColor: 'var(--warning-bg)',
                      borderRadius: 'var(--radius-sm)',
                      border: '1px solid var(--warning-border)',
                      fontSize: '0.8rem',
                      color: 'var(--warning-text)',
                      display: 'flex',
                      flexDirection: 'column',
                      gap: '0.25rem',
                    }}
                  >
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', fontWeight: 600 }}>
                      <ShieldAlert size={14} />
                      <span>Lý do ghi đè gợi ý AI:</span>
                    </div>
                    <span style={{ color: 'var(--slate-800)' }}>{dec.overrideReason}</span>
                  </div>
                )}

                {dec.notes && (
                  <div style={{ fontSize: '0.8rem', color: 'var(--slate-600)', display: 'flex', alignItems: 'flex-start', gap: '0.35rem' }}>
                    <FileText size={14} style={{ flexShrink: 0, marginTop: '2px' }} />
                    <span>Ghi chú: {dec.notes}</span>
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </Modal>
  );
}
