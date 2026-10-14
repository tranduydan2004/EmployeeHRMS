import { create } from 'zustand';
import {
  fetchCandidateSession,
  startCandidateInterview,
  uploadAnswerAudio,
} from './candidatePortalApi';
import {
  saveOfflineAnswer,
  getOfflineAnswer,
  deleteOfflineAnswer,
} from './offlineStorage';

export const useCandidateInterviewStore = create((set, get) => ({
  // ============================================================
  // Session State
  // ============================================================
  inviteToken: null,
  session: null,
  isLoading: false,
  error: null,
  errorStatus: null, // 404, 410, 409, 500

  // Stepper flow: 'welcome' -> 'mic_check' -> 'in_progress' -> 'completed'
  stage: 'welcome',
  currentQuestionIndex: 0,
  isConsentGiven: false,

  // Global Anti-Cheat Telemetry
  tabSwitchCount: 0,
  outOfFocusDurationSeconds: 0,
  activeTabWarning: false,
  flags: [], // Ví dụ: ['MicDisconnected']

  // Network State
  isOnline: typeof navigator !== 'undefined' ? navigator.onLine : true,

  // ============================================================
  // Per-Question Runtime State: Record<questionId, QuestionState>
  // ============================================================
  questionsState: {},

  // ============================================================
  // Actions
  // ============================================================

  setInviteToken: (token) => set({ inviteToken: token }),

  setNetworkStatus: (isOnline) => set({ isOnline }),

  setConsent: (isConsentGiven) => set({ isConsentGiven }),

  setStage: (stage) => set({ stage }),

  dismissTabWarning: () => set({ activeTabWarning: false }),

  recordTabSwitch: (secondsOutOfFocus = 0) => {
    const { currentQuestionIndex, session, questionsState } = get();
    const currentQ = session?.questions?.[currentQuestionIndex];

    set((state) => ({
      tabSwitchCount: state.tabSwitchCount + 1,
      outOfFocusDurationSeconds: state.outOfFocusDurationSeconds + secondsOutOfFocus,
      activeTabWarning: true,
    }));

    if (currentQ) {
      const qState = questionsState[currentQ.id] || {};
      set({
        questionsState: {
          ...questionsState,
          [currentQ.id]: {
            ...qState,
            tabSwitchCount: (qState.tabSwitchCount || 0) + 1,
            outOfFocusDurationSeconds: (qState.outOfFocusDurationSeconds || 0) + secondsOutOfFocus,
          },
        },
      });
    }
  },

  addFlag: (flag) => {
    if (!flag) return;
    set((state) => {
      const updated = state.flags.includes(flag) ? state.flags : [...state.flags, flag];
      return { flags: updated };
    });
  },

  // 1. Tải thông tin phiên phỏng vấn từ server
  loadSession: async (token) => {
    const inviteToken = token || get().inviteToken;
    if (!inviteToken) return;

    set({ isLoading: true, error: null, errorStatus: null });

    try {
      const data = await fetchCandidateSession(inviteToken);

      // Khởi tạo trạng thái từng câu hỏi
      const initialQuestionsState = {};
      let firstUnansweredIndex = 0;
      let foundUnanswered = false;

      (data.questions || []).forEach((q, idx) => {
        initialQuestionsState[q.id] = {
          id: q.id,
          status: q.hasAnswer ? 'completed' : 'pending', // 'pending' | 'thinking' | 'recording' | 'uploading' | 'completed' | 'failed'
          audioBlob: null,
          audioDurationSeconds: 0,
          uploadProgress: q.hasAnswer ? 100 : 0,
          uploadError: null,
          tabSwitchCount: 0,
          outOfFocusDurationSeconds: 0,
          flags: [],
        };

        if (!q.hasAnswer && !foundUnanswered) {
          firstUnansweredIndex = idx;
          foundUnanswered = true;
        }
      });

      // Nếu tất cả câu hỏi đã được trả lời
      const isAllDone = data.questions?.length > 0 && data.questions.every((q) => q.hasAnswer);

      set({
        session: data,
        inviteToken,
        isLoading: false,
        questionsState: initialQuestionsState,
        currentQuestionIndex: isAllDone ? (data.questions.length - 1) : firstUnansweredIndex,
        stage: isAllDone ? 'completed' : (data.consentedAt ? 'in_progress' : 'welcome'),
      });
    } catch (err) {
      const status = err.response?.status || 500;
      const message =
        err.response?.data?.error ||
        err.message ||
        'Không thể tải thông tin phiên phỏng vấn. Vui lòng kiểm tra lại liên kết.';

      set({
        isLoading: false,
        error: message,
        errorStatus: status,
      });
    }
  },

  // 2. Ứng viên đồng ý điều khoản và bắt đầu buổi phỏng vấn
  confirmStartInterview: async () => {
    const { inviteToken } = get();
    if (!inviteToken) return;

    set({ isLoading: true, error: null });

    try {
      const result = await startCandidateInterview(inviteToken);
      set((state) => ({
        isLoading: false,
        stage: 'in_progress',
        session: state.session
          ? { ...state.session, status: result.status, consentedAt: result.consentedAt }
          : null,
      }));
    } catch (err) {
      set({
        isLoading: false,
        error: err.response?.data?.error || 'Không thể bắt đầu phỏng vấn. Vui lòng thử lại.',
      });
    }
  },

  // 3. Đổi câu hỏi
  setCurrentQuestionIndex: (index) => {
    const { session } = get();
    if (!session?.questions || index < 0 || index >= session.questions.length) return;
    set({ currentQuestionIndex: index });
  },

  // 4. Bắt đầu giai đoạn suy nghĩ của câu hỏi hiện tại
  startThinkingPhase: (questionId) => {
    set((state) => ({
      questionsState: {
        ...state.questionsState,
        [questionId]: {
          ...(state.questionsState[questionId] || {}),
          status: 'thinking',
        },
      },
    }));
  },

  // 5. Bắt đầu giai đoạn ghi âm
  startRecordingPhase: (questionId) => {
    set((state) => ({
      questionsState: {
        ...state.questionsState,
        [questionId]: {
          ...(state.questionsState[questionId] || {}),
          status: 'recording',
        },
      },
    }));
  },

  // 6. Ghi âm xong: Lưu an toàn vào IndexedDB trước, sau đó tự động kích hoạt upload
  finishRecording: async (questionId, audioBlob, durationSeconds) => {
    const { inviteToken, questionsState, flags } = get();
    const currentQState = questionsState[questionId] || {};

    const combinedFlags = Array.from(new Set([...(currentQState.flags || []), ...flags]));

    // Cập nhật state cục bộ sang 'uploading'
    set((state) => ({
      questionsState: {
        ...state.questionsState,
        [questionId]: {
          ...currentQState,
          status: 'uploading',
          audioBlob,
          audioDurationSeconds: durationSeconds,
          uploadProgress: 0,
          uploadError: null,
          flags: combinedFlags,
        },
      },
    }));

    // Lưu ngay lập tức vào IndexedDB (resilience backup)
    await saveOfflineAnswer(inviteToken, questionId, audioBlob, {
      durationSeconds,
      tabSwitchCount: currentQState.tabSwitchCount || 0,
      outOfFocusDurationSeconds: currentQState.outOfFocusDurationSeconds || 0,
      flags: combinedFlags,
    });

    // Thực hiện upload lên server
    get().uploadAnswer(questionId);
  },

  // 7. Thực hiện upload câu trả lời lên server kèm theo dõi % tiến trình
  uploadAnswer: async (questionId) => {
    const { inviteToken, questionsState, session, currentQuestionIndex } = get();
    const qState = questionsState[questionId];
    if (!qState || !qState.audioBlob) return;

    set((state) => ({
      questionsState: {
        ...state.questionsState,
        [questionId]: {
          ...qState,
          status: 'uploading',
          uploadError: null,
        },
      },
    }));

    try {
      await uploadAnswerAudio(
        inviteToken,
        questionId,
        {
          audioBlob: qState.audioBlob,
          durationSeconds: qState.audioDurationSeconds,
          tabSwitchCount: qState.tabSwitchCount || 0,
          outOfFocusDurationSeconds: qState.outOfFocusDurationSeconds || 0,
          flags: qState.flags || [],
        },
        (progressPercent) => {
          set((state) => ({
            questionsState: {
              ...state.questionsState,
              [questionId]: {
                ...(state.questionsState[questionId] || {}),
                uploadProgress: progressPercent,
              },
            },
          }));
        }
      );

      // Upload thành công -> Xóa backup trong IndexedDB và đánh dấu hoàn thành
      await deleteOfflineAnswer(inviteToken, questionId);

      set((state) => {
        const updatedQuestions = {
          ...state.questionsState,
          [questionId]: {
            ...state.questionsState[questionId],
            status: 'completed',
            uploadProgress: 100,
          },
        };

        // Kiểm tra xem đã hoàn thành tất cả câu hỏi chưa
        const allCompleted = session?.questions?.every(
          (q) => updatedQuestions[q.id]?.status === 'completed'
        );

        return {
          questionsState: updatedQuestions,
          stage: allCompleted ? 'completed' : state.stage,
          // Nếu chưa hết câu thì tự động chuyển sang câu tiếp theo sau 1 giây
          currentQuestionIndex:
            !allCompleted && currentQuestionIndex < (session?.questions?.length || 1) - 1
              ? currentQuestionIndex + 1
              : currentQuestionIndex,
        };
      });
    } catch (err) {
      const errorMsg =
        err.response?.data?.error ||
        err.message ||
        'Lỗi đường truyền khi tải lên file ghi âm. File đã được lưu an toàn trong máy.';

      set((state) => ({
        questionsState: {
          ...state.questionsState,
          [questionId]: {
            ...state.questionsState[questionId],
            status: 'failed',
            uploadError: errorMsg,
          },
        },
      }));
    }
  },

  // 8. Thử lại tải lên từ file đã lưu trong IndexedDB khi mạng ổn định
  retryUpload: async (questionId) => {
    const { inviteToken } = get();
    const offlineItem = await getOfflineAnswer(inviteToken, questionId);

    if (offlineItem && offlineItem.audioBlob) {
      set((state) => ({
        questionsState: {
          ...state.questionsState,
          [questionId]: {
            ...(state.questionsState[questionId] || {}),
            audioBlob: offlineItem.audioBlob,
            audioDurationSeconds: offlineItem.metadata?.durationSeconds || 0,
            tabSwitchCount: offlineItem.metadata?.tabSwitchCount || 0,
            outOfFocusDurationSeconds: offlineItem.metadata?.outOfFocusDurationSeconds || 0,
            flags: offlineItem.metadata?.flags || [],
            status: 'uploading',
            uploadProgress: 0,
            uploadError: null,
          },
        },
      }));

      get().uploadAnswer(questionId);
    } else {
      // Nếu không có trong IndexedDB thì gọi upload bình thường từ state
      get().uploadAnswer(questionId);
    }
  },
}));
