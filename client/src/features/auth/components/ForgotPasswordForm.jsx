import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link } from 'react-router-dom';
import { forgotPasswordSchema } from '../schemas/authSchemas';
import { forgotPasswordApi } from '@/api/authApi';
import { Input, Button } from '@/components';
import { Mail, ArrowLeft, CheckCircle, RefreshCw } from 'lucide-react';

const RESEND_COOLDOWN = 60; // seconds

export default function ForgotPasswordForm() {
  const [isLoading, setIsLoading] = useState(false);
  const [isSent, setIsSent] = useState(false);
  const [networkError, setNetworkError] = useState('');
  const [cooldown, setCooldown] = useState(0);

  const {
    register,
    handleSubmit,
    getValues,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: { email: '' },
  });

  const startCooldown = () => {
    setCooldown(RESEND_COOLDOWN);
    const interval = setInterval(() => {
      setCooldown((prev) => {
        if (prev <= 1) {
          clearInterval(interval);
          return 0;
        }
        return prev - 1;
      });
    }, 1000);
  };

  const sendRequest = async (email) => {
    setIsLoading(true);
    setNetworkError('');
    try {
      await forgotPasswordApi(email);
      setIsSent(true);
      startCooldown();
    } catch (err) {
      const status = err.response?.status;
      if (status === 429) {
        // Rate limit — axiosClient interceptor đã hiện toast warning
        // Không cần xử lý thêm
      } else if (!err.response) {
        // Lỗi mạng thật — hiện inline
        setNetworkError('Không thể kết nối đến máy chủ. Vui lòng kiểm tra kết nối mạng.');
      }
      // Các lỗi khác (400, 500...): interceptor đã xử lý
    } finally {
      setIsLoading(false);
    }
  };

  const onSubmit = async (data) => {
    await sendRequest(data.email);
  };

  const handleResend = async () => {
    if (cooldown > 0) return;
    const email = getValues('email');
    if (email) {
      await sendRequest(email);
    }
  };

  // ─── Trạng thái đã gửi thành công ───
  if (isSent) {
    return (
      <div className="forgot-password-success">
        <div className="success-icon-wrapper">
          <CheckCircle size={48} strokeWidth={1.5} />
        </div>
        <h2 style={{ fontSize: '1.25rem', fontWeight: 700, color: 'var(--slate-900)', marginTop: '1rem' }}>
          Kiểm tra hộp thư email
        </h2>
        <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem', marginTop: '0.5rem', lineHeight: 1.6 }}>
          Nếu địa chỉ <strong style={{ color: 'var(--slate-700)' }}>{getValues('email')}</strong> tồn tại trong hệ thống,
          chúng tôi đã gửi link khôi phục mật khẩu. Hãy kiểm tra cả mục <em>Spam/Junk</em>.
        </p>

        <Button
          variant="ghost"
          onClick={handleResend}
          disabled={cooldown > 0 || isLoading}
          isLoading={isLoading}
          icon={RefreshCw}
          style={{ width: '100%', marginTop: '1.5rem' }}
        >
          {cooldown > 0 ? `Gửi lại sau ${cooldown}s` : 'Gửi lại email khôi phục'}
        </Button>

        <div style={{ textAlign: 'center', marginTop: '1.25rem', fontSize: '0.875rem' }}>
          <Link to="/login" style={{ fontWeight: 600, color: 'var(--primary-600)', display: 'inline-flex', alignItems: 'center', gap: '0.375rem' }}>
            <ArrowLeft size={14} /> Quay lại đăng nhập
          </Link>
        </div>
      </div>
    );
  }

  // ─── Form nhập email ───
  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      <div style={{ textAlign: 'center', marginBottom: '1.5rem' }}>
        <div
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            justifyContent: 'center',
            width: '48px',
            height: '48px',
            borderRadius: 'var(--radius-md)',
            background: 'linear-gradient(135deg, var(--warning-light), #fef3c7)',
            color: 'var(--warning-main)',
            marginBottom: '0.75rem',
          }}
        >
          <Mail size={24} />
        </div>
        <h2 style={{ fontSize: '1.25rem', fontWeight: 700, color: 'var(--slate-900)' }}>
          Quên mật khẩu?
        </h2>
        <p style={{ color: 'var(--text-muted)', fontSize: '0.875rem', marginTop: '0.375rem' }}>
          Nhập email đã đăng ký để nhận link đặt lại mật khẩu.
        </p>
      </div>

      {networkError && (
        <div className="form-error-banner" role="alert">
          {networkError}
        </div>
      )}

      <Input
        label="Email tài khoản"
        type="email"
        placeholder="email@example.com"
        error={errors.email?.message}
        required
        {...register('email')}
      />

      <div style={{ marginTop: '1.5rem' }}>
        <Button
          type="submit"
          variant="primary"
          isLoading={isLoading}
          icon={Mail}
          style={{ width: '100%' }}
        >
          Gửi link khôi phục
        </Button>
      </div>

      <div style={{ textAlign: 'center', marginTop: '1.5rem', fontSize: '0.875rem' }}>
        <Link to="/login" style={{ fontWeight: 600, color: 'var(--primary-600)', display: 'inline-flex', alignItems: 'center', gap: '0.375rem' }}>
          <ArrowLeft size={14} /> Quay lại đăng nhập
        </Link>
      </div>
    </form>
  );
}
