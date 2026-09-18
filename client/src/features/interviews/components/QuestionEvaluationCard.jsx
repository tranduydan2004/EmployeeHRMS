import { useState } from 'react';
import { useInterviewMutations } from '../hooks/useInterviews';
import { useAuthStore } from '@/features/auth';
import { Button, Input, Badge } from '@/components';
import { Sparkles, Plus, Edit3, MessageSquare } from 'lucide-react';

export default function QuestionEvaluationCard({ interview }) {
  const { addQuestion, isAddingQuestion, updateQuestionAnswer, isUpdatingAnswer } = useInterviewMutations();
  const role = useAuthStore((state) => state.role);
  const userId = useAuthStore((state) => state.userId);

  // Ownership check: Admin, HR, or the assigned Interviewer (by userId)
  const canEdit = role === 'Admin' || role === 'HR' || (role === 'Interviewer' && interview.interviewerId === userId);

  const [showAddForm, setShowAddForm] = useState(false);
  const [questionText, setQuestionText] = useState('');

  // Editing state for CandidateAnswer
  const [editingQuestionId, setEditingQuestionId] = useState(null);
  const [answerText, setAnswerText] = useState('');

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

  return (
    <div style={{ marginTop: '1rem', borderTop: '1px solid var(--border-color)', paddingTop: '1rem' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.75rem' }}>
        <h4 style={{ fontSize: '0.95rem', fontWeight: 600, color: 'var(--slate-800)', display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
          <MessageSquare size={16} color="var(--primary-600)" />
          Câu Hỏi & Đánh Giá Phỏng Vấn ({interview.questions?.length || 0})
        </h4>
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
      </div>

      {showAddForm && canEdit && (
        <form onSubmit={handleAddQuestion} style={{ marginBottom: '1rem', padding: '1rem', backgroundColor: 'var(--slate-50)', borderRadius: 'var(--radius-md)' }}>
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
        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
          {interview.questions.map((q, idx) => {
            const isEditingThis = editingQuestionId === q.id;

            return (
              <div
                key={q.id || idx}
                style={{
                  padding: '0.85rem 1rem',
                  backgroundColor: 'var(--slate-50)',
                  borderRadius: 'var(--radius-md)',
                  border: '1px solid var(--border-color)',
                  display: 'flex',
                  flexDirection: 'column',
                  gap: '0.5rem',
                }}
              >
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '1rem' }}>
                  <div style={{ flex: 1 }}>
                    <div style={{ fontSize: '0.875rem', fontWeight: 600, color: 'var(--slate-900)' }}>
                      <strong>Câu {idx + 1}:</strong> {q.question}
                    </div>
                  </div>

                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                    {q.aiQuestionScore !== null && q.aiQuestionScore !== undefined && (
                      <Badge variant="success" icon={Sparkles}>
                        {q.aiQuestionScore}/10
                      </Badge>
                    )}
                    {canEdit && !isEditingThis && (
                      <Button
                        variant="ghost"
                        size="xs"
                        icon={Edit3}
                        onClick={() => handleStartEditAnswer(q)}
                        title={q.candidateAnswer ? 'Sửa câu trả lời' : 'Nhập câu trả lời'}
                      >
                        {q.candidateAnswer ? 'Sửa' : 'Nhập trả lời'}
                      </Button>
                    )}
                  </div>
                </div>

                {isEditingThis ? (
                  <div style={{ marginTop: '0.25rem', padding: '0.5rem', backgroundColor: 'white', borderRadius: 'var(--radius-sm)', border: '1px solid var(--primary-300)' }}>
                    <label style={{ fontSize: '0.8rem', fontWeight: 600, color: 'var(--slate-700)', display: 'block', marginBottom: '0.25rem' }}>
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
                        fontSize: '0.85rem',
                        fontFamily: 'inherit',
                        resize: 'vertical',
                        boxSizing: 'border-box',
                      }}
                    />
                    <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.5rem', marginTop: '0.5rem' }}>
                      <Button variant="secondary" size="xs" onClick={handleCancelEditAnswer}>
                        Hủy
                      </Button>
                      <Button
                        variant="primary"
                        size="xs"
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
                          color: 'var(--slate-700)',
                          marginTop: '0.2rem',
                          backgroundColor: 'white',
                          padding: '0.5rem 0.75rem',
                          borderRadius: 'var(--radius-sm)',
                          border: '1px solid var(--border-color)',
                        }}
                      >
                        <span style={{ fontWeight: 600, color: 'var(--primary-700)' }}>Trả lời: </span>
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
                    }}
                  >
                    <Sparkles size={12} style={{ display: 'inline', marginRight: '4px' }} />
                    <em>Nhận xét AI:</em> {q.aiFeedback}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      ) : (
        <p style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Chưa có câu hỏi phỏng vấn nào được thiết lập.</p>
      )}
    </div>
  );
}
