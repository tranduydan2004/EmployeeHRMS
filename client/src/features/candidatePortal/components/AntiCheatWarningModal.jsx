import { ShieldAlert, AlertTriangle, ArrowRight } from 'lucide-react';
import { useCandidateInterviewStore } from '../useCandidateInterviewStore';

export function AntiCheatWarningModal() {
  const { activeTabWarning, dismissTabWarning, tabSwitchCount } = useCandidateInterviewStore();

  if (!activeTabWarning) return null;

  return (
    <div style={{
      position: 'fixed',
      top: 0,
      left: 0,
      right: 0,
      bottom: 0,
      backgroundColor: 'rgba(15, 23, 42, 0.75)',
      backdropFilter: 'blur(6px)',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      zIndex: 9999,
      padding: '20px',
    }}>
      <div style={{
        maxWidth: '480px',
        width: '100%',
        backgroundColor: '#ffffff',
        borderRadius: '20px',
        padding: '32px',
        boxShadow: '0 25px 50px -12px rgba(0, 0, 0, 0.25)',
        border: '2px solid #fecaca',
        textAlign: 'center',
      }}>
        <div style={{
          width: '60px',
          height: '60px',
          borderRadius: '50%',
          backgroundColor: '#fee2e2',
          color: '#ef4444',
          display: 'inline-flex',
          alignItems: 'center',
          justifyContent: 'center',
          marginBottom: '16px',
        }}>
          <ShieldAlert size={32} />
        </div>

        <h3 style={{ fontSize: '20px', fontWeight: '800', color: '#991b1b', marginBottom: '8px' }}>
          Cảnh Báo Giám Sát Phỏng Vấn
        </h3>

        <p style={{ color: '#475569', fontSize: '15px', lineHeight: '1.5', marginBottom: '20px' }}>
          Hệ thống phát hiện bạn vừa rời khỏi màn hình hoặc chuyển sang tab ứng dụng khác.
        </p>

        <div style={{
          backgroundColor: '#fff1f2',
          border: '1px solid #ffe4e6',
          borderRadius: '10px',
          padding: '12px 16px',
          fontSize: '14px',
          color: '#be123c',
          marginBottom: '24px',
          textAlign: 'left',
          display: 'flex',
          alignItems: 'center',
          gap: '10px'
        }}>
          <AlertTriangle size={18} style={{ flexShrink: 0 }} />
          <span>
            Số lần rời màn hình ghi nhận: <strong>{tabSwitchCount} lần</strong>. Mọi sự kiện đều được báo cáo cho nhà tuyển dụng.
          </span>
        </div>

        <button
          onClick={dismissTabWarning}
          style={{
            width: '100%',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            gap: '8px',
            backgroundColor: '#dc2626',
            color: '#ffffff',
            padding: '14px 24px',
            borderRadius: '10px',
            border: 'none',
            fontSize: '15px',
            fontWeight: '600',
            cursor: 'pointer',
            transition: 'background-color 0.2s',
          }}
          onMouseEnter={(e) => e.currentTarget.style.backgroundColor = '#b91c1c'}
          onMouseLeave={(e) => e.currentTarget.style.backgroundColor = '#dc2626'}
        >
          <span>Tôi đã hiểu & Tiếp tục làm bài</span>
          <ArrowRight size={18} />
        </button>
      </div>
    </div>
  );
}
