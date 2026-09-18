import { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/features/auth';
import { useCandidateMutations, useResumeActions } from '@/features/recruitment/candidates';
import { useCreateApplication } from '@/features/recruitment/applications';
import { Modal, Button, Input } from '@/components';
import { Send, Upload, FileText, CheckCircle2, X, Building2 } from 'lucide-react';

const MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
const ALLOWED_EXTENSIONS = ['.pdf', '.docx'];

export default function ApplyModal({ isOpen, onClose, job, candidate, onApplied }) {
  const navigate = useNavigate();
  const userEmail = useAuthStore((state) => state.userEmail);

  const { createCandidate, updateCandidate } = useCandidateMutations();
  const { uploadResume, downloadResume } = useResumeActions();
  const { createApplication } = useCreateApplication({
    successMessage: job ? `Đã nộp đơn ứng tuyển vị trí "${job.title}" thành công!` : undefined,
  });

  const fileInputRef = useRef(null);

  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [selectedFile, setSelectedFile] = useState(null);
  const [errors, setErrors] = useState({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setFullName(candidate?.fullName || '');
      setEmail(candidate?.email || userEmail || '');
      setPhone(candidate?.phone || '');
      setSelectedFile(null);
      setErrors({});
    }
  }, [isOpen, candidate, userEmail]);

  const handleFileChange = (e) => {
    const file = e.target.files?.[0];
    if (!file) return;

    // Validate size
    if (file.size > MAX_FILE_SIZE) {
      setErrors((prev) => ({
        ...prev,
        file: 'Dung lượng file CV không được vượt quá 5MB.',
      }));
      return;
    }

    // Validate extension
    const ext = '.' + file.name.split('.').pop().toLowerCase();
    if (!ALLOWED_EXTENSIONS.includes(ext)) {
      setErrors((prev) => ({
        ...prev,
        file: 'Chỉ chấp nhận file định dạng PDF (.pdf) hoặc Word (.docx).',
      }));
      return;
    }

    setSelectedFile(file);
    setErrors((prev) => {
      const copy = { ...prev };
      delete copy.file;
      return copy;
    });
  };

  const handleRemoveFile = () => {
    setSelectedFile(null);
    if (fileInputRef.current) fileInputRef.current.value = '';
  };

  const formatFileSize = (bytes) => {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
  };

  const handleSubmit = async (e) => {
    e?.preventDefault();

    const newErrors = {};
    if (!fullName.trim()) newErrors.fullName = 'Vui lòng nhập họ và tên.';
    if (!email.trim()) newErrors.email = 'Vui lòng nhập email.';
    if (!phone.trim()) newErrors.phone = 'Vui lòng nhập số điện thoại liên hệ.';

    // Kiểm tra CV: nếu chưa có CV trong hồ sơ và chưa chọn file mới
    const hasExistingResume = Boolean(candidate?.resumeUrl);
    if (!hasExistingResume && !selectedFile) {
      newErrors.file = 'Vui lòng đính kèm file CV (.pdf hoặc .docx) để nhà tuyển dụng xét duyệt.';
    }

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    setIsSubmitting(true);
    try {
      let candidateId = candidate?.id;

      // 1. Tạo mới hoặc cập nhật hồ sơ ứng viên
      if (!candidateId) {
        const created = await createCandidate({
          fullName: fullName.trim(),
          email: email.trim(),
          phone: phone.trim(),
          resumeUrl: '',
        });
        candidateId = created.id;
      } else if (fullName.trim() !== candidate.fullName || phone.trim() !== candidate.phone) {
        await updateCandidate({
          id: candidateId,
          payload: {
            fullName: fullName.trim(),
            email: email.trim(),
            phone: phone.trim(),
            resumeUrl: candidate.resumeUrl || '',
          },
        });
      }

      // 2. Tải lên file CV mới (nếu có chọn)
      if (selectedFile) {
        await uploadResume({ candidateId, file: selectedFile });
      }

      // 3. Nộp đơn ứng tuyển cho vị trí hiện tại
      await createApplication({
        jobPostingId: job.id,
        candidateId,
      });

      onApplied?.();
      onClose();
      navigate('/my-applications');
    } catch (err) {
      // Lỗi đã được axiosClient interceptor hiển thị toast thông báo thân thiện
      console.warn('Hệ thống ghi nhận lỗi nộp đơn:', err?.response?.data?.error || err.message);
    } finally {
      setIsSubmitting(false);
    }
  };

  if (!isOpen || !job) return null;

  const hasExistingResume = Boolean(candidate?.resumeUrl);

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Nộp Đơn Ứng Tuyển Việc Làm"
      maxWidth="600px"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={isSubmitting}>
            Hủy
          </Button>
          <Button
            variant="primary"
            icon={Send}
            onClick={handleSubmit}
            isLoading={isSubmitting}
          >
            {hasExistingResume ? 'Xác Nhận Nộp Đơn' : 'Tạo Hồ Sơ & Nộp Đơn'}
          </Button>
        </>
      }
    >
      <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
        {/* Job Summary Banner */}
        <div
          style={{
            backgroundColor: 'var(--slate-50)',
            border: '1px solid var(--border-color)',
            borderRadius: 'var(--radius-md)',
            padding: '1rem',
          }}
        >
          <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
            Vị trí ứng tuyển
          </div>
          <div style={{ fontSize: '1.15rem', fontWeight: 600, color: 'var(--slate-900)', marginTop: '0.25rem' }}>
            {job.title}
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', color: 'var(--slate-600)', fontSize: '0.85rem', marginTop: '0.35rem' }}>
            <Building2 size={15} color="var(--primary-600)" />
            <span>Phòng ban: <strong>{job.departmentName}</strong></span>
          </div>
        </div>

        {/* Form Inputs */}
        <div>
          <Input
            label="Họ và tên ứng viên"
            placeholder="Ví dụ: Nguyễn Văn A"
            value={fullName}
            onChange={(e) => setFullName(e.target.value)}
            error={errors.fullName}
            required
          />

          <Input
            label="Email liên hệ"
            type="email"
            placeholder="ungvien@example.com"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            error={errors.email}
            required
          />

          <Input
            label="Số điện thoại liên hệ"
            placeholder="Ví dụ: 0912345678"
            value={phone}
            onChange={(e) => setPhone(e.target.value)}
            error={errors.phone}
            required
          />
        </div>

        {/* CV / Resume Section */}
        <div>
          <label style={{ display: 'block', fontSize: '0.875rem', fontWeight: 600, color: 'var(--slate-800)', marginBottom: '0.5rem' }}>
            Hồ sơ CV / Resume {!hasExistingResume && <span style={{ color: 'var(--danger-main)' }}>*</span>}
          </label>

          {/* Nếu đã có CV trong hệ thống */}
          {hasExistingResume && (
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                padding: '0.75rem 1rem',
                backgroundColor: 'var(--success-bg, #f0fdf4)',
                border: '1px solid var(--success-border, #bbf7d0)',
                borderRadius: 'var(--radius-md)',
                marginBottom: '0.75rem',
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.6rem' }}>
                <CheckCircle2 size={18} color="var(--success-main, #16a34a)" />
                <span style={{ fontSize: '0.875rem', color: 'var(--slate-800)' }}>
                  Hồ sơ của bạn đã có sẵn file CV lưu trữ.
                </span>
              </div>
              <Button
                variant="outline"
                size="sm"
                type="button"
                onClick={() => downloadResume(candidate.id, candidate.fullName)}
              >
                Xem lại CV
              </Button>
            </div>
          )}

          {/* Chọn file CV mới */}
          <input
            type="file"
            ref={fileInputRef}
            onChange={handleFileChange}
            accept=".pdf,.docx,application/pdf,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            style={{ display: 'none' }}
          />

          {!selectedFile ? (
            <div
              onClick={() => fileInputRef.current?.click()}
              style={{
                border: errors.file ? '2px dashed var(--danger-main)' : '2px dashed var(--slate-300)',
                borderRadius: 'var(--radius-md)',
                padding: '1.25rem 1rem',
                textAlign: 'center',
                cursor: 'pointer',
                backgroundColor: 'var(--slate-50)',
                transition: 'all 0.2s',
              }}
              onMouseEnter={(e) => (e.currentTarget.style.borderColor = 'var(--primary-500)')}
              onMouseLeave={(e) => (e.currentTarget.style.borderColor = errors.file ? 'var(--danger-main)' : 'var(--slate-300)')}
            >
              <Upload size={24} color="var(--primary-600)" style={{ margin: '0 auto 0.5rem auto' }} />
              <div style={{ fontSize: '0.9rem', fontWeight: 500, color: 'var(--slate-800)' }}>
                {hasExistingResume ? 'Nhấn để chọn file CV mới thay thế (tuỳ chọn)' : 'Nhấn để tải lên file CV của bạn'}
              </div>
              <div style={{ fontSize: '0.775rem', color: 'var(--text-muted)', marginTop: '0.25rem' }}>
                Hỗ trợ định dạng PDF (.pdf) hoặc Word (.docx) &bull; Dung lượng tối đa 5MB
              </div>
            </div>
          ) : (
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                padding: '0.75rem 1rem',
                border: '1px solid var(--primary-300)',
                borderRadius: 'var(--radius-md)',
                backgroundColor: 'var(--primary-50)',
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', overflow: 'hidden' }}>
                <FileText size={22} color="var(--primary-600)" style={{ flexShrink: 0 }} />
                <div style={{ overflow: 'hidden' }}>
                  <div style={{ fontWeight: 500, fontSize: '0.875rem', color: 'var(--slate-900)', textOverflow: 'ellipsis', overflow: 'hidden', whiteSpace: 'nowrap' }}>
                    {selectedFile.name}
                  </div>
                  <div style={{ fontSize: '0.75rem', color: 'var(--slate-500)' }}>
                    {formatFileSize(selectedFile.size)}
                  </div>
                </div>
              </div>

              <button
                type="button"
                className="btn-icon"
                onClick={handleRemoveFile}
                title="Bỏ chọn file"
                style={{ color: 'var(--slate-400)' }}
              >
                <X size={18} />
              </button>
            </div>
          )}

          {errors.file && (
            <div style={{ color: 'var(--danger-main)', fontSize: '0.775rem', marginTop: '0.35rem' }}>
              {errors.file}
            </div>
          )}
        </div>
      </div>
    </Modal>
  );
}
