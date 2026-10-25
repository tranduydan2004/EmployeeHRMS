import { useState, useEffect } from 'react';
import { Modal, Button, Badge } from '@/components';
import {
  Gavel,
  AlertTriangle,
  CheckCircle2,
  XCircle,
  HelpCircle,
  Clock,
  Sparkles,
  Info,
  ShieldAlert,
} from 'lucide-react';

export default function QuickScreeningDecisionModal({
  isOpen,
  onClose,
  interview,
  onConfirm,
  isLoading,
}) {
  const [decisionType, setDecisionType] = useState('AdvanceToInDepth');
  const [overrideReason, setOverrideReason] = useState('');
  const [notes, setNotes] = useState('');
  const [validationError, setValidationError] = useState('');

  useEffect(() => {
    if (isOpen && interview) {
      // Default human decision based on system recommendation if reasonable
      if (interview.recommendation === 'Pass') {
        setDecisionType('AdvanceToInDepth');
      } else if (interview.recommendation === 'Reject') {
        setDecisionType('Reject');
      } else {
        setDecisionType('HoldForReview');
      }
      setOverrideReason('');
      setNotes('');
      setValidationError('');
    }
  }, [isOpen, interview]);

  if (!interview) return null;

  // Determine if this human decision is overriding the system recommendation
  const rec = interview.recommendation; // "Pass", "Consider", "Reject", or null
  const isOverriding =
    (rec === 'Pass' && (decisionType === 'Reject' || decisionType === 'HoldForReview')) ||
    (rec === 'Reject' && decisionType === 'AdvanceToInDepth') ||
    (rec === 'Consider' && decisionType === 'Reject');

  const handleSubmit = async (e) => {
    e.preventDefault();
    setValidationError('');

    if (isOverriding && (!overrideReason || overrideReason.trim().length < 10)) {
      setValidationError('Quy trình AI có trách nhiệm: Bạn phải nhập lý do ghi đè gợi ý AI (tối thiểu 10 ký tự).');
      return;
    }

    try {
      await onConfirm({
        interviewId: interview.id,
        payload: {
          decisionType,
          overrideReason: isOverriding ? overrideReason.trim() : (overrideReason.trim() || null),
          notes: notes.trim() || null,
        },
      });
      onClose();
    } catch (err) {
      const msg = err.response?.data?.error || err.message || 'Lỗi khi ghi nhận quyết định.';
      setValidationError(msg);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={!isLoading ? onClose : undefined}
      title="Quyết Định Sơ Loại Nhân Sự (Screening Decision)"
      maxWidth="620px"
      footer={
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', width: '100%' }}>
          <span style={{ fontSize: '0.75rem', color: 'var(--slate-500)', fontStyle: 'italic' }}>
            * Con người giữ toàn quyền quyết định tuyển dụng cuối cùng
          </span>
          <div style={{ display: 'flex', gap: '0.75rem' }}>
            <Button variant="secondary" onClick={onClose} disabled={isLoading}>
              Hủy
            </Button>
            <Button
              variant={decisionType === 'Reject' ? 'danger' : decisionType === 'AdvanceToInDepth' ? 'primary' : 'warning'}
              icon={Gavel}
              onClick={handleSubmit}
              isLoading={isLoading}
            >
              Lưu Quyết Định
            </Button>
          </div>
        </div>
      }
    >
      <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
        {/* Candidate & AI Snapshot Banner */}
        <div
          style={{
            backgroundColor: 'var(--slate-50)',
            padding: '1rem',
            borderRadius: 'var(--radius-md)',
            border: '1px solid var(--border-color)',
            display: 'flex',
            flexDirection: 'column',
            gap: '0.65rem',
          }}
        >
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '0.5rem' }}>
            <div>
              <h4 style={{ margin: 0, fontSize: '1rem', fontWeight: 600, color: 'var(--slate-900)' }}>
                {interview.candidateName}
              </h4>
              <p style={{ margin: 0, fontSize: '0.825rem', color: 'var(--slate-600)' }}>
                {interview.candidateEmail} &bull; Vị trí: <strong>{interview.jobTitle}</strong>
              </p>
            </div>
            <div style={{ textAlign: 'right' }}>
              <span style={{ fontSize: '0.775rem', color: 'var(--slate-500)', display: 'block' }}>Tiến độ chấm:</span>
              <span style={{ fontSize: '0.85rem', fontWeight: 600, color: 'var(--slate-800)' }}>
                {interview.scoredQuestions}/{interview.totalQuestions} câu hỏi đã chấm
              </span>
            </div>
          </div>

          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '1rem',
              paddingTop: '0.65rem',
              borderTop: '1px solid var(--border-color)',
              flexWrap: 'wrap',
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', fontSize: '0.85rem' }}>
              <Sparkles size={16} color="var(--primary-600)" />
              <span style={{ color: 'var(--slate-600)' }}>Điểm tổng AI:</span>
              <strong>
                {interview.aiOverallScore !== null && interview.aiOverallScore !== undefined
                  ? `${interview.aiOverallScore}/10`
                  : 'Chưa đủ câu / Đang xử lý'}
              </strong>
            </div>

            <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', fontSize: '0.85rem' }}>
              <Info size={16} color="var(--slate-500)" />
              <span style={{ color: 'var(--slate-600)' }}>Gợi ý sơ bộ:</span>
              {interview.recommendation === 'Pass' ? (
                <span style={{ color: 'var(--success-text)', fontWeight: 700 }}>Đạt (Pass)</span>
              ) : interview.recommendation === 'Consider' ? (
                <span style={{ color: 'var(--warning-text)', fontWeight: 700 }}>Cân nhắc (Consider)</span>
              ) : interview.recommendation === 'Reject' ? (
                <span style={{ color: 'var(--danger-text)', fontWeight: 700 }}>Chưa đạt (Reject)</span>
              ) : (
                <span style={{ color: 'var(--slate-500)', fontStyle: 'italic' }}>Chưa có gợi ý</span>
              )}
            </div>
          </div>
        </div>

        {/* Responsible AI Disclaimer */}
        <div
          style={{
            padding: '0.75rem 1rem',
            backgroundColor: 'var(--primary-50)',
            borderRadius: 'var(--radius-sm)',
            border: '1px solid var(--primary-200)',
            fontSize: '0.825rem',
            color: 'var(--primary-900)',
            display: 'flex',
            alignItems: 'flex-start',
            gap: '0.5rem',
          }}
        >
          <Info size={16} color="var(--primary-700)" style={{ flexShrink: 0, marginTop: '2px' }} />
          <div>
            <strong>Nguyên tắc AI có trách nhiệm:</strong> Kết quả AI chỉ mang tính chất tham vấn sơ bộ. Bạn là người quyết định cuối cùng. Không thực hiện duyệt tự động mà không xem xét bằng chứng.
          </div>
        </div>

        {/* Decision Selection Options */}
        <div>
          <label style={{ display: 'block', fontSize: '0.875rem', fontWeight: 600, color: 'var(--slate-800)', marginBottom: '0.5rem' }}>
            Lựa Chọn Quyết Định Nhân Sự <span style={{ color: 'var(--danger-main)' }}>*</span>
          </label>

          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(170px, 1fr))', gap: '0.75rem' }}>
            <label
              style={{
                display: 'flex',
                flexDirection: 'column',
                gap: '0.35rem',
                padding: '0.85rem',
                borderRadius: 'var(--radius-md)',
                border: decisionType === 'AdvanceToInDepth' ? '2px solid var(--primary-600)' : '1px solid var(--border-color)',
                backgroundColor: decisionType === 'AdvanceToInDepth' ? 'var(--primary-50)' : 'var(--bg-card)',
                cursor: 'pointer',
                transition: 'all 0.15s ease',
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontWeight: 600, color: 'var(--primary-700)', fontSize: '0.9rem' }}>
                <input
                  type="radio"
                  name="decisionType"
                  value="AdvanceToInDepth"
                  checked={decisionType === 'AdvanceToInDepth'}
                  onChange={(e) => setDecisionType(e.target.value)}
                />
                Chuyên Sâu
              </div>
              <span style={{ fontSize: '0.75rem', color: 'var(--slate-600)' }}>
                Vượt qua sơ loại &rarr; chuyển Application sang trạng thái Phỏng vấn chuyên sâu.
              </span>
            </label>

            <label
              style={{
                display: 'flex',
                flexDirection: 'column',
                gap: '0.35rem',
                padding: '0.85rem',
                borderRadius: 'var(--radius-md)',
                border: decisionType === 'HoldForReview' ? '2px solid var(--warning-main)' : '1px solid var(--border-color)',
                backgroundColor: decisionType === 'HoldForReview' ? 'var(--warning-bg)' : 'var(--bg-card)',
                cursor: 'pointer',
                transition: 'all 0.15s ease',
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontWeight: 600, color: 'var(--warning-text)', fontSize: '0.9rem' }}>
                <input
                  type="radio"
                  name="decisionType"
                  value="HoldForReview"
                  checked={decisionType === 'HoldForReview'}
                  onChange={(e) => setDecisionType(e.target.value)}
                />
                Tạm Hoãn
              </div>
              <span style={{ fontSize: '0.75rem', color: 'var(--slate-600)' }}>
                Cần thêm thông tin hoặc phỏng vấn viên khác thẩm định lại. Giữ nguyên vòng Sơ loại.
              </span>
            </label>

            <label
              style={{
                display: 'flex',
                flexDirection: 'column',
                gap: '0.35rem',
                padding: '0.85rem',
                borderRadius: 'var(--radius-md)',
                border: decisionType === 'Reject' ? '2px solid var(--danger-main)' : '1px solid var(--border-color)',
                backgroundColor: decisionType === 'Reject' ? 'var(--danger-bg)' : 'var(--bg-card)',
                cursor: 'pointer',
                transition: 'all 0.15s ease',
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontWeight: 600, color: 'var(--danger-text)', fontSize: '0.9rem' }}>
                <input
                  type="radio"
                  name="decisionType"
                  value="Reject"
                  checked={decisionType === 'Reject'}
                  onChange={(e) => setDecisionType(e.target.value)}
                />
                Từ Chối
              </div>
              <span style={{ fontSize: '0.75rem', color: 'var(--slate-600)' }}>
                Không phù hợp tiêu chí sơ loại &rarr; chuyển Application sang trạng thái Đã từ chối.
              </span>
            </label>
          </div>
        </div>

        {/* Override Warning & Mandatory Field */}
        {isOverriding && (
          <div
            style={{
              padding: '0.85rem 1rem',
              backgroundColor: 'var(--warning-bg)',
              borderRadius: 'var(--radius-md)',
              border: '1px solid var(--warning-border)',
              display: 'flex',
              flexDirection: 'column',
              gap: '0.5rem',
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontWeight: 600, color: 'var(--warning-text)', fontSize: '0.875rem' }}>
              <ShieldAlert size={18} color="var(--warning-main)" />
              <span>Ghi đè gợi ý AI: Bắt buộc cung cấp lý do chuyên môn</span>
            </div>
            <p style={{ margin: 0, fontSize: '0.8rem', color: 'var(--slate-700)' }}>
              Quyết định hiện tại của bạn khác với khuyến nghị sơ bộ của hệ thống (Gợi ý: <strong>{rec}</strong> &rarr; Chọn: <strong>{decisionType}</strong>). Hãy giải trình cơ sở đánh giá để lưu vào lịch sử kiểm toán bất biến.
            </p>

            <textarea
              value={overrideReason}
              onChange={(e) => setOverrideReason(e.target.value)}
              placeholder="VD: Ứng viên có kỹ năng thực tế tốt ở câu 2 bù đắp câu 1; hoặc phát hiện thông tin không trung thực..."
              rows={3}
              style={{
                width: '100%',
                padding: '0.5rem 0.75rem',
                borderRadius: 'var(--radius-sm)',
                border: '1px solid var(--warning-border)',
                fontSize: '0.85rem',
                fontFamily: 'inherit',
                marginTop: '0.25rem',
              }}
              required
            />
          </div>
        )}

        {/* Optional Notes */}
        <div>
          <label style={{ display: 'block', fontSize: '0.875rem', fontWeight: 600, color: 'var(--slate-700)', marginBottom: '0.4rem' }}>
            Ghi Chú Bổ Sung (Tùy chọn)
          </label>
          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            placeholder="Ghi chú nội bộ cho vòng tiếp theo hoặc gửi đến phỏng vấn viên chuyên sâu..."
            rows={2}
            style={{
              width: '100%',
              padding: '0.5rem 0.75rem',
              borderRadius: 'var(--radius-sm)',
              border: '1px solid var(--border-color)',
              fontSize: '0.85rem',
              fontFamily: 'inherit',
            }}
          />
        </div>

        {/* Validation Error Banner */}
        {validationError && (
          <div
            style={{
              padding: '0.65rem 0.85rem',
              backgroundColor: 'var(--danger-bg)',
              color: 'var(--danger-text)',
              border: '1px solid var(--danger-border)',
              borderRadius: 'var(--radius-sm)',
              fontSize: '0.825rem',
            }}
          >
            {validationError}
          </div>
        )}
      </form>
    </Modal>
  );
}
