import { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { interviewCreateSchema } from '../schemas/interviewSchemas';
import { useApplications } from '@/features/recruitment/applications';
import { useInterviewMutations } from '../hooks/useInterviews';
import { Modal, Input, Select, Button } from '@/components';
import {
  Headphones,
  Calendar,
  Sparkles,
  Copy,
  Check,
  Mail,
  AlertTriangle,
  Clock,
} from 'lucide-react';

function getInitialScheduleDate() {
  const tomorrow = new Date();
  tomorrow.setDate(tomorrow.getDate() + 1);
  return tomorrow.toISOString().slice(0, 16);
}

export default function InterviewScheduleModal({
  isOpen,
  onClose,
  onSave,
  isLoading,
}) {
  const [activeTab, setActiveTab] = useState('audio'); // 'audio' | 'direct'
  const { data: applicationsData } = useApplications({ pageNumber: 1, pageSize: 100 });
  const { createInterviewInvite, isCreatingInvite } = useInterviewMutations();

  // Audio Screening form state
  const [audioAppId, setAudioAppId] = useState('');
  const [validityDays, setValidityDays] = useState(3);
  const [sendEmail, setSendEmail] = useState(true);
  const [note, setNote] = useState('');
  const [audioResult, setAudioResult] = useState(null);
  const [isCopied, setIsCopied] = useState(false);
  const [audioError, setAudioError] = useState('');

  // Direct Interview form (hook form)
  const {
    register,
    handleSubmit: handleDirectSubmit,
    reset: resetDirectForm,
    formState: { errors: directErrors },
  } = useForm({
    resolver: zodResolver(interviewCreateSchema),
    defaultValues: {
      applicationId: '',
      scheduledDate: getInitialScheduleDate(),
      interviewerId: '',
    },
  });

  useEffect(() => {
    if (isOpen) {
      setActiveTab('audio');
      setAudioResult(null);
      setIsCopied(false);
      setAudioError('');
      setValidityDays(3);
      setSendEmail(true);
      setNote('');

      const firstAppId = applicationsData?.items?.[0]?.id?.toString() || '';
      setAudioAppId(firstAppId);

      resetDirectForm({
        applicationId: firstAppId,
        scheduledDate: getInitialScheduleDate(),
        interviewerId: '',
      });
    }
  }, [isOpen, applicationsData, resetDirectForm]);

  const onAudioSubmit = async (e) => {
    e?.preventDefault?.();
    setAudioError('');

    const appId = parseInt(audioAppId, 10);
    if (!appId) {
      setAudioError('Vui lòng chọn hồ sơ ứng tuyển cần mời phỏng vấn.');
      return;
    }

    try {
      const payload = {
        validityDays: parseInt(validityDays, 10) || 3,
        sendEmail: Boolean(sendEmail),
        note: note?.trim() || null,
      };

      const res = await createInterviewInvite({ applicationId: appId, payload });
      setAudioResult(res);
    } catch (err) {
      const msg = err.response?.data?.error || err.message || 'Không thể tạo lời mời phỏng vấn audio.';
      setAudioError(msg);
    }
  };

  const handleCopyLink = () => {
    if (!audioResult?.inviteUrl) return;
    navigator.clipboard.writeText(audioResult.inviteUrl);
    setIsCopied(true);
    setTimeout(() => setIsCopied(false), 2500);
  };

  const onDirectSubmit = (data) => {
    const payload = {
      applicationId: parseInt(data.applicationId, 10),
      scheduledDate: new Date(data.scheduledDate).toISOString(),
      interviewerId: data.interviewerId ? parseInt(data.interviewerId, 10) : null,
    };
    onSave(payload);
  };

  const applicationOptions =
    applicationsData?.items?.map((app) => ({
      value: app.id.toString(),
      label: `#${app.id} - ${app.candidateName} (${app.jobTitle}) [${app.status}]`,
    })) || [];

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={
        audioResult
          ? 'Tạo Lời Mời Phỏng Vấn Thành Công'
          : 'Lên Lịch & Mời Phỏng Vấn'
      }
      footer={
        audioResult ? (
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
        ) : activeTab === 'audio' ? (
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', width: '100%' }}>
            <Button variant="secondary" onClick={onClose} disabled={isCreatingInvite}>
              Hủy
            </Button>
            <Button
              variant="primary"
              icon={Sparkles}
              onClick={onAudioSubmit}
              isLoading={isCreatingInvite}
            >
              Mời Phỏng Vấn Audio
            </Button>
          </div>
        ) : (
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', width: '100%' }}>
            <Button variant="secondary" onClick={onClose} disabled={isLoading}>
              Hủy
            </Button>
            <Button
              variant="primary"
              onClick={handleDirectSubmit(onDirectSubmit)}
              isLoading={isLoading}
            >
              Tạo Lịch Phỏng Vấn
            </Button>
          </div>
        )
      }
    >
      {audioResult ? (
        // Màn hình thành công cho Audio Screening
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
                Đã khởi tạo phiên phỏng vấn sơ loại AI thành công!
              </div>
              <div style={{ fontSize: '0.825rem', color: 'var(--slate-600)', marginTop: '0.25rem' }}>
                Ứng viên: <strong>{audioResult.candidateName}</strong> &bull; Vị trí: <strong>{audioResult.jobTitle}</strong>
              </div>
            </div>
          </div>

          <div
            style={{
              padding: '0.75rem 1rem',
              borderRadius: 'var(--radius-md)',
              backgroundColor: audioResult.emailSent ? 'var(--primary-50)' : 'var(--warning-bg)',
              border: `1px solid ${audioResult.emailSent ? 'var(--primary-200)' : 'var(--warning-border)'}`,
              fontSize: '0.85rem',
              display: 'flex',
              alignItems: 'center',
              gap: '0.65rem',
            }}
          >
            {audioResult.emailSent ? (
              <>
                <Mail size={18} color="var(--primary-600)" />
                <span style={{ color: 'var(--primary-800)' }}>
                  Email đã được gửi đến <strong>{audioResult.candidateEmail}</strong> kèm liên kết truy cập bài thi.
                </span>
              </>
            ) : (
              <>
                <AlertTriangle size={18} color="var(--warning-main)" style={{ flexShrink: 0 }} />
                <span style={{ color: 'var(--warning-text)' }}>
                  {audioResult.emailMessage || 'Chưa gửi được email qua SMTP. Hãy sao chép liên kết bên dưới để gửi trực tiếp cho ứng viên.'}
                </span>
              </>
            )}
          </div>

          <div>
            <label style={{ display: 'block', fontSize: '0.85rem', fontWeight: 600, color: 'var(--slate-700)', marginBottom: '0.4rem' }}>
              Liên kết làm bài phỏng vấn trực tuyến (Candidate Portal):
            </label>
            <div style={{ display: 'flex', gap: '0.5rem' }}>
              <input
                type="text"
                readOnly
                value={audioResult.inviteUrl}
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
                {new Date(audioResult.inviteExpiresAt).toLocaleString('vi-VN')}
              </div>
            </div>
            <div>
              <span style={{ color: 'var(--slate-500)' }}>Số câu hỏi sơ loại:</span>
              <div style={{ fontWeight: 600, color: 'var(--slate-800)', marginTop: '0.2rem' }}>
                {audioResult.totalQuestions} câu (kèm barem chấm AI)
              </div>
            </div>
          </div>
        </div>
      ) : (
        <div>
          {/* Tab Selector */}
          <div
            style={{
              display: 'flex',
              borderBottom: '1px solid var(--border-color)',
              marginBottom: '1.25rem',
              gap: '0.5rem',
            }}
          >
            <button
              type="button"
              onClick={() => setActiveTab('audio')}
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: '0.45rem',
                padding: '0.65rem 1rem',
                border: 'none',
                background: 'none',
                cursor: 'pointer',
                fontWeight: activeTab === 'audio' ? 600 : 500,
                color: activeTab === 'audio' ? 'var(--primary-600)' : 'var(--slate-500)',
                borderBottom: activeTab === 'audio' ? '2px solid var(--primary-600)' : '2px solid transparent',
                fontSize: '0.875rem',
              }}
            >
              <Headphones size={16} />
              <span>Phỏng Vấn Audio Sơ Loại (AI)</span>
              <span
                style={{
                  fontSize: '0.7rem',
                  padding: '0.1rem 0.4rem',
                  borderRadius: 'var(--radius-full)',
                  backgroundColor: 'var(--primary-100)',
                  color: 'var(--primary-800)',
                  fontWeight: 700,
                }}
              >
                Khuyên dùng
              </span>
            </button>

            <button
              type="button"
              onClick={() => setActiveTab('direct')}
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: '0.45rem',
                padding: '0.65rem 1rem',
                border: 'none',
                background: 'none',
                cursor: 'pointer',
                fontWeight: activeTab === 'direct' ? 600 : 500,
                color: activeTab === 'direct' ? 'var(--primary-600)' : 'var(--slate-500)',
                borderBottom: activeTab === 'direct' ? '2px solid var(--primary-600)' : '2px solid transparent',
                fontSize: '0.875rem',
              }}
            >
              <Calendar size={16} />
              <span>Lên Lịch Trực Tiếp (Interviewer)</span>
            </button>
          </div>

          {activeTab === 'audio' ? (
            // Tab 1: Mời Audio Screening
            <form onSubmit={onAudioSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              {audioError && (
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
                  <span>{audioError}</span>
                </div>
              )}

              <Select
                label="Chọn Đơn Ứng Tuyển"
                placeholder="-- Chọn đơn ứng tuyển cần phỏng vấn audio --"
                options={applicationOptions}
                value={audioAppId}
                onChange={(e) => setAudioAppId(e.target.value)}
                required
              />

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

              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginTop: '0.25rem' }}>
                <input
                  type="checkbox"
                  id="sendEmailSchedule"
                  checked={sendEmail}
                  onChange={(e) => setSendEmail(e.target.checked)}
                  style={{ width: '16px', height: '16px', cursor: 'pointer' }}
                />
                <label htmlFor="sendEmailSchedule" style={{ fontSize: '0.875rem', color: 'var(--slate-700)', cursor: 'pointer' }}>
                  Gửi email tự động kèm link làm bài trực tuyến cho ứng viên qua MailKit
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
                💡 Hệ thống sẽ tự động sao chép câu hỏi từ Question Bank, kích hoạt portal thi trực tuyến và gửi email thông báo kèm token cho ứng viên.
              </div>
            </form>
          ) : (
            // Tab 2: Lên lịch phỏng vấn trực tiếp
            <form onSubmit={handleDirectSubmit(onDirectSubmit)} style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              <Select
                label="Chọn Đơn Ứng Tuyển"
                placeholder="-- Chọn đơn ứng tuyển cần phỏng vấn --"
                options={applicationOptions}
                error={directErrors.applicationId?.message}
                required
                {...register('applicationId')}
              />

              <Input
                label="Thời Gian Phỏng Vấn"
                type="datetime-local"
                error={directErrors.scheduledDate?.message}
                required
                {...register('scheduledDate')}
              />

              <Input
                label="ID Người Phỏng Vấn (Tùy chọn)"
                type="number"
                placeholder="Nhập User ID người phụ trách phỏng vấn (nếu có)"
                error={directErrors.interviewerId?.message}
                {...register('interviewerId')}
              />
            </form>
          )}
        </div>
      )}
    </Modal>
  );
}
