import { useState } from 'react';
import { Modal, Button, Select } from '@/components';
import ApplicationStatusBadge from './ApplicationStatusBadge';
import { AlertTriangle } from 'lucide-react';

function getAvailableTransitions(currentStatus) {
  switch (currentStatus) {
    case 'Applied':
      return [
        { value: 'Screening', label: 'Screening (Sàng lọc hồ sơ)' },
        { value: 'Rejected', label: 'Rejected (Từ chối đơn)' },
      ];
    case 'Screening':
      return [
        { value: 'Interviewing', label: 'Interviewing (Lên lịch phỏng vấn)' },
        { value: 'Rejected', label: 'Rejected (Từ chối đơn)' },
      ];
    case 'Interviewing':
      return [
        { value: 'Offered', label: 'Offered (Gửi thư mời nhận việc)' },
        { value: 'Rejected', label: 'Rejected (Từ chối đơn)' },
      ];
    case 'Offered':
    case 'Rejected':
      return [];
    default:
      return [];
  }
}

export default function StatusTransitionModal({
  isOpen,
  onClose,
  onTransition,
  application,
  isLoading,
}) {
  const availableOptions = application ? getAvailableTransitions(application.status) : [];
  const [selectedStatus, setSelectedStatus] = useState('');

  const currentSelection = selectedStatus || (availableOptions.length > 0 ? availableOptions[0].value : '');

  const handleSubmit = (e) => {
    e.preventDefault();
    if (!currentSelection) return;
    onTransition({ id: application.id, status: currentSelection });
    setSelectedStatus('');
  };

  const handleClose = () => {
    setSelectedStatus('');
    onClose();
  };

  if (!application) return null;

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title="Chuyển Trạng Thái Đơn Ứng Tuyển"
      footer={
        <>
          <Button variant="secondary" onClick={handleClose} disabled={isLoading}>
            Đóng
          </Button>
          <Button
            variant="primary"
            onClick={handleSubmit}
            disabled={!currentSelection || availableOptions.length === 0}
            isLoading={isLoading}
          >
            Xác Nhận Chuyển Trạng Thái
          </Button>
        </>
      }
    >
      <div>
        <div
          style={{
            padding: '1rem',
            backgroundColor: 'var(--slate-50)',
            borderRadius: 'var(--radius-md)',
            marginBottom: '1.25rem',
            border: '1px solid var(--border-color)',
          }}
        >
          <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Ứng viên:</div>
          <div style={{ fontWeight: 600, fontSize: '1rem' }}>{application.candidateName}</div>
          <div style={{ fontSize: '0.85rem', color: 'var(--slate-600)', marginTop: '0.25rem' }}>
            Vị trí: <strong>{application.jobTitle}</strong>
          </div>
          <div style={{ marginTop: '0.75rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <span style={{ fontSize: '0.85rem' }}>Trạng thái hiện tại:</span>
            <ApplicationStatusBadge status={application.status} />
          </div>
        </div>

        {availableOptions.length === 0 ? (
          <div
            style={{
              padding: '1rem',
              backgroundColor: 'var(--warning-bg)',
              color: 'var(--warning-text)',
              borderRadius: 'var(--radius-md)',
              display: 'flex',
              alignItems: 'center',
              gap: '0.75rem',
              fontSize: '0.875rem',
            }}
          >
            <AlertTriangle size={20} />
            <span>
              Đơn ứng tuyển đang ở trạng thái cuối (<strong>{application.status}</strong>) và không thể chuyển đổi tiếp.
            </span>
          </div>
        ) : (
          <form onSubmit={handleSubmit}>
            <Select
              label="Chọn trạng thái tiếp theo hợp lệ (State Machine):"
              options={availableOptions}
              value={currentSelection}
              onChange={(e) => setSelectedStatus(e.target.value)}
              required
            />
          </form>
        )}
      </div>
    </Modal>
  );
}
