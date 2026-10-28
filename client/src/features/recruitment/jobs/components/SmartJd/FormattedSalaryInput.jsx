import { useId } from 'react';
import { Input } from '@/components';

/**
 * Format số với dấu phân cách hàng nghìn vi-VN (VD: 15000000 -> 15.000.000)
 */
export function formatThousands(value) {
  if (value === undefined || value === null || value === '') return '';
  const num = Number(value);
  if (isNaN(num)) return '';
  return num.toLocaleString('vi-VN');
}

/**
 * Trợ giúp đọc tiền trực quan (VD: 15.000.000 VND -> 15 triệu ₫)
 */
export function getFriendlySalaryText(value, currency = 'VND') {
  if (!value || isNaN(value) || value <= 0) return null;
  const num = Number(value);

  if (currency === 'VND') {
    if (num >= 1000000000) {
      const ty = (num / 1000000000).toLocaleString('vi-VN', { maximumFractionDigits: 2 });
      return `≈ ${ty} tỷ ₫`;
    }
    if (num >= 1000000) {
      const trieu = (num / 1000000).toLocaleString('vi-VN', { maximumFractionDigits: 1 });
      return `≈ ${trieu} triệu ₫`;
    }
    return `${num.toLocaleString('vi-VN')} ₫`;
  }

  return `${num.toLocaleString('en-US')} ${currency}`;
}

export default function FormattedSalaryInput({
  value,
  onChange,
  placeholder = 'Nhập số tiền...',
  error,
  currency = 'VND',
  label,
  id,
  name,
  disabled = false,
}) {
  const autoId = useId();
  const inputId = id || name || autoId;

  const displayString = formatThousands(value);
  const friendlyText = getFriendlySalaryText(value, currency);

  const handleChange = (e) => {
    // Chỉ giữ lại các chữ số
    const rawDigits = e.target.value.replace(/\D/g, '');
    if (!rawDigits) {
      onChange(undefined);
    } else {
      onChange(Number(rawDigits));
    }
  };

  return (
    <div style={{ width: '100%' }}>
      {label && (
        <label htmlFor={inputId} className="form-label" style={{ fontWeight: 600 }}>
          {label}
        </label>
      )}
      <Input
        id={inputId}
        name={name}
        type="text"
        inputMode="numeric"
        placeholder={placeholder}
        value={displayString}
        onChange={handleChange}
        error={error}
        disabled={disabled}
      />
      {friendlyText && !error && (
        <div
          style={{
            fontSize: '0.75rem',
            color: 'var(--primary-700)',
            fontWeight: 500,
            marginTop: '0.2rem',
            paddingLeft: '0.25rem',
          }}
        >
          {friendlyText}
        </div>
      )}
    </div>
  );
}
