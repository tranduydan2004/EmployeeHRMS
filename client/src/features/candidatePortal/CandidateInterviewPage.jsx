import { useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import {
  Clock,
  CheckCircle2,
  AlertTriangle,
  WifiOff,
  Briefcase,
} from 'lucide-react';
import { useCandidateInterviewStore } from './useCandidateInterviewStore';
import { useAntiCheat } from './hooks/useAntiCheat';
import { CandidateErrorBoundary } from './components/CandidateErrorBoundary';
import { InterviewWelcomeCard } from './components/InterviewWelcomeCard';
import { MicCheckCard } from './components/MicCheckCard';
import { QuestionSteppedCard } from './components/QuestionSteppedCard';
import { InterviewCompletionCard } from './components/InterviewCompletionCard';
import { AntiCheatWarningModal } from './components/AntiCheatWarningModal';

function CandidateInterviewContent() {
  const { token } = useParams();
  const {
    session,
    stage,
    isLoading,
    error,
    errorStatus,
    isOnline,
    loadSession,
    setNetworkStatus,
    recordTabSwitch,
  } = useCandidateInterviewStore();

  // 1. Tải dữ liệu phiên phỏng vấn khi mount
  useEffect(() => {
    if (token) {
      loadSession(token);
    }
  }, [token, loadSession]);

  // 2. Theo dõi trạng thái online / offline
  useEffect(() => {
    const handleOnline = () => setNetworkStatus(true);
    const handleOffline = () => setNetworkStatus(false);

    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);

    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, [setNetworkStatus]);

  // 3. Theo dõi Anti-Cheat khi đang làm bài
  useAntiCheat({
    isActive: stage === 'in_progress',
    onTabSwitched: (durationSec) => {
      recordTabSwitch(durationSec);
    },
  });

  return (
    <div style={{
      minHeight: '100vh',
      backgroundColor: '#f8fafc',
      color: '#0f172a',
      fontFamily: 'Inter, system-ui, -apple-system, sans-serif',
      display: 'flex',
      flexDirection: 'column',
    }}>
      {/* Offline Alert Bar */}
      {!isOnline && (
        <div style={{
          backgroundColor: '#dc2626',
          color: '#ffffff',
          padding: '10px 20px',
          textAlign: 'center',
          fontSize: '14px',
          fontWeight: '600',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          gap: '8px',
        }}>
          <WifiOff size={18} />
          <span>Mất kết nối Internet! Mọi câu trả lời của bạn vẫn được lưu dự phòng trong bộ nhớ máy. Vui lòng kiểm tra lại mạng.</span>
        </div>
      )}

      {/* Header Bar */}
      <header style={{
        backgroundColor: '#ffffff',
        borderBottom: '1px solid #e2e8f0',
        padding: '16px 32px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
          <div style={{
            width: '36px',
            height: '36px',
            borderRadius: '10px',
            backgroundColor: '#4f46e5',
            color: '#ffffff',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            fontWeight: '800',
            fontSize: '18px',
          }}>
            H
          </div>
          <div>
            <div style={{ fontSize: '16px', fontWeight: '800', color: '#0f172a', lineHeight: '1.2' }}>
              EmployeeHRMS
            </div>
            <div style={{ fontSize: '12px', color: '#64748b' }}>
              Candidate Audio Screening Portal
            </div>
          </div>
        </div>

        {session && (
          <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
            <span style={{ fontSize: '13px', color: '#64748b' }}>
              Ứng viên: <strong style={{ color: '#0f172a' }}>{session.candidateName}</strong>
            </span>
            <span style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '6px',
              padding: '4px 10px',
              borderRadius: '20px',
              backgroundColor: '#e0e7ff',
              color: '#4338ca',
              fontSize: '12px',
              fontWeight: '600',
            }}>
              <Briefcase size={14} />
              {session.jobTitle}
            </span>
          </div>
        )}
      </header>

      {/* Main Container */}
      <main style={{
        flex: 1,
        padding: '40px 20px',
        maxWidth: '1000px',
        width: '100%',
        margin: '0 auto',
      }}>
        {/* Loading State */}
        {isLoading && (
          <div style={{
            textAlign: 'center',
            padding: '80px 20px',
            backgroundColor: '#ffffff',
            borderRadius: '20px',
            border: '1px solid #e2e8f0',
            maxWidth: '600px',
            margin: '0 auto'
          }}>
            <div style={{
              width: '48px',
              height: '48px',
              border: '4px solid #e2e8f0',
              borderTopColor: '#4f46e5',
              borderRadius: '50%',
              animation: 'spin 1s linear infinite',
              margin: '0 auto 20px auto',
            }} />
            <style>{`@keyframes spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }`}</style>
            <h3 style={{ fontSize: '18px', fontWeight: '700', marginBottom: '8px' }}>
              Đang tải phiên phỏng vấn...
            </h3>
            <p style={{ color: '#64748b', fontSize: '14px' }}>
              Đang xác thực liên kết bảo mật và chuẩn bị danh sách câu hỏi.
            </p>
          </div>
        )}

        {/* 410 Gone: Link Expiration */}
        {!isLoading && errorStatus === 410 && (
          <div style={{
            textAlign: 'center',
            padding: '48px 32px',
            backgroundColor: '#ffffff',
            borderRadius: '20px',
            border: '1px solid #fecaca',
            maxWidth: '560px',
            margin: '0 auto'
          }}>
            <div style={{
              width: '64px',
              height: '64px',
              borderRadius: '50%',
              backgroundColor: '#fee2e2',
              color: '#ef4444',
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              marginBottom: '20px'
            }}>
              <Clock size={32} />
            </div>
            <h2 style={{ fontSize: '22px', fontWeight: '800', color: '#991b1b', marginBottom: '12px' }}>
              Liên Kết Phỏng Vấn Đã Hết Hạn
            </h2>
            <p style={{ color: '#64748b', fontSize: '15px', lineHeight: '1.6', marginBottom: '24px' }}>
              Thời hạn tham gia bài phỏng vấn audio này đã kết thúc. Vui lòng liên hệ với bộ phận Tuyển dụng (HR) nếu bạn cần được cấp lại liên kết mời phỏng vấn mới.
            </p>
            <Link
              to="/jobs"
              style={{
                display: 'inline-block',
                backgroundColor: '#0f172a',
                color: '#ffffff',
                padding: '12px 24px',
                borderRadius: '10px',
                textDecoration: 'none',
                fontWeight: '600',
                fontSize: '14px'
              }}
            >
              Xem các vị trí tuyển dụng khác
            </Link>
          </div>
        )}

        {/* 409 Conflict: Interview Completed */}
        {!isLoading && errorStatus === 409 && (
          <div style={{
            textAlign: 'center',
            padding: '48px 32px',
            backgroundColor: '#ffffff',
            borderRadius: '20px',
            border: '1px solid #a7f3d0',
            maxWidth: '560px',
            margin: '0 auto'
          }}>
            <div style={{
              width: '64px',
              height: '64px',
              borderRadius: '50%',
              backgroundColor: '#ecfdf5',
              color: '#10b981',
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              marginBottom: '20px'
            }}>
              <CheckCircle2 size={32} />
            </div>
            <h2 style={{ fontSize: '22px', fontWeight: '800', color: '#065f46', marginBottom: '12px' }}>
              Phiên Phỏng Vấn Đã Hoàn Tất
            </h2>
            <p style={{ color: '#64748b', fontSize: '15px', lineHeight: '1.6', marginBottom: '24px' }}>
              Buổi phỏng vấn này đã được nộp và ghi nhận đầy đủ trên hệ thống. Bạn không thể thực hiện làm bài lại.
            </p>
            <Link
              to="/jobs"
              style={{
                display: 'inline-block',
                backgroundColor: '#10b981',
                color: '#ffffff',
                padding: '12px 24px',
                borderRadius: '10px',
                textDecoration: 'none',
                fontWeight: '600',
                fontSize: '14px'
              }}
            >
              Khám phá thêm công việc
            </Link>
          </div>
        )}

        {/* 404 or other generic errors */}
        {!isLoading && error && errorStatus !== 410 && errorStatus !== 409 && (
          <div style={{
            textAlign: 'center',
            padding: '48px 32px',
            backgroundColor: '#ffffff',
            borderRadius: '20px',
            border: '1px solid #e2e8f0',
            maxWidth: '560px',
            margin: '0 auto'
          }}>
            <div style={{
              width: '64px',
              height: '64px',
              borderRadius: '50%',
              backgroundColor: '#fef2f2',
              color: '#ef4444',
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              marginBottom: '20px'
            }}>
              <AlertTriangle size={32} />
            </div>
            <h2 style={{ fontSize: '22px', fontWeight: '800', color: '#0f172a', marginBottom: '12px' }}>
              Không Tìm Thấy Phiên Phỏng Vấn
            </h2>
            <p style={{ color: '#64748b', fontSize: '15px', lineHeight: '1.6', marginBottom: '24px' }}>
              {error}
            </p>
            <Link
              to="/jobs"
              style={{
                display: 'inline-block',
                backgroundColor: '#0f172a',
                color: '#ffffff',
                padding: '12px 24px',
                borderRadius: '10px',
                textDecoration: 'none',
                fontWeight: '600',
                fontSize: '14px'
              }}
            >
              Về trang chủ Tuyển dụng
            </Link>
          </div>
        )}

        {/* Main Stage Switching */}
        {!isLoading && !error && (
          <>
            {stage === 'welcome' && <InterviewWelcomeCard />}
            {stage === 'mic_check' && <MicCheckCard />}
            {stage === 'in_progress' && <QuestionSteppedCard />}
            {stage === 'completed' && <InterviewCompletionCard />}
          </>
        )}
      </main>

      {/* Anti-Cheat Overlay Modal */}
      <AntiCheatWarningModal />

      {/* Footer */}
      <footer style={{
        textAlign: 'center',
        padding: '24px',
        fontSize: '13px',
        color: '#94a3b8',
        borderTop: '1px solid #f1f5f9',
      }}>
        © {new Date().getFullYear()} Employee HRMS — Nền tảng Phỏng vấn Trực tuyến Sơ loại An toàn.
      </footer>
    </div>
  );
}

export function CandidateInterviewPage() {
  return (
    <CandidateErrorBoundary>
      <CandidateInterviewContent />
    </CandidateErrorBoundary>
  );
}
export default CandidateInterviewPage;
