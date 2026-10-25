import { useState, useEffect } from 'react';
import { Modal, Button, Select, Skeleton } from '@/components';
import { useAvailableInterviewers } from '../hooks/useInterviews';
import { UserCheck, ShieldCheck, Mail, Briefcase } from 'lucide-react';

export default function AssignInterviewerModal({
  isOpen,
  onClose,
  interview,
  onAssign,
  isLoading,
}) {
  const { data: interviewers = [], isLoading: isLoadingInterviewers } = useAvailableInterviewers();
  const [selectedInterviewerId, setSelectedInterviewerId] = useState('');

  useEffect(() => {
    if (isOpen && interview) {
      setSelectedInterviewerId(interview.interviewerId ? String(interview.interviewerId) : '');
    }
  }, [isOpen, interview]);

  if (!interview) return null;

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!selectedInterviewerId) return;

    await onAssign({
      interviewId: interview.id,
      interviewerId: parseInt(selectedInterviewerId, 10),
    });
    onClose();
  };

  const interviewerOptions = interviewers.map((item) => ({
    value: String(item.id),
    label: `${item.name} (${item.email})`,
  }));

  const currentInterviewer = interviewers.find(
    (i) => String(i.id) === selectedInterviewerId
  );

  return (
    <Modal
      isOpen={isOpen}
      onClose={!isLoading ? onClose : undefined}
      title="Phân Công Người Phỏng Vấn Chuyên Sâu"
      maxWidth="500px"
      footer={
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', width: '100%' }}>
          <Button variant="secondary" onClick={onClose} disabled={isLoading}>
            Đóng
          </Button>
          <Button
            variant="primary"
            icon={UserCheck}
            onClick={handleSubmit}
            isLoading={isLoading}
            disabled={!selectedInterviewerId || selectedInterviewerId === String(interview.interviewerId || '')}
          >
            Lưu Phân Công
          </Button>
        </div>
      }
    >
      <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
        <div
          style={{
            backgroundColor: 'var(--slate-50)',
            padding: '1rem',
            borderRadius: 'var(--radius-md)',
            border: '1px solid var(--border-color)',
            display: 'flex',
            flexDirection: 'column',
            gap: '0.5rem',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontWeight: 600, color: 'var(--slate-800)' }}>
            <Briefcase size={16} color="var(--primary-600)" />
            <span>Ứng viên: {interview.candidateName}</span>
          </div>
          <div style={{ fontSize: '0.85rem', color: 'var(--slate-600)' }}>
            Vị trí ứng tuyển: <strong>{interview.jobTitle || 'N/A'}</strong> (Mã HS: #{interview.applicationId})
          </div>
          <div style={{ fontSize: '0.825rem', color: 'var(--slate-500)' }}>
            Hiện tại:{' '}
            {interview.interviewerName ? (
              <span style={{ color: 'var(--success-text)', fontWeight: 600 }}>
                {interview.interviewerName}
              </span>
            ) : (
              <span style={{ color: 'var(--warning-text)', fontStyle: 'italic' }}>
                Chưa có người phỏng vấn phụ trách
              </span>
            )}
          </div>
        </div>

        <div>
          <label
            style={{
              display: 'block',
              fontSize: '0.875rem',
              fontWeight: 600,
              color: 'var(--slate-700)',
              marginBottom: '0.5rem',
            }}
          >
            Chọn Interviewer Phụ Trách <span style={{ color: 'var(--danger-main)' }}>*</span>
          </label>

          {isLoadingInterviewers ? (
            <Skeleton height="40px" />
          ) : (
            <Select
              value={selectedInterviewerId}
              onChange={(e) => setSelectedInterviewerId(e.target.value)}
              options={[{ value: '', label: '-- Chọn người phỏng vấn --' }, ...interviewerOptions]}
              disabled={isLoading}
            />
          )}

          <p style={{ fontSize: '0.775rem', color: 'var(--slate-500)', marginTop: '0.4rem' }}>
            Người phỏng vấn được gán sẽ có quyền xem xét bản ghi âm, bảng chấm điểm rubric và đưa ra nhận xét chuyên môn.
          </p>
        </div>

        {currentInterviewer && (
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '0.75rem',
              padding: '0.75rem',
              backgroundColor: 'var(--primary-50)',
              borderRadius: 'var(--radius-sm)',
              border: '1px solid var(--primary-200)',
            }}
          >
            <ShieldCheck size={20} color="var(--primary-700)" />
            <div style={{ fontSize: '0.825rem', color: 'var(--primary-900)' }}>
              <strong>{currentInterviewer.name}</strong>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', color: 'var(--primary-700)' }}>
                <Mail size={13} />
                <span>{currentInterviewer.email}</span>
              </div>
            </div>
          </div>
        )}
      </form>
    </Modal>
  );
}
