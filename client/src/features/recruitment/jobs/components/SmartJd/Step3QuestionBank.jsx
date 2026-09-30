import { useState } from 'react';
import { useSmartJdStore } from '../../stores/useSmartJdStore';
import { Button, Badge } from '@/components';
import {
  CheckCircle2,
  HelpCircle,
  ChevronDown,
  ChevronUp,
  Award,
  Sparkles,
  ArrowLeft,
  Briefcase,
  Layers,
} from 'lucide-react';

const CATEGORY_COLORS = {
  Technical: { bg: '#eff6ff', text: '#1e40af', border: '#bfdbfe' },
  Behavioral: { bg: '#faf5ff', text: '#6b21a8', border: '#e9d5ff' },
  Situational: { bg: '#fff7ed', text: '#9a3412', border: '#fed7aa' },
  CulturalFit: { bg: '#f0fdfa', text: '#115e59', border: '#99f6e4' },
};

export default function Step3QuestionBank({ onBackToJd, onClose }) {
  const { currentJob, questions } = useSmartJdStore();
  const [expandedIndex, setExpandedIndex] = useState(0); // Mở câu hỏi đầu tiên mặc định

  const toggleExpand = (idx) => {
    setExpandedIndex(expandedIndex === idx ? -1 : idx);
  };

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
            flexshrink: 0,
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
            Vị trí <strong>{currentJob?.title}</strong> đã chuyển sang trạng thái <strong>Approved</strong>. AI đã sinh thành công {questions?.length || 0} câu hỏi phỏng vấn tình huống độc quyền kèm barem chấm điểm 4 mức chi tiết.
          </p>
        </div>
      </div>

      {/* Danh sách câu hỏi */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
        {questions?.map((item, idx) => {
          const isExpanded = expandedIndex === idx;
          const catStyle = CATEGORY_COLORS[item.category] || CATEGORY_COLORS.Technical;
          const rubric = item.scoringRubric;

          return (
            <div
              key={item.id || idx}
              style={{
                border: '1px solid var(--border-color)',
                borderRadius: 'var(--radius-md)',
                backgroundColor: 'var(--bg-card)',
                overflow: 'hidden',
                boxShadow: isExpanded ? 'var(--shadow-sm)' : 'none',
                transition: 'all var(--transition-fast)',
              }}
            >
              {/* Question Header (Click to expand) */}
              <div
                onClick={() => toggleExpand(idx)}
                style={{
                  padding: '1rem 1.25rem',
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'flex-start',
                  gap: '1rem',
                  cursor: 'pointer',
                  backgroundColor: isExpanded ? 'var(--slate-50)' : 'transparent',
                }}
              >
                <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'flex-start', flex: 1 }}>
                  <span
                    style={{
                      display: 'inline-flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      width: '26px',
                      height: '26px',
                      borderRadius: 'var(--radius-full)',
                      backgroundColor: 'var(--primary-100)',
                      color: 'var(--primary-700)',
                      fontSize: '0.8rem',
                      fontWeight: 700,
                      flexshrink: 0,
                      marginTop: '0.1rem',
                    }}
                  >
                    {idx + 1}
                  </span>

                  <div>
                    <h4
                      style={{
                        margin: 0,
                        fontSize: '0.975rem',
                        fontWeight: 600,
                        color: 'var(--slate-900)',
                        lineHeight: 1.5,
                      }}
                    >
                      {item.question}
                    </h4>

                    <div style={{ display: 'flex', gap: '0.5rem', marginTop: '0.5rem', flexWrap: 'wrap' }}>
                      <span
                        style={{
                          fontSize: '0.75rem',
                          fontWeight: 600,
                          padding: '0.15rem 0.55rem',
                          borderRadius: 'var(--radius-full)',
                          backgroundColor: catStyle.bg,
                          color: catStyle.text,
                          border: `1px solid ${catStyle.border}`,
                        }}
                      >
                        {item.category}
                      </span>

                      <span
                        style={{
                          fontSize: '0.75rem',
                          fontWeight: 500,
                          padding: '0.15rem 0.55rem',
                          borderRadius: 'var(--radius-full)',
                          backgroundColor: 'var(--slate-100)',
                          color: 'var(--slate-700)',
                        }}
                      >
                        Độ khó: {item.difficulty}
                      </span>
                    </div>
                  </div>
                </div>

                <button
                  type="button"
                  style={{
                    border: 'none',
                    background: 'transparent',
                    cursor: 'pointer',
                    color: 'var(--slate-400)',
                    padding: '0.25rem',
                  }}
                  aria-label={isExpanded ? 'Thu gọn barem' : 'Mở rộng barem'}
                >
                  {isExpanded ? <ChevronUp size={18} /> : <ChevronDown size={18} />}
                </button>
              </div>

              {/* Scoring Rubric (Collapsible) */}
              {isExpanded && rubric && (
                <div
                  style={{
                    padding: '1.25rem',
                    borderTop: '1px solid var(--border-color)',
                    backgroundColor: 'var(--bg-card)',
                  }}
                >
                  <div
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: '0.45rem',
                      marginBottom: '1rem',
                      color: 'var(--slate-700)',
                      fontSize: '0.85rem',
                      fontWeight: 600,
                    }}
                  >
                    <Award size={16} color="var(--primary-600)" />
                    <span>Barem đánh giá câu trả lời (Scoring Rubric):</span>
                  </div>

                  <div
                    style={{
                      display: 'grid',
                      gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))',
                      gap: '0.75rem',
                    }}
                  >
                    {/* Xuất sắc (9-10đ) */}
                    <div
                      style={{
                        padding: '0.85rem',
                        borderRadius: 'var(--radius-md)',
                        backgroundColor: '#ecfdf5',
                        border: '1px solid #a7f3d0',
                      }}
                    >
                      <div
                        style={{
                          display: 'flex',
                          alignItems: 'center',
                          gap: '0.35rem',
                          fontSize: '0.8rem',
                          fontWeight: 700,
                          color: '#065f46',
                          marginBottom: '0.35rem',
                        }}
                      >
                        <span>🌟 Xuất Sắc (9 - 10 điểm)</span>
                      </div>
                      <p style={{ margin: 0, fontSize: '0.8rem', color: '#047857', lineHeight: 1.5 }}>
                        {rubric.excellent}
                      </p>
                    </div>

                    {/* Tốt (7-8đ) */}
                    <div
                      style={{
                        padding: '0.85rem',
                        borderRadius: 'var(--radius-md)',
                        backgroundColor: '#eff6ff',
                        border: '1px solid #bfdbfe',
                      }}
                    >
                      <div
                        style={{
                          display: 'flex',
                          alignItems: 'center',
                          gap: '0.35rem',
                          fontSize: '0.8rem',
                          fontWeight: 700,
                          color: '#1e40af',
                          marginBottom: '0.35rem',
                        }}
                      >
                        <span>👍 Tốt (7 - 8 điểm)</span>
                      </div>
                      <p style={{ margin: 0, fontSize: '0.8rem', color: '#1d4ed8', lineHeight: 1.5 }}>
                        {rubric.good}
                      </p>
                    </div>

                    {/* Chấp nhận được (5-6đ) */}
                    <div
                      style={{
                        padding: '0.85rem',
                        borderRadius: 'var(--radius-md)',
                        backgroundColor: '#fffbeb',
                        border: '1px solid #fde68a',
                      }}
                    >
                      <div
                        style={{
                          display: 'flex',
                          alignItems: 'center',
                          gap: '0.35rem',
                          fontSize: '0.8rem',
                          fontWeight: 700,
                          color: '#92400e',
                          marginBottom: '0.35rem',
                        }}
                      >
                        <span>👌 Chấp Nhận (5 - 6 điểm)</span>
                      </div>
                      <p style={{ margin: 0, fontSize: '0.8rem', color: '#b45309', lineHeight: 1.5 }}>
                        {rubric.acceptable}
                      </p>
                    </div>

                    {/* Kém (< 5đ) */}
                    <div
                      style={{
                        padding: '0.85rem',
                        borderRadius: 'var(--radius-md)',
                        backgroundColor: '#fef2f2',
                        border: '1px solid #fecaca',
                      }}
                    >
                      <div
                        style={{
                          display: 'flex',
                          alignItems: 'center',
                          gap: '0.35rem',
                          fontSize: '0.8rem',
                          fontWeight: 700,
                          color: '#991b1b',
                          marginBottom: '0.35rem',
                        }}
                      >
                        <span>⚠️ Cần Cải Thiện (&lt; 5 điểm)</span>
                      </div>
                      <p style={{ margin: 0, fontSize: '0.8rem', color: '#b91c1c', lineHeight: 1.5 }}>
                        {rubric.poor}
                      </p>
                    </div>
                  </div>
                </div>
              )}
            </div>
          );
        })}
      </div>

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
