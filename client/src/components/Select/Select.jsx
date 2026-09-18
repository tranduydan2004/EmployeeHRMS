export default function Select({
  label,
  error,
  id,
  name,
  options = [],
  required = false,
  placeholder,
  className = '',
  containerClassName = '',
  containerStyle = {},
  style = {},
  ...props
}) {
  const selectId = id || name;

  return (
    <div
      className={`form-group ${containerClassName}`.trim()}
      style={{
        ...(!label ? { marginBottom: 0 } : {}),
        ...containerStyle,
      }}
    >
      {label && (
        <label htmlFor={selectId} className="form-label">
          {label} {required && <span style={{ color: 'var(--danger-main)' }}>*</span>}
        </label>
      )}
      <select
        id={selectId}
        name={name}
        className={`form-control ${error ? 'is-invalid' : ''} ${className}`.trim()}
        style={{
          height: '38px',
          boxSizing: 'border-box',
          ...style,
        }}
        {...props}
      >
        {placeholder && <option value="">{placeholder}</option>}
        {options.map((opt) => (
          <option key={opt.value} value={opt.value}>
            {opt.label}
          </option>
        ))}
      </select>
      {error && <div className="form-error">{error}</div>}
    </div>
  );
}
