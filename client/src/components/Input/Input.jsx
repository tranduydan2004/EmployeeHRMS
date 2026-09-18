export default function Input({
  label,
  error,
  id,
  name,
  type = 'text',
  required = false,
  className = '',
  ...props
}) {
  const inputId = id || name;

  return (
    <div className="form-group">
      {label && (
        <label htmlFor={inputId} className="form-label">
          {label} {required && <span style={{ color: 'var(--danger-main)' }}>*</span>}
        </label>
      )}
      <input
        id={inputId}
        name={name}
        type={type}
        className={`form-control ${error ? 'is-invalid' : ''} ${className}`.trim()}
        {...props}
      />
      {error && <div className="form-error">{error}</div>}
    </div>
  );
}
