import { Link } from 'react-router-dom';
import { ShieldAlert, ArrowLeft } from 'lucide-react';
import { Button } from '../components';
import { useAuthStore } from '../features/auth';

export default function UnauthorizedPage() {
  const { role } = useAuthStore();

  const getHomeRoute = () => {
    if (role === 'Candidate') return '/my-applications';
    if (role === 'Interviewer') return '/interviews';
    if (role === 'Employee') return '/my-department';
    return '/dashboard';
  };

  return (
    <div
      style={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        minHeight: '60vh',
        textAlign: 'center',
        padding: '2rem',
      }}
    >
      <div
        style={{
          width: '72px',
          height: '72px',
          borderRadius: 'var(--radius-full)',
          backgroundColor: 'var(--danger-bg)',
          color: 'var(--danger-main)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          marginBottom: '1.5rem',
        }}
      >
        <ShieldAlert size={36} />
      </div>
      <h1 style={{ fontSize: '1.75rem', fontWeight: 700, color: 'var(--slate-900)' }}>
        403 - Quyền Truy Cập Bị Từ Chối
      </h1>
      <p
        style={{
          color: 'var(--text-muted)',
          maxWidth: '480px',
          margin: '0.75rem 0 1.5rem 0',
          fontSize: '0.95rem',
        }}
      >
        Bạn không có quyền hạn cần thiết để xem trang hoặc tài nguyên này với vai trò hiện tại (
        <strong>{role || 'Khách'}</strong>).
      </p>
      <div style={{ display: 'flex', gap: '1rem' }}>
        <Link to={getHomeRoute()}>
          <Button variant="primary" icon={ArrowLeft}>
            Về Trang Phù Hợp
          </Button>
        </Link>
        <Link to="/jobs">
          <Button variant="secondary">Xem Tin Tuyển Dụng</Button>
        </Link>
      </div>
    </div>
  );
}
