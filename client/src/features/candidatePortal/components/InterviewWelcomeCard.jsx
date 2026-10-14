import { Mic, Clock, ShieldCheck, CheckCircle2, AlertCircle, ArrowRight, Sparkles } from 'lucide-react';
import { useCandidateInterviewStore } from '../useCandidateInterviewStore';

export function InterviewWelcomeCard() {
  const { session, isConsentGiven, setConsent, setStage } = useCandidateInterviewStore();

  if (!session) return null;

  const totalQuestions = session.questions?.length || 0;
  // Tính tổng thời lượng ước lượng
  const totalSeconds = (session.questions || []).reduce(
    (acc, q) => acc + (q.thinkSeconds || 20) + (q.answerSeconds || 60),
    0
  );
  const estimatedMinutes = Math.ceil(totalSeconds / 60);

  return (
    <div style={{
      maxWidth: '720px',
      margin: '0 auto',
      backgroundColor: '#ffffff',
      borderRadius: '20px',
      boxShadow: '0 20px 25px -5px rgba(0, 0, 0, 0.05), 0 8px 10px -6px rgba(0, 0, 0, 0.01)',
      border: '1px solid #e2e8f0',
      overflow: 'hidden',
    }}>
      {/* Header Banner */}
      <div style={{
        background: 'linear-gradient(135deg, #4f46e5 0%, #3b82f6 100%)',
        padding: '32px 36px',
        color: '#ffffff',
      }}>
        <div style={{ display: 'inline-flex', alignItems: 'center', gap: '8px', padding: '6px 14px', borderRadius: '20px', backgroundColor: 'rgba(255, 255, 255, 0.15)', backdropFilter: 'blur(8px)', fontSize: '13px', fontWeight: '600', marginBottom: '16px' }}>
          <Sparkles size={16} />
          Phỏng vấn sơ loại thông minh AI
        </div>
        <h1 style={{ fontSize: '28px', fontWeight: '800', lineHeight: '1.2', marginBottom: '8px' }}>
          {session.jobTitle || 'Vị trí Ứng tuyển'}
        </h1>
        <p style={{ color: '#e0e7ff', fontSize: '16px', margin: 0 }}>
          Chào mừng <strong>{session.candidateName || 'Ứng viên'}</strong> tham gia vòng phỏng vấn ghi âm định hướng.
        </p>
      </div>

      <div style={{ padding: '36px' }}>
        {/* Quick Stats Grid */}
        <div style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
          gap: '16px',
          marginBottom: '32px'
        }}>
          <div style={{ backgroundColor: '#f8fafc', padding: '16px', borderRadius: '12px', border: '1px solid #e2e8f0' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: '#64748b', fontSize: '13px', marginBottom: '6px' }}>
              <Clock size={16} />
              Thời lượng ước tính
            </div>
            <div style={{ fontSize: '20px', fontWeight: '700', color: '#0f172a' }}>
              ~{estimatedMinutes} phút
            </div>
          </div>

          <div style={{ backgroundColor: '#f8fafc', padding: '16px', borderRadius: '12px', border: '1px solid #e2e8f0' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: '#64748b', fontSize: '13px', marginBottom: '6px' }}>
              <Mic size={16} />
              Số lượng câu hỏi
            </div>
            <div style={{ fontSize: '20px', fontWeight: '700', color: '#0f172a' }}>
              {totalQuestions} câu hỏi
            </div>
          </div>

          <div style={{ backgroundColor: '#f8fafc', padding: '16px', borderRadius: '12px', border: '1px solid #e2e8f0' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: '#64748b', fontSize: '13px', marginBottom: '6px' }}>
              <ShieldCheck size={16} />
              Hình thức
            </div>
            <div style={{ fontSize: '20px', fontWeight: '700', color: '#0f172a' }}>
              Ghi âm 1 chiều
            </div>
          </div>
        </div>

        {/* Instructions */}
        <h3 style={{ fontSize: '17px', fontWeight: '700', color: '#0f172a', marginBottom: '16px' }}>
          Quy trình và lưu ý quan trọng:
        </h3>

        <div style={{ display: 'flex', flexDirection: 'column', gap: '14px', marginBottom: '32px' }}>
          <div style={{ display: 'flex', gap: '12px', alignItems: 'flex-start' }}>
            <div style={{ color: '#4f46e5', marginTop: '2px' }}><CheckCircle2 size={18} /></div>
            <div style={{ fontSize: '14px', color: '#334155', lineHeight: '1.5' }}>
              <strong>Kiểm tra thiết bị:</strong> Bạn sẽ được kiểm tra âm thanh micro trước khi bắt đầu bài thi thật.
            </div>
          </div>

          <div style={{ display: 'flex', gap: '12px', alignItems: 'flex-start' }}>
            <div style={{ color: '#4f46e5', marginTop: '2px' }}><CheckCircle2 size={18} /></div>
            <div style={{ fontSize: '14px', color: '#334155', lineHeight: '1.5' }}>
              <strong>Bộ đếm thời gian:</strong> Mỗi câu hỏi sẽ có thời gian chuẩn bị (20–40 giây) và thời gian trả lời (60–120 giây). Hệ thống sẽ tự động bắt đầu ghi âm và nộp bài khi hết giờ.
            </div>
          </div>

          <div style={{ display: 'flex', gap: '12px', alignItems: 'flex-start' }}>
            <div style={{ color: '#f59e0b', marginTop: '2px' }}><AlertCircle size={18} /></div>
            <div style={{ fontSize: '14px', color: '#334155', lineHeight: '1.5' }}>
              <strong>Giám sát chống gian lận:</strong> Vui lòng giữ tab phỏng vấn trên màn hình. Mọi hành vi chuyển tab, thoát cửa sổ hoặc tắt âm thanh sẽ được tự động ghi nhận vào báo cáo tuyển dụng.
            </div>
          </div>

          <div style={{ display: 'flex', gap: '12px', alignItems: 'flex-start' }}>
            <div style={{ color: '#10b981', marginTop: '2px' }}><CheckCircle2 size={18} /></div>
            <div style={{ fontSize: '14px', color: '#334155', lineHeight: '1.5' }}>
              <strong>Sao lưu dữ liệu an toàn:</strong> File ghi âm của bạn được lưu tạm thời trên trình duyệt ngay khi kết thúc câu trả lời, đảm bảo không bị mất bài nếu mạng internet bị ngắt quãng.
            </div>
          </div>
        </div>

        {/* Consent Checkbox */}
        <div style={{
          backgroundColor: '#f1f5f9',
          padding: '16px 20px',
          borderRadius: '12px',
          marginBottom: '28px',
          display: 'flex',
          alignItems: 'center',
          gap: '12px',
          cursor: 'pointer'
        }}
        onClick={() => setConsent(!isConsentGiven)}
        >
          <input
            type="checkbox"
            id="consent-checkbox"
            checked={isConsentGiven}
            onChange={(e) => setConsent(e.target.checked)}
            style={{ width: '18px', height: '18px', accentColor: '#4f46e5', cursor: 'pointer' }}
          />
          <label htmlFor="consent-checkbox" style={{ fontSize: '14px', color: '#1e293b', fontWeight: '500', cursor: 'pointer', userSelect: 'none' }}>
            Tôi đã đọc kỹ hướng dẫn và đồng ý cho phép ghi âm câu trả lời phỏng vấn cho vị trí này.
          </label>
        </div>

        {/* Action Button */}
        <button
          disabled={!isConsentGiven}
          onClick={() => setStage('mic_check')}
          style={{
            width: '100%',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            gap: '10px',
            backgroundColor: isConsentGiven ? '#4f46e5' : '#cbd5e1',
            color: '#ffffff',
            padding: '16px 28px',
            borderRadius: '12px',
            fontSize: '16px',
            fontWeight: '600',
            border: 'none',
            cursor: isConsentGiven ? 'pointer' : 'not-allowed',
            transition: 'all 0.2s',
            boxShadow: isConsentGiven ? '0 10px 15px -3px rgba(79, 70, 229, 0.3)' : 'none'
          }}
          onMouseEnter={(e) => { if (isConsentGiven) e.currentTarget.style.backgroundColor = '#4338ca'; }}
          onMouseLeave={(e) => { if (isConsentGiven) e.currentTarget.style.backgroundColor = '#4f46e5'; }}
        >
          <span>Tiếp tục kiểm tra thiết bị Micro</span>
          <ArrowRight size={20} />
        </button>
      </div>
    </div>
  );
}
