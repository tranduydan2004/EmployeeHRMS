import { Link } from 'react-router-dom';
import { FileQuestion, ArrowLeft } from 'lucide-react';
import { Button } from '../components';

export default function NotFoundPage() {
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
          backgroundColor: 'var(--slate-100)',
          color: 'var(--slate-600)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          marginBottom: '1.5rem',
        }}
      >
        <FileQuestion size={36} />
      </div>
      <h1 style={{ fontSize: '1.75rem', fontWeight: 700, color: 'var(--slate-900)' }}>
        404 - Trang Không Tồn Tại
      </h1>
      <p
        style={{
          color: 'var(--text-muted)',
          maxWidth: '450px',
          margin: '0.75rem 0 1.5rem 0',
          fontSize: '0.95rem',
        }}
      >
        Đường dẫn bạn yêu cầu không tồn tại hoặc đã bị di chuyển.
      </p>
      <Link to="/jobs">
        <Button variant="primary" icon={ArrowLeft}>
          Trang Tuyển Dụng
        </Button>
      </Link>
    </div>
  );
}
