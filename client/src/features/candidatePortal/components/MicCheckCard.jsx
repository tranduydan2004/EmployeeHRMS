import { useEffect } from 'react';
import { Mic, Volume2, CheckCircle2, ArrowLeft, Play, ShieldAlert } from 'lucide-react';
import { useAudioRecorder } from '../hooks/useAudioRecorder';
import { useCandidateInterviewStore } from '../useCandidateInterviewStore';

export function MicCheckCard() {
  const { confirmStartInterview, setStage, isLoading } = useCandidateInterviewStore();
  const {
    hasPermission,
    permissionError,
    deviceLabel,
    volumeLevel,
    requestPermission,
  } = useAudioRecorder();

  // Tự động kiểm tra quyền khi mở card
  useEffect(() => {
    requestPermission();
  }, [requestPermission]);

  return (
    <div style={{
      maxWidth: '680px',
      margin: '0 auto',
      backgroundColor: '#ffffff',
      borderRadius: '20px',
      boxShadow: '0 20px 25px -5px rgba(0, 0, 0, 0.05)',
      border: '1px solid #e2e8f0',
      padding: '36px',
    }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '20px' }}>
        <button
          onClick={() => setStage('welcome')}
          style={{
            background: 'none',
            border: 'none',
            cursor: 'pointer',
            color: '#64748b',
            display: 'flex',
            alignItems: 'center',
            padding: '4px',
            borderRadius: '6px',
          }}
          title="Quay lại"
        >
          <ArrowLeft size={20} />
        </button>
        <h2 style={{ fontSize: '22px', fontWeight: '800', color: '#0f172a', margin: 0 }}>
          Kiểm tra Micro & Thiết bị
        </h2>
      </div>

      <p style={{ color: '#64748b', fontSize: '15px', lineHeight: '1.5', marginBottom: '28px' }}>
        Hãy thử nói một vài câu ngắn vào micro (ví dụ: "Xin chào, tôi là ứng viên...") để kiểm tra xem hệ thống đã thu âm tốt chưa.
      </p>

      {/* Mic Status & Sound Visualizer */}
      <div style={{
        backgroundColor: '#f8fafc',
        borderRadius: '16px',
        padding: '24px',
        border: '1px solid #e2e8f0',
        marginBottom: '28px',
      }}>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '16px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <div style={{
              width: '40px',
              height: '40px',
              borderRadius: '50%',
              backgroundColor: hasPermission ? 'rgba(16, 185, 129, 0.15)' : 'rgba(239, 68, 68, 0.1)',
              color: hasPermission ? '#10b981' : '#ef4444',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}>
              <Mic size={20} />
            </div>
            <div>
              <div style={{ fontSize: '15px', fontWeight: '600', color: '#0f172a' }}>
                {deviceLabel || 'Đang kết nối thiết bị...'}
              </div>
              <div style={{ fontSize: '13px', color: hasPermission ? '#059669' : '#dc2626' }}>
                {hasPermission === true && 'Micro sẵn sàng hoạt động'}
                {hasPermission === false && 'Chưa có quyền truy cập micro'}
                {hasPermission === null && 'Đang xin quyền...'}
              </div>
            </div>
          </div>

          {hasPermission && (
            <div style={{ display: 'flex', alignItems: 'center', gap: '6px', color: '#059669', fontSize: '13px', fontWeight: '600', backgroundColor: '#ecfdf5', padding: '6px 12px', borderRadius: '20px' }}>
              <CheckCircle2 size={16} />
              Đã kết nối
            </div>
          )}
        </div>

        {/* Live Audio Visualizer Bars */}
        <div style={{ marginTop: '18px' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '12px', color: '#64748b', marginBottom: '8px' }}>
            <span style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
              <Volume2 size={14} /> Mức âm lượng thu nhận
            </span>
            <span>{volumeLevel}%</span>
          </div>

          <div style={{
            height: '14px',
            backgroundColor: '#e2e8f0',
            borderRadius: '7px',
            overflow: 'hidden',
            position: 'relative'
          }}>
            <div style={{
              height: '100%',
              width: `${Math.max(5, volumeLevel)}%`,
              backgroundColor: volumeLevel > 70 ? '#ef4444' : volumeLevel > 20 ? '#10b981' : '#6366f1',
              borderRadius: '7px',
              transition: 'width 0.08s ease-out, background-color 0.2s',
            }} />
          </div>

          {/* Dynamic Frequency Graphic Visualizer */}
          <div style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            gap: '4px',
            height: '36px',
            marginTop: '16px',
            padding: '4px 0',
          }}>
            {[0.4, 0.7, 1.0, 0.6, 0.9, 0.5, 0.8, 1.2, 0.7, 0.4].map((multiplier, i) => {
              const barHeight = Math.max(6, Math.min(32, (volumeLevel * multiplier) / 2.5));
              return (
                <div
                  key={i}
                  style={{
                    width: '6px',
                    height: `${barHeight}px`,
                    backgroundColor: volumeLevel > 15 ? '#4f46e5' : '#cbd5e1',
                    borderRadius: '3px',
                    transition: 'height 0.05s ease-out, background-color 0.15s',
                  }}
                />
              );
            })}
          </div>
        </div>

        {/* Permission Error Banner */}
        {permissionError && (
          <div style={{
            marginTop: '16px',
            backgroundColor: '#fef2f2',
            border: '1px solid #fecaca',
            borderRadius: '10px',
            padding: '14px',
            display: 'flex',
            gap: '12px',
            color: '#991b1b',
            fontSize: '14px',
            lineHeight: '1.4',
          }}>
            <ShieldAlert size={20} style={{ flexShrink: 0, marginTop: '2px' }} />
            <div>
              <div><strong>Lỗi quyền micro:</strong> {permissionError}</div>
              <button
                onClick={requestPermission}
                style={{
                  marginTop: '10px',
                  backgroundColor: '#dc2626',
                  color: '#ffffff',
                  border: 'none',
                  borderRadius: '6px',
                  padding: '6px 14px',
                  fontSize: '13px',
                  fontWeight: '600',
                  cursor: 'pointer'
                }}
              >
                Cấp quyền lại
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Start Button */}
      <button
        disabled={!hasPermission || isLoading}
        onClick={confirmStartInterview}
        style={{
          width: '100%',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          gap: '10px',
          backgroundColor: hasPermission && !isLoading ? '#10b981' : '#cbd5e1',
          color: '#ffffff',
          padding: '16px 28px',
          borderRadius: '12px',
          fontSize: '16px',
          fontWeight: '600',
          border: 'none',
          cursor: hasPermission && !isLoading ? 'pointer' : 'not-allowed',
          transition: 'all 0.2s',
          boxShadow: hasPermission ? '0 10px 15px -3px rgba(16, 185, 129, 0.3)' : 'none',
        }}
        onMouseEnter={(e) => { if (hasPermission && !isLoading) e.currentTarget.style.backgroundColor = '#059669'; }}
        onMouseLeave={(e) => { if (hasPermission && !isLoading) e.currentTarget.style.backgroundColor = '#10b981'; }}
      >
        <Play size={20} fill="#ffffff" />
        <span>{isLoading ? 'Đang khởi tạo bài thi...' : 'Micro đã ổn định — Bắt đầu bài thi ngay'}</span>
      </button>
    </div>
  );
}
