import { useState, useEffect, useRef } from 'react';
import { useSearchParams, useNavigate, Link } from 'react-router-dom';
import { verifyEmailApi, resendVerificationApi } from '@/api/authApi';
import { Button, Input } from '@/components';
import { CheckCircle2, AlertCircle, Mail, Loader2, ArrowRight, RefreshCw } from 'lucide-react';
import { toast } from '@/components/Toast/useToastStore';

export default function VerifyEmail() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const token = searchParams.get('token');

  const [status, setStatus] = useState('idle'); // 'idle' | 'loading' | 'success' | 'error' | 'no-token'
  const [errorMessage, setErrorMessage] = useState('');
  const [resendEmail, setResendEmail] = useState('');
  const [isResending, setIsResending] = useState(false);
  const [resendSuccess, setResendSuccess] = useState(false);
  const hasTriggeredRef = useRef(false);

  useEffect(() => {
    if (!token) {
      setStatus('no-token');
      return;
    }

    if (hasTriggeredRef.current) return;
    hasTriggeredRef.current = true;

    const performVerification = async () => {
      setStatus('loading');
      try {
        await verifyEmailApi(token);
        setStatus('success');
      } catch (err) {
        console.error('Email verification error:', err);
        const msg =
          err.response?.data?.error ||
          err.response?.data?.message ||
          'Liên kết xác thực không hợp lệ hoặc đã hết hạn. Vui lòng yêu cầu liên kết mới.';
        setErrorMessage(msg);
        setStatus('error');
      }
    };

    performVerification();
  }, [token]);

  const handleResend = async (e) => {
    e.preventDefault();
    if (!resendEmail || !resendEmail.trim()) {
      toast.error('Vui lòng nhập địa chỉ email.');
      return;
    }

    setIsResending(true);
    setResendSuccess(false);
    try {
      await resendVerificationApi(resendEmail.trim());
      setResendSuccess(true);
      toast.success('Nếu email tồn tại, liên kết xác thực mới đã được gửi!');
    } catch (err) {
      const msg = err.response?.data?.error || 'Không thể gửi lại email xác thực. Vui lòng thử lại sau.';
      toast.error(msg);
    } finally {
      setIsResending(false);
    }
  };

  return (
    <div style={{ padding: '1rem 0' }}>
      {/* 1. Loading State */}
      {status === 'loading' && (
        <div style={{ textAlign: 'center', padding: '2rem 1rem' }}>
          <div
            style={{
              display: 'inline-flex',
              padding: '1rem',
              borderRadius: '50%',
              backgroundColor: 'var(--primary-50, #eff6ff)',
              color: 'var(--primary-600, #2563eb)',
              marginBottom: '1rem',
            }}
          >
            <Loader2 size={36} className="animate-spin" />
          </div>
          <h3 style={{ fontSize: '1.25rem', fontWeight: 600, marginBottom: '0.5rem', color: 'var(--gray-900)' }}>
            Đang xác thực email của bạn...
          </h3>
          <p style={{ color: 'var(--gray-500)', fontSize: '0.875rem' }}>
            Vui lòng chờ trong giây lát, hệ thống đang kiểm tra liên kết xác thực.
          </p>
        </div>
      )}

      {/* 2. Success State */}
      {status === 'success' && (
        <div style={{ textAlign: 'center', padding: '1rem 0' }}>
          <div
            style={{
              width: '64px',
              height: '64px',
              borderRadius: '50%',
              backgroundColor: 'var(--success-50, #ecfdf5)',
              color: 'var(--success-600, #16a34a)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              margin: '0 auto 1.25rem',
            }}
          >
            <CheckCircle2 size={36} />
          </div>
          <h3 style={{ fontSize: '1.35rem', fontWeight: 700, marginBottom: '0.5rem', color: 'var(--gray-900)' }}>
            Xác thực thành công!
          </h3>
          <p style={{ color: 'var(--gray-600)', fontSize: '0.925rem', lineHeight: '1.6', marginBottom: '1.75rem' }}>
            Địa chỉ email của bạn đã được xác minh thành công. Tài khoản hiện đã sẵn sàng để đăng nhập.
          </p>
          <Button
            type="button"
            variant="primary"
            icon={ArrowRight}
            onClick={() => navigate('/login')}
            style={{ width: '100%', padding: '0.75rem' }}
          >
            Đăng nhập ngay
          </Button>
        </div>
      )}

      {/* 3. Error or No-Token State */}
      {(status === 'error' || status === 'no-token') && (
        <div>
          <div style={{ textAlign: 'center', marginBottom: '1.5rem' }}>
            <div
              style={{
                width: '64px',
                height: '64px',
                borderRadius: '50%',
                backgroundColor: 'var(--danger-50, #fef2f2)',
                color: 'var(--danger-600, #dc2626)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                margin: '0 auto 1.25rem',
              }}
            >
              <AlertCircle size={36} />
            </div>
            <h3 style={{ fontSize: '1.25rem', fontWeight: 700, marginBottom: '0.5rem', color: 'var(--gray-900)' }}>
              {status === 'no-token' ? 'Thiếu thông tin xác thực' : 'Xác thực không thành công'}
            </h3>
            <p style={{ color: 'var(--gray-600)', fontSize: '0.875rem', lineHeight: '1.6' }}>
              {status === 'no-token'
                ? 'Không tìm thấy mã xác thực hợp lệ trên đường dẫn. Vui lòng kiểm tra lại liên kết trong email của bạn.'
                : errorMessage}
            </p>
          </div>

          <div
            style={{
              borderTop: '1px solid var(--gray-200, #e5e7eb)',
              paddingTop: '1.5rem',
              marginTop: '1.5rem',
            }}
          >
            <h4 style={{ fontSize: '0.95rem', fontWeight: 600, marginBottom: '0.5rem', color: 'var(--gray-800)' }}>
              Yêu cầu gửi lại liên kết mới
            </h4>
            <p style={{ fontSize: '0.825rem', color: 'var(--gray-500)', marginBottom: '1rem' }}>
              Nhập email tài khoản của bạn để nhận liên kết xác thực mới:
            </p>

            <form onSubmit={handleResend} style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              <Input
                label="Địa chỉ email"
                type="email"
                placeholder="name@example.com"
                value={resendEmail}
                onChange={(e) => setResendEmail(e.target.value)}
                required
              />

              {resendSuccess && (
                <div
                  style={{
                    padding: '0.75rem 1rem',
                    borderRadius: '0.375rem',
                    backgroundColor: 'var(--success-50, #ecfdf5)',
                    border: '1px solid var(--success-200, #bbf7d0)',
                    color: 'var(--success-700, #15803d)',
                    fontSize: '0.85rem',
                  }}
                >
                  Liên kết xác minh mới đã được gửi! Vui lòng kiểm tra hòm thư của bạn.
                </div>
              )}

              <Button
                type="submit"
                variant="secondary"
                isLoading={isResending}
                icon={RefreshCw}
                style={{ width: '100%' }}
              >
                Gửi lại email xác thực
              </Button>
            </form>
          </div>

          <div style={{ textAlign: 'center', marginTop: '1.5rem', fontSize: '0.875rem' }}>
            <Link to="/login" style={{ fontWeight: 600, color: 'var(--primary-600)' }}>
              ← Quay lại trang Đăng nhập
            </Link>
          </div>
        </div>
      )}
    </div>
  );
}
