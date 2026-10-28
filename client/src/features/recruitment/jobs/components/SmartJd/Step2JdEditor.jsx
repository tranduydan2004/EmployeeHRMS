import { useState, useEffect, useRef } from 'react';
import { useSmartJdStore } from '../../stores/useSmartJdStore';
import { Button, Badge, Input, Select } from '@/components';
import {
  FileText,
  CheckCircle2,
  Plus,
  Trash2,
  Save,
  ArrowLeft,
  Sparkles,
  Building2,
  Briefcase,
  Layers,
  Award,
  HeartHandshake,
  Lock,
  Edit3,
  ChevronUp,
  ChevronDown,
  Sliders,
} from 'lucide-react';
import TagInput from './TagInput';
import FormattedSalaryInput, { formatThousands } from './FormattedSalaryInput';

const LEVELS = [
  { value: 'Intern', label: 'Intern' },
  { value: 'Fresher', label: 'Fresher' },
  { value: 'Junior', label: 'Junior' },
  { value: 'Middle', label: 'Middle' },
  { value: 'Senior', label: 'Senior' },
  { value: 'Lead', label: 'Lead' },
];

const WORK_MODES = [
  { value: 'Onsite', label: 'Tại văn phòng (Onsite)' },
  { value: 'Hybrid', label: 'Linh hoạt (Hybrid)' },
  { value: 'Remote', label: 'Từ xa (Remote)' },
];

const CURRENCIES = [
  { value: 'VND', label: 'VND (₫)' },
  { value: 'USD', label: 'USD ($)' },
  { value: 'EUR', label: 'EUR (€)' },
];

/**
 * Textarea tự động co giãn theo chiều dài nội dung
 */
function AutoResizeTextarea({ value, onChange, placeholder, onKeyDown, style }) {
  const textareaRef = useRef(null);

  useEffect(() => {
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
      textareaRef.current.style.height = `${Math.max(textareaRef.current.scrollHeight, 28)}px`;
    }
  }, [value]);

  return (
    <textarea
      ref={textareaRef}
      value={value}
      onChange={onChange}
      onKeyDown={onKeyDown}
      placeholder={placeholder}
      rows={1}
      style={{
        flex: 1,
        border: 'none',
        outline: 'none',
        background: 'transparent',
        fontSize: '0.875rem',
        color: 'var(--slate-800)',
        lineHeight: 1.5,
        resize: 'none',
        overflow: 'hidden',
        padding: 0,
        margin: 0,
        fontFamily: 'inherit',
        ...style,
      }}
    />
  );
}

