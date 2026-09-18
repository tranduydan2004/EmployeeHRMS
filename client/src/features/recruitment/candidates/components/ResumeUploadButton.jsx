import { useRef } from 'react';
import { useResumeActions } from '../hooks/useResumeActions';
import { Button } from '@/components';
import { Upload, Download } from 'lucide-react';

export default function ResumeUploadButton({ candidate, size = 'sm' }) {
  const fileInputRef = useRef(null);
  const { uploadResume, isUploading, downloadResume } = useResumeActions();

  const handleFileChange = async (e) => {
    const file = e.target.files?.[0];
    if (!file) return;

    try {
      await uploadResume({ candidateId: candidate.id, file });
    } catch (err) {
      console.error(err);
    } finally {
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  };

  return (
    <div style={{ display: 'inline-flex', alignItems: 'center', gap: '0.35rem' }}>
      <input
        type="file"
        ref={fileInputRef}
        onChange={handleFileChange}
        accept=".pdf,.docx,application/pdf,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        style={{ display: 'none' }}
      />

      <Button
        variant="outline"
        size={size}
        icon={Upload}
        isLoading={isUploading}
        onClick={() => fileInputRef.current?.click()}
        title="Upload CV mới (.pdf, .docx <= 5MB)"
      >
        {candidate.resumeUrl ? 'Cập nhật CV' : 'Tải lên CV'}
      </Button>

      {candidate.resumeUrl && (
        <Button
          variant="secondary"
          size={size}
          icon={Download}
          onClick={() => downloadResume(candidate.id, candidate.fullName)}
          title="Tải về CV"
        >
          Tải CV
        </Button>
      )}
    </div>
  );
}
