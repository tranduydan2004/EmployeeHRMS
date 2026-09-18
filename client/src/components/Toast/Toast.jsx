import { useToastStore } from './useToastStore';
import { CheckCircle2, AlertCircle, AlertTriangle, Info, X } from 'lucide-react';

export default function ToastContainer() {
  const { toasts, removeToast } = useToastStore();

  if (toasts.length === 0) return null;

  const getIcon = (type) => {
    switch (type) {
      case 'success':
        return <CheckCircle2 size={18} color="var(--success-main)" />;
      case 'error':
        return <AlertCircle size={18} color="var(--danger-main)" />;
      case 'warning':
        return <AlertTriangle size={18} color="var(--warning-main)" />;
      default:
        return <Info size={18} color="var(--info-main)" />;
    }
  };

  return (
    <div className="toast-container" aria-live="polite">
      {toasts.map((t) => (
        <div key={t.id} className={`toast-item ${t.type}`} role="alert">
          {getIcon(t.type)}
          <div style={{ flex: 1 }}>{t.message}</div>
          <button
            type="button"
            onClick={() => removeToast(t.id)}
            style={{
              background: 'transparent',
              border: 'none',
              cursor: 'pointer',
              color: 'var(--slate-400)',
              display: 'flex',
              padding: 0,
            }}
          >
            <X size={16} />
          </button>
        </div>
      ))}
    </div>
  );
}
