import { useState } from 'react';
import { Button } from '@/components';
import {
  Clock,
  Mic,
  Award,
  ChevronDown,
  ChevronUp,
  Edit2,
  Info,
  Timer,
  CheckCircle2,
  Sparkles,
} from 'lucide-react';
import EditQuestionModal from './EditQuestionModal';

const CATEGORY_COLORS = {
  Technical: { bg: '#eff6ff', text: '#1e40af', border: '#bfdbfe' },
  Behavioral: { bg: '#faf5ff', text: '#6b21a8', border: '#e9d5ff' },
  Situational: { bg: '#fff7ed', text: '#9a3412', border: '#fed7aa' },
  CulturalFit: { bg: '#f0fdfa', text: '#115e59', border: '#99f6e4' },
};

function formatDuration(totalSeconds) {
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  if (minutes === 0) return `${seconds} giây`;
  if (seconds === 0) return `~${minutes} phút`;
  return `~${minutes} phút ${seconds}s`;
}

export default function QuestionBankSection({
  questions = [],
  jobId,
  canEdit = true,
  jobStatus,
}) {
  const [expandedIndex, setExpandedIndex] = useState(0);
  const [editingQuestion, setEditingQuestion] = useState(null);

  const toggleExpand = (idx) => {
    setExpandedIndex(expandedIndex === idx ? -1 : idx);
  };

  const isClosed = jobStatus === 'Closed';
  const allowEdit = canEdit && !isClosed;

  // Tính tổng thời lượng phỏng vấn
  const totalThinkSeconds = questions.reduce(
    (acc, q) => acc + (q.thinkSeconds !== undefined && q.thinkSeconds !== null ? q.thinkSeconds : 20),
    0
  );
  const totalAnswerSeconds = questions.reduce(
    (acc, q) => acc + (q.answerSeconds !== undefined && q.answerSeconds !== null ? q.answerSeconds : 90),
    0
  );
  const totalDurationSeconds = totalThinkSeconds + totalAnswerSeconds;

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
      {/* Overview Stat & Total Duration Card */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
          gap: '1rem',
          padding: '1rem 1.25rem',
          backgroundColor: 'var(--slate-50)',
          borderRadius: 'var(--radius-md)',
          border: '1px solid var(--border-color)',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          <div
            style={{
              width: '38px',
              height: '38px',
              borderRadius: 'var(--radius-full)',
              backgroundColor: 'var(--primary-100)',
              color: 'var(--primary-700)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              flexShrink: 0,
            }}
          >
            <Timer size={20} />
          </div>
          <div>
            <div style={{ fontSize: '0.75rem', color: 'var(--slate-500)', fontWeight: 500 }}>
              Tổng thời lượng ước tính
            </div>
            <div style={{ fontSize: '1.1rem', fontWeight: 700, color: 'var(--slate-900)' }}>
              {formatDuration(totalDurationSeconds)}
            </div>
          </div>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          <div
            style={{
              width: '38px',
              height: '38px',
              borderRadius: 'var(--radius-full)',
              backgroundColor: '#ecfdf5',
              color: '#059669',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              flexShrink: 0,
            }}
          >
            <Clock size={20} />
          </div>
          <div>
            <div style={{ fontSize: '0.75rem', color: 'var(--slate-500)', fontWeight: 500 }}>
              Thời gian suy nghĩ / trả lời
            </div>
            <div style={{ fontSize: '0.9rem', fontWeight: 600, color: 'var(--slate-800)' }}>
              {totalThinkSeconds}s suy nghĩ • {totalAnswerSeconds}s ghi âm
            </div>
          </div>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          <div
            style={{
              width: '38px',
              height: '38px',
              borderRadius: 'var(--radius-full)',
              backgroundColor: '#f5f3ff',
              color: '#7c3aed',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              flexShrink: 0,
            }}
          >
            <Sparkles size={20} />
          </div>
          <div>
            <div style={{ fontSize: '0.75rem', color: 'var(--slate-500)', fontWeight: 500 }}>
              Số lượng câu hỏi
            </div>
            <div style={{ fontSize: '1.1rem', fontWeight: 700, color: 'var(--slate-900)' }}>
              {questions.length} câu hỏi tình huống
            </div>
          </div>
        </div>
      </div>

      {/* Disclaimer / Snapshot Semantics Alert Banner */}
      <div
        style={{
          display: 'flex',
          alignItems: 'flex-start',
          gap: '0.75rem',
          padding: '0.75rem 1rem',
          borderRadius: 'var(--radius-md)',
          backgroundColor: '#eff6ff',
          border: '1px solid #bfdbfe',
          color: '#1e40af',
          fontSize: '0.825rem',
          lineHeight: 1.45,
        }}
      >
        <Info size={18} style={{ flexShrink: 0, marginTop: '0.1rem', color: '#2563eb' }} />
        <div>
          <strong>Quy tắc Snapshot:</strong> Thời gian suy nghĩ và ghi âm được cấu hình tại đây sẽ áp dụng cho tất cả các lời mời phỏng vấn AI được tạo sau thời điểm này. Các buổi phỏng vấn đã phát link trước đó vẫn giữ nguyên thời lượng ban đầu.
        </div>
      </div>

      {/* Danh sách câu hỏi */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: '0.85rem' }}>
        {questions.map((item, idx) => {
          const isExpanded = expandedIndex === idx;
          const catStyle = CATEGORY_COLORS[item.category] || CATEGORY_COLORS.Technical;
          const rubric = item.scoringRubric;
          const thinkSec = item.thinkSeconds !== undefined && item.thinkSeconds !== null ? item.thinkSeconds : 20;
          const answerSec = item.answerSeconds !== undefined && item.answerSeconds !== null ? item.answerSeconds : 90;

          return (
            <div
              key={item.id || idx}
              style={{
                border: '1px solid var(--border-color)',
                borderRadius: 'var(--radius-md)',
                backgroundColor: 'var(--bg-card)',
                overflow: 'hidden',
                boxShadow: isExpanded ? 'var(--shadow-sm)' : 'none',
                transition: 'all 0.15s ease',
              }}
            >
              {/* Question Header */}
              <div
                style={{
                  padding: '1rem 1.25rem',
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'flex-start',
                  gap: '1rem',
                  backgroundColor: isExpanded ? 'var(--slate-50)' : 'transparent',
                }}
              >
                <div
                  onClick={() => toggleExpand(idx)}
                  style={{
                    display: 'flex',
                    gap: '0.75rem',
                    alignItems: 'flex-start',
                    flex: 1,
                    cursor: 'pointer',
                  }}
                >
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
                      flexShrink: 0,
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

                    {/* Metadata Chips: Category, Difficulty, Think Time, Answer Time */}
                    <div style={{ display: 'flex', gap: '0.45rem', marginTop: '0.5rem', flexWrap: 'wrap', alignItems: 'center' }}>
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

                      {/* Think Time Chip */}
                      <span
                        style={{
                          display: 'inline-flex',
                          alignItems: 'center',
                          gap: '0.25rem',
                          fontSize: '0.75rem',
                          fontWeight: 600,
                          padding: '0.15rem 0.55rem',
                          borderRadius: 'var(--radius-full)',
                          backgroundColor: '#ecfdf5',
                          color: '#065f46',
                          border: '1px solid #a7f3d0',
                        }}
                        title="Thời gian chuẩn bị suy nghĩ của ứng viên"
                      >
                        <Clock size={12} />
                        <span>Suy nghĩ: {thinkSec}s</span>
                      </span>

                      {/* Answer Time Chip */}
                      <span
                        style={{
                          display: 'inline-flex',
                          alignItems: 'center',
                          gap: '0.25rem',
                          fontSize: '0.75rem',
                          fontWeight: 600,
                          padding: '0.15rem 0.55rem',
                          borderRadius: 'var(--radius-full)',
                          backgroundColor: '#f5f3ff',
                          color: '#6d28d9',
                          border: '1px solid #ddd6fe',
                        }}
                        title="Thời gian ghi âm tối đa cho câu trả lời"
                      >
                        <Mic size={12} />
                        <span>Trả lời: {answerSec}s</span>
                      </span>
                    </div>
                  </div>
                </div>

                {/* Header Action Buttons */}
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', flexShrink: 0 }}>
                  {allowEdit && (
                    <Button
                      variant="secondary"
                      size="sm"
                      icon={Edit2}
                      onClick={() => setEditingQuestion(item)}
                      style={{ padding: '0.35rem 0.65rem', fontSize: '0.8rem' }}
                    >
                      Sửa
                    </Button>
                  )}

                  <button
                    type="button"
                    onClick={() => toggleExpand(idx)}
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
                      gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
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
                          fontSize: '0.8rem',
                          fontWeight: 700,
                          color: '#065f46',
                          marginBottom: '0.35rem',
                        }}
                      >
                        🌟 Xuất Sắc (9 - 10 điểm)
                      </div>
                      <p style={{ margin: 0, fontSize: '0.8rem', color: '#047857', lineHeight: 1.5 }}>
                        {rubric.excellent || 'Chưa thiết lập barem chi tiết'}
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
                          fontSize: '0.8rem',
                          fontWeight: 700,
                          color: '#1e40af',
                          marginBottom: '0.35rem',
                        }}
                      >
                        👍 Tốt (7 - 8 điểm)
                      </div>
                      <p style={{ margin: 0, fontSize: '0.8rem', color: '#1d4ed8', lineHeight: 1.5 }}>
                        {rubric.good || 'Chưa thiết lập barem chi tiết'}
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
                          fontSize: '0.8rem',
                          fontWeight: 700,
                          color: '#92400e',
                          marginBottom: '0.35rem',
                        }}
                      >
                        👌 Chấp Nhận (5 - 6 điểm)
                      </div>
                      <p style={{ margin: 0, fontSize: '0.8rem', color: '#b45309', lineHeight: 1.5 }}>
                        {rubric.acceptable || 'Chưa thiết lập barem chi tiết'}
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
                          fontSize: '0.8rem',
                          fontWeight: 700,
                          color: '#991b1b',
                          marginBottom: '0.35rem',
                        }}
                      >
                        ⚠️ Cần Cải Thiện (&lt; 5 điểm)
                      </div>
                      <p style={{ margin: 0, fontSize: '0.8rem', color: '#b91c1c', lineHeight: 1.5 }}>
                        {rubric.poor || 'Chưa thiết lập barem chi tiết'}
                      </p>
                    </div>
                  </div>
                </div>
              )}
            </div>
          );
        })}
      </div>

      {/* Edit Question Modal */}
      {editingQuestion && (
        <EditQuestionModal
          isOpen={!!editingQuestion}
          onClose={() => setEditingQuestion(null)}
          question={editingQuestion}
          jobId={jobId || editingQuestion.jobPostingId}
        />
      )}
    </div>
  );
}
