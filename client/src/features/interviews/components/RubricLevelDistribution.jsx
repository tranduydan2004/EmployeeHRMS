import { useMemo } from 'react';
import { Badge } from '@/components';
import { CheckCircle2, Sparkles, HelpCircle, AlertCircle, XCircle } from 'lucide-react';

/**
 * Trực quan hóa đánh giá Rubric trung thực theo thang đo thứ bậc (Ordinal Data)
 * Thay thế cho Radar Chart: Dùng thanh ngang 4 nấc rời rạc (Segmented Horizontal Bar)
 * và Bảng phân bố mức (Level Distribution Summary).
 */
export default function RubricLevelDistribution({ questions = [] }) {
  const levels = [
    { key: 'poor', label: 'Chưa đạt (Poor)', shortLabel: 'Chưa đạt', bg: 'var(--danger-bg)', activeBg: 'var(--danger-main)', text: 'var(--danger-text)', border: 'var(--danger-border)' },
    { key: 'acceptable', label: 'Đạt (Acceptable)', shortLabel: 'Đạt', bg: 'var(--warning-bg)', activeBg: 'var(--warning-main)', text: 'var(--warning-text)', border: 'var(--warning-border)' },
    { key: 'good', label: 'Tốt (Good)', shortLabel: 'Tốt', bg: 'var(--info-bg)', activeBg: 'var(--primary-600)', text: 'var(--info-text)', border: 'var(--info-border)' },
    { key: 'excellent', label: 'Xuất sắc (Excellent)', shortLabel: 'Xuất sắc', bg: 'var(--success-bg)', activeBg: 'var(--success-main)', text: 'var(--success-text)', border: 'var(--success-border)' },
  ];

  // Thống kê phân bố các mức
  const distribution = useMemo(() => {
    const counts = { excellent: 0, good: 0, acceptable: 0, poor: 0, unrated: 0 };
    questions.forEach((q) => {
      const activeEval = q.answer?.evaluations?.find((e) => e.isActive) || q.answer?.evaluations?.[0];
      const levelKey = activeEval?.level?.toLowerCase();
      if (levelKey && counts[levelKey] !== undefined) {
        counts[levelKey]++;
      } else {
        counts.unrated++;
      }
    });
    return counts;
  }, [questions]);

  return (
    <div
      style={{
        display: 'flex',
        flexDirection: 'column',
        gap: '1rem',
        padding: '1.25rem',
        backgroundColor: '#ffffff',
        borderRadius: 'var(--radius-md)',
        border: '1px solid var(--border-color)',
        boxShadow: 'var(--shadow-sm)',
      }}
    >
      {/* Header: Title + Distribution Summary */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '0.75rem' }}>
        <div>
          <h4 style={{ margin: 0, fontSize: '1rem', fontWeight: 600, color: 'var(--slate-900)', display: 'flex', alignItems: 'center', gap: '0.45rem' }}>
            <Sparkles size={16} color="var(--primary-600)" />
            <span>Phân Bố Mức Đánh Giá Rubric (Segmented Visualizer)</span>
          </h4>
          <span style={{ fontSize: '0.775rem', color: 'var(--slate-500)' }}>
            Trực quan hóa rời rạc theo 4 nấc chuẩn, nhằm bảo toàn tính trung thực của dữ liệu định tính.
          </span>
        </div>

        {/* Level Distribution Summary Chips */}
        <div style={{ display: 'flex', gap: '0.4rem', flexWrap: 'wrap' }}>
          <span
            style={{
              padding: '0.2rem 0.55rem',
              borderRadius: 'var(--radius-full)',
              fontSize: '0.75rem',
              fontWeight: 700,
              backgroundColor: 'var(--success-bg)',
              color: 'var(--success-text)',
              border: '1px solid var(--success-border)',
            }}
          >
            Xuất sắc: {distribution.excellent}
          </span>
          <span
            style={{
              padding: '0.2rem 0.55rem',
              borderRadius: 'var(--radius-full)',
              fontSize: '0.75rem',
              fontWeight: 700,
              backgroundColor: 'var(--info-bg)',
              color: 'var(--info-text)',
              border: '1px solid var(--info-border)',
            }}
          >
            Tốt: {distribution.good}
          </span>
          <span
            style={{
              padding: '0.2rem 0.55rem',
              borderRadius: 'var(--radius-full)',
              fontSize: '0.75rem',
              fontWeight: 700,
              backgroundColor: 'var(--warning-bg)',
              color: 'var(--warning-text)',
              border: '1px solid var(--warning-border)',
            }}
          >
            Đạt: {distribution.acceptable}
          </span>
          <span
            style={{
              padding: '0.2rem 0.55rem',
              borderRadius: 'var(--radius-full)',
              fontSize: '0.75rem',
              fontWeight: 700,
              backgroundColor: 'var(--danger-bg)',
              color: 'var(--danger-text)',
              border: '1px solid var(--danger-border)',
            }}
          >
            Chưa đạt: {distribution.poor}
          </span>
        </div>
      </div>

      {/* Segmented Horizontal Bars Per Question */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem', marginTop: '0.25rem' }}>
        {questions.map((q, idx) => {
          const activeEval = q.answer?.evaluations?.find((e) => e.isActive) || q.answer?.evaluations?.[0];
          const activeLevelKey = activeEval?.level?.toLowerCase();

          return (
            <div
              key={q.id || idx}
              style={{
                display: 'grid',
                gridTemplateColumns: 'minmax(220px, 1.2fr) minmax(280px, 2fr) auto',
                gap: '1rem',
                alignItems: 'center',
                padding: '0.65rem 0.85rem',
                backgroundColor: 'var(--slate-50)',
                borderRadius: 'var(--radius-sm)',
                border: '1px solid var(--border-color)',
              }}
            >
              {/* Left Column: Question title & Category */}
              <div style={{ overflow: 'hidden' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
                  <span style={{ fontSize: '0.85rem', fontWeight: 600, color: 'var(--slate-800)', whiteSpace: 'nowrap' }}>
                    Câu {idx + 1}:
                  </span>
                  <span
                    style={{
                      fontSize: '0.825rem',
                      color: 'var(--slate-700)',
                      whiteSpace: 'nowrap',
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                    }}
                    title={q.question}
                  >
                    {q.question}
                  </span>
                </div>
                {q.category && (
                  <span style={{ fontSize: '0.725rem', color: 'var(--primary-700)', fontWeight: 500 }}>
                    {q.category}
                  </span>
                )}
              </div>

              {/* Center Column: 4-Segmented Bar */}
              <div
                style={{
                  display: 'grid',
                  gridTemplateColumns: 'repeat(4, 1fr)',
                  gap: '4px',
                  backgroundColor: 'var(--slate-200)',
                  padding: '3px',
                  borderRadius: 'var(--radius-sm)',
                }}
              >
                {levels.map((lvl) => {
                  const isCurrent = activeLevelKey === lvl.key;
                  return (
                    <div
                      key={lvl.key}
                      style={{
                        padding: '0.35rem 0.2rem',
                        textAlign: 'center',
                        fontSize: '0.725rem',
                        borderRadius: '3px',
                        transition: 'all 0.15s ease',
                        backgroundColor: isCurrent ? lvl.activeBg : '#ffffff',
                        color: isCurrent ? '#ffffff' : 'var(--slate-600)',
                        fontWeight: isCurrent ? 700 : 500,
                        border: isCurrent ? `1px solid ${lvl.activeBg}` : '1px solid transparent',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        gap: '0.2rem',
                        boxShadow: isCurrent ? '0 1px 3px rgba(0,0,0,0.15)' : 'none',
                      }}
                      title={`${lvl.label} ${isCurrent ? '(Mức đạt được)' : ''}`}
                    >
                      {isCurrent && <CheckCircle2 size={11} />}
                      <span>{lvl.shortLabel}</span>
                    </div>
                  );
                })}
              </div>

              {/* Right Column: Score */}
              <div style={{ textAlign: 'right', minWidth: '70px' }}>
                {activeEval?.score !== null && activeEval?.score !== undefined ? (
                  <span
                    style={{
                      fontSize: '0.85rem',
                      fontWeight: 700,
                      color: 'var(--slate-900)',
                      backgroundColor: '#ffffff',
                      padding: '0.2rem 0.5rem',
                      borderRadius: 'var(--radius-sm)',
                      border: '1px solid var(--border-color)',
                      whiteSpace: 'nowrap',
                    }}
                  >
                    {activeEval.score}/10
                  </span>
                ) : (
                  <span style={{ fontSize: '0.75rem', color: 'var(--slate-400)', fontStyle: 'italic', whiteSpace: 'nowrap' }}>
                    Chưa chấm
                  </span>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
