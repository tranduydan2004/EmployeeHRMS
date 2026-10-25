import { useState, useEffect } from 'react';
import { Modal, Button } from '@/components';
import { ShieldAlert, AlertTriangle, Trash2, CheckCircle2 } from 'lucide-react';

export default function ErasePersonalDataModal({
  isOpen,
  onClose,
  interview,
  onConfirm,
  isLoading,
}) {
  const [reason, setReason] = useState('');
  const [isConfirmed, setIsConfirmed] = useState(false);
  const [validationError, setValidationError] = useState('');

  useEffect(() => {
    if (isOpen) {
      setReason('');
      setIsConfirmed(false);
      setValidationError('');
    }
  }, [isOpen]);

  if (!interview) return null;

  const quickReasons = [
    'Theo yêu cầu rút lại sự đồng ý & xóa dữ liệu của ứng viên',
    'Hết thời hạn lưu trữ dữ liệu cá nhân tuyển dụng theo quy chế công ty',
    'Rà soát bảo mật định kỳ nhằm giảm thiểu rủi ro lưu trữ sinh trắc giọng nói',
  ];

  const handleSubmit = async (e) => {
    e.preventDefault();
    setValidationError('');

    if (!reason || reason.trim().length < 5) {
      setValidationError('Vui lòng nhập lý do xóa dữ liệu cá nhân (tối thiểu 5 ký tự).');
      return;
    }

    if (!isConfirmed) {
      setValidationError('Vui lòng tích chọn xác nhận bạn hiểu rõ hành động xóa dữ liệu.');
      return;
    }

    try {
      await onConfirm({
        interviewId: interview.id,
        reason: reason.trim(),
      });
      onClose();
    } catch {
      // Error handled by mutation hook
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', color: 'var(--danger-text)' }}>
          <ShieldAlert size={20} />
          <span>Xóa Dữ Liệu Cá Nhân (Quyền Riêng Tư & Xóa Dữ Liệu)</span>
        </div>
      }
      size="md"
    >
      <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
        {/* Compliance Warning Banner */}
        <div
          style={{
            padding: '1rem',
            borderRadius: 'var(--radius-md)',
            backgroundColor: 'var(--danger-bg)',
            border: '1px solid var(--danger-border)',
            color: 'var(--danger-text)',
            fontSize: '0.875rem',
            display: 'flex',
            flexDirection: 'column',
            gap: '0.5rem',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontWeight: 700 }}>
            <AlertTriangle size={16} />
            <span>Cảnh báo: Hành động này là vĩnh viễn và không thể hoàn tác</span>
          </div>
          <p style={{ margin: 0, lineHeight: 1.5 }}>
            Theo Luật Bảo vệ dữ liệu cá nhân số 91/2025/QH15, ứng viên có quyền yêu cầu xóa dữ liệu cá nhân (Right to Erasure). Hệ thống sẽ thực hiện làm sạch dữ liệu như sau:
          </p>
          <ul style={{ margin: '0.25rem 0 0 1.25rem', padding: 0, lineHeight: 1.5 }}>
            <li>
              <strong>Dữ liệu bị xóa vĩnh viễn:</strong> Tất cả các file ghi âm audio vật lý, toàn bộ nội dung văn bản bóc băng (transcript), trích dẫn bằng chứng và phân tích của AI.
            </li>
            <li>
              <strong>Dữ liệu được bảo lưu kiểm toán:</strong> Mức đánh giá Rubric (Poor/Acceptable/Good/Excellent), điểm số, và biên bản quyết định sơ loại của con người (ScreeningDecisions) được giữ nguyên để phục vụ báo cáo kiểm toán tuyển dụng.
            </li>
          </ul>
        </div>

        {/* Candidate Information Summary */}
        <div
          style={{
            padding: '0.75rem 1rem',
            backgroundColor: 'var(--slate-50)',
            borderRadius: 'var(--radius-md)',
            border: '1px solid var(--slate-200)',
            fontSize: '0.875rem',
            display: 'flex',
            flexDirection: 'column',
            gap: '0.25rem',
          }}
        >
          <div><strong>Ứng viên:</strong> {interview.candidateName || `Hồ sơ #${interview.applicationId}`}</div>
          <div><strong>Vị trí:</strong> {interview.jobTitle || 'Chưa cập nhật'}</div>
          <div><strong>Buổi phỏng vấn:</strong> #{interview.id}</div>
        </div>

        {/* Reason Textarea */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
          <label style={{ fontSize: '0.875rem', fontWeight: 600, color: 'var(--slate-700)' }}>
            Lý do xóa dữ liệu cá nhân <span style={{ color: 'var(--danger-text)' }}>*</span>
          </label>
          <textarea
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder="Nhập lý do thực hiện xóa dữ liệu cá nhân của ứng viên..."
            rows={3}
            style={{
              padding: '0.625rem 0.75rem',
              borderRadius: 'var(--radius-md)',
              border: '1px solid var(--slate-300)',
              fontSize: '0.875rem',
              resize: 'vertical',
            }}
          />
          {/* Quick reason suggestions */}
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.35rem', marginTop: '0.25rem' }}>
            <span style={{ fontSize: '0.75rem', color: 'var(--slate-500)', alignSelf: 'center' }}>Gợi ý nhanh:</span>
            {quickReasons.map((qr, idx) => (
              <button
                type="button"
                key={idx}
                onClick={() => setReason(qr)}
                style={{
                  fontSize: '0.75rem',
                  padding: '0.2rem 0.5rem',
                  borderRadius: 'var(--radius-sm)',
                  backgroundColor: 'var(--slate-100)',
                  border: '1px solid var(--slate-300)',
                  color: 'var(--slate-700)',
                  cursor: 'pointer',
                  textAlign: 'left',
                }}
              >
                {qr}
              </button>
            ))}
          </div>
        </div>

        {/* Confirmation Checkbox */}
        <label
          style={{
            display: 'flex',
            alignItems: 'flex-start',
            gap: '0.625rem',
            cursor: 'pointer',
            padding: '0.75rem',
            backgroundColor: 'var(--slate-50)',
            borderRadius: 'var(--radius-md)',
            border: '1px solid var(--slate-200)',
          }}
        >
          <input
            type="checkbox"
            checked={isConfirmed}
            onChange={(e) => setIsConfirmed(e.target.checked)}
            style={{ marginTop: '0.2rem' }}
          />
          <span style={{ fontSize: '0.85rem', color: 'var(--slate-700)', lineHeight: 1.4 }}>
            Tôi xác nhận muốn xóa toàn bộ file âm thanh và transcript của buổi phỏng vấn này. Tôi hiểu rằng file âm thanh sẽ bị xóa vĩnh viễn trên máy chủ lưu trữ và không thể phục hồi.
          </span>
        </label>

        {validationError && (
          <div style={{ color: 'var(--danger-text)', fontSize: '0.85rem', fontWeight: 500 }}>
            {validationError}
          </div>
        )}

        {/* Action Buttons */}
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '0.5rem' }}>
          <Button type="button" variant="secondary" onClick={onClose} disabled={isLoading}>
            Hủy Bỏ
          </Button>
          <Button
            type="submit"
            variant="danger"
            icon={Trash2}
            disabled={isLoading || !isConfirmed || reason.trim().length < 5}
            isLoading={isLoading}
          >
            Xác Nhận Xóa Vĩnh Viễn
          </Button>
        </div>
      </form>
    </Modal>
  );
}
