import { CheckCircle2, Award, ArrowRight, ShieldCheck } from 'lucide-react';
import { useCandidateInterviewStore } from '../useCandidateInterviewStore';

export function InterviewCompletionCard() {
  const { session, tabSwitchCount } = useCandidateInterviewStore();

  const totalQuestions = session?.questions?.length || 0;

  return (
    <div style={{
      maxWidth: '680px',
      margin: '0 auto',
      backgroundColor: '#ffffff',
      borderRadius: '24px',
      boxShadow: '0 20px 25px -5px rgba(0, 0, 0, 0.05)',
      border: '1px solid #e2e8f0',
      overflow: 'hidden',
      textAlign: 'center',
    }}>
      {/* Celebration Header */}
      <div style={{
        background: 'linear-gradient(135deg, #10b981 0%, #059669 100%)',
        padding: '40px 32px',
        color: '#ffffff',
      }}>
        <div style={{
          width: '72px',
          height: '72px',
          borderRadius: '50%',
          backgroundColor: 'rgba(255, 255, 255, 0.2)',
          backdropFilter: 'blur(8px)',
          display: 'inline-flex',
          alignItems: 'center',
          justifyContent: 'center',
          marginBottom: '20px',
        }}>
          <Award size={40} />
        </div>

        <h1 style={{ fontSize: '26px', fontWeight: '800', marginBottom: '8px' }}>
          Hoàn Thành Bài Phỏng Vấn!
        </h1>

        <p style={{ color: '#d1fae5', fontSize: '16px', margin: 0 }}>
          Cảm ơn bạn đã nỗ lực hoàn thành tất cả {totalQuestions} câu hỏi cho vị trí <strong>{session?.jobTitle}</strong>.
        </p>
      </div>

      <div style={{ padding: '36px' }}>
        {/* Summary Info Cards */}
        <div style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
          gap: '16px',
          marginBottom: '32px',
          textAlign: 'left'
        }}>
          <div style={{ backgroundColor: '#f8fafc', padding: '16px', borderRadius: '12px', border: '1px solid #e2e8f0' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: '#64748b', fontSize: '13px', marginBottom: '6px' }}>
              <CheckCircle2 size={16} style={{ color: '#10b981' }} />
              Câu hỏi đã trả lời
            </div>
            <div style={{ fontSize: '20px', fontWeight: '700', color: '#0f172a' }}>
              {totalQuestions} / {totalQuestions} câu
            </div>
          </div>

          <div style={{ backgroundColor: '#f8fafc', padding: '16px', borderRadius: '12px', border: '1px solid #e2e8f0' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: '#64748b', fontSize: '13px', marginBottom: '6px' }}>
              <ShieldCheck size={16} style={{ color: '#6366f1' }} />
              Tình trạng giám sát
            </div>
            <div style={{ fontSize: '20px', fontWeight: '700', color: '#0f172a' }}>
              {tabSwitchCount === 0 ? 'Trung thực 100%' : `${tabSwitchCount} lần rời tab`}
            </div>
          </div>
        </div>

        {/* Next Steps */}
        <div style={{
          backgroundColor: '#f0fdf4',
          borderRadius: '16px',
          padding: '24px',
          border: '1px solid #bbf7d0',
          textAlign: 'left',
          marginBottom: '32px',
        }}>
          <h4 style={{ fontSize: '15px', fontWeight: '700', color: '#166534', margin: '0 0 12px 0' }}>
            Các bước tiếp theo:
          </h4>
          <ul style={{ margin: 0, paddingLeft: '20px', color: '#15803d', fontSize: '14px', lineHeight: '1.6' }}>
            <li>Hệ thống AI Whisper đang chuyển đổi giọng nói của bạn thành văn bản.</li>
            <li>Hội đồng tuyển dụng và quản lý kỹ thuật sẽ đánh giá bài nói và hồ sơ của bạn.</li>
            <li>Kết quả sơ loại sẽ được gửi qua email cho bạn trong vòng 2–3 ngày làm việc.</li>
          </ul>
        </div>

        <button
          onClick={() => {
            window.location.href = '/jobs';
          }}
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: '8px',
            backgroundColor: '#0f172a',
            color: '#ffffff',
            padding: '14px 28px',
            borderRadius: '12px',
            fontSize: '15px',
            fontWeight: '600',
            border: 'none',
            cursor: 'pointer',
            transition: 'background-color 0.2s',
          }}
          onMouseEnter={(e) => e.currentTarget.style.backgroundColor = '#1e293b'}
          onMouseLeave={(e) => e.currentTarget.style.backgroundColor = '#0f172a'}
        >
          <span>Khám phá các vị trí tuyển dụng khác</span>
          <ArrowRight size={18} />
        </button>
      </div>
    </div>
  );
}
