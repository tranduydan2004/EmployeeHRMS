import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { loginSchema } from '../schemas/authSchemas';
import { useAuthStore } from '../useAuthStore';
import { Input, Button } from '@/components';
import { LogIn } from 'lucide-react';
import { toast } from '@/components/Toast/useToastStore';

export default function LoginForm() {
  const [isLoading, setIsLoading] = useState(false);
  const login = useAuthStore((state) => state.login);
  const navigate = useNavigate();
  const location = useLocation();

  const from = location.state?.from?.pathname || null;

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: '',
      password: '',
    },
  });

  const onSubmit = async (data) => {
    setIsLoading(true);
    try {
      const res = await login(data.email, data.password);
      toast.success(`Đăng nhập thành công! Chào mừng (${res.role}).`);

      if (from) {
        navigate(from, { replace: true });
      } else if (res.role === 'Candidate') {
        navigate('/my-applications', { replace: true });
      } else if (res.role === 'Interviewer') {
        navigate('/interviews', { replace: true });
      } else {
        navigate('/dashboard', { replace: true });
      }
    } catch (err) {
      console.error('Login error:', err);
      toast.error('Đăng nhập thất bại.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      <Input
        label="Email tài khoản"
        type="email"
        placeholder="admin@hrms.com hoặc candidate@hrms.com"
        error={errors.email?.message}
        required
        {...register('email')}
      />

      <Input
        label="Mật khẩu"
        type="password"
        placeholder="••••••••"
        error={errors.password?.message}
        required
        {...register('password')}
      />

      <div style={{ textAlign: 'right', marginTop: '0.375rem' }}>
        <Link to="/forgot-password" style={{ fontSize: '0.8125rem', fontWeight: 500, color: 'var(--primary-600)' }}>
          Quên mật khẩu?
        </Link>
      </div>

      <div style={{ marginTop: '1.5rem' }}>
        <Button
          type="submit"
          variant="primary"
          isLoading={isLoading}
          icon={LogIn}
          style={{ width: '100%' }}
        >
          Đăng nhập
        </Button>
      </div>

      <div style={{ textAlign: 'center', marginTop: '1.5rem', fontSize: '0.875rem' }}>
        Chưa có tài khoản ứng viên?{' '}
        <Link to="/register" style={{ fontWeight: 600, color: 'var(--primary-600)' }}>
          Đăng ký ngay
        </Link>
      </div>

      <div
        style={{
          marginTop: '1.5rem',
          padding: '0.75rem',
          backgroundColor: 'var(--slate-50)',
          borderRadius: 'var(--radius-md)',
          fontSize: '0.8rem',
          color: 'var(--slate-600)',
          border: '1px dashed var(--slate-300)',
        }}
      >
        <strong>Tài khoản Demo có sẵn:</strong>
        <div>• Admin: <code>admin@hrms.com</code> / <code>Admin@123</code></div>
      </div>
    </form>
  );
}
