import { useState, useMemo } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, useSearchParams, useNavigate } from 'react-router-dom';
import { resetPasswordSchema } from '../schemas/authSchemas';
import { resetPasswordApi } from '@/api/authApi';
import { Input, Button } from '@/components';
import { KeyRound, Eye, EyeOff, ArrowLeft, CheckCircle, AlertTriangle } from 'lucide-react';

export default function ResetPasswordForm() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const tokenFromUrl = searchParams.get('token') || '';
  const emailFromUrl = searchParams.get('email') || '';

  const [isLoading, setIsLoading] = useState(false);
  const [isSuccess, setIsSuccess] = useState(false);
  const [formError, setFormError] = useState('');
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

  // Kiểm tra URL params thiếu
  const isMissingParams = useMemo(() => !tokenFromUrl || !emailFromUrl, [tokenFromUrl, emailFromUrl]);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: {
      email: emailFromUrl,
      token: tokenFromUrl,
      newPassword: '',
      confirmPassword: '',
    },
  });

  const onSubmit = async (data) => {
    setIsLoading(true);
    setFormError('');
    try {
      await resetPasswordApi({
        email: data.email,
        token: data.token,
        newPassword: data.newPassword,
      });
      setIsSuccess(true);
    } catch (err) {
      const status = err.response?.status;
      const backendMsg = err.response?.data?.error;

      if (status === 400 || status === 401) {
        // Token hết hạn / sai — hiện INLINE, không dùng toast global
        setFormError(backendMsg || 'Token không hợp lệ hoặc đã hết hạn. Vui lòng yêu cầu link mới.');
      } else if (!err.response) {
        setFormError('Không thể kết nối đến máy chủ. Vui lòng kiểm tra kết nối mạng.');
      }
      // 429, 500: axiosClient interceptor đã xử lý
    } finally {
      setIsLoading(false);
    }
  };

  // ─── Trạng thái: Thiếu params ───
  if (isMissingParams) {
    return (
      <div style={{ textAlign: 'center' }}>
        <div className="error-icon-wrapper">
          <AlertTriangle size={48} strokeWidth={1.5} />
        </div>
        <h2 style={{ fontSize: '1.25rem', fontWeight: 700, color: 'var(--slate-900)', marginTop: '1rem' }}>
          Link không hợp lệ
        </h2>
        <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem', marginTop: '0.5rem', lineHeight: 1.6 }}>
          Đường dẫn đặt lại mật khẩu thiếu thông tin cần thiết. Vui lòng sử dụng link trong email hoặc yêu cầu link mới.
        </p>
        <div style={{ marginTop: '1.5rem', display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
          <Link to="/forgot-password">
            <Button variant="primary" style={{ width: '100%' }}>
              Yêu cầu link mới
            </Button>
          </Link>
          <Link to="/login" style={{ fontWeight: 600, color: 'var(--primary-600)', fontSize: '0.875rem', display: 'inline-flex', alignItems: 'center', justifyContent: 'center', gap: '0.375rem' }}>
            <ArrowLeft size={14} /> Quay lại đăng nhập
          </Link>
        </div>
      </div>
    );
  }

  // ─── Trạng thái: Thành công ───
  if (isSuccess) {
    return (
      <div style={{ textAlign: 'center' }}>
        <div className="success-icon-wrapper">
          <CheckCircle size={48} strokeWidth={1.5} />
        </div>
        <h2 style={{ fontSize: '1.25rem', fontWeight: 700, color: 'var(--slate-900)', marginTop: '1rem' }}>
          Đặt lại mật khẩu thành công!
        </h2>
        <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem', marginTop: '0.5rem', lineHeight: 1.6 }}>
          Mật khẩu đã được cập nhật. Bạn có thể đăng nhập bằng mật khẩu mới.
        </p>
        <div style={{ marginTop: '1.5rem' }}>
          <Button
            variant="primary"
            onClick={() => navigate('/login', { replace: true })}
            style={{ width: '100%' }}
          >
            Đăng nhập ngay
          </Button>
        </div>
      </div>
    );
  }

  // ─── Form đặt lại mật khẩu ───
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
            background: 'linear-gradient(135deg, var(--primary-500), var(--primary-700))',
            color: '#fff',
            marginBottom: '0.75rem',
          }}
        >
          <KeyRound size={24} />
        </div>
        <h2 style={{ fontSize: '1.25rem', fontWeight: 700, color: 'var(--slate-900)' }}>
          Đặt lại mật khẩu
        </h2>
        <p style={{ color: 'var(--text-muted)', fontSize: '0.875rem', marginTop: '0.375rem' }}>
          Nhập mật khẩu mới cho tài khoản <strong style={{ color: 'var(--slate-700)' }}>{emailFromUrl}</strong>
        </p>
      </div>

      {/* INLINE error for token invalid/expired — KHÔNG dùng Toast */}
      {formError && (
        <div className="form-error-banner" role="alert">
          <AlertTriangle size={16} style={{ flexShrink: 0 }} />
          <span>{formError}</span>
          <Link to="/forgot-password" style={{ fontWeight: 600, color: 'var(--primary-600)', fontSize: '0.8125rem', marginLeft: 'auto', whiteSpace: 'nowrap' }}>
            Yêu cầu link mới
          </Link>
        </div>
      )}

      {/* Hidden fields */}
      <input type="hidden" {...register('email')} />
      <input type="hidden" {...register('token')} />

      <div style={{ position: 'relative' }}>
        <Input
          label="Mật khẩu mới"
          type={showNewPassword ? 'text' : 'password'}
          placeholder="Tối thiểu 6 ký tự"
          error={errors.newPassword?.message}
          required
          {...register('newPassword')}
        />
        <button
          type="button"
          onClick={() => setShowNewPassword((prev) => !prev)}
          className="password-toggle-btn"
          aria-label={showNewPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
        >
          {showNewPassword ? <EyeOff size={18} /> : <Eye size={18} />}
        </button>
      </div>

      <div style={{ position: 'relative' }}>
        <Input
          label="Xác nhận mật khẩu"
          type={showConfirmPassword ? 'text' : 'password'}
          placeholder="Nhập lại mật khẩu mới"
          error={errors.confirmPassword?.message}
          required
          {...register('confirmPassword')}
        />
        <button
          type="button"
          onClick={() => setShowConfirmPassword((prev) => !prev)}
          className="password-toggle-btn"
          aria-label={showConfirmPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
        >
          {showConfirmPassword ? <EyeOff size={18} /> : <Eye size={18} />}
        </button>
      </div>

      <div style={{ marginTop: '1.5rem' }}>
        <Button
          type="submit"
          variant="primary"
          isLoading={isLoading}
          icon={KeyRound}
          style={{ width: '100%' }}
        >
          Đặt lại mật khẩu
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
