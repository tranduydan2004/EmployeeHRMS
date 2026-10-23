import { useState } from 'react';
import { Modal, Button, Badge } from '@/components';
import { useQuestionEvaluations } from '../hooks/useInterviews';
import { History, Sparkles, CheckCircle, Quote, ThumbsUp, AlertCircle, Bot, Calendar, User, Clock, Loader2 } from 'lucide-react';

export default function EvaluationAuditTrailModal({
  isOpen,
  onClose,
  interviewId,
  questionId,
  questionText,
}) {
  const { data: evaluations, isLoading, error } = useQuestionEvaluations(interviewId, questionId);
  const [selectedVersionId, setSelectedVersionId] = useState(null);

  const getLevelVariant = (level) => {
    switch (level?.toLowerCase()) {
      case 'excellent':
        return { variant: 'success', bg: 'var(--success-bg)', text: 'var(--success-text)', border: 'var(--success-border)' };
      case 'good':
        return { variant: 'info', bg: 'var(--info-bg)', text: 'var(--info-text)', border: 'var(--info-border)' };
      case 'acceptable':
        return { variant: 'warning', bg: 'var(--warning-bg)', text: 'var(--warning-text)', border: 'var(--warning-border)' };
      case 'poor':
        return { variant: 'danger', bg: 'var(--danger-bg)', text: 'var(--danger-text)', border: 'var(--danger-border)' };
      default:
        return { variant: 'secondary', bg: 'var(--slate-100)', text: 'var(--slate-700)', border: 'var(--slate-200)' };
    }
  };

  const evalList = evaluations || [];
  const activeEval = evalList.find((e) => e.id === selectedVersionId) || evalList[0];

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Lịch Sử Đánh Giá Barem Rubric (Audit Trail)"
      maxWidth="780px"
      footer={
        <div style={{ display: 'flex', justifyContent: 'flex-end', width: '100%' }}>
          <Button variant="secondary" size="sm" onClick={onClose}>
            Đóng
          </Button>
        </div>
      }
    >
      <div style={{ display: 'flex', flexDirection: 'column', gap: '0.85rem' }}>
        <div
          style={{
            padding: '0.65rem 0.85rem',
            backgroundColor: 'var(--primary-50)',
            borderRadius: 'var(--radius-sm)',
            border: '1px solid var(--primary-200)',
            fontSize: '0.8rem',
            color: 'var(--primary-900)',
          }}
        >
          <strong>Câu hỏi #{questionId}:</strong> {questionText}
        </div>

        {isLoading ? (
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '2.5rem', gap: '0.5rem', color: 'var(--text-muted)' }}>
            <Loader2 size={20} className="animate-spin" />
            <span>Đang tải lịch sử chấm điểm...</span>
          </div>
        ) : error ? (
          <div style={{ padding: '1rem', color: 'var(--danger-main)', fontSize: '0.85rem' }}>
            Không thể tải lịch sử đánh giá câu hỏi này.
          </div>
        ) : evalList.length === 0 ? (
          <div style={{ padding: '2rem', textAlign: 'center', color: 'var(--text-muted)', fontSize: '0.85rem' }}>
            Chưa có lượt đánh giá nào được ghi nhận cho câu hỏi này.
          </div>
        ) : (
          <div>
            {/* Version Selector Tabs */}
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '0.5rem',
                borderBottom: '1px solid var(--border-color)',
                paddingBottom: '0.5rem',
                marginBottom: '1rem',
                overflowX: 'auto',
              }}
            >
              <span style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--slate-500)', display: 'flex', alignItems: 'center', gap: '0.25rem' }}>
                <History size={14} />
                Phiên bản:
              </span>
              {evalList.map((ev) => {
                const isSelected = (activeEval && activeEval.id === ev.id) || (!activeEval && ev === evalList[0]);
                const badgeStyle = getLevelVariant(ev.level);

                return (
                  <button
                    key={ev.id}
                    type="button"
                    onClick={() => setSelectedVersionId(ev.id)}
                    style={{
                      padding: '0.35rem 0.65rem',
                      borderRadius: 'var(--radius-sm)',
                      fontSize: '0.75rem',
                      fontWeight: isSelected ? 600 : 500,
                      backgroundColor: isSelected ? 'var(--primary-600)' : 'var(--slate-100)',
                      color: isSelected ? '#ffffff' : 'var(--slate-700)',
                      border: isSelected ? '1px solid var(--primary-700)' : '1px solid var(--border-color)',
                      cursor: 'pointer',
                      display: 'flex',
                      alignItems: 'center',
                      gap: '0.35rem',
                      transition: 'all 0.15s ease',
                      flexShrink: 0,
                    }}
                  >
                    <span>Lần chấm #{ev.versionNumber}</span>
                    {ev.score !== null && (
                      <span
                        style={{
                          fontSize: '0.7rem',
                          padding: '0.05rem 0.35rem',
                          borderRadius: '4px',
                          backgroundColor: isSelected ? 'rgba(255,255,255,0.2)' : badgeStyle.bg,
                          color: isSelected ? '#ffffff' : badgeStyle.text,
                        }}
                      >
                        {ev.score}/10
                      </span>
                    )}
                    {ev.isActive && (
                      <span
                        style={{
                          width: '6px',
                          height: '6px',
                          borderRadius: '50%',
                          backgroundColor: isSelected ? '#a7f3d0' : 'var(--success-main)',
                        }}
                        title="Bản đánh giá hiện hành"
                      />
                    )}
                  </button>
                );
              })}
            </div>

            {/* Active Evaluation Details */}
            {activeEval && (
              <div
                style={{
                  display: 'flex',
                  flexDirection: 'column',
                  gap: '0.75rem',
                  backgroundColor: 'var(--slate-50)',
                  padding: '1rem',
                  borderRadius: 'var(--radius-md)',
                  border: '1px solid var(--border-color)',
                }}
              >
                {/* Header: Score, Level & Metadata */}
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: '0.5rem' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', flexWrap: 'wrap' }}>
                    <div
                      style={{
                        padding: '0.35rem 0.75rem',
                        borderRadius: 'var(--radius-full)',
                        fontSize: '0.85rem',
                        fontWeight: 700,
                        backgroundColor: getLevelVariant(activeEval.level).bg,
                        color: getLevelVariant(activeEval.level).text,
                        border: `1px solid ${getLevelVariant(activeEval.level).border}`,
                        display: 'flex',
                        alignItems: 'center',
                        gap: '0.35rem',
                      }}
                    >
                      <Sparkles size={14} />
                      <span>{activeEval.level || 'Chưa phân mức'}: {activeEval.score !== null ? `${activeEval.score}/10 điểm` : 'N/A'}</span>
                    </div>

                    {activeEval.isActive && (
                      <span
                        style={{
                          fontSize: '0.7rem',
                          fontWeight: 600,
                          padding: '0.2rem 0.5rem',
                          borderRadius: 'var(--radius-full)',
                          backgroundColor: 'var(--success-bg)',
                          color: 'var(--success-text)',
                          border: '1px solid var(--success-border)',
                        }}
                      >
                        ✓ Bản hiện hành
                      </span>
                    )}

                    <span
                      style={{
                        fontSize: '0.7rem',
                        color: 'var(--slate-500)',
                        backgroundColor: '#ffffff',
                        padding: '0.2rem 0.5rem',
                        borderRadius: '4px',
                        border: '1px solid var(--border-color)',
                      }}
                    >
                      Trạng thái: <strong>{activeEval.outcome}</strong>
                    </span>
                  </div>

                  <div style={{ fontSize: '0.7rem', color: 'var(--slate-500)', display: 'flex', alignItems: 'center', gap: '0.5rem', flexWrap: 'wrap' }}>
                    <span style={{ display: 'flex', alignItems: 'center', gap: '0.25rem' }}>
                      <Bot size={13} color="var(--primary-600)" />
                      {activeEval.modelName} ({activeEval.promptVersion})
                    </span>
                    <span style={{ display: 'flex', alignItems: 'center', gap: '0.25rem' }}>
                      <Calendar size={13} />
                      {new Date(activeEval.evaluatedAt).toLocaleString('vi-VN')}
                    </span>
                    {activeEval.evaluatedByUserId ? (
                      <span style={{ display: 'flex', alignItems: 'center', gap: '0.25rem' }}>
                        <User size={13} color="var(--info-main)" />
                        Bởi User #{activeEval.evaluatedByUserId}
                      </span>
                    ) : (
                      <span style={{ display: 'flex', alignItems: 'center', gap: '0.25rem' }}>
                        <Clock size={13} />
                        Tự động (STT pipeline)
                      </span>
                    )}
                  </div>
                </div>

                {/* AI Analysis Reasoning */}
                {activeEval.analysis && (
                  <div
                    style={{
                      padding: '0.65rem 0.85rem',
                      backgroundColor: '#ffffff',
                      borderRadius: 'var(--radius-sm)',
                      border: '1px solid var(--slate-200)',
                      fontSize: '0.8rem',
                      color: 'var(--slate-800)',
                      lineHeight: 1.5,
                    }}
                  >
                    <div style={{ fontWeight: 600, color: 'var(--primary-700)', marginBottom: '0.25rem' }}>
                      Nhận xét phân tích barem:
                    </div>
                    {activeEval.analysis}
                  </div>
                )}

                {/* Evidence Quotes */}
                {activeEval.evidenceQuotes && activeEval.evidenceQuotes.length > 0 && (
                  <div
                    style={{
                      padding: '0.65rem 0.85rem',
                      backgroundColor: '#ffffff',
                      borderRadius: 'var(--radius-sm)',
                      border: '1px solid var(--slate-200)',
                    }}
                  >
                    <div style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--slate-700)', display: 'flex', alignItems: 'center', gap: '0.3rem', marginBottom: '0.35rem' }}>
                      <Quote size={13} color="var(--primary-600)" />
                      Bằng chứng trích dẫn trực tiếp từ câu trả lời:
                    </div>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '0.35rem' }}>
                      {activeEval.evidenceQuotes.map((quote, qIdx) => (
                        <div
                          key={qIdx}
                          style={{
                            padding: '0.35rem 0.65rem',
                            backgroundColor: 'var(--primary-50)',
                            borderRadius: '4px',
                            borderLeft: '3px solid var(--primary-500)',
                            fontSize: '0.75rem',
                            color: 'var(--primary-900)',
                            fontStyle: 'italic',
                          }}
                        >
                          "{quote}"
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {/* Strengths & Concerns */}
                {((activeEval.strengths && activeEval.strengths.length > 0) ||
                  (activeEval.concerns && activeEval.concerns.length > 0)) && (
                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '0.5rem' }}>
                    {activeEval.strengths && activeEval.strengths.length > 0 && (
                      <div
                        style={{
                          padding: '0.5rem 0.75rem',
                          backgroundColor: 'var(--success-bg)',
                          borderRadius: 'var(--radius-sm)',
                          border: '1px solid var(--success-border)',
                          fontSize: '0.75rem',
                        }}
                      >
                        <div style={{ fontWeight: 600, color: 'var(--success-text)', display: 'flex', alignItems: 'center', gap: '0.25rem', marginBottom: '0.25rem' }}>
                          <ThumbsUp size={12} />
                          Điểm mạnh:
                        </div>
                        <ul style={{ margin: 0, paddingLeft: '1.2rem', color: 'var(--success-text)' }}>
                          {activeEval.strengths.map((s, idx) => (
                            <li key={idx}>{s}</li>
                          ))}
                        </ul>
                      </div>
                    )}

                    {activeEval.concerns && activeEval.concerns.length > 0 && (
                      <div
                        style={{
                          padding: '0.5rem 0.75rem',
                          backgroundColor: 'var(--warning-bg)',
                          borderRadius: 'var(--radius-sm)',
                          border: '1px solid var(--warning-border)',
                          fontSize: '0.75rem',
                        }}
                      >
                        <div style={{ fontWeight: 600, color: 'var(--warning-text)', display: 'flex', alignItems: 'center', gap: '0.25rem', marginBottom: '0.25rem' }}>
                          <AlertCircle size={12} />
                          Điểm cần lưu ý:
                        </div>
                        <ul style={{ margin: 0, paddingLeft: '1.2rem', color: 'var(--warning-text)' }}>
                          {activeEval.concerns.map((c, idx) => (
                            <li key={idx}>{c}</li>
                          ))}
                        </ul>
                      </div>
                    )}
                  </div>
                )}

                {/* Transcript Snapshot for this version */}
                {activeEval.transcriptSnapshot && (
                  <details style={{ fontSize: '0.75rem', color: 'var(--slate-600)', marginTop: '0.25rem' }}>
                    <summary style={{ cursor: 'pointer', fontWeight: 600, color: 'var(--slate-500)' }}>
                      Xem bản snapshot transcript đã dùng để chấm ở lần này
                    </summary>
                    <div
                      style={{
                        marginTop: '0.35rem',
                        padding: '0.5rem 0.75rem',
                        backgroundColor: '#ffffff',
                        borderRadius: 'var(--radius-sm)',
                        border: '1px solid var(--slate-200)',
                        fontStyle: 'italic',
                        maxHeight: '120px',
                        overflowY: 'auto',
                        whiteSpace: 'pre-wrap',
                      }}
                    >
                      {activeEval.transcriptSnapshot}
                    </div>
                  </details>
                )}
              </div>
            )}
          </div>
        )}
      </div>
    </Modal>
  );
}
