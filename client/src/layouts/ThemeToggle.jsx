import { useTheme } from '../contexts/ThemeContext';
import { Sun, Moon } from 'lucide-react';

export default function ThemeToggle() {
  const { isDark, toggleTheme } = useTheme();

  return (
    <button
      type="button"
      className="btn-icon theme-toggle-btn"
      onClick={toggleTheme}
      aria-label={isDark ? 'Chuyển sang chế độ sáng' : 'Chuyển sang chế độ tối'}
      title={isDark ? 'Chuyển sang giao diện Sáng' : 'Chuyển sang giao diện Tối'}
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        justifyContent: 'center',
        width: '38px',
        height: '38px',
        borderRadius: 'var(--radius-full)',
        border: '1px solid var(--border-color)',
        backgroundColor: 'var(--bg-card)',
        color: isDark ? '#fbbf24' : 'var(--slate-600)',
        cursor: 'pointer',
        transition: 'all var(--transition-normal)',
      }}
    >
      {isDark ? (
        <Sun size={19} style={{ transition: 'transform 0.3s cubic-bezier(0.34, 1.56, 0.64, 1)' }} />
      ) : (
        <Moon size={19} style={{ transition: 'transform 0.3s cubic-bezier(0.34, 1.56, 0.64, 1)' }} />
      )}
    </button>
  );
}
