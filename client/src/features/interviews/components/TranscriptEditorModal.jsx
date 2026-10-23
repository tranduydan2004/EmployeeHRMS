import { useState, useEffect } from 'react';
import { Modal, Button } from '@/components';
import { Edit3, Info, AlertCircle } from 'lucide-react';

export default function TranscriptEditorModal({
  isOpen,
  onClose,
  interviewId,
  questionId,
  questionText,
  rawTranscript,
  initialTranscript,
  onSave,
  isLoading,
}) {
  const [text, setText] = useState('');
  const [error, setError] = useState('');

  useEffect(() => {
    if (isOpen) {
      setText(initialTranscript || rawTranscript || '');
      setError('');
    }
  }, [isOpen, initialTranscript, rawTranscript]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    const trimmed = text.trim();
    if (!trimmed) {
      setError('Bản bóc băng không được để trống.');
      return;
    }
    if (trimmed.length > 10000) {
      setError('Bản bóc băng không được vượt quá 10,000 ký tự.');
      return;
    }

    try {
      await onSave({
        interviewId,
        questionId,
        transcript: trimmed,
      });
      onClose();
    } catch (err) {
      // Error handled by mutation
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Chỉnh Sửa Bản Bóc Băng (Transcript)"
      maxWidth="680px"
      footer={
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.5rem', width: '100%' }}>
          <Button variant="secondary" size="sm" onClick={onClose} disabled={isLoading}>
            Hủy
          </Button>
          <Button variant="primary" size="sm" onClick={handleSubmit} isLoading={isLoading}>
            Lưu bản bóc băng
          </Button>
        </div>
      }
    >
      <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '0.85rem' }}>
        <div
          style={{
            padding: '0.65rem 0.85rem',
            backgroundColor: 'var(--primary-50)',
            borderRadius: 'var(--radius-sm)',
            border: '1px solid var(--primary-200)',
            fontSize: '0.8rem',
            color: 'var(--primary-900)',
          }}
        >
          <strong>Câu hỏi #{questionId}:</strong> {questionText}
        </div>

        {rawTranscript && rawTranscript !== initialTranscript && (
          <div
            style={{
              padding: '0.65rem 0.85rem',
              backgroundColor: 'var(--slate-100)',
              borderRadius: 'var(--radius-sm)',
              border: '1px solid var(--slate-200)',
              fontSize: '0.75rem',
              color: 'var(--slate-600)',
            }}
          >
            <div style={{ fontWeight: 600, marginBottom: '0.25rem', color: 'var(--slate-700)' }}>
              Bản bóc băng gốc từ Whisper (Read-only reference):
            </div>
            <div style={{ fontStyle: 'italic', maxHeight: '80px', overflowY: 'auto' }}>
              "{rawTranscript}"
            </div>
          </div>
        )}

        <div>
          <label
            style={{
              display: 'block',
              fontSize: '0.8rem',
              fontWeight: 600,
              color: 'var(--text-main)',
              marginBottom: '0.35rem',
            }}
          >
            Nội dung bản bóc băng chỉnh sửa (sẽ dùng để AI chấm điểm):
          </label>
          <textarea
            rows={7}
            value={text}
            onChange={(e) => {
              setText(e.target.value);
              if (error) setError('');
            }}
            placeholder="Chỉnh sửa từ ngữ chuyên môn, thuật ngữ viết tắt hoặc câu từ Whisper nhận diện nhầm..."
            style={{
              width: '100%',
              padding: '0.65rem 0.85rem',
              borderRadius: 'var(--radius-sm)',
              border: error ? '1px solid var(--danger-main)' : '1px solid var(--border-color)',
              backgroundColor: 'var(--bg-card)',
              color: 'var(--text-main)',
              fontSize: '0.85rem',
              fontFamily: 'inherit',
              lineHeight: 1.5,
              resize: 'vertical',
              boxSizing: 'border-box',
            }}
          />
          <div
            style={{
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              marginTop: '0.25rem',
              fontSize: '0.7rem',
              color: 'var(--text-muted)',
            }}
          >
            <span style={{ display: 'flex', alignItems: 'center', gap: '0.25rem' }}>
              <Info size={12} color="var(--primary-600)" />
              Sửa transcript giúp AI hiểu chính xác từ vựng chuyên môn mà không làm mất bản gốc.
            </span>
            <span>{text.length} / 10,000 ký tự</span>
          </div>

          {error && (
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', fontSize: '0.75rem', color: 'var(--danger-main)', marginTop: '0.4rem' }}>
              <AlertCircle size={14} />
              <span>{error}</span>
            </div>
          )}
        </div>
      </form>
    </Modal>
  );
}
