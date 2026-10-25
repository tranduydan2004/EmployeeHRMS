import { useState, useEffect } from 'react';
import { useApplications } from '@/features/recruitment/applications';
import { useInterviewMutations } from '../hooks/useInterviews';
import { Modal, Input, Select, Button, Badge } from '@/components';
import {
  Headphones,
  Sparkles,
  Copy,
  Check,
  Mail,
  AlertTriangle,
  Clock,
  Calendar,
  ExternalLink,
} from 'lucide-react';

export default function AudioInterviewInviteModal({
  isOpen,
  onClose,
  application = null,
  onSuccess,
}) {
  const { data: applicationsData } = useApplications({ pageNumber: 1, pageSize: 100 });
  const { createInterviewInvite, isCreatingInvite } = useInterviewMutations();

  const [selectedAppId, setSelectedAppId] = useState('');
  const [validityDays, setValidityDays] = useState(3);
  const [sendEmail, setSendEmail] = useState(true);
  const [note, setNote] = useState('');
  const [createdInvite, setCreatedInvite] = useState(null);
  const [isCopied, setIsCopied] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  // Reset form when modal opens
  useEffect(() => {
    if (isOpen) {
      setCreatedInvite(null);
      setIsCopied(false);
      setErrorMsg('');
      setValidityDays(3);
      setSendEmail(true);
      setNote('');

      if (application?.id) {
        setSelectedAppId(application.id.toString());
      } else if (applicationsData?.items?.length) {
        setSelectedAppId(applicationsData.items[0].id.toString());
      } else {
        setSelectedAppId('');
      }
    }
  }, [isOpen, application, applicationsData]);

  const handleSubmit = async (e) => {
    e?.preventDefault?.();
    setErrorMsg('');

    const appId = application?.id || parseInt(selectedAppId, 10);
    if (!appId) {
      setErrorMsg('Vui lòng chọn hồ sơ ứng tuyển cần mời phỏng vấn.');
      return;
    }

    try {
      const payload = {
        validityDays: parseInt(validityDays, 10) || 3,
        sendEmail: Boolean(sendEmail),
        note: note?.trim() || null,
      };

      const res = await createInterviewInvite({ applicationId: appId, payload });
      setCreatedInvite(res);
      if (onSuccess) {
        onSuccess(res);
      }
    } catch (err) {
      const msg = err.response?.data?.error || err.message || 'Không thể tạo lời mời phỏng vấn audio.';
      setErrorMsg(msg);
    }
  };

  const handleCopyLink = () => {
    if (!createdInvite?.inviteUrl) return;
    navigator.clipboard.writeText(createdInvite.inviteUrl);
    setIsCopied(true);
    setTimeout(() => setIsCopied(false), 2500);
  };

  // Filter application options (prioritize Applied & Screening)
  const applicationOptions =
    applicationsData?.items?.map((app) => ({
      value: app.id.toString(),
      label: `#${app.id} - ${app.candidateName} (${app.jobTitle}) [${app.status}]`,
    })) || [];

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={createdInvite ? 'Tạo Lời Mời Phỏng Vấn Thành Công' : 'Mời Phỏng Vấn Sơ Loại AI (Audio Screening)'}
      footer={
        createdInvite ? (
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', width: '100%' }}>
            <Button variant="secondary" onClick={onClose}>
              Đóng
            </Button>
            <Button
              variant="primary"
              icon={isCopied ? Check : Copy}
              onClick={handleCopyLink}
            >
              {isCopied ? 'Đã sao chép link!' : 'Sao chép link làm bài'}
            </Button>
          </div>
        ) : (
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', width: '100%' }}>
            <Button variant="secondary" onClick={onClose} disabled={isCreatingInvite}>
              Hủy
            </Button>
            <Button
              variant="primary"
              icon={Sparkles}
              onClick={handleSubmit}
              isLoading={isCreatingInvite}
            >
              Tạo Lời Mời & Gửi Link
            </Button>
          </div>
        )
      }
    >
      {createdInvite ? (
        // Bước 2: Hiển thị kết quả & Link làm bài
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem', padding: '0.5rem 0' }}>
          <div
            style={{
              padding: '1rem',
              borderRadius: 'var(--radius-md)',
              backgroundColor: 'var(--success-bg)',
              border: '1px solid var(--success-border)',
              display: 'flex',
              alignItems: 'flex-start',
              gap: '0.75rem',
            }}
          >
            <Headphones size={22} color="var(--success-main)" style={{ flexShrink: 0, marginTop: '2px' }} />
            <div>
              <div style={{ fontWeight: 600, color: 'var(--success-text)', fontSize: '0.95rem' }}>
                Đã khởi tạo phiên phỏng vấn audio sơ loại thành công!
              </div>
              <div style={{ fontSize: '0.825rem', color: 'var(--slate-600)', marginTop: '0.25rem' }}>
                Ứng viên: <strong>{createdInvite.candidateName}</strong> ({createdInvite.candidateEmail}) &bull; Vị trí: <strong>{createdInvite.jobTitle}</strong>
              </div>
            </div>
          </div>

          {/* Email Status Notification */}
          <div
            style={{
              padding: '0.75rem 1rem',
              borderRadius: 'var(--radius-md)',
              backgroundColor: createdInvite.emailSent ? 'var(--primary-50)' : 'var(--warning-bg)',
              border: `1px solid ${createdInvite.emailSent ? 'var(--primary-200)' : 'var(--warning-border)'}`,
              fontSize: '0.85rem',
              display: 'flex',
              alignItems: 'center',
              gap: '0.65rem',
            }}
          >
            {createdInvite.emailSent ? (
              <>
                <Mail size={18} color="var(--primary-600)" />
                <span style={{ color: 'var(--primary-800)' }}>
                  Email thư mời phỏng vấn đã được gửi thành công đến <strong>{createdInvite.candidateEmail}</strong>.
                </span>
              </>
            ) : (
              <>
                <AlertTriangle size={18} color="var(--warning-main)" style={{ flexShrink: 0 }} />
                <span style={{ color: 'var(--warning-text)' }}>
                  {createdInvite.emailMessage || 'Chưa gửi được email qua SMTP server. Vui lòng sao chép liên kết bên dưới để gửi trực tiếp cho ứng viên qua Zalo/Email.'}
                </span>
              </>
            )}
          </div>

          {/* Candidate Portal URL Box */}
          <div>
            <label style={{ display: 'block', fontSize: '0.85rem', fontWeight: 600, color: 'var(--slate-700)', marginBottom: '0.4rem' }}>
              Đường dẫn làm bài phỏng vấn trực tuyến của ứng viên:
            </label>
            <div style={{ display: 'flex', gap: '0.5rem' }}>
              <input
                type="text"
                readOnly
                value={createdInvite.inviteUrl}
                style={{
                  flex: 1,
                  padding: '0.5rem 0.75rem',
                  borderRadius: 'var(--radius-md)',
                  border: '1px solid var(--border-color)',
                  backgroundColor: 'var(--slate-50)',
                  fontFamily: 'monospace',
                  fontSize: '0.85rem',
                  color: 'var(--slate-800)',
                }}
                onClick={(e) => e.target.select()}
              />
              <Button
                variant="secondary"
                size="sm"
                icon={isCopied ? Check : Copy}
                onClick={handleCopyLink}
              >
                {isCopied ? 'Đã chép' : 'Sao chép'}
              </Button>
            </div>
          </div>

          {/* Meta details */}
          <div
            style={{
              display: 'grid',
              gridTemplateColumns: '1fr 1fr',
              gap: '0.75rem',
              backgroundColor: 'var(--slate-100)',
              padding: '0.75rem 1rem',
              borderRadius: 'var(--radius-md)',
              fontSize: '0.825rem',
            }}
          >
            <div>
              <span style={{ color: 'var(--slate-500)' }}>Hạn chót truy cập:</span>
              <div style={{ fontWeight: 600, color: 'var(--slate-800)', display: 'flex', alignItems: 'center', gap: '0.35rem', marginTop: '0.2rem' }}>
                <Clock size={14} color="var(--primary-600)" />
                {new Date(createdInvite.inviteExpiresAt).toLocaleString('vi-VN')}
              </div>
            </div>
            <div>
              <span style={{ color: 'var(--slate-500)' }}>Số câu hỏi sơ loại:</span>
              <div style={{ fontWeight: 600, color: 'var(--slate-800)', marginTop: '0.2rem' }}>
                {createdInvite.totalQuestions} câu (kèm barem AI Rubric)
              </div>
            </div>
          </div>
        </div>
      ) : (
        // Bước 1: Form cấu hình lời mời
        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          {errorMsg && (
            <div
              style={{
                padding: '0.75rem 1rem',
                borderRadius: 'var(--radius-md)',
                backgroundColor: 'var(--danger-bg)',
                border: '1px solid var(--danger-border)',
                color: 'var(--danger-text)',
                fontSize: '0.85rem',
                display: 'flex',
                alignItems: 'center',
                gap: '0.5rem',
              }}
            >
              <AlertTriangle size={16} />
              <span>{errorMsg}</span>
            </div>
          )}

          {application ? (
            <div
              style={{
                padding: '0.75rem 1rem',
                borderRadius: 'var(--radius-md)',
                backgroundColor: 'var(--primary-50)',
                border: '1px solid var(--primary-200)',
              }}
            >
              <div style={{ fontSize: '0.8rem', color: 'var(--primary-700)', fontWeight: 600 }}>
                ỨNG VIÊN ĐƯỢC CHỌN:
              </div>
              <div style={{ fontSize: '1rem', fontWeight: 700, color: 'var(--slate-900)', marginTop: '0.2rem' }}>
                {application.candidateName}
              </div>
              <div style={{ fontSize: '0.825rem', color: 'var(--slate-600)', marginTop: '0.15rem' }}>
                Vị trí: <strong>{application.jobTitle}</strong> &bull; Đơn #{application.id}
              </div>
            </div>
          ) : (
            <Select
              label="Chọn Đơn Ứng Tuyển"
              placeholder="-- Chọn ứng viên cần mời phỏng vấn audio --"
              options={applicationOptions}
              value={selectedAppId}
              onChange={(e) => setSelectedAppId(e.target.value)}
              required
            />
          )}

          <div style={{ display: 'grid', gridTemplateColumns: '1fr', gap: '1rem' }}>
            <Input
              label="Thời Hạn Hiệu Lực Của Link (Số ngày)"
              type="number"
              min="1"
              max="30"
              value={validityDays}
              onChange={(e) => setValidityDays(e.target.value)}
              required
              helperText="Mặc định 3 ngày. Sau thời gian này link mời sẽ tự động hết hạn."
            />
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginTop: '0.25rem' }}>
            <input
              type="checkbox"
              id="sendEmailCheckbox"
              checked={sendEmail}
              onChange={(e) => setSendEmail(e.target.checked)}
              style={{ width: '16px', height: '16px', cursor: 'pointer' }}
            />
            <label htmlFor="sendEmailCheckbox" style={{ fontSize: '0.875rem', color: 'var(--slate-700)', cursor: 'pointer' }}>
              Tự động gửi email thông báo kèm liên kết làm bài cho ứng viên qua MailKit
            </label>
          </div>

          <Input
            label="Lời Nhắn Kèm Theo (Tùy chọn)"
            type="text"
            placeholder="Ví dụ: Vui lòng chuẩn bị tai nghe và micrô yên tĩnh..."
            value={note}
            onChange={(e) => setNote(e.target.value)}
          />

          <div
            style={{
              padding: '0.75rem 1rem',
              borderRadius: 'var(--radius-md)',
              backgroundColor: 'var(--slate-50)',
              border: '1px solid var(--border-color)',
              fontSize: '0.8rem',
              color: 'var(--slate-600)',
              lineHeight: 1.5,
            }}
          >
            💡 <strong>Quy trình tự động:</strong> Hệ thống sẽ tự động sao chép các câu hỏi phỏng vấn và barem rubric từ vị trí tuyển dụng, thiết lập thời gian suy nghĩ/trả lời chuẩn xác và cấp mã định danh bảo mật cho ứng viên.
          </div>
        </form>
      )}
    </Modal>
  );
}
