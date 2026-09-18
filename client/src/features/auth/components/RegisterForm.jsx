import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, useNavigate } from 'react-router-dom';
import { registerSchema } from '../schemas/authSchemas';
import { useAuthStore } from '../useAuthStore';
import { Input, Button } from '@/components';
import { UserPlus, Mail, RefreshCw } from 'lucide-react';
import { toast } from '@/components/Toast/useToastStore';
import { resendVerificationApi } from '@/api/authApi';

export default function RegisterForm() {
  const [isLoading, setIsLoading] = useState(false);
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [registeredEmail, setRegisteredEmail] = useState('');
  const [isResending, setIsResending] = useState(false);
  const [submitError, setSubmitError] = useState('');
  const registerAuth = useAuthStore((state) => state.register);
  const navigate = useNavigate();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      email: '',
      password: '',
      confirmPassword: '',
    },
  });

  const onSubmit = async (data) => {
    setIsLoading(true);
    setSubmitError('');
    try {
      await registerAuth(data.email, data.password);
      setRegisteredEmail(data.email);
      setIsSubmitted(true);
      toast.success('Đăng ký thành công! Vui lòng kiểm tra email để kích hoạt tài khoản.');
    } catch (err) {
      console.error('Register error:', err);
      const msg = err.response?.data?.error || err.message || 'Đăng ký thất bại. Vui lòng thử lại.';
      setSubmitError(msg);
      toast.error(msg);
    } finally {
      setIsLoading(false);
    }
  };

  const handleResend = async () => {
    if (!registeredEmail) return;
    setIsResending(true);
    try {
      await resendVerificationApi(registeredEmail);
      toast.success('Liên kết xác thực mới đã được gửi vào email của bạn!');
    } catch (err) {
      const msg = err.response?.data?.error || 'Không thể gửi lại email xác thực. Vui lòng thử lại sau.';
      toast.error(msg);
    } finally {
      setIsResending(false);
    }
  };

  if (isSubmitted) {
    return (
      <div style={{ textAlign: 'center', padding: '1rem 0' }}>
        <div
          style={{
            width: '60px',
            height: '60px',
            borderRadius: '50%',
            background: 'var(--primary-50, #eff6ff)',
            color: 'var(--primary-600, #2563eb)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            margin: '0 auto 1.25rem',
          }}
        >
          <Mail size={32} />
        </div>
        <h3 style={{ fontSize: '1.25rem', fontWeight: 600, marginBottom: '0.5rem', color: 'var(--gray-900)' }}>
          Kiểm tra hộp thư của bạn
        </h3>
        <p style={{ color: 'var(--gray-600)', fontSize: '0.875rem', lineHeight: '1.6', marginBottom: '1.5rem' }}>
          Chúng tôi đã gửi email xác thực đến <strong>{registeredEmail}</strong>.<br />
          Vui lòng nhấn vào liên kết trong thư để kích hoạt tài khoản trước khi đăng nhập.
        </p>

        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
          <Button
            type="button"
            variant="primary"
            onClick={() => navigate('/login')}
            style={{ width: '100%' }}
          >
            Đến trang đăng nhập
          </Button>
          <Button
            type="button"
            variant="secondary"
            isLoading={isResending}
            icon={RefreshCw}
            onClick={handleResend}
            style={{ width: '100%' }}
          >
            Chưa nhận được email? Gửi lại
          </Button>
        </div>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      {submitError && (
        <div
          style={{
            padding: '0.75rem 1rem',
            marginBottom: '1rem',
            borderRadius: '0.375rem',
            backgroundColor: 'var(--danger-50, #fef2f2)',
            border: '1px solid var(--danger-200, #fecaca)',
            color: 'var(--danger-700, #b91c1c)',
            fontSize: '0.875rem',
          }}
        >
          {submitError}
        </div>
      )}
      <Input
        label="Email ứng viên"
        type="email"
        placeholder="candidate@example.com"
        error={errors.email?.message}
        required
        {...register('email')}
      />

      <Input
        label="Mật khẩu"
        type="password"
        placeholder="Tối thiểu 6 ký tự"
        error={errors.password?.message}
        required
        {...register('password')}
      />

      <Input
        label="Xác nhận mật khẩu"
        type="password"
        placeholder="Nhập lại mật khẩu"
        error={errors.confirmPassword?.message}
        required
        {...register('confirmPassword')}
      />

      <div style={{ marginTop: '1.5rem' }}>
        <Button
          type="submit"
          variant="primary"
          isLoading={isLoading}
          icon={UserPlus}
          style={{ width: '100%' }}
        >
          Đăng ký tài khoản
        </Button>
      </div>

      <div style={{ textAlign: 'center', marginTop: '1.5rem', fontSize: '0.875rem' }}>
        Đã có tài khoản?{' '}
        <Link to="/login" style={{ fontWeight: 600, color: 'var(--primary-600)' }}>
          Đăng nhập ngay
        </Link>
      </div>
    </form>
  );
}
