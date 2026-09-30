import { useState } from 'react';
import { useSmartJdStore } from '../../stores/useSmartJdStore';
import { Button, Badge } from '@/components';
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
} from 'lucide-react';

function BulletSectionEditor({
  title,
  icon: Icon,
  badgeText,
  items = [],
  onAdd,
  onUpdate,
  onRemove,
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
    if (e.key === 'Enter') {
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
                alignItems: 'center',
                gap: '0.5rem',
                padding: '0.4rem 0.6rem',
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
                  flexShrink: 0,
                }}
              />
              <input
                type="text"
                value={item}
                onChange={(e) => onUpdate(index, e.target.value)}
                style={{
                  flex: 1,
                  border: 'none',
                  outline: 'none',
                  background: 'transparent',
                  fontSize: '0.875rem',
                  color: 'var(--slate-800)',
                  lineHeight: 1.4,
                }}
              />
              <button
                type="button"
                onClick={() => onRemove(index)}
                style={{
                  border: 'none',
                  background: 'transparent',
                  cursor: 'pointer',
                  color: 'var(--slate-400)',
                  padding: '0.2rem',
                  display: 'inline-flex',
                  borderRadius: 'var(--radius-sm)',
                }}
                title="Xóa mục này"
              >
                <Trash2 size={14} />
              </button>
            </div>
          ))}
        </div>
      )}

      {/* Ô thêm mục mới (Chỉ hiển thị khi KHÔNG ở chế độ ReadOnly) */}
      {!isReadOnly && (
        <div style={{ display: 'flex', gap: '0.5rem', marginTop: '0.75rem' }}>
          <input
            type="text"
            value={newText}
            onChange={(e) => setNewText(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder={placeholder}
            style={{
              flex: 1,
              padding: '0.4rem 0.75rem',
              borderRadius: 'var(--radius-sm)',
              border: '1px dashed var(--slate-300)',
              backgroundColor: 'var(--bg-card)',
              fontSize: '0.85rem',
              outline: 'none',
            }}
          />
          <Button variant="secondary" size="sm" icon={Plus} onClick={handleAdd}>
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
  } = useSmartJdStore();

  if (!currentJob) return null;

  const isDraft = currentJob.status === 'Draft';
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
          marginBottom: '1.25rem',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'flex-start',
          flexWrap: 'wrap',
          gap: '1rem',
        }}
      >
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.65rem' }}>
            <h2 style={{ fontSize: '1.35rem', fontWeight: 700, margin: 0, color: 'var(--slate-900)' }}>
              {currentJob.title}
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
              {currentJob.level} • {currentJob.workMode}
            </span>
            {currentJob.coreSkills?.length > 0 && (
              <span style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                <Layers size={15} color="var(--slate-400)" />
                {currentJob.coreSkills.slice(0, 3).join(', ')}
                {currentJob.coreSkills.length > 3 && ` +${currentJob.coreSkills.length - 3}`}
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
                onClick={() => onSaveContent(editedJd)}
              >
                Lưu Bản Thảo
              </Button>
              <Button
                variant="primary"
                size="sm"
                icon={CheckCircle2}
                isLoading={isApproving}
                onClick={onApprove}
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
            Tin tuyển dụng đang ở trạng thái <strong>{currentJob.status}</strong>. Nội dung JD đã được đóng băng ở chế độ chỉ đọc để đảm bảo tính đồng bộ với Ngân hàng câu hỏi phỏng vấn đã tạo. Nếu cần thay đổi yêu cầu tuyển dụng, vui lòng tạo tin tuyển dụng mới.
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
            Bản thảo JD dưới đây được AI cấu trúc tự động dựa trên tham số đã nhập. Bạn có thể sửa trực tiếp từng mục. Khi bấm <strong>Phê Duyệt</strong>, hệ thống sẽ kích hoạt AI sinh bộ câu hỏi phỏng vấn tình huống kèm barem chấm điểm tương ứng.
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
                onClick={() => onSaveContent(editedJd)}
              >
                Lưu Bản Thảo
              </Button>
              <Button
                variant="primary"
                icon={CheckCircle2}
                isLoading={isApproving}
                onClick={onApprove}
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
