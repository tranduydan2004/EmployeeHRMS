import { Outlet } from 'react-router-dom';
import { Sparkles } from 'lucide-react';

export default function AuthLayout() {
  return (
    <div className="auth-layout">
      <div className="auth-card">
        <div className="auth-header">
          <div
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              width: '48px',
              height: '48px',
              borderRadius: 'var(--radius-md)',
              background: 'linear-gradient(135deg, var(--primary-500), var(--primary-700))',
              color: '#fff',
              marginBottom: '0.75rem',
            }}
          >
            <Sparkles size={24} />
          </div>
          <h1>HRMS Enterprise</h1>
          <p>Hệ thống Quản trị Nhân sự & Tuyển dụng Thông minh</p>
        </div>
        <Outlet />
      </div>
    </div>
  );
}
