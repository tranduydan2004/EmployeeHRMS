import { create } from 'zustand';

const initialJdContent = {
  intro: '',
  responsibilities: [],
  mustHave: [],
  niceToHave: [],
  benefits: [],
};

export const useSmartJdStore = create((set, get) => ({
  isOpen: false,
  step: 1, // 1: Input Params | 2: Preview & Edit JD | 3: Question Bank
  isReadOnly: false, // true khi tin tuyển dụng đã Approved hoặc Published
  currentJob: null, // JobPostingDetailResponseDto
  editedJd: { ...initialJdContent },
  questions: [], // QuestionBankItemResponseDto[]

  // Modal Control
  openModal: (job = null, startStep = 1, readOnly = null, initialQuestions = []) => {
    if (job) {
      let jdContent = job.jdContent;
      if (!jdContent && (job.description || job.requirements)) {
        jdContent = {
          intro: job.description || '',
          responsibilities: [],
          mustHave: job.requirements ? [job.requirements] : [],
          niceToHave: [],
          benefits: [],
        };
      } else if (!jdContent) {
        jdContent = { ...initialJdContent };
      }

      const calculatedReadOnly = readOnly !== null ? readOnly : job.status !== 'Draft';
      set({
        isOpen: true,
        step: startStep,
        isReadOnly: calculatedReadOnly,
        currentJob: job,
        editedJd: {
          intro: jdContent.intro || '',
          responsibilities: [...(jdContent.responsibilities || [])],
          mustHave: [...(jdContent.mustHave || [])],
          niceToHave: [...(jdContent.niceToHave || [])],
          benefits: [...(jdContent.benefits || [])],
        },
        questions: initialQuestions || [],
      });
    } else {
      set({
        isOpen: true,
        step: 1,
        isReadOnly: false,
        currentJob: null,
        editedJd: { ...initialJdContent },
        questions: [],
      });
    }
  },

  closeModal: () => {
    set({ isOpen: false });
  },

  setStep: (step) => set({ step }),

  // Set response sau khi gọi POST /draft-jd thành công
  setGeneratedJob: (job) => {
    const jdContent = job.jdContent || { ...initialJdContent };
    set({
      currentJob: job,
      isReadOnly: false,
      editedJd: {
        intro: jdContent.intro || '',
        responsibilities: [...(jdContent.responsibilities || [])],
        mustHave: [...(jdContent.mustHave || [])],
        niceToHave: [...(jdContent.niceToHave || [])],
        benefits: [...(jdContent.benefits || [])],
      },
      step: 2,
    });
  },

  // Set response sau khi Approve thành công hoặc khi Inspect Approved/Published Job
  setApprovedJob: (job, questions = []) => {
    let jdContent = job?.jdContent;
    if (!jdContent && (job?.description || job?.requirements)) {
      jdContent = {
        intro: job.description || '',
        responsibilities: [],
        mustHave: job.requirements ? [job.requirements] : [],
        niceToHave: [],
        benefits: [],
      };
    } else if (!jdContent) {
      jdContent = { ...initialJdContent };
    }

    set({
      isOpen: true,
      currentJob: job,
      isReadOnly: true,
      questions: questions || [],
      editedJd: {
        intro: jdContent.intro || '',
        responsibilities: [...(jdContent.responsibilities || [])],
        mustHave: [...(jdContent.mustHave || [])],
        niceToHave: [...(jdContent.niceToHave || [])],
        benefits: [...(jdContent.benefits || [])],
      },
      step: 3,
    });
  },

  setQuestions: (questions) => set({ questions }),

  // Cập nhật text Intro
  updateIntro: (intro) => {
    set((state) => ({
      editedJd: {
        ...state.editedJd,
        intro,
      },
    }));
  },

  // Thêm mục vào mảng (responsibilities, mustHave, niceToHave, benefits)
  addBullet: (section, text) => {
    if (!text || !text.trim()) return;
    set((state) => ({
      editedJd: {
        ...state.editedJd,
        [section]: [...(state.editedJd[section] || []), text.trim()],
      },
    }));
  },

  // Cập nhật 1 mục trong mảng
  updateBullet: (section, index, text) => {
    set((state) => {
      const list = [...(state.editedJd[section] || [])];
      list[index] = text;
      return {
        editedJd: {
          ...state.editedJd,
          [section]: list,
        },
      };
    });
  },

  // Xóa 1 mục khỏi mảng
  removeBullet: (section, index) => {
    set((state) => ({
      editedJd: {
        ...state.editedJd,
        [section]: state.editedJd[section].filter((_, i) => i !== index),
      },
    }));
  },

  reset: () => {
    set({
      isOpen: false,
      step: 1,
      currentJob: null,
      editedJd: { ...initialJdContent },
      questions: [],
    });
  },
}));

export default useSmartJdStore;
