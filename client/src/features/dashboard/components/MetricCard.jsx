export default function MetricCard({ title, value, icon: Icon, color = 'var(--primary-600)', bg = 'var(--primary-50)', subtitle }) {
  return (
    <div className="stat-card">
      <div className="stat-icon" style={{ backgroundColor: bg, color }}>
        {Icon && <Icon size={24} />}
      </div>
      <div className="stat-content">
        <h4>{title}</h4>
        <div className="stat-number">{value}</div>
        {subtitle && <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginTop: '2px' }}>{subtitle}</div>}
      </div>
    </div>
  );
}
