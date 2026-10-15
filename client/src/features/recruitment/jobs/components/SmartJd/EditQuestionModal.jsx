import { useState, useEffect } from 'react';
import { Modal, Button } from '@/components';
import { useUpdateQuestionMutation } from '../../hooks/useSmartJd';
import {
  QUESTION_TIME_LIMITS,
  updateQuestionBankItemSchema,
} from '../../schemas/smartJdSchemas';
import {
  Clock,
  Mic,
  Award,
  Sparkles,
  ChevronDown,
  ChevronUp,
  Save,
  X,
  AlertCircle,
} from 'lucide-react';

export default function EditQuestionModal({
  isOpen,
  onClose,
  question,
  jobId,
}) {
  const updateMutation = useUpdateQuestionMutation();

  const [form, setForm] = useState({
    question: '',
    thinkSeconds: QUESTION_TIME_LIMITS.think.default,
    answerSeconds: QUESTION_TIME_LIMITS.answer.default,
    scoringRubric: {
      excellent: '',
      good: '',
      acceptable: '',
      poor: '',
    },
  });

  const [errors, setErrors] = useState({});
  const [isRubricOpen, setIsRubricOpen] = useState(false);

  useEffect(() => {
    if (question && isOpen) {
      setForm({
        question: question.question || '',
        thinkSeconds:
          question.thinkSeconds !== undefined && question.thinkSeconds !== null
            ? question.thinkSeconds
            : QUESTION_TIME_LIMITS.think.default,
        answerSeconds:
          question.answerSeconds !== undefined && question.answerSeconds !== null
            ? question.answerSeconds
            : QUESTION_TIME_LIMITS.answer.default,
        scoringRubric: {
          excellent: question.scoringRubric?.excellent || '',
          good: question.scoringRubric?.good || '',
          acceptable: question.scoringRubric?.acceptable || '',
          poor: question.scoringRubric?.poor || '',
        },
      });
      setErrors({});
      setIsRubricOpen(false);
    }
  }, [question, isOpen]);

  const handleSubmit = async (e) => {
    e?.preventDefault();

    const payload = {
      question: form.question.trim(),
      thinkSeconds: Number(form.thinkSeconds),
      answerSeconds: Number(form.answerSeconds),
      scoringRubric: {
        excellent: form.scoringRubric.excellent.trim(),
        good: form.scoringRubric.good.trim(),
        acceptable: form.scoringRubric.acceptable.trim(),
        poor: form.scoringRubric.poor.trim(),
      },
    };

    const validation = updateQuestionBankItemSchema.safeParse(payload);
    if (!validation.success) {
      const fieldErrors = {};
      validation.error.issues.forEach((err) => {
        fieldErrors[err.path[0]] = err.message;
      });
      setErrors(fieldErrors);
      return;
    }

    setErrors({});
    await updateMutation.mutateAsync({
      jobId: jobId || question.jobPostingId,
      questionId: question.id,
      payload,
    });

    onClose();
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <Sparkles size={18} color="var(--primary-600)" />
          <span>Chỉnh sửa câu hỏi #{question ? question.orderIndex + 1 : ''}</span>
        </div>
      }
      maxWidth="680px"
      footer={
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', width: '100%' }}>
          <Button variant="secondary" onClick={onClose} disabled={updateMutation.isPending}>
            Hủy
          </Button>
          <Button
            variant="primary"
            icon={Save}
            onClick={handleSubmit}
            isLoading={updateMutation.isPending}
          >
            Lưu thay đổi
          </Button>
        </div>
      }
    >
      <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
        {/* Nội dung câu hỏi */}
        <div>
          <label
            style={{
              display: 'block',
              fontSize: '0.875rem',
              fontWeight: 600,
              color: 'var(--slate-800)',
              marginBottom: '0.35rem',
            }}
          >
            Nội dung câu hỏi phỏng vấn <span style={{ color: 'var(--danger-500)' }}>*</span>
          </label>
          <textarea
            rows={3}
            value={form.question}
            onChange={(e) => setForm({ ...form, question: e.target.value })}
            placeholder="Nhập nội dung câu hỏi phỏng vấn..."
            style={{
              width: '100%',
              padding: '0.65rem 0.85rem',
              borderRadius: 'var(--radius-md)',
              border: `1px solid ${errors.question ? 'var(--danger-500)' : 'var(--border-color)'}`,
              fontSize: '0.9rem',
              fontFamily: 'inherit',
              lineHeight: 1.5,
              resize: 'vertical',
            }}
          />
          {errors.question && (
            <p style={{ margin: '0.25rem 0 0', fontSize: '0.8rem', color: 'var(--danger-600)' }}>
              {errors.question}
            </p>
          )}
        </div>

        {/* Cấu hình thời gian (Per-question timeboxing) */}
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: '1fr 1fr',
            gap: '1rem',
            padding: '1rem',
            backgroundColor: 'var(--slate-50)',
            borderRadius: 'var(--radius-md)',
            border: '1px solid var(--border-color)',
          }}
        >
          {/* Thời gian suy nghĩ */}
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', marginBottom: '0.35rem' }}>
              <Clock size={16} color="var(--primary-600)" />
              <label style={{ fontSize: '0.85rem', fontWeight: 600, color: 'var(--slate-800)' }}>
                Thời gian suy nghĩ
              </label>
            </div>

            <div style={{ display: 'flex', gap: '0.35rem', flexWrap: 'wrap', marginBottom: '0.5rem' }}>
              {QUESTION_TIME_LIMITS.think.presets.map((preset) => {
                const isSelected = form.thinkSeconds === preset;
                return (
                  <button
                    key={preset}
                    type="button"
                    onClick={() => setForm({ ...form, thinkSeconds: preset })}
                    style={{
                      padding: '0.2rem 0.55rem',
                      fontSize: '0.75rem',
                      fontWeight: 600,
                      borderRadius: 'var(--radius-full)',
                      border: isSelected
                        ? '1px solid var(--primary-600)'
                        : '1px solid var(--border-color)',
                      backgroundColor: isSelected ? 'var(--primary-50)' : '#ffffff',
                      color: isSelected ? 'var(--primary-700)' : 'var(--slate-700)',
                      cursor: 'pointer',
                      transition: 'all 0.15s ease',
                    }}
                  >
                    {preset}s {preset === 20 && '(Chuẩn)'}
                  </button>
                );
              })}
            </div>

            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <input
                type="number"
                min={QUESTION_TIME_LIMITS.think.min}
                max={QUESTION_TIME_LIMITS.think.max}
                value={form.thinkSeconds}
                onChange={(e) => setForm({ ...form, thinkSeconds: e.target.value })}
                style={{
                  width: '90px',
                  padding: '0.35rem 0.6rem',
                  borderRadius: 'var(--radius-sm)',
                  border: `1px solid ${errors.thinkSeconds ? 'var(--danger-500)' : 'var(--border-color)'}`,
                  fontSize: '0.85rem',
                }}
              />
              <span style={{ fontSize: '0.8rem', color: 'var(--slate-500)' }}>
                giây ({QUESTION_TIME_LIMITS.think.min} - {QUESTION_TIME_LIMITS.think.max}s)
              </span>
            </div>
            {errors.thinkSeconds && (
              <p style={{ margin: '0.25rem 0 0', fontSize: '0.75rem', color: 'var(--danger-600)' }}>
                {errors.thinkSeconds}
              </p>
            )}
          </div>

          {/* Thời gian trả lời */}
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', marginBottom: '0.35rem' }}>
              <Mic size={16} color="var(--secondary-600)" />
              <label style={{ fontSize: '0.85rem', fontWeight: 600, color: 'var(--slate-800)' }}>
                Thời gian ghi âm tối đa
              </label>
            </div>

            <div style={{ display: 'flex', gap: '0.35rem', flexWrap: 'wrap', marginBottom: '0.5rem' }}>
              {QUESTION_TIME_LIMITS.answer.presets.map((preset) => {
                const isSelected = form.answerSeconds === preset;
                return (
                  <button
                    key={preset}
                    type="button"
                    onClick={() => setForm({ ...form, answerSeconds: preset })}
                    style={{
                      padding: '0.2rem 0.55rem',
                      fontSize: '0.75rem',
                      fontWeight: 600,
                      borderRadius: 'var(--radius-full)',
                      border: isSelected
                        ? '1px solid #7c3aed'
                        : '1px solid var(--border-color)',
                      backgroundColor: isSelected ? '#f5f3ff' : '#ffffff',
                      color: isSelected ? '#6d28d9' : 'var(--slate-700)',
                      cursor: 'pointer',
                      transition: 'all 0.15s ease',
                    }}
                  >
                    {preset}s {preset === 90 && '(Chuẩn)'}
                  </button>
                );
              })}
            </div>

            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <input
                type="number"
                min={QUESTION_TIME_LIMITS.answer.min}
                max={QUESTION_TIME_LIMITS.answer.max}
                value={form.answerSeconds}
                onChange={(e) => setForm({ ...form, answerSeconds: e.target.value })}
                style={{
                  width: '90px',
                  padding: '0.35rem 0.6rem',
                  borderRadius: 'var(--radius-sm)',
                  border: `1px solid ${errors.answerSeconds ? 'var(--danger-500)' : 'var(--border-color)'}`,
                  fontSize: '0.85rem',
                }}
              />
              <span style={{ fontSize: '0.8rem', color: 'var(--slate-500)' }}>
                giây ({QUESTION_TIME_LIMITS.answer.min} - {QUESTION_TIME_LIMITS.answer.max}s)
              </span>
            </div>
            {errors.answerSeconds && (
              <p style={{ margin: '0.25rem 0 0', fontSize: '0.75rem', color: 'var(--danger-600)' }}>
                {errors.answerSeconds}
              </p>
            )}
          </div>
        </div>

        {/* Collapsible Scoring Rubric Section */}
        <div style={{ border: '1px solid var(--border-color)', borderRadius: 'var(--radius-md)' }}>
          <button
            type="button"
            onClick={() => setIsRubricOpen(!isRubricOpen)}
            style={{
              width: '100%',
              padding: '0.75rem 1rem',
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              background: 'none',
              border: 'none',
              cursor: 'pointer',
              color: 'var(--slate-800)',
              fontWeight: 600,
              fontSize: '0.875rem',
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
              <Award size={16} color="var(--primary-600)" />
              <span>Barem điểm đánh giá (Scoring Rubric)</span>
            </div>
            {isRubricOpen ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
          </button>

          {isRubricOpen && (
            <div style={{ padding: '0 1rem 1rem 1rem', display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
              {/* Xuất sắc */}
              <div>
                <label style={{ display: 'block', fontSize: '0.78rem', fontWeight: 600, color: '#065f46', marginBottom: '0.25rem' }}>
                  🌟 Mức Xuất Sắc (9 - 10 điểm)
                </label>
                <textarea
                  rows={2}
                  value={form.scoringRubric.excellent}
                  onChange={(e) =>
                    setForm({
                      ...form,
                      scoringRubric: { ...form.scoringRubric, excellent: e.target.value },
                    })
                  }
                  style={{ width: '100%', padding: '0.45rem', fontSize: '0.8rem', borderRadius: 'var(--radius-sm)', border: '1px solid #a7f3d0' }}
                />
              </div>

              {/* Tốt */}
              <div>
                <label style={{ display: 'block', fontSize: '0.78rem', fontWeight: 600, color: '#1e40af', marginBottom: '0.25rem' }}>
                  👍 Mức Tốt (7 - 8 điểm)
                </label>
                <textarea
                  rows={2}
                  value={form.scoringRubric.good}
                  onChange={(e) =>
                    setForm({
                      ...form,
                      scoringRubric: { ...form.scoringRubric, good: e.target.value },
                    })
                  }
                  style={{ width: '100%', padding: '0.45rem', fontSize: '0.8rem', borderRadius: 'var(--radius-sm)', border: '1px solid #bfdbfe' }}
                />
              </div>

              {/* Chấp nhận */}
              <div>
                <label style={{ display: 'block', fontSize: '0.78rem', fontWeight: 600, color: '#92400e', marginBottom: '0.25rem' }}>
                  👌 Mức Chấp Nhận (5 - 6 điểm)
                </label>
                <textarea
                  rows={2}
                  value={form.scoringRubric.acceptable}
                  onChange={(e) =>
                    setForm({
                      ...form,
                      scoringRubric: { ...form.scoringRubric, acceptable: e.target.value },
                    })
                  }
                  style={{ width: '100%', padding: '0.45rem', fontSize: '0.8rem', borderRadius: 'var(--radius-sm)', border: '1px solid #fde68a' }}
                />
              </div>

              {/* Kém */}
              <div>
                <label style={{ display: 'block', fontSize: '0.78rem', fontWeight: 600, color: '#991b1b', marginBottom: '0.25rem' }}>
                  ⚠️ Cần Cải Thiện (&lt; 5 điểm)
                </label>
                <textarea
                  rows={2}
                  value={form.scoringRubric.poor}
                  onChange={(e) =>
                    setForm({
                      ...form,
                      scoringRubric: { ...form.scoringRubric, poor: e.target.value },
                    })
                  }
                  style={{ width: '100%', padding: '0.45rem', fontSize: '0.8rem', borderRadius: 'var(--radius-sm)', border: '1px solid #fecaca' }}
                />
              </div>
            </div>
          )}
        </div>
      </form>
    </Modal>
  );
}
