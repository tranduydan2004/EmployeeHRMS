export default function Badge({
  children,
  variant = 'neutral',
  icon: Icon,
  className = '',
  style,
}) {
  const variantClass = {
    primary: 'badge-info',
    success: 'badge-success',
    warning: 'badge-warning',
    danger: 'badge-danger',
    info: 'badge-info',
    neutral: 'badge-neutral',
  }[variant] || 'badge-neutral';

  return (
    <span className={`badge ${variantClass} ${className}`.trim()} style={style}>
      {Icon && <Icon size={12} />}
      {children}
    </span>
  );
}
