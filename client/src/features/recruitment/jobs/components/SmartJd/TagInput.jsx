import { useState } from 'react';
import { X, Plus } from 'lucide-react';

export default function TagInput({
  label,
  value = [],
  onChange,
  placeholder = 'Nhập và nhấn Enter hoặc phẩy...',
  suggestions = [],
  error,
  required = false,
  helperText,
}) {
  const [inputValue, setInputValue] = useState('');

  const addTag = (tag) => {
    const trimmed = tag.trim().replace(/^,+|,+$/g, '');
    if (!trimmed) return;
    if (!value.includes(trimmed)) {
      onChange([...value, trimmed]);
    }
    setInputValue('');
  };

  const removeTag = (indexToRemove) => {
    onChange(value.filter((_, idx) => idx !== indexToRemove));
  };

  const handleKeyDown = (e) => {
    if (e.key === 'Enter' || e.key === ',') {
      e.preventDefault();
      addTag(inputValue);
    } else if (e.key === 'Backspace' && !inputValue && value.length > 0) {
      removeTag(value.length - 1);
    }
  };

  const availableSuggestions = suggestions.filter((s) => !value.includes(s));

  return (
    <div className="form-group" style={{ marginBottom: '1rem' }}>
      {label && (
        <label className="form-label" style={{ fontWeight: 600, display: 'flex', justifyContent: 'space-between' }}>
          <span>
            {label} {required && <span style={{ color: 'var(--danger-main)' }}>*</span>}
          </span>
          {value.length > 0 && (
            <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)', fontWeight: 400 }}>
              {value.length} đã chọn
            </span>
          )}
        </label>
      )}

      <div
        className={`tag-input-container ${error ? 'has-error' : ''}`}
        style={{
          display: 'flex',
          flexWrap: 'wrap',
          alignItems: 'center',
          gap: '0.4rem',
          padding: '0.45rem 0.65rem',
          minHeight: '44px',
          border: error ? '1px solid var(--danger-main)' : '1px solid var(--border-color)',
          borderRadius: 'var(--radius-md)',
          backgroundColor: 'var(--bg-card)',
          transition: 'border-color var(--transition-fast)',
        }}
      >
        {value.map((tag, idx) => (
          <span
            key={idx}
            className="smart-tag"
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '0.3rem',
              backgroundColor: 'var(--primary-50)',
              color: 'var(--primary-700)',
              border: '1px solid var(--primary-200)',
              padding: '0.2rem 0.55rem',
              borderRadius: 'var(--radius-full)',
              fontSize: '0.825rem',
              fontWeight: 500,
            }}
          >
            {tag}
            <button
              type="button"
              onClick={() => removeTag(idx)}
              style={{
                border: 'none',
                background: 'transparent',
                cursor: 'pointer',
                padding: 0,
                display: 'inline-flex',
                color: 'var(--primary-500)',
              }}
              title="Xóa tag"
            >
              <X size={13} />
            </button>
          </span>
        ))}

        <input
          type="text"
          value={inputValue}
          onChange={(e) => setInputValue(e.target.value)}
          onKeyDown={handleKeyDown}
          onBlur={() => addTag(inputValue)}
          placeholder={value.length === 0 ? placeholder : 'Thêm...'}
          style={{
            border: 'none',
            outline: 'none',
            flex: '1 1 120px',
            background: 'transparent',
            color: 'var(--text-main)',
            fontSize: '0.9rem',
            padding: '0.25rem 0.2rem',
          }}
        />
      </div>

      {error && <div className="form-error" style={{ color: 'var(--danger-main)', fontSize: '0.8rem', marginTop: '0.25rem' }}>{error}</div>}
      {helperText && !error && (
        <div style={{ fontSize: '0.78rem', color: 'var(--text-muted)', marginTop: '0.25rem' }}>
          {helperText}
        </div>
      )}

      {/* Gợi ý tags nhanh */}
      {availableSuggestions.length > 0 && (
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', flexWrap: 'wrap', marginTop: '0.45rem' }}>
          <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)', fontWeight: 500 }}>Gợi ý:</span>
          {availableSuggestions.slice(0, 6).map((item, idx) => (
            <button
              key={idx}
              type="button"
              onClick={() => addTag(item)}
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: '0.2rem',
                border: '1px dashed var(--slate-300)',
                background: 'var(--slate-50)',
                color: 'var(--slate-600)',
                fontSize: '0.75rem',
                padding: '0.15rem 0.45rem',
                borderRadius: 'var(--radius-sm)',
                cursor: 'pointer',
              }}
            >
              <Plus size={11} /> {item}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
