import { useState, useEffect } from 'react';
import { Modal, Button } from '@/components';
import { AlertTriangle, CheckCircle, ArrowLeft, ArrowRight, HelpCircle, FileText } from 'lucide-react';

export default function CompleteInterviewModal({
  isOpen,
  onClose,
  interview,
  onConfirm,
  isLoading,
}) {
  const [step, setStep] = useState(1);
  const [summary, setSummary] = useState('');

  useEffect(() => {
    if (isOpen) {
      setStep(1);
      setSummary('');
    }
  }, [isOpen]);

  if (!interview) return null;

  const totalQuestions = interview.questions?.length || 0;
  const answeredQuestions =
    interview.questions?.filter((q) => q.candidateAnswer && q.candidateAnswer.trim().length > 0).length || 0;

  const handleClose = () => {
    if (!isLoading) {
      setStep(1);
      setSummary('');
      onClose();
    }
  };

  const handleNextStep = () => {
    setStep(2);
  };

  const handlePrevStep = () => {
    setStep(1);
  };

  const handleFinalSubmit = async () => {
    await onConfirm(summary.trim() || undefined);
    handleClose();
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title={
        step === 1
          ? `Kết Thúc Buổi Phỏng Vấn #${interview.id} — Bước 1/2`
          : `Xác Nhận Chốt Kết Quả #${interview.id} — Bước 2/2`
      }
      maxWidth="560px"
      footer={
        step === 1 ? (
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', width: '100%' }}>
            <Button variant="secondary" onClick={handleClose} disabled={isLoading}>
              Hủy
            </Button>
            <Button variant="primary" icon={ArrowRight} onClick={handleNextStep} disabled={isLoading}>
              Tiếp tục
            </Button>
          </div>
        ) : (
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', width: '100%' }}>
            <Button variant="secondary" icon={ArrowLeft} onClick={handlePrevStep} disabled={isLoading}>
              Quay lại
            </Button>
            <Button
              variant="danger"
              icon={CheckCircle}
              onClick={handleFinalSubmit}
              isLoading={isLoading}
            >
              Xác nhận kết thúc
            </Button>
          </div>
        )
      }
    >
      {step === 1 ? (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          <div
            style={{
              display: 'grid',
              gridTemplateColumns: '1fr 1fr',
              gap: '0.75rem',
              backgroundColor: 'var(--slate-50)',
              padding: '0.85rem 1rem',
              borderRadius: 'var(--radius-md)',
              border: '1px solid var(--border-color)',
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <HelpCircle size={18} color="var(--primary-600)" />
              <div>
                <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>Tổng số câu hỏi</div>
                <div style={{ fontSize: '1.1rem', fontWeight: 700, color: 'var(--text-main)' }}>
                  {totalQuestions}
                </div>
              </div>
            </div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <FileText size={18} color={answeredQuestions === totalQuestions && totalQuestions > 0 ? 'var(--success-main)' : 'var(--warning-main)'} />
              <div>
                <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>Đã có câu trả lời</div>
                <div style={{ fontSize: '1.1rem', fontWeight: 700, color: 'var(--text-main)' }}>
                  {answeredQuestions} / {totalQuestions}
                </div>
              </div>
            </div>
          </div>

          <div
            style={{
              padding: '0.85rem 1rem',
              backgroundColor: 'var(--warning-bg)',
              border: '1px solid var(--warning-border)',
              borderRadius: 'var(--radius-md)',
              color: 'var(--warning-text)',
              fontSize: '0.85rem',
              lineHeight: 1.5,
              display: 'flex',
              gap: '0.65rem',
            }}
          >
            <AlertTriangle size={20} style={{ flexShrink: 0, marginTop: '2px' }} />
            <div>
              <strong>Lưu ý quan trọng:</strong> Sau khi kết thúc, kết quả đánh giá sẽ được chốt và thông báo
              sẽ được gửi tới <strong>Admin & HR</strong>. Bạn sẽ không thể thêm hoặc sửa đổi câu hỏi, câu trả
              lời của ứng viên nữa.
            </div>
          </div>

          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.4rem' }}>
            <label
              htmlFor="interview-summary"
              style={{ fontSize: '0.875rem', fontWeight: 600, color: 'var(--text-main)' }}
            >
              Ghi chú / Nhận xét tổng kết buổi phỏng vấn (tùy chọn)
            </label>
            <textarea
              id="interview-summary"
              rows={4}
              value={summary}
              onChange={(e) => setSummary(e.target.value)}
              placeholder="Nhập nhận xét tổng quan về phong thái, kỹ năng chuyên môn, đề xuất tuyển dụng hoặc lưu ý thêm..."
              style={{
                width: '100%',
                padding: '0.65rem 0.85rem',
                borderRadius: 'var(--radius-sm)',
                border: '1px solid var(--border-color)',
                backgroundColor: 'var(--bg-card)',
                color: 'var(--text-main)',
                fontSize: '0.875rem',
                fontFamily: 'inherit',
                resize: 'vertical',
                boxSizing: 'border-box',
              }}
            />
          </div>
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
          <div
            style={{
              padding: '1rem',
              backgroundColor: 'var(--danger-bg)',
              border: '1px solid var(--danger-border)',
              borderRadius: 'var(--radius-md)',
              color: 'var(--danger-text)',
              display: 'flex',
              flexDirection: 'column',
              gap: '0.5rem',
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.65rem', fontWeight: 700, fontSize: '0.95rem' }}>
              <AlertTriangle size={22} color="var(--danger-main)" />
              Bạn có CHẮC CHẮN muốn kết thúc buổi phỏng vấn này?
            </div>
            <p style={{ margin: 0, fontSize: '0.85rem', lineHeight: 1.5, opacity: 0.9 }}>
              Hành động này sẽ lập tức chốt hồ sơ đánh giá buổi phỏng vấn. Dữ liệu câu hỏi, câu trả lời sẽ chuyển
              sang chế độ chỉ đọc (Read-only).
            </p>
          </div>

          <div
            style={{
              padding: '0.85rem 1rem',
              backgroundColor: 'var(--slate-50)',
              border: '1px solid var(--border-color)',
              borderRadius: 'var(--radius-md)',
              fontSize: '0.85rem',
              display: 'flex',
              flexDirection: 'column',
              gap: '0.5rem',
            }}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', color: 'var(--text-muted)' }}>
              <span>Thống kê câu hỏi:</span>
              <strong style={{ color: 'var(--text-main)' }}>
                {answeredQuestions}/{totalQuestions} câu đã trả lời
              </strong>
            </div>

            <div>
              <div style={{ color: 'var(--text-muted)', marginBottom: '0.25rem' }}>Ghi chú tổng kết:</div>
              <div
                style={{
                  padding: '0.5rem 0.75rem',
                  backgroundColor: 'var(--bg-card)',
                  borderRadius: 'var(--radius-sm)',
                  border: '1px solid var(--border-color)',
                  color: summary.trim() ? 'var(--text-main)' : 'var(--text-muted)',
                  fontStyle: summary.trim() ? 'normal' : 'italic',
                  minHeight: '40px',
                  whiteSpace: 'pre-wrap',
                  wordBreak: 'break-word',
                }}
              >
                {summary.trim() ? summary : '(Không có ghi chú thêm)'}
              </div>
            </div>
          </div>
        </div>
      )}
    </Modal>
  );
}
