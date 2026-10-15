import { useSmartJdStore } from '../../stores/useSmartJdStore';
import { Button } from '@/components';
import { CheckCircle2, ArrowLeft } from 'lucide-react';
import QuestionBankSection from './QuestionBankSection';

export default function Step3QuestionBank({ onBackToJd, onClose }) {
  const { currentJob, questions } = useSmartJdStore();

  return (
    <div>
      {/* Success Celebration Banner */}
      <div
        style={{
          background: 'linear-gradient(135deg, #ecfdf5 0%, #d1fae5 100%)',
          border: '1px solid #a7f3d0',
          borderRadius: 'var(--radius-md)',
          padding: '1.25rem',
          marginBottom: '1.5rem',
          display: 'flex',
          alignItems: 'center',
          gap: '1rem',
        }}
      >
        <div
          style={{
            width: '46px',
            height: '46px',
            borderRadius: 'var(--radius-full)',
            background: 'linear-gradient(135deg, #059669 0%, #10b981 100%)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: '#ffffff',
            flexShrink: 0,
            boxShadow: '0 4px 10px rgba(16, 185, 129, 0.35)',
          }}
        >
          <CheckCircle2 size={24} />
        </div>
        <div>
          <h3 style={{ margin: 0, fontSize: '1.1rem', fontWeight: 700, color: '#065f46' }}>
            Tin Tuyển Dụng Đã Được Phê Duyệt & Khởi Tạo Ngân Hàng Câu Hỏi!
          </h3>
          <p style={{ margin: '0.25rem 0 0 0', fontSize: '0.85rem', color: '#047857', lineHeight: 1.4 }}>
            Vị trí <strong>{currentJob?.title}</strong> đã chuyển sang trạng thái <strong>Approved</strong>. AI đã sinh thành công {questions?.length || 0} câu hỏi phỏng vấn tình huống kèm barem chấm điểm và thời lượng suy nghĩ / trả lời có thể tùy chỉnh.
          </p>
        </div>
      </div>

      {/* Reusable Question Bank Section with Timeboxing & Edit */}
      <QuestionBankSection
        questions={questions || []}
        jobId={currentJob?.id}
        jobStatus={currentJob?.status}
        canEdit={true}
      />

      {/* Bottom Actions */}
      <div
        style={{
          marginTop: '1.75rem',
          paddingTop: '1.25rem',
          borderTop: '1px solid var(--border-color)',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
        }}
      >
        <Button variant="secondary" icon={ArrowLeft} onClick={onBackToJd}>
          Xem Nội Dung JD (Đã Duyệt)
        </Button>

        <Button variant="primary" onClick={onClose}>
          Hoàn Tất & Đóng
        </Button>
      </div>
    </div>
  );
}
