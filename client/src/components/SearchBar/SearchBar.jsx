import { useState, useEffect, useRef } from 'react';
import { Search, X } from 'lucide-react';

export default function SearchBar({
  value = '',
  onChange,
  placeholder = 'Tìm kiếm...',
  className = '',
  style = {},
}) {
  const [inputValue, setInputValue] = useState(value);
  const debounceTimerRef = useRef(null);

  // Sync state if external value changes (e.g., URL navigation)
  useEffect(() => {
    setInputValue(value || '');
  }, [value]);

  const handleChange = (e) => {
    const nextVal = e.target.value;
    setInputValue(nextVal);

    if (debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current);
    }

    debounceTimerRef.current = setTimeout(() => {
      onChange?.(nextVal);
    }, 400);
  };

  const handleClear = () => {
    if (debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current);
    }
    setInputValue('');
    onChange?.('');
  };

  useEffect(() => {
    return () => {
      if (debounceTimerRef.current) {
        clearTimeout(debounceTimerRef.current);
      }
    };
  }, []);

  return (
    <div
      className={`search-bar-container ${className}`.trim()}
      style={{
        position: 'relative',
        display: 'flex',
        alignItems: 'center',
        width: '100%',
        maxWidth: '360px',
        ...style,
      }}
    >
      <Search
        size={18}
        style={{
          position: 'absolute',
          left: '0.85rem',
          color: 'var(--slate-400)',
          pointerEvents: 'none',
        }}
      />
      <input
        type="text"
        value={inputValue}
        onChange={handleChange}
        placeholder={placeholder}
        aria-label={placeholder}
        className="form-control"
        style={{
          paddingLeft: '2.4rem',
          paddingRight: inputValue ? '2.2rem' : '0.85rem',
          height: '38px',
          boxSizing: 'border-box',
          borderRadius: 'var(--radius-md)',
          fontSize: '0.875rem',
          width: '100%',
        }}
      />
      {inputValue && (
        <button
          type="button"
          onClick={handleClear}
          title="Xóa tìm kiếm"
          aria-label="Xóa tìm kiếm"
          style={{
            position: 'absolute',
            right: '0.65rem',
            background: 'none',
            border: 'none',
            cursor: 'pointer',
            padding: '4px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'var(--slate-400)',
            borderRadius: '50%',
            transition: 'color 0.15s ease',
          }}
        >
          <X size={15} />
        </button>
      )}
    </div>
  );
}
