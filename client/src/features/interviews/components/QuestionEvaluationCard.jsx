import { useState } from 'react';
import { useInterviewMutations } from '../hooks/useInterviews';
import { useAuthStore } from '@/features/auth';
import { Button, Input, Badge } from '@/components';
import {
  Sparkles,
  Plus,
  Edit3,
  MessageSquare,
  CheckCircle,
  Lock,
  RefreshCw,
  History,
  BookOpen,
  ChevronDown,
  ChevronUp,
  FileText,
  AlertCircle,
  Quote,
  Loader2,
  Tag,
} from 'lucide-react';
import CompleteInterviewModal from './CompleteInterviewModal';
import AudioAnswerPlayer from './AudioAnswerPlayer';
import SpeechMetricsCard from './SpeechMetricsCard';
import TranscriptEditorModal from './TranscriptEditorModal';
import EvaluationAuditTrailModal from './EvaluationAuditTrailModal';

export default function QuestionEvaluationCard({ interview }) {
  const {
    addQuestion,
    isAddingQuestion,
    updateQuestionAnswer,
    isUpdatingAnswer,
    completeInterview,
    isCompleting,
    updateTranscript,
    isUpdatingTranscript,
    reevaluateAnswer,
    isReevaluating,
  } = useInterviewMutations();

  const role = useAuthStore((state) => state.role);
  const userId = useAuthStore((state) => state.userId);

  const isCompleted = interview.status === 'Completed';
  const isCancelled = interview.status === 'Cancelled';

  // Ownership check: Admin, HR, or assigned Interviewer
  const canEdit =
    (role === 'Admin' || role === 'HR' || (role === 'Interviewer' && interview.interviewerId === userId)) &&
    !isCompleted &&
    !isCancelled;

  const canComplete = canEdit;

  const [showAddForm, setShowAddForm] = useState(false);
  const [questionText, setQuestionText] = useState('');
  const [isCompleteModalOpen, setIsCompleteModalOpen] = useState(false);

  // Legacy CandidateAnswer state
  const [editingQuestionId, setEditingQuestionId] = useState(null);
  const [answerText, setAnswerText] = useState('');

  // Phase 4: Modal states
  const [transcriptModalQuestion, setTranscriptModalQuestion] = useState(null);
  const [auditTrailModalQuestion, setAuditTrailModalQuestion] = useState(null);
  const [expandedRubricQuestionId, setExpandedRubricQuestionId] = useState(null);
  const [reevaluatingQuestionId, setReevaluatingQuestionId] = useState(null);

  const handleAddQuestion = async (e) => {
    e.preventDefault();
    if (!questionText.trim()) return;

    await addQuestion({
      interviewId: interview.id,
      payload: {
        question: questionText.trim(),
        orderIndex: interview.questions?.length || 0,
      },
    });
    setQuestionText('');
    setShowAddForm(false);
  };

  const handleStartEditAnswer = (q) => {
    setEditingQuestionId(q.id);
    setAnswerText(q.candidateAnswer || '');
  };

  const handleCancelEditAnswer = () => {
    setEditingQuestionId(null);
    setAnswerText('');
  };

  const handleSaveAnswer = async (questionId) => {
    if (!answerText.trim()) return;

    await updateQuestionAnswer({
      interviewId: interview.id,
      questionId,
      candidateAnswer: answerText.trim(),
    });
    setEditingQuestionId(null);
    setAnswerText('');
  };

  const handleConfirmComplete = async (summary) => {
    await completeInterview({
      id: interview.id,
      summary,
    });
  };

  const handleReevaluate = async (questionId) => {
    try {
      setReevaluatingQuestionId(questionId);
      await reevaluateAnswer({
        interviewId: interview.id,
        questionId,
      });
    } finally {
      setReevaluatingQuestionId(null);
    }
  };

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

  const getStatusBadge = (status) => {
    switch (status) {
      case 'Completed':
        return <Badge variant="success">Hoàn thành</Badge>;
      case 'Scoring':
        return (
          <Badge variant="warning">
            <Loader2 size={12} className="animate-spin" style={{ marginRight: '4px' }} />
            Đang chấm AI...
          </Badge>
        );
      case 'Transcribing':
        return (
          <Badge variant="info">
            <Loader2 size={12} className="animate-spin" style={{ marginRight: '4px' }} />
            Đang bóc băng...
          </Badge>
        );
      case 'Pending':
        return <Badge variant="secondary">Chờ xử lý</Badge>;
      case 'Failed':
        return <Badge variant="danger">Lỗi xử lý</Badge>;
      default:
        return null;
    }
  };

  return (
    <div style={{ marginTop: '1rem', borderTop: '1px solid var(--border-color)', paddingTop: '1rem' }}>
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: '0.75rem',
          flexWrap: 'wrap',
          gap: '0.5rem',
        }}
      >
        <h4
          style={{
            fontSize: '0.95rem',
            fontWeight: 600,
            color: 'var(--text-main)',
            display: 'flex',
            alignItems: 'center',
            gap: '0.4rem',
            margin: 0,
          }}
        >
          <MessageSquare size={16} color="var(--primary-600)" />
          Câu Hỏi & Đánh Giá Phỏng Vấn ({interview.questions?.length || 0})
        </h4>

        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', flexWrap: 'wrap' }}>
          {canEdit && (
            <Button
              variant="outline"
              size="sm"
              icon={Plus}
              onClick={() => setShowAddForm((prev) => !prev)}
            >
              {showAddForm ? 'Đóng form' : 'Thêm câu hỏi'}
            </Button>
          )}

          {canComplete && (
            <Button
              variant="warning"
              size="sm"
              icon={CheckCircle}
              onClick={() => setIsCompleteModalOpen(true)}
            >
              Kết thúc phỏng vấn
            </Button>
          )}

          {isCompleted && (
            <div
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: '0.35rem',
                padding: '0.3rem 0.65rem',
                borderRadius: 'var(--radius-full)',
                fontSize: '0.75rem',
                fontWeight: 600,
                backgroundColor: 'var(--success-bg)',
                color: 'var(--success-text)',
                border: '1px solid var(--success-border)',
              }}
            >
              <Lock size={12} />
              Đã hoàn tất (Chỉ đọc)
            </div>
          )}
        </div>
      </div>

      {showAddForm && canEdit && (
        <form
          onSubmit={handleAddQuestion}
          style={{
            marginBottom: '1rem',
            padding: '1rem',
            backgroundColor: 'var(--slate-50)',
            borderRadius: 'var(--radius-md)',
            border: '1px solid var(--border-color)',
          }}
        >
          <Input
            label="Nội dung câu hỏi phỏng vấn"
            placeholder="Ví dụ: Bạn có kinh nghiệm giải quyết N+1 query problem trong EF Core như thế nào?"
            value={questionText}
            onChange={(e) => setQuestionText(e.target.value)}
            required
          />
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.5rem', marginTop: '0.5rem' }}>
            <Button variant="secondary" size="sm" onClick={() => setShowAddForm(false)}>
              Hủy
            </Button>
            <Button variant="primary" size="sm" type="submit" isLoading={isAddingQuestion}>
              Lưu câu hỏi
            </Button>
          </div>
        </form>
      )}

      {interview.questions && interview.questions.length > 0 ? (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.85rem' }}>
          {interview.questions.map((q, idx) => {
            const isEditingThis = editingQuestionId === q.id;
            const audioAnswer = q.answer;
            const evaluations = audioAnswer?.evaluations || [];
            const activeEvaluation = evaluations.find((e) => e.isActive) || evaluations[0];
            const isRubricExpanded = expandedRubricQuestionId === q.id;
            const isAnswerProcessing =
              audioAnswer?.status === 'Scoring' ||
              audioAnswer?.status === 'Transcribing' ||
              reevaluatingQuestionId === q.id;

            return (
              <div
                key={q.id || idx}
                style={{
                  padding: '1rem',
                  backgroundColor: 'var(--slate-50)',
                  borderRadius: 'var(--radius-md)',
                  border: '1px solid var(--border-color)',
                  display: 'flex',
                  flexDirection: 'column',
                  gap: '0.65rem',
                }}
              >
                {/* Header: Question Text, Category, Rubric Toggle */}
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '1rem', flexWrap: 'wrap' }}>
                  <div style={{ flex: 1 }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', flexWrap: 'wrap' }}>
                      <span style={{ fontSize: '0.9rem', fontWeight: 600, color: 'var(--text-main)' }}>
                        <strong>Câu {idx + 1}:</strong> {q.question}
                      </span>
                      {q.category && (
                        <Badge variant="secondary" icon={Tag}>
                          {q.category}
                        </Badge>
                      )}
                    </div>
                  </div>

                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', flexWrap: 'wrap' }}>
                    {/* Rubric Details Toggle */}
                    {q.scoringRubric && (
                      <button
                        type="button"
                        onClick={() => setExpandedRubricQuestionId(isRubricExpanded ? null : q.id)}
                        style={{
                          display: 'inline-flex',
                          alignItems: 'center',
                          gap: '0.25rem',
                          background: 'none',
                          border: 'none',
                          color: 'var(--primary-600)',
                          fontSize: '0.75rem',
                          cursor: 'pointer',
                          fontWeight: 500,
                          padding: '0.2rem 0.4rem',
                        }}
                      >
                        <BookOpen size={13} />
                        <span>Barem Rubric</span>
                        {isRubricExpanded ? <ChevronUp size={13} /> : <ChevronDown size={13} />}
                      </button>
                    )}

                    {/* Audio Answer Status Badge */}
                    {audioAnswer && getStatusBadge(audioAnswer.status)}

                    {/* Active Rubric Score Badge (Phase 4) */}
                    {activeEvaluation && activeEvaluation.score !== null ? (
                      <div
                        style={{
                          display: 'inline-flex',
                          alignItems: 'center',
                          gap: '0.35rem',
                          padding: '0.25rem 0.6rem',
                          borderRadius: 'var(--radius-full)',
                          fontSize: '0.75rem',
                          fontWeight: 700,
                          backgroundColor: getLevelVariant(activeEvaluation.level).bg,
                          color: getLevelVariant(activeEvaluation.level).text,
                          border: `1px solid ${getLevelVariant(activeEvaluation.level).border}`,
                        }}
                      >
                        <Sparkles size={12} />
                        <span>{activeEvaluation.level}: {activeEvaluation.score}/10</span>
                      </div>
                    ) : (
                      q.aiQuestionScore !== null && q.aiQuestionScore !== undefined && (
                        <Badge variant="success" icon={Sparkles}>
                          {q.aiQuestionScore}/10
                        </Badge>
                      )
                    )}

                    {/* Legacy Answer Edit Button */}
                    {!audioAnswer && canEdit && !isEditingThis && (
                      <Button
                        variant="secondary"
                        size="sm"
                        icon={Edit3}
                        onClick={() => handleStartEditAnswer(q)}
                        title={q.candidateAnswer ? 'Sửa câu trả lời' : 'Nhập câu trả lời'}
                      >
                        {q.candidateAnswer ? 'Sửa' : 'Nhập trả lời'}
                      </Button>
                    )}
                  </div>
                </div>

                {/* Expandable Scoring Rubric Preview */}
                {isRubricExpanded && q.scoringRubric && (
                  <div
                    style={{
                      padding: '0.65rem 0.85rem',
                      backgroundColor: '#ffffff',
                      borderRadius: 'var(--radius-sm)',
                      border: '1px solid var(--primary-200)',
                      fontSize: '0.75rem',
                      display: 'grid',
                      gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
                      gap: '0.5rem',
                    }}
                  >
                    <div style={{ padding: '0.4rem', backgroundColor: 'var(--success-bg)', borderRadius: '4px' }}>
                      <strong style={{ color: 'var(--success-text)' }}>Xuất sắc (10đ):</strong>
                      <div style={{ color: 'var(--slate-700)', marginTop: '2px' }}>{q.scoringRubric.excellent}</div>
                    </div>
                    <div style={{ padding: '0.4rem', backgroundColor: 'var(--info-bg)', borderRadius: '4px' }}>
                      <strong style={{ color: 'var(--info-text)' }}>Tốt (7.5đ):</strong>
                      <div style={{ color: 'var(--slate-700)', marginTop: '2px' }}>{q.scoringRubric.good}</div>
                    </div>
                    <div style={{ padding: '0.4rem', backgroundColor: 'var(--warning-bg)', borderRadius: '4px' }}>
                      <strong style={{ color: 'var(--warning-text)' }}>Đạt (5.0đ):</strong>
                      <div style={{ color: 'var(--slate-700)', marginTop: '2px' }}>{q.scoringRubric.acceptable}</div>
                    </div>
                    <div style={{ padding: '0.4rem', backgroundColor: 'var(--danger-bg)', borderRadius: '4px' }}>
                      <strong style={{ color: 'var(--danger-text)' }}>Chưa đạt (2.5đ):</strong>
                      <div style={{ color: 'var(--slate-700)', marginTop: '2px' }}>{q.scoringRubric.poor}</div>
                    </div>
                  </div>
                )}

                {/* Phase 4 Audio Answer Section */}
                {audioAnswer ? (
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                    {/* Audio Player */}
                    {audioAnswer.audioStorageKey && (
                      <AudioAnswerPlayer
                        storageKey={audioAnswer.audioStorageKey}
                        durationSeconds={audioAnswer.audioDurationSeconds || 0}
                      />
                    )}

                    {/* Effective Transcript Box */}
                    <div
                      style={{
                        padding: '0.65rem 0.85rem',
                        backgroundColor: '#ffffff',
                        borderRadius: 'var(--radius-sm)',
                        border: '1px solid var(--border-color)',
                        fontSize: '0.85rem',
                      }}
                    >
                      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '0.35rem' }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', fontWeight: 600, color: 'var(--primary-700)', fontSize: '0.8rem' }}>
                          <FileText size={14} />
                          <span>Bản bóc băng câu trả lời:</span>
                          {audioAnswer.editedTranscript && (
                            <span
                              style={{
                                fontSize: '0.7rem',
                                fontWeight: 500,
                                padding: '0.1rem 0.4rem',
                                borderRadius: '4px',
                                backgroundColor: 'var(--primary-100)',
                                color: 'var(--primary-800)',
                              }}
                            >
                              Đã chỉnh sửa bởi HR
                            </span>
                          )}
                        </div>

                        {canEdit && (
                          <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
                            <Button
                              variant="secondary"
                              size="sm"
                              icon={Edit3}
                              onClick={() => setTranscriptModalQuestion(q)}
                              title="Sửa bản bóc băng trước khi chấm lại"
                            >
                              Sửa Transcript
                            </Button>

                            <Button
                              variant="primary"
                              size="sm"
                              icon={RefreshCw}
                              onClick={() => handleReevaluate(q.id)}
                              disabled={isAnswerProcessing}
                              isLoading={reevaluatingQuestionId === q.id}
                              title={
                                isAnswerProcessing
                                  ? 'Câu trả lời đang trong tiến trình xử lý'
                                  : 'Kích hoạt AI chấm lại với barem và transcript hiện hành'
                              }
                            >
                              Re-evaluate
                            </Button>
                          </div>
                        )}
                      </div>

                      <div style={{ color: 'var(--text-main)', lineHeight: 1.5, fontStyle: audioAnswer.effectiveTranscript ? 'normal' : 'italic' }}>
                        {audioAnswer.effectiveTranscript || audioAnswer.transcript || (
                          <span style={{ color: 'var(--text-muted)' }}>Chưa có nội dung bóc băng.</span>
                        )}
                      </div>
                    </div>

                    {/* Speech Metrics Telemetry */}
                    {audioAnswer.speechMetrics && <SpeechMetricsCard metrics={audioAnswer.speechMetrics} />}

                    {/* Active AI Evaluation Details */}
                    {activeEvaluation && (
                      <div
                        style={{
                          padding: '0.75rem 1rem',
                          backgroundColor: 'var(--primary-50)',
                          borderRadius: 'var(--radius-sm)',
                          border: '1px solid var(--primary-200)',
                          fontSize: '0.8rem',
                          display: 'flex',
                          flexDirection: 'column',
                          gap: '0.4rem',
                        }}
                      >
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '0.4rem' }}>
                          <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', fontWeight: 600, color: 'var(--primary-900)' }}>
                            <Sparkles size={14} color="var(--primary-600)" />
                            <span>Đánh giá AI (Version #{activeEvaluation.versionNumber}):</span>
                            <span
                              style={{
                                padding: '0.15rem 0.45rem',
                                borderRadius: '4px',
                                backgroundColor: getLevelVariant(activeEvaluation.level).bg,
                                color: getLevelVariant(activeEvaluation.level).text,
                                fontWeight: 700,
                              }}
                            >
                              {activeEvaluation.level} ({activeEvaluation.score}/10)
                            </span>
                          </div>

                          {evaluations.length > 0 && (
                            <button
                              type="button"
                              onClick={() => setAuditTrailModalQuestion(q)}
                              style={{
                                display: 'inline-flex',
                                alignItems: 'center',
                                gap: '0.25rem',
                                background: 'none',
                                border: 'none',
                                color: 'var(--primary-700)',
                                fontSize: '0.75rem',
                                cursor: 'pointer',
                                fontWeight: 600,
                                textDecoration: 'underline',
                              }}
                            >
                              <History size={13} />
                              Lịch sử ({evaluations.length} lần chấm)
                            </button>
                          )}
                        </div>

                        {activeEvaluation.analysis && (
                          <div style={{ color: 'var(--slate-800)', lineHeight: 1.5 }}>
                            {activeEvaluation.analysis}
                          </div>
                        )}

                        {activeEvaluation.evidenceQuotes && activeEvaluation.evidenceQuotes.length > 0 && (
                          <div style={{ marginTop: '0.25rem' }}>
                            <div style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--primary-900)', display: 'flex', alignItems: 'center', gap: '0.25rem', marginBottom: '0.2rem' }}>
                              <Quote size={12} />
                              Trích dẫn từ câu trả lời:
                            </div>
                            <div style={{ display: 'flex', flexDirection: 'column', gap: '0.25rem' }}>
                              {activeEvaluation.evidenceQuotes.map((quote, qIdx) => (
                                <div
                                  key={qIdx}
                                  style={{
                                    padding: '0.25rem 0.5rem',
                                    backgroundColor: '#ffffff',
                                    borderRadius: '4px',
                                    borderLeft: '3px solid var(--primary-500)',
                                    fontSize: '0.75rem',
                                    color: 'var(--slate-800)',
                                    fontStyle: 'italic',
                                  }}
                                >
                                  "{quote}"
                                </div>
                              ))}
                            </div>
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                ) : (
                  /* Legacy CandidateAnswer flow */
                  <div>
                    {isEditingThis ? (
                      <div
                        style={{
                          marginTop: '0.25rem',
                          padding: '0.75rem',
                          backgroundColor: 'var(--bg-card)',
                          borderRadius: 'var(--radius-sm)',
                          border: '1px solid var(--primary-400)',
                        }}
                      >
                        <label
                          style={{
                            fontSize: '0.8rem',
                            fontWeight: 600,
                            color: 'var(--text-main)',
                            display: 'block',
                            marginBottom: '0.35rem',
                          }}
                        >
                          Câu trả lời của ứng viên:
                        </label>
                        <textarea
                          rows={3}
                          value={answerText}
                          onChange={(e) => setAnswerText(e.target.value)}
                          placeholder="Nhập câu trả lời thực tế của ứng viên trong buổi phỏng vấn..."
                          style={{
                            width: '100%',
                            padding: '0.5rem 0.75rem',
                            borderRadius: 'var(--radius-sm)',
                            border: '1px solid var(--border-color)',
                            backgroundColor: 'var(--bg-card)',
                            color: 'var(--text-main)',
                            fontSize: '0.85rem',
                            fontFamily: 'inherit',
                            resize: 'vertical',
                            boxSizing: 'border-box',
                          }}
                        />
                        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.5rem', marginTop: '0.5rem' }}>
                          <Button variant="secondary" size="sm" onClick={handleCancelEditAnswer}>
                            Hủy
                          </Button>
                          <Button
                            variant="primary"
                            size="sm"
                            isLoading={isUpdatingAnswer}
                            onClick={() => handleSaveAnswer(q.id)}
                          >
                            Lưu câu trả lời
                          </Button>
                        </div>
                      </div>
                    ) : (
                      <div>
                        {q.candidateAnswer ? (
                          <div
                            style={{
                              fontSize: '0.85rem',
                              color: 'var(--text-main)',
                              marginTop: '0.2rem',
                              backgroundColor: 'var(--bg-card)',
                              padding: '0.5rem 0.75rem',
                              borderRadius: 'var(--radius-sm)',
                              border: '1px solid var(--border-color)',
                            }}
                          >
                            <span style={{ fontWeight: 600, color: 'var(--primary-600)' }}>Trả lời: </span>
                            {q.candidateAnswer}
                          </div>
                        ) : (
                          <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', fontStyle: 'italic' }}>
                            Chưa có câu trả lời từ ứng viên.
                          </div>
                        )}
                      </div>
                    )}

                    {q.aiFeedback && (
                      <div
                        style={{
                          fontSize: '0.8rem',
                          color: 'var(--primary-800)',
                          backgroundColor: 'var(--primary-50)',
                          padding: '0.4rem 0.6rem',
                          borderRadius: 'var(--radius-sm)',
                          border: '1px solid var(--primary-200)',
                          marginTop: '0.35rem',
                        }}
                      >
                        <Sparkles size={12} style={{ display: 'inline', marginRight: '4px' }} />
                        <em>Nhận xét AI:</em> {q.aiFeedback}
                      </div>
                    )}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      ) : (
        <p style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Chưa có câu hỏi phỏng vấn nào được thiết lập.</p>
      )}

      {/* Complete Interview Modal */}
      {canComplete && (
        <CompleteInterviewModal
          isOpen={isCompleteModalOpen}
          onClose={() => setIsCompleteModalOpen(false)}
          interview={interview}
          onConfirm={handleConfirmComplete}
          isLoading={isCompleting}
        />
      )}

      {/* Phase 4: Transcript Editor Modal */}
      {transcriptModalQuestion && (
        <TranscriptEditorModal
          isOpen={!!transcriptModalQuestion}
          onClose={() => setTranscriptModalQuestion(null)}
          interviewId={interview.id}
          questionId={transcriptModalQuestion.id}
          questionText={transcriptModalQuestion.question}
          rawTranscript={transcriptModalQuestion.answer?.transcript}
          initialTranscript={transcriptModalQuestion.answer?.effectiveTranscript}
          onSave={updateTranscript}
          isLoading={isUpdatingTranscript}
        />
      )}

      {/* Phase 4: Evaluation Audit Trail Modal */}
      {auditTrailModalQuestion && (
        <EvaluationAuditTrailModal
          isOpen={!!auditTrailModalQuestion}
          onClose={() => setAuditTrailModalQuestion(null)}
          interviewId={interview.id}
          questionId={auditTrailModalQuestion.id}
          questionText={auditTrailModalQuestion.question}
        />
      )}
    </div>
  );
}