function BulletSectionEditor({
  title,
  icon: Icon,
  badgeText,
  items = [],
  onAdd,
  onUpdate,
  onRemove,
  onMove,
  isReadOnly = false,
  placeholder = 'Nhập mục mới...',
}) {
  const [newText, setNewText] = useState('');

  const handleAdd = () => {
    if (newText.trim()) {
      onAdd(newText.trim());
      setNewText('');
    }
  };

  const handleKeyDown = (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleAdd();
    }
  };

  return (
    <div
      style={{
        border: '1px solid var(--border-color)',
        borderRadius: 'var(--radius-md)',
        padding: '1.25rem',
        backgroundColor: 'var(--bg-card)',
        marginBottom: '1rem',
      }}
    >
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: '0.85rem',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          {Icon && <Icon size={18} color="var(--primary-600)" />}
          <h4 style={{ margin: 0, fontSize: '0.95rem', fontWeight: 600, color: 'var(--slate-900)' }}>
            {title}
          </h4>
        </div>
        <span
          style={{
            fontSize: '0.75rem',
            padding: '0.15rem 0.55rem',
            borderRadius: 'var(--radius-full)',
            backgroundColor: 'var(--slate-100)',
            color: 'var(--slate-700)',
            fontWeight: 500,
          }}
        >
          {items.length} {badgeText}
        </span>
      </div>

      {/* Danh sách các bullet points */}
      {isReadOnly ? (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.65rem' }}>
          {items.length === 0 && (
            <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)', fontStyle: 'italic', padding: '0.25rem 0' }}>
              Chưa có mục nào được thiết lập.
            </div>
          )}
          {items.map((item, index) => (
            <div
              key={index}
              style={{
                display: 'flex',
                alignItems: 'flex-start',
                gap: '0.65rem',
                padding: '0.15rem 0',
              }}
            >
              <span
                style={{
                  width: '6px',
                  height: '6px',
                  borderRadius: '50%',
                  backgroundColor: 'var(--primary-600)',
                  marginTop: '0.5rem',
                  flexShrink: 0,
                }}
              />
              <span
                style={{
                  flex: 1,
                  fontSize: '0.875rem',
                  color: 'var(--slate-800)',
                  lineHeight: 1.6,
                }}
              >
                {item}
              </span>
            </div>
          ))}
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
          {items.length === 0 && (
            <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)', fontStyle: 'italic', padding: '0.25rem 0' }}>
              Chưa có mục nào được thiết lập.
            </div>
          )}
          {items.map((item, index) => (
            <div
              key={index}
              style={{
                display: 'flex',
                alignItems: 'flex-start',
                gap: '0.5rem',
                padding: '0.5rem 0.65rem',
                borderRadius: 'var(--radius-sm)',
                backgroundColor: 'var(--slate-50)',
                border: '1px solid var(--slate-200)',
              }}
            >
              <span
                style={{
                  width: '6px',
                  height: '6px',
                  borderRadius: '50%',
                  backgroundColor: 'var(--primary-600)',
                  marginTop: '0.45rem',
                  flexShrink: 0,
                }}
              />
              <AutoResizeTextarea
                value={item}
                onChange={(e) => onUpdate(index, e.target.value)}
                placeholder="Nội dung mục..."
              />
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.15rem', marginTop: '0.1rem' }}>
                {onMove && (
                  <>
                    <button
                      type="button"
                      disabled={index === 0}
                      onClick={() => onMove(index, index - 1)}
                      style={{
                        border: 'none',
                        background: 'transparent',
                        cursor: index === 0 ? 'default' : 'pointer',
                        color: index === 0 ? 'var(--slate-300)' : 'var(--slate-500)',
                        padding: '0.2rem',
                        display: 'inline-flex',
                        borderRadius: 'var(--radius-sm)',
                      }}
                      title="Di chuyển lên"
                    >
                      <ChevronUp size={14} />
                    </button>
                    <button
                      type="button"
                      disabled={index === items.length - 1}
                      onClick={() => onMove(index, index + 1)}
                      style={{
                        border: 'none',
                        background: 'transparent',
                        cursor: index === items.length - 1 ? 'default' : 'pointer',
                        color: index === items.length - 1 ? 'var(--slate-300)' : 'var(--slate-500)',
                        padding: '0.2rem',
                        display: 'inline-flex',
                        borderRadius: 'var(--radius-sm)',
                      }}
                      title="Di chuyển xuống"
                    >
                      <ChevronDown size={14} />
                    </button>
                  </>
                )}
                <button
                  type="button"
                  onClick={() => onRemove(index)}
                  style={{
                    border: 'none',
                    background: 'transparent',
                    cursor: 'pointer',
                    color: 'var(--danger-main)',
                    padding: '0.2rem',
                    display: 'inline-flex',
                    borderRadius: 'var(--radius-sm)',
                  }}
                  title="Xóa mục này"
                >
                  <Trash2 size={14} />
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Ô thêm mục mới */}
      {!isReadOnly && (
        <div style={{ display: 'flex', gap: '0.5rem', marginTop: '0.75rem', alignItems: 'flex-start' }}>
          <div style={{ flex: 1 }}>
            <AutoResizeTextarea
              value={newText}
              onChange={(e) => setNewText(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder={placeholder}
              style={{
                width: '100%',
                padding: '0.45rem 0.75rem',
                borderRadius: 'var(--radius-sm)',
                border: '1px dashed var(--slate-300)',
                backgroundColor: 'var(--bg-card)',
                boxSizing: 'border-box',
              }}
            />
          </div>
          <Button variant="secondary" size="sm" icon={Plus} onClick={handleAdd} style={{ alignSelf: 'flex-start' }}>
            Thêm
          </Button>
        </div>
      )}
    </div>
  );
}

export default function Step2JdEditor({
  onSaveContent,
  onApprove,
  onBack,
  isSaving,
  isApproving,
}) {
  const {
    currentJob,
    editedJd,
    isReadOnly,
    setStep,
    closeModal,
    updateIntro,
    addBullet,
    updateBullet,
    removeBullet,
    moveBullet,
  } = useSmartJdStore();

  if (!currentJob) return null;

  // Local state lưu trữ các metadata được phép tinh chỉnh trực tiếp ở Bước 2
  const [metadata, setMetadata] = useState({
    title: currentJob.title || '',
    level: currentJob.level || 'Junior',
    workMode: currentJob.workMode || 'Onsite',
    coreSkills: currentJob.coreSkills || [],
    yearsOfExperience: currentJob.yearsOfExperience ?? '',
    salaryMin: currentJob.salaryRange?.salaryMin ?? undefined,
    salaryMax: currentJob.salaryRange?.salaryMax ?? undefined,
    currency: currentJob.salaryRange?.currency || 'VND',
  });

  const [isEditingMetadata, setIsEditingMetadata] = useState(false);

  // Đồng bộ metadata khi currentJob cập nhật từ backend
  useEffect(() => {
    if (currentJob) {
      setMetadata({
        title: currentJob.title || '',
        level: currentJob.level || 'Junior',
        workMode: currentJob.workMode || 'Onsite',
        coreSkills: currentJob.coreSkills || [],
        yearsOfExperience: currentJob.yearsOfExperience ?? '',
        salaryMin: currentJob.salaryRange?.salaryMin ?? undefined,
        salaryMax: currentJob.salaryRange?.salaryMax ?? undefined,
        currency: currentJob.salaryRange?.currency || 'VND',
      });
    }
  }, [currentJob]);

  const badgeVariant =
    currentJob.status === 'Published'
      ? 'success'
      : currentJob.status === 'Approved'
      ? 'info'
      : 'warning';

  return (
    <div>
      {/* Header tóm tắt Job */}
      <div
        style={{
          borderBottom: '1px solid var(--border-color)',
          paddingBottom: '1rem',
          marginBottom: '1rem',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'flex-start',
          flexWrap: 'wrap',
          gap: '1rem',
        }}
      >
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.65rem', flexWrap: 'wrap' }}>
            <h2 style={{ fontSize: '1.35rem', fontWeight: 700, margin: 0, color: 'var(--slate-900)' }}>
              {metadata.title || currentJob.title}
            </h2>
            <Badge variant={badgeVariant}>{currentJob.status}</Badge>
            {isReadOnly && (
              <span
                style={{
                  display: 'inline-flex',
                  alignItems: 'center',
                  gap: '0.3rem',
                  fontSize: '0.75rem',
                  color: 'var(--slate-600)',
                  backgroundColor: 'var(--slate-100)',
                  padding: '0.2rem 0.55rem',
                  borderRadius: 'var(--radius-full)',
                  fontWeight: 500,
                }}
              >
                <Lock size={12} /> Chỉ đọc
              </span>
            )}
          </div>
          <div
            style={{
              display: 'flex',
              gap: '1rem',
              marginTop: '0.5rem',
              fontSize: '0.85rem',
              color: 'var(--slate-600)',
              flexWrap: 'wrap',
            }}
          >
            <span style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
              <Building2 size={15} color="var(--primary-600)" />
              {currentJob.departmentName}
            </span>
            <span style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
              <Briefcase size={15} color="var(--slate-400)" />
              {metadata.level} • {metadata.workMode}
            </span>
            {metadata.coreSkills?.length > 0 && (
              <span style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                <Layers size={15} color="var(--slate-400)" />
                {metadata.coreSkills.slice(0, 3).join(', ')}
                {metadata.coreSkills.length > 3 && ` +${metadata.coreSkills.length - 3}`}
              </span>
            )}
          </div>
        </div>

        <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
          {isReadOnly ? (
            <Button
              variant="primary"
              size="sm"
              icon={Sparkles}
              onClick={() => setStep(3)}
              style={{
                background: 'linear-gradient(135deg, var(--primary-600) 0%, #7c3aed 100%)',
                border: 'none',
              }}
            >
              Xem Ngân Hàng Câu Hỏi & Barem
            </Button>
          ) : (
            <>
              <Button
                variant="secondary"
                size="sm"
                icon={Save}
                isLoading={isSaving}
                onClick={() => onSaveContent(editedJd, metadata)}
              >
                Lưu Bản Thảo
              </Button>
              <Button
                variant="primary"
                size="sm"
                icon={CheckCircle2}
                isLoading={isApproving}
                onClick={() => onApprove(editedJd, metadata)}
                style={{
                  background: 'linear-gradient(135deg, #059669 0%, #10b981 100%)',
                  border: 'none',
                  boxShadow: '0 2px 6px rgba(16, 185, 129, 0.3)',
                }}
              >
                {isApproving ? 'Đang duyệt & sinh câu hỏi...' : 'Phê Duyệt & Sinh Question Bank'}
              </Button>
            </>
          )}
        </div>
      </div>

      {/* Thông số tuyển dụng (Metadata Quick Edit Panel) */}
      <div
        style={{
          marginBottom: '1.25rem',
          borderRadius: 'var(--radius-md)',
          border: isEditingMetadata ? '1px solid #c7d2fe' : '1px solid var(--slate-200)',
          backgroundColor: isEditingMetadata ? '#f8fafc' : 'var(--slate-50)',
          padding: '1rem',
          boxShadow: isEditingMetadata ? '0 2px 8px rgba(99, 102, 241, 0.08)' : 'none',
          transition: 'all 0.2s ease',
        }}
      >
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            marginBottom: isEditingMetadata ? '1rem' : '0',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <Sliders size={16} color="var(--primary-600)" />
            <span style={{ fontSize: '0.9rem', fontWeight: 600, color: 'var(--slate-800)' }}>
              Thông số tuyển dụng {isEditingMetadata ? '(Đang chỉnh sửa trực tiếp)' : ''}
            </span>
          </div>
          {!isReadOnly && (
            <Button
              variant={isEditingMetadata ? 'primary' : 'secondary'}
              size="sm"
              icon={Edit3}
              onClick={() => setIsEditingMetadata((prev) => !prev)}
              style={{ fontSize: '0.8rem', padding: '0.25rem 0.65rem' }}
            >
              {isEditingMetadata ? 'Thu gọn' : 'Chỉnh sửa thông số'}
            </Button>
          )}
        </div>

        {/* Form chỉnh sửa thông số mở rộng */}
        {isEditingMetadata && !isReadOnly ? (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.85rem' }}>
            {/* Tiêu đề công việc */}
            <div>
              <label className="form-label" style={{ fontWeight: 600, fontSize: '0.825rem' }}>
                Tiêu đề vị trí tuyển dụng
              </label>
              <Input
                value={metadata.title}
                onChange={(e) => setMetadata({ ...metadata, title: e.target.value })}
                placeholder="VD: Senior .NET Developer"
              />
            </div>

            {/* Grid 3 cột: Cấp bậc, Hình thức, Kinh nghiệm */}
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: '0.75rem' }}>
              <div>
                <label className="form-label" style={{ fontWeight: 600, fontSize: '0.825rem' }}>
                  Cấp bậc (Level)
                </label>
                <Select
                  options={LEVELS}
                  value={metadata.level}
                  onChange={(e) => setMetadata({ ...metadata, level: e.target.value })}
                />
              </div>
              <div>
                <label className="form-label" style={{ fontWeight: 600, fontSize: '0.825rem' }}>
                  Hình thức làm việc
                </label>
                <Select
                  options={WORK_MODES}
                  value={metadata.workMode}
                  onChange={(e) => setMetadata({ ...metadata, workMode: e.target.value })}
                />
              </div>
              <div>
                <label className="form-label" style={{ fontWeight: 600, fontSize: '0.825rem' }}>
                  Kinh nghiệm (Năm)
                </label>
                <Input
                  type="number"
                  min={0}
                  max={50}
                  value={metadata.yearsOfExperience}
                  onChange={(e) => setMetadata({ ...metadata, yearsOfExperience: e.target.value })}
                  placeholder="VD: 3"
                />
              </div>
            </div>

            {/* Dải lương dự kiến với định dạng phân cách */}
            <div>
              <label className="form-label" style={{ fontWeight: 600, fontSize: '0.825rem' }}>
                Dải lương dự kiến (có dấu phân cách đơn vị)
              </label>
              <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'flex-start' }}>
                <div style={{ flex: 1 }}>
                  <FormattedSalaryInput
                    placeholder="Lương tối thiểu"
                    value={metadata.salaryMin}
                    onChange={(val) => setMetadata({ ...metadata, salaryMin: val })}
                    currency={metadata.currency}
                  />
                </div>
                <span style={{ alignSelf: 'center', color: 'var(--text-muted)' }}>-</span>
                <div style={{ flex: 1 }}>
                  <FormattedSalaryInput
                    placeholder="Lương tối đa"
                    value={metadata.salaryMax}
                    onChange={(val) => setMetadata({ ...metadata, salaryMax: val })}
                    currency={metadata.currency}
                  />
                </div>
                <div style={{ width: '100px' }}>
                  <Select
                    options={CURRENCIES}
                    value={metadata.currency}
                    onChange={(e) => setMetadata({ ...metadata, currency: e.target.value })}
                  />
                </div>
              </div>
            </div>

            {/* Kỹ năng cốt lõi */}
            <div>
              <TagInput
                label="Kỹ năng cốt lõi (Core Skills)"
                value={metadata.coreSkills}
                onChange={(skills) => setMetadata({ ...metadata, coreSkills: skills })}
                placeholder="Nhập kỹ năng rồi bấm Enter (VD: C#, React...)"
              />
            </div>
          </div>
        ) : (
          /* Tóm tắt metadata ở trạng thái thu gọn */
          <div
            style={{
              display: 'flex',
              flexWrap: 'wrap',
              gap: '0.75rem',
              alignItems: 'center',
              fontSize: '0.825rem',
              color: 'var(--slate-700)',
            }}
          >
            <span><strong>Cấp bậc:</strong> {metadata.level}</span>
            <span>•</span>
            <span><strong>Hình thức:</strong> {metadata.workMode}</span>
            {metadata.yearsOfExperience !== '' && metadata.yearsOfExperience !== undefined && (
              <>
                <span>•</span>
                <span><strong>Kinh nghiệm:</strong> {metadata.yearsOfExperience} năm</span>
              </>
            )}
            {(metadata.salaryMin || metadata.salaryMax) && (
              <>
                <span>•</span>
                <span style={{ color: '#059669', fontWeight: 600 }}>
                  💰 {metadata.salaryMin ? formatThousands(metadata.salaryMin) : ''}
                  {metadata.salaryMin && metadata.salaryMax ? ' - ' : ''}
                  {metadata.salaryMax ? formatThousands(metadata.salaryMax) : ''} {metadata.currency}
                </span>
              </>
            )}
            {metadata.coreSkills?.length > 0 && (
              <div style={{ display: 'flex', gap: '0.35rem', flexWrap: 'wrap', width: '100%', marginTop: '0.35rem' }}>
                <span style={{ fontWeight: 600, color: 'var(--slate-600)' }}>Kỹ năng:</span>
                {metadata.coreSkills.map((sk, idx) => (
                  <span
                    key={idx}
                    style={{
                      fontSize: '0.75rem',
                      backgroundColor: 'var(--primary-50)',
                      color: 'var(--primary-700)',
                      border: '1px solid var(--primary-200)',
                      borderRadius: 'var(--radius-full)',
                      padding: '0.1rem 0.5rem',
                      fontWeight: 500,
                    }}
                  >
                    {sk}
                  </span>
                ))}
              </div>
            )}
          </div>
        )}
      </div>

      {/* Thông báo hướng dẫn nghiệp vụ */}
      {isReadOnly ? (
        <div
          style={{
            background: '#fffbeb',
            border: '1px solid #fde68a',
            borderRadius: 'var(--radius-md)',
            padding: '0.85rem 1rem',
            fontSize: '0.85rem',
            color: '#92400e',
            marginBottom: '1.25rem',
            display: 'flex',
            alignItems: 'center',
            gap: '0.65rem',
          }}
        >
          <Lock size={18} color="#d97706" style={{ flexShrink: 0 }} />
          <span>
            Tin tuyển dụng đang ở trạng thái <strong>{currentJob.status}</strong>. Nội dung JD đã được đóng băng ở chế độ chỉ đọc để đảm bảo tính đồng bộ với Ngân hàng câu hỏi phỏng vấn đã tạo.
          </span>
        </div>
      ) : (
        <div
          style={{
            background: 'var(--primary-50)',
            border: '1px solid var(--primary-200)',
            borderRadius: 'var(--radius-md)',
            padding: '0.75rem 1rem',
            fontSize: '0.85rem',
            color: 'var(--primary-800)',
            marginBottom: '1.25rem',
            display: 'flex',
            alignItems: 'center',
            gap: '0.5rem',
          }}
        >
          <Sparkles size={16} color="var(--primary-600)" style={{ flexShrink: 0 }} />
          <span>
            Bản thảo JD dưới đây được AI cấu trúc tự động. Bạn có thể sửa trực tiếp nội dung từng mục hoặc thông số tuyển dụng ở trên. Khi bấm <strong>Phê Duyệt</strong>, hệ thống sẽ kích hoạt AI sinh bộ câu hỏi phỏng vấn tình huống kèm barem tương ứng.
          </span>
        </div>
      )}

      {/* Section 1: Intro */}
      <div
        style={{
          border: '1px solid var(--border-color)',
          borderRadius: 'var(--radius-md)',
          padding: '1.25rem',
          backgroundColor: 'var(--bg-card)',
          marginBottom: '1rem',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.65rem' }}>
          <FileText size={18} color="var(--primary-600)" />
          <h4 style={{ margin: 0, fontSize: '0.95rem', fontWeight: 600, color: 'var(--slate-900)' }}>
            Giới thiệu vị trí & Môi trường làm việc (Intro)
          </h4>
        </div>
        {isReadOnly ? (
          <div
            style={{
              padding: '0.75rem 1rem',
              backgroundColor: 'var(--slate-50)',
              borderRadius: 'var(--radius-sm)',
              fontSize: '0.875rem',
              lineHeight: 1.6,
              color: 'var(--slate-800)',
              border: '1px solid var(--slate-200)',
            }}
          >
            {editedJd.intro || 'Chưa có thông tin giới thiệu.'}
          </div>
        ) : (
          <textarea
            className="form-control"
            rows={3}
            value={editedJd.intro || ''}
            onChange={(e) => updateIntro(e.target.value)}
            placeholder="Đoạn văn giới thiệu ngắn về vị trí công việc..."
            style={{ fontSize: '0.875rem', lineHeight: 1.6 }}
          />
        )}
      </div>

      {/* Section 2: Trách nhiệm chính (Responsibilities) */}
      <BulletSectionEditor
        title="Trách nhiệm công việc chính (Responsibilities)"
        icon={Layers}
        badgeText="trách nhiệm"
        items={editedJd.responsibilities}
        onAdd={(text) => addBullet('responsibilities', text)}
        onUpdate={(idx, text) => updateBullet('responsibilities', idx, text)}
        onRemove={(idx) => removeBullet('responsibilities', idx)}
        onMove={(from, to) => moveBullet('responsibilities', from, to)}
        isReadOnly={isReadOnly}
        placeholder="Thêm trách nhiệm công việc..."
      />

      {/* Section 3: Yêu cầu bắt buộc (Must-Have) */}
      <BulletSectionEditor
        title="Yêu cầu bắt buộc (Must-Have)"
        icon={CheckCircle2}
        badgeText="yêu cầu"
        items={editedJd.mustHave}
        onAdd={(text) => addBullet('mustHave', text)}
        onUpdate={(idx, text) => updateBullet('mustHave', idx, text)}
        onRemove={(idx) => removeBullet('mustHave', idx)}
        onMove={(from, to) => moveBullet('mustHave', from, to)}
        isReadOnly={isReadOnly}
        placeholder="Thêm yêu cầu bắt buộc (kỹ năng, kinh nghiệm, bằng cấp)..."
      />

      {/* Section 4: Yêu cầu ưu tiên (Nice-To-Have) */}
      <BulletSectionEditor
        title="Yêu cầu ưu tiên (Nice-To-Have)"
        icon={Award}
        badgeText="ưu tiên"
        items={editedJd.niceToHave}
        onAdd={(text) => addBullet('niceToHave', text)}
        onUpdate={(idx, text) => updateBullet('niceToHave', idx, text)}
        onRemove={(idx) => removeBullet('niceToHave', idx)}
        onMove={(from, to) => moveBullet('niceToHave', from, to)}
        isReadOnly={isReadOnly}
        placeholder="Thêm yêu cầu ưu tiên/điểm cộng..."
      />

      {/* Section 5: Quyền lợi & Phúc lợi (Benefits) */}
      <BulletSectionEditor
        title="Quyền lợi & Chế độ đãi ngộ (Benefits)"
        icon={HeartHandshake}
        badgeText="phúc lợi"
        items={editedJd.benefits}
        onAdd={(text) => addBullet('benefits', text)}
        onUpdate={(idx, text) => updateBullet('benefits', idx, text)}
        onRemove={(idx) => removeBullet('benefits', idx)}
        onMove={(from, to) => moveBullet('benefits', from, to)}
        isReadOnly={isReadOnly}
        placeholder="Thêm quyền lợi, phúc lợi, bảo hiểm..."
      />

      {/* Bottom Actions */}
      <div
        style={{
          marginTop: '1.5rem',
          paddingTop: '1.25rem',
          borderTop: '1px solid var(--border-color)',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
        }}
      >
        {isReadOnly ? (
          <Button variant="secondary" onClick={closeModal}>
            Đóng
          </Button>
        ) : (
          <Button variant="secondary" icon={ArrowLeft} onClick={onBack}>
            Quay Lại Tham Số
          </Button>
        )}

        <div style={{ display: 'flex', gap: '0.75rem' }}>
          {isReadOnly ? (
            <Button
              variant="primary"
              icon={Sparkles}
              onClick={() => setStep(3)}
              style={{
                background: 'linear-gradient(135deg, var(--primary-600) 0%, #7c3aed 100%)',
                border: 'none',
              }}
            >
              Xem Ngân Hàng Câu Hỏi & Barem
            </Button>
          ) : (
            <>
              <Button
                variant="secondary"
                icon={Save}
                isLoading={isSaving}
                onClick={() => onSaveContent(editedJd, metadata)}
              >
                Lưu Bản Thảo
              </Button>
              <Button
                variant="primary"
                icon={CheckCircle2}
                isLoading={isApproving}
                onClick={() => onApprove(editedJd, metadata)}
                style={{
                  background: 'linear-gradient(135deg, #059669 0%, #10b981 100%)',
                  border: 'none',
                  boxShadow: '0 4px 10px rgba(16, 185, 129, 0.3)',
                }}
              >
                {isApproving ? 'Đang duyệt & sinh câu hỏi...' : 'Phê Duyệt & Sinh Question Bank'}
              </Button>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
