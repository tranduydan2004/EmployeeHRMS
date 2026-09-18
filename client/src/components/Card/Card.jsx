export default function Card({ children, title, subtitle, action, className = '', ...props }) {
  return (
    <div className={`card ${className}`.trim()} {...props}>
      {(title || action) && (
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' }}>
          <div>
            {title && <h3 style={{ fontSize: '1.1rem', fontWeight: 600, color: 'var(--slate-900)' }}>{title}</h3>}
            {subtitle && <p style={{ fontSize: '0.825rem', color: 'var(--text-muted)', marginTop: '0.2rem' }}>{subtitle}</p>}
          </div>
          {action && <div>{action}</div>}
        </div>
      )}
      {children}
    </div>
  );
}
