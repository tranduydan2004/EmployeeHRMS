import { useSmartJdStore } from '../../stores/useSmartJdStore';
import {
  useDraftJdMutation,
  useUpdateJdContentMutation,
  useApproveJdMutation,
} from '../../hooks/useSmartJd';
import { Modal } from '@/components';
import { Sparkles, FileText, CheckCircle2, HelpCircle } from 'lucide-react';
import Step1ParamForm from './Step1ParamForm';
import Step2JdEditor from './Step2JdEditor';
import Step3QuestionBank from './Step3QuestionBank';

export default function SmartJdModal() {
  const { isOpen, step, closeModal, setStep, currentJob, isReadOnly } = useSmartJdStore();

  const draftMutation = useDraftJdMutation();
  const updateContentMutation = useUpdateJdContentMutation();
  const approveMutation = useApproveJdMutation();

  const handleDraftSubmit = (data) => {
    const payload = {
      ...data,
      departmentId: Number(data.departmentId),
      yearsOfExperience:
        data.yearsOfExperience !== undefined ? Number(data.yearsOfExperience) : null,
      salaryMin: data.salaryMin !== undefined ? Number(data.salaryMin) : null,
      salaryMax: data.salaryMax !== undefined ? Number(data.salaryMax) : null,
      additionalNotes: data.additionalNotes || null,
      certifications: data.certifications || [],
    };
    draftMutation.mutate(payload);
  };

  const handleSaveContent = (editedJd) => {
    if (!currentJob) return;
    updateContentMutation.mutate({
      id: currentJob.id,
      payload: {
        intro: editedJd.intro,
        responsibilities: editedJd.responsibilities,
        mustHave: editedJd.mustHave,
        niceToHave: editedJd.niceToHave || [],
        benefits: editedJd.benefits || [],
      },
    });
  };

  const handleApprove = () => {
    if (!currentJob) return;
    approveMutation.mutate(currentJob.id);
  };

  const getModalTitle = () => {
    switch (step) {
      case 1:
        return 'Tạo Tin Tuyển Dụng & Sinh JD Bằng AI';
      case 2:
        return isReadOnly ? 'Xem Nội Dung JD (Chế Độ Chỉ Đọc)' : 'Xem & Tinh Chỉnh Bản Thảo JD';
      case 3:
        return 'Ngân Hàng Câu Hỏi Phỏng Vấn & Barem Chấm Điểm';
      default:
        return 'Smart JD & Question Bank';
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={closeModal}
      title={getModalTitle()}
      maxWidth={step === 1 ? '720px' : '880px'}
    >
      {/* Multi-step progress bar */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          marginBottom: '1.5rem',
          paddingBottom: '1rem',
          borderBottom: '1px solid var(--border-color)',
        }}
      >
        {/* Step 1 Item */}
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '0.5rem',
            cursor: !isReadOnly && step > 1 ? 'pointer' : 'default',
            opacity: step === 1 ? 1 : isReadOnly ? 0.35 : 0.7,
          }}
          onClick={() => !isReadOnly && step > 1 && setStep(1)}
        >
          <div
            style={{
              width: '28px',
              height: '28px',
              borderRadius: 'var(--radius-full)',
              backgroundColor: step === 1 ? 'var(--primary-600)' : step > 1 ? 'var(--success-main)' : 'var(--slate-200)',
              color: '#ffffff',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontSize: '0.8rem',
              fontWeight: 700,
            }}
          >
            {step > 1 ? '✓' : '1'}
          </div>
          <span style={{ fontSize: '0.85rem', fontWeight: step === 1 ? 600 : 500, color: step === 1 ? 'var(--primary-700)' : 'var(--slate-600)' }}>
            1. Tham số tuyển dụng
          </span>
        </div>

        <div style={{ flex: 1, height: '2px', backgroundColor: step > 1 ? 'var(--success-main)' : 'var(--slate-200)', margin: '0 0.75rem' }} />

        {/* Step 2 Item */}
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '0.5rem',
            cursor: currentJob && step !== 2 ? 'pointer' : 'default',
            opacity: step === 2 ? 1 : step > 2 ? 0.7 : 0.4,
          }}
          onClick={() => currentJob && setStep(2)}
        >
          <div
            style={{
              width: '28px',
              height: '28px',
              borderRadius: 'var(--radius-full)',
              backgroundColor: step === 2 ? 'var(--primary-600)' : step > 2 ? 'var(--success-main)' : 'var(--slate-300)',
              color: '#ffffff',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontSize: '0.8rem',
              fontWeight: 700,
            }}
          >
            {step > 2 ? '✓' : '2'}
          </div>
          <span style={{ fontSize: '0.85rem', fontWeight: step === 2 ? 600 : 500, color: step === 2 ? 'var(--primary-700)' : 'var(--slate-600)' }}>
            {isReadOnly ? '2. Nội dung JD (Đã duyệt)' : '2. Tinh chỉnh JD (AI)'}
          </span>
        </div>

        <div style={{ flex: 1, height: '2px', backgroundColor: step > 2 ? 'var(--success-main)' : 'var(--slate-200)', margin: '0 0.75rem' }} />

        {/* Step 3 Item */}
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '0.5rem',
            cursor: currentJob && step !== 3 ? 'pointer' : 'default',
            opacity: step === 3 ? 1 : 0.4,
          }}
          onClick={() => currentJob && setStep(3)}
        >
          <div
            style={{
              width: '28px',
              height: '28px',
              borderRadius: 'var(--radius-full)',
              backgroundColor: step === 3 ? 'var(--success-main)' : 'var(--slate-300)',
              color: '#ffffff',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontSize: '0.8rem',
              fontWeight: 700,
            }}
          >
            {step === 3 ? '3' : '3'}
          </div>
          <span style={{ fontSize: '0.85rem', fontWeight: step === 3 ? 600 : 500, color: step === 3 ? 'var(--success-text)' : 'var(--slate-600)' }}>
            3. Question Bank & Barem
          </span>
        </div>
      </div>

      {/* Step Contents */}
      {step === 1 && (
        <Step1ParamForm
          onSubmit={handleDraftSubmit}
          isLoading={draftMutation.isPending}
        />
      )}

      {step === 2 && (
        <Step2JdEditor
          onSaveContent={handleSaveContent}
          onApprove={handleApprove}
          onBack={() => setStep(1)}
          isSaving={updateContentMutation.isPending}
          isApproving={approveMutation.isPending}
        />
      )}

      {step === 3 && (
        <Step3QuestionBank
          onBackToJd={() => setStep(2)}
          onClose={closeModal}
        />
      )}
    </Modal>
  );
}
