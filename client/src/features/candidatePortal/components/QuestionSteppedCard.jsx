import { useEffect, useCallback, useRef } from 'react';
import {
  Mic,
  Clock,
  CheckCircle2,
  Square,
  Sparkles,
  ChevronRight,
} from 'lucide-react';
import { useCandidateInterviewStore } from '../useCandidateInterviewStore';
import { useAudioRecorder } from '../hooks/useAudioRecorder';
import { useInterviewTimer } from '../hooks/useInterviewTimer';
import { AudioUploadProgressBar } from './AudioUploadProgressBar';

export function QuestionSteppedCard() {
  const {
    session,
    currentQuestionIndex,
    advanceToNextQuestion,
    questionsState,
    startThinkingPhase,
    startRecordingPhase,
    finishRecording,
    retryUpload,
    addFlag,
  } = useCandidateInterviewStore();

  const questions = session?.questions || [];
  const currentQuestion = questions[currentQuestionIndex];
  const totalQuestions = questions.length;

  const currentQState = currentQuestion ? questionsState[currentQuestion.id] : null;
  const currentStatus = currentQState?.status || 'pending';

  // Audio Recorder Hook
  const {
    volumeLevel,
    startRecording,
    stopRecording,
  } = useAudioRecorder({
    onMicDisconnected: () => {
      addFlag('MicDisconnected');
    },
  });

  // 1. Timer chuẩn bị suy nghĩ (Think Timer - đồng bộ với mốc Server nếu có)
  const serverRemainingThink = currentQState?.remainingThinkSeconds;
  const thinkSeconds = serverRemainingThink !== undefined && serverRemainingThink !== null
    ? serverRemainingThink
    : (currentQuestion?.thinkSeconds || 20);

  // 2. Timer trả lời (Answer Timer - đồng bộ với mốc Server nếu có)
  const serverRemainingAnswer = currentQState?.remainingAnswerSeconds;
  const answerSeconds = serverRemainingAnswer !== undefined && serverRemainingAnswer !== null
    ? serverRemainingAnswer
    : (currentQuestion?.answerSeconds || 60);

  // Refs để giải quyết circular dependency giữa timer callbacks và handlers
  const handleStartRecordingRef = useRef(null);
  const handleTimeExpiredRef = useRef(null);

  const {
    start: startThinkTimer,
    reset: resetThinkTimer,
    formattedTime: formattedThinkTime,
  } = useInterviewTimer({
    totalSeconds: thinkSeconds,
    onFinish: () => {
      handleStartRecordingRef.current?.();
    },
  });

  const {
    secondsLeft: answerTimeLeft,
    start: startAnswerTimer,
    reset: resetAnswerTimer,
    formattedTime: formattedAnswerTime,
  } = useInterviewTimer({
    totalSeconds: answerSeconds,
    onFinish: () => {
      handleTimeExpiredRef.current?.();
    },
  });

  // Hành động khi hết giờ trả lời: dừng ghi âm, nộp bài và tự động chuyển câu tiếp theo
  const handleTimeExpired = useCallback(async () => {
    if (!currentQuestion) return;
    resetAnswerTimer(0);
    resetThinkTimer(0);

    try {
      const result = await stopRecording();
      if (result?.blob) {
        await finishRecording(currentQuestion.id, result.blob, result.durationSeconds);
      } else {
        // Nếu không có audio blob (ví dụ mic chưa bật hoặc vào lại tab khi đã hết giờ):
        advanceToNextQuestion(true);
      }
    } catch (err) {
      console.error('[QuestionSteppedCard] Lỗi khi xử lý hết giờ trả lời:', err);
      advanceToNextQuestion(true);
    }
  }, [currentQuestion, resetAnswerTimer, resetThinkTimer, stopRecording, finishRecording, advanceToNextQuestion]);

  // Hành động bắt đầu ghi âm sớm (hoặc tự động khi hết think time)
  const handleStartRecording = useCallback(async () => {
    if (!currentQuestion) return;
    resetThinkTimer(0);
    startRecordingPhase(currentQuestion.id);

    try {
      await startRecording();
      resetAnswerTimer(answerSeconds);
      startAnswerTimer(answerSeconds);
    } catch (err) {
      console.error('[QuestionSteppedCard] Không thể bật ghi âm:', err);
    }
  }, [currentQuestion, resetThinkTimer, startRecordingPhase, startRecording, resetAnswerTimer, answerSeconds, startAnswerTimer]);

  // Hành động chủ động dừng ghi âm và nộp bài trước giờ
  const handleStopRecording = useCallback(async () => {
    if (!currentQuestion) return;
    resetAnswerTimer(0);

    try {
      const result = await stopRecording();
      if (result?.blob) {
        await finishRecording(currentQuestion.id, result.blob, result.durationSeconds);
      } else {
        advanceToNextQuestion(true);
      }
    } catch (err) {
      console.error('[QuestionSteppedCard] Lỗi khi hoàn tất câu trả lời:', err);
    }
  }, [currentQuestion, resetAnswerTimer, stopRecording, finishRecording, advanceToNextQuestion]);

  handleStartRecordingRef.current = handleStartRecording;
  handleTimeExpiredRef.current = handleTimeExpired;

  // Tự động quản lý pha làm bài theo mốc thời gian thực từ Server (kể cả khi ứng viên tải lại trang hoặc mở trình duyệt mới)
  useEffect(() => {
    if (!currentQuestion) return;

    if (currentStatus === 'pending') {
      if (thinkSeconds > 0) {
        startThinkingPhase(currentQuestion.id);
        resetThinkTimer(thinkSeconds);
        startThinkTimer(thinkSeconds);
      } else if (answerSeconds > 0) {
        // Mở lại trang mà thời gian suy nghĩ đã hết trên Server -> vào thẳng pha ghi âm
        handleStartRecording();
      } else {
        // Đã hết cả thời gian suy nghĩ và trả lời trên Server -> chuyển câu ngay
        handleTimeExpired();
      }
    } else if (currentStatus === 'thinking') {
      if (thinkSeconds > 0) {
        resetThinkTimer(thinkSeconds);
        startThinkTimer(thinkSeconds);
      } else {
        handleStartRecording();
      }
    } else if (currentStatus === 'recording') {
      if (answerSeconds > 0) {
        // Vào lại trang khi đang trong pha recording: bật mic và đếm tiếp
        startRecording().catch(console.error);
        resetAnswerTimer(answerSeconds);
        startAnswerTimer(answerSeconds);
      } else {
        // Đã hết giờ trả lời
        handleTimeExpired();
      }
    }
  }, [currentQuestion?.id]);

  if (!currentQuestion) return null;

  return (
    <div style={{
      maxWidth: '780px',
      margin: '0 auto',
      backgroundColor: '#ffffff',
      borderRadius: '24px',
      boxShadow: '0 20px 25px -5px rgba(0, 0, 0, 0.05)',
      border: '1px solid #e2e8f0',
      overflow: 'hidden',
    }}>
      {/* Top Stepper Navigation */}
      <div style={{
        padding: '20px 32px',
        borderBottom: '1px solid #f1f5f9',
        backgroundColor: '#fafafa',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        flexWrap: 'wrap',
        gap: '12px'
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <span style={{ fontSize: '14px', fontWeight: '700', color: '#4f46e5' }}>
            CÂU HỎI {currentQuestionIndex + 1} / {totalQuestions}
          </span>
          <span style={{ fontSize: '13px', color: '#94a3b8' }}>•</span>
          <span style={{ fontSize: '13px', color: '#64748b' }}>
            {currentStatus === 'thinking' && 'Đang suy nghĩ'}
            {currentStatus === 'recording' && 'Đang ghi âm'}
            {currentStatus === 'uploading' && 'Đang tải lên'}
            {currentStatus === 'completed' && 'Đã hoàn thành'}
            {currentStatus === 'failed' && 'Lỗi upload'}
          </span>
        </div>

        {/* Stepper Dots: Cố định 1 chiều - Chỉ hiển thị tiến trình, không cho phép click nhảy câu */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
          {questions.map((q, idx) => {
            const state = questionsState[q.id];
            const isDone = state?.status === 'completed';
            const isCurrent = idx === currentQuestionIndex;

            return (
              <div
                key={q.id}
                style={{
                  width: isCurrent ? '26px' : '10px',
                  height: '10px',
                  borderRadius: '5px',
                  backgroundColor: isDone ? '#10b981' : isCurrent ? '#4f46e5' : '#e2e8f0',
                  cursor: 'default',
                  transition: 'all 0.2s',
                }}
                title={isDone ? `Câu ${idx + 1}: Đã hoàn thành` : isCurrent ? `Câu ${idx + 1}: Đang thực hiện` : `Câu ${idx + 1}: Chưa đến lượt`}
              />
            );
          })}
        </div>
      </div>

      <div style={{ padding: '36px' }}>
        {/* Question Text Card */}
        <div style={{
          backgroundColor: '#f8fafc',
          borderRadius: '16px',
          padding: '28px',
          border: '1px solid #e2e8f0',
          marginBottom: '32px',
        }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: '#6366f1', fontSize: '13px', fontWeight: '700', textTransform: 'uppercase', marginBottom: '12px' }}>
            <Sparkles size={16} /> Nội dung câu hỏi
          </div>
          <h2 style={{ fontSize: '22px', fontWeight: '700', color: '#0f172a', lineHeight: '1.4', margin: 0 }}>
            {currentQuestion.question}
          </h2>
        </div>

        {/* Dynamic Phase Interface */}

        {/* Phase 1: Thinking Time */}
        {currentStatus === 'thinking' && (
          <div style={{
            backgroundColor: '#eff6ff',
            borderRadius: '16px',
            padding: '28px',
            border: '1px solid #bfdbfe',
            textAlign: 'center',
            marginBottom: '24px'
          }}>
            <div style={{ display: 'inline-flex', alignItems: 'center', gap: '6px', color: '#1d4ed8', fontWeight: '600', fontSize: '14px', marginBottom: '12px' }}>
              <Clock size={18} /> Thời gian chuẩn bị câu trả lời
            </div>

            <div style={{ fontSize: '48px', fontWeight: '800', color: '#1e40af', fontFamily: 'monospace', marginBottom: '8px' }}>
              {formattedThinkTime}
            </div>

            <p style={{ color: '#475569', fontSize: '14px', marginBottom: '24px' }}>
              Hãy sắp xếp các ý trong đầu. Khi hết {thinkSeconds}s, hệ thống sẽ tự động bật micro ghi âm.
            </p>

            <button
              onClick={handleStartRecording}
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: '8px',
                backgroundColor: '#2563eb',
                color: '#ffffff',
                padding: '14px 28px',
                borderRadius: '12px',
                border: 'none',
                fontSize: '15px',
                fontWeight: '600',
                cursor: 'pointer',
                boxShadow: '0 10px 15px -3px rgba(37, 99, 235, 0.3)',
                transition: 'all 0.2s',
              }}
              onMouseEnter={(e) => e.currentTarget.style.backgroundColor = '#1d4ed8'}
              onMouseLeave={(e) => e.currentTarget.style.backgroundColor = '#2563eb'}
            >
              <Mic size={18} />
              <span>Bắt đầu trả lời ngay</span>
            </button>
          </div>
        )}

        {/* Phase 2: Recording Phase */}
        {currentStatus === 'recording' && (
          <div style={{
            backgroundColor: '#fef2f2',
            borderRadius: '16px',
            padding: '28px',
            border: '2px solid #fecaca',
            textAlign: 'center',
            marginBottom: '24px'
          }}>
            <div style={{ display: 'inline-flex', alignItems: 'center', gap: '8px', color: '#dc2626', fontWeight: '700', fontSize: '14px', marginBottom: '12px' }}>
              <span style={{ width: '10px', height: '10px', borderRadius: '50%', backgroundColor: '#ef4444', display: 'inline-block', animation: 'pulse 1s infinite' }} />
              ĐANG GHI ÂM CÂU TRẢ LỜI
            </div>

            <div style={{ fontSize: '52px', fontWeight: '800', color: answerTimeLeft <= 10 ? '#ef4444' : '#991b1b', fontFamily: 'monospace', marginBottom: '12px' }}>
              {formattedAnswerTime}
            </div>

            {/* Audio Waveform Meter */}
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '5px', height: '40px', marginBottom: '20px' }}>
              {[0.3, 0.6, 1.0, 0.8, 1.2, 0.7, 1.1, 0.5, 0.9, 0.4].map((mult, i) => {
                const h = Math.max(8, Math.min(36, (volumeLevel * mult) / 2));
                return (
                  <div
                    key={i}
                    style={{
                      width: '6px',
                      height: `${h}px`,
                      backgroundColor: volumeLevel > 15 ? '#ef4444' : '#cbd5e1',
                      borderRadius: '3px',
                      transition: 'height 0.05s ease-out',
                    }}
                  />
                );
              })}
            </div>

            <p style={{ color: '#475569', fontSize: '14px', marginBottom: '24px' }}>
              Nói to, rõ ràng vào micro. Bạn có thể nhấn nút bên dưới để hoàn tất câu trả lời bất kỳ lúc nào.
            </p>

            <button
              onClick={handleStopRecording}
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: '8px',
                backgroundColor: '#dc2626',
                color: '#ffffff',
                padding: '14px 28px',
                borderRadius: '12px',
                border: 'none',
                fontSize: '15px',
                fontWeight: '600',
                cursor: 'pointer',
                boxShadow: '0 10px 15px -3px rgba(220, 38, 38, 0.3)',
                transition: 'all 0.2s',
              }}
              onMouseEnter={(e) => e.currentTarget.style.backgroundColor = '#b91c1c'}
              onMouseLeave={(e) => e.currentTarget.style.backgroundColor = '#dc2626'}
            >
              <Square size={16} fill="#ffffff" />
              <span>Hoàn tất câu trả lời</span>
            </button>
          </div>
        )}

        {/* Phase 3: Uploading or Error */}
        {(currentStatus === 'uploading' || currentStatus === 'failed') && (
          <AudioUploadProgressBar
            progress={currentQState?.uploadProgress || 0}
            isFailed={currentStatus === 'failed'}
            isCompleted={currentStatus === 'completed'}
            errorMessage={currentQState?.uploadError}
            onRetry={() => retryUpload(currentQuestion.id)}
          />
        )}

        {/* Phase 4: Completed Question */}
        {currentStatus === 'completed' && (
          <div style={{
            backgroundColor: '#ecfdf5',
            borderRadius: '16px',
            padding: '24px',
            border: '1px solid #a7f3d0',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            marginBottom: '24px',
          }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
              <CheckCircle2 size={24} style={{ color: '#10b981' }} />
              <div>
                <div style={{ fontSize: '15px', fontWeight: '700', color: '#065f46' }}>
                  Câu trả lời đã được ghi nhận thành công!
                </div>
                <div style={{ fontSize: '13px', color: '#047857' }}>
                  Hệ thống AI đang bóc băng và chấm điểm cho câu hỏi này.
                </div>
              </div>
            </div>

            {currentQuestionIndex < totalQuestions - 1 && (
              <button
                onClick={advanceToNextQuestion}
                style={{
                  display: 'inline-flex',
                  alignItems: 'center',
                  gap: '6px',
                  backgroundColor: '#10b981',
                  color: '#ffffff',
                  padding: '10px 18px',
                  borderRadius: '10px',
                  border: 'none',
                  fontSize: '14px',
                  fontWeight: '600',
                  cursor: 'pointer',
                }}
              >
                <span>Câu tiếp theo</span>
                <ChevronRight size={16} />
              </button>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
