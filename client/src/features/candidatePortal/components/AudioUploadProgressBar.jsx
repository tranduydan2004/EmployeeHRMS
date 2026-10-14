import { UploadCloud, CheckCircle2, AlertTriangle, RotateCw, Database } from 'lucide-react';

export function AudioUploadProgressBar({
  progress = 0,
  isFailed = false,
  isCompleted = false,
  errorMessage = '',
  onRetry,
}) {
  return (
    <div style={{
      backgroundColor: '#f8fafc',
      borderRadius: '14px',
      padding: '20px',
      border: isFailed ? '1px solid #fecaca' : isCompleted ? '1px solid #a7f3d0' : '1px solid #e2e8f0',
      marginBottom: '20px',
    }}>
      {/* Header Info */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '12px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '14px', fontWeight: '600' }}>
          {isCompleted ? (
            <span style={{ color: '#059669', display: 'flex', alignItems: 'center', gap: '6px' }}>
              <CheckCircle2 size={18} /> Đã nộp thành công!
            </span>
          ) : isFailed ? (
            <span style={{ color: '#dc2626', display: 'flex', alignItems: 'center', gap: '6px' }}>
              <AlertTriangle size={18} /> Không thể kết nối máy chủ
            </span>
          ) : (
            <span style={{ color: '#4f46e5', display: 'flex', alignItems: 'center', gap: '6px' }}>
              <UploadCloud size={18} /> Đang tải câu trả lời lên máy chủ...
            </span>
          )}
        </div>

        <div style={{ fontSize: '14px', fontWeight: '700', color: isFailed ? '#dc2626' : isCompleted ? '#059669' : '#4f46e5' }}>
          {progress}%
        </div>
      </div>

      {/* Progress Track */}
      <div style={{
        height: '10px',
        backgroundColor: '#e2e8f0',
        borderRadius: '5px',
        overflow: 'hidden',
        position: 'relative',
        marginBottom: '12px'
      }}>
        <div style={{
          height: '100%',
          width: `${Math.max(4, progress)}%`,
          backgroundColor: isFailed ? '#ef4444' : isCompleted ? '#10b981' : '#4f46e5',
          borderRadius: '5px',
          transition: 'width 0.2s ease-in-out',
        }} />
      </div>

      {/* Subtext info */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', fontSize: '13px', color: '#64748b' }}>
        <span style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
          <Database size={14} style={{ color: '#10b981' }} />
          Đã lưu an toàn trong bộ nhớ máy (IndexedDB)
        </span>

        {isFailed && onRetry && (
          <button
            onClick={onRetry}
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '6px',
              backgroundColor: '#4f46e5',
              color: '#ffffff',
              padding: '6px 14px',
              borderRadius: '8px',
              border: 'none',
              fontSize: '13px',
              fontWeight: '600',
              cursor: 'pointer',
            }}
          >
            <RotateCw size={14} /> Thử lại
          </button>
        )}
      </div>

      {isFailed && errorMessage && (
        <div style={{ marginTop: '10px', fontSize: '13px', color: '#b91c1c' }}>
          {errorMessage}
        </div>
      )}
    </div>
  );
}
