import { useMyProfile } from '../hooks/useEmployees';
import { Card, Badge, Skeleton } from '@/components';
import {
  User,
  Mail,
  Briefcase,
  Building2,
  Calendar,
  DollarSign,
  ShieldCheck,
  Clock,
  IdCard,
} from 'lucide-react';

export default function MyProfilePage() {
  const { data: profile, isLoading, error } = useMyProfile();

  const getStatusVariant = (status) => {
    switch (status) {
      case 'Probation':
        return 'warning';
      case 'Active':
        return 'success';
      case 'OnLeave':
        return 'info';
      case 'Terminated':
      case 'Resigned':
        return 'danger';
      default:
        return 'secondary';
    }
  };

  const getStatusText = (status) => {
    switch (status) {
      case 'Probation':
        return 'Thử việc';
      case 'Active':
        return 'Đang làm việc';
      case 'OnLeave':
        return 'Nghỉ phép';
      case 'Terminated':
      case 'Resigned':
        return 'Đã thôi việc';
      default:
        return status || 'N/A';
    }
  };

  if (isLoading) {
    return (
      <div style={{ maxWidth: '960px', margin: '0 auto', display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
        <div className="page-header">
          <Skeleton height="36px" width="300px" />
          <Skeleton height="20px" width="200px" style={{ marginTop: '0.5rem' }} />
        </div>
        <Card>
          <div style={{ display: 'flex', gap: '1.5rem', alignItems: 'center' }}>
            <Skeleton height="80px" width="80px" borderRadius="50%" />
            <div style={{ flex: 1 }}>
              <Skeleton height="28px" width="220px" />
              <Skeleton height="18px" width="160px" style={{ marginTop: '0.5rem' }} />
            </div>
          </div>
        </Card>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: '1.25rem' }}>
          <Card><Skeleton height="140px" /></Card>
          <Card><Skeleton height="140px" /></Card>
        </div>
      </div>
    );
  }

  if (error || !profile) {
    return (
      <div style={{ maxWidth: '960px', margin: '0 auto' }}>
        <Card style={{ textAlign: 'center', padding: '3.5rem 1.5rem' }}>
          <User size={48} color="var(--slate-400)" style={{ margin: '0 auto 1rem auto' }} />
          <h3 style={{ fontSize: '1.25rem', fontWeight: 600, color: 'var(--slate-800)' }}>
            Chưa có thông tin nhân viên
          </h3>
          <p style={{ color: 'var(--text-muted)', marginTop: '0.5rem' }}>
            Tài khoản này hiện chưa được liên kết với hồ sơ nhân viên trong hệ thống.
          </p>
        </Card>
      </div>
    );
  }

  const initials = profile.fullName
    ? profile.fullName
        .split(' ')
        .filter(Boolean)
        .slice(-2)
        .map((n) => n[0])
        .join('')
        .toUpperCase()
    : 'NV';

  return (
    <div style={{ maxWidth: '960px', margin: '0 auto', display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
      {/* Header Banner */}
      <div className="page-header" style={{ marginBottom: 0 }}>
        <div className="page-title-group">
          <h1>Hồ Sơ Của Tôi</h1>
          <p>Xem thông tin chi tiết cá nhân, phòng ban và chế độ đãi ngộ</p>
        </div>
      </div>

      {/* Hero Profile Card */}
      <Card
        style={{
          background: 'linear-gradient(135deg, rgba(255, 255, 255, 0.95), rgba(240, 244, 255, 0.85))',
          borderColor: 'var(--primary-200)',
          boxShadow: '0 8px 24px rgba(99, 102, 241, 0.08)',
        }}
      >
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            flexWrap: 'wrap',
            gap: '1.5rem',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '1.25rem' }}>
            <div
              style={{
                width: '76px',
                height: '76px',
                borderRadius: '50%',
                background: 'linear-gradient(135deg, var(--primary-600), var(--primary-800))',
                color: '#fff',
                fontSize: '1.6rem',
                fontWeight: 700,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                boxShadow: '0 4px 12px rgba(79, 70, 229, 0.3)',
              }}
            >
              {initials}
            </div>

            <div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', flexWrap: 'wrap' }}>
                <h2 style={{ fontSize: '1.5rem', fontWeight: 700, color: 'var(--slate-900)' }}>
                  {profile.fullName}
                </h2>
                <Badge variant={getStatusVariant(profile.status)}>
                  {getStatusText(profile.status)}
                </Badge>
              </div>

              <div
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: '1.25rem',
                  marginTop: '0.5rem',
                  fontSize: '0.9rem',
                  color: 'var(--slate-600)',
                  flexWrap: 'wrap',
                }}
              >
                <span style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                  <Briefcase size={16} color="var(--primary-600)" />
                  {profile.position}
                </span>
                <span style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                  <Building2 size={16} color="var(--primary-600)" />
                  {profile.departmentName}
                </span>
                <span style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                  <IdCard size={16} color="var(--slate-500)" />
                  Mã NV: #{profile.employeeId}
                </span>
              </div>
            </div>
          </div>

          <div
            style={{
              padding: '0.85rem 1.25rem',
              backgroundColor: 'white',
              borderRadius: 'var(--radius-md)',
              border: '1px solid var(--primary-100)',
              boxShadow: '0 2px 8px rgba(0, 0, 0, 0.04)',
              textAlign: 'right',
            }}
          >
            <div style={{ fontSize: '0.75rem', color: 'var(--slate-500)', textTransform: 'uppercase', letterSpacing: '0.5px' }}>
              Mức Lương Hiện Tại
            </div>
            <div style={{ fontSize: '1.4rem', fontWeight: 700, color: 'var(--primary-700)', marginTop: '0.2rem' }}>
              {new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(profile.salary)}
            </div>
          </div>
        </div>
      </Card>

      {/* Details Grid */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(420px, 1fr))', gap: '1.5rem' }}>
        {/* Contact & Personal Info */}
        <Card>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '1.25rem' }}>
            <User size={18} color="var(--primary-600)" />
            <h3 style={{ fontSize: '1.05rem', fontWeight: 600, color: 'var(--slate-900)' }}>
              Thông Tin Liên Hệ & Định Danh
            </h3>
          </div>

          <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', paddingBottom: '0.75rem', borderBottom: '1px solid var(--border-color)' }}>
              <span style={{ color: 'var(--slate-500)', fontSize: '0.875rem', display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
                <Mail size={15} /> Email công việc
              </span>
              <span style={{ fontWeight: 500, color: 'var(--slate-800)', fontSize: '0.875rem' }}>
                {profile.email}
              </span>
            </div>

            <div style={{ display: 'flex', justifyContent: 'space-between', paddingBottom: '0.75rem', borderBottom: '1px solid var(--border-color)' }}>
              <span style={{ color: 'var(--slate-500)', fontSize: '0.875rem', display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
                <IdCard size={15} /> Mã số nhân viên
              </span>
              <span style={{ fontWeight: 500, color: 'var(--slate-800)', fontSize: '0.875rem' }}>
                #{profile.employeeId}
              </span>
            </div>

            <div style={{ display: 'flex', justifyContent: 'space-between', paddingBottom: '0.75rem', borderBottom: '1px solid var(--border-color)' }}>
              <span style={{ color: 'var(--slate-500)', fontSize: '0.875rem', display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
                <Calendar size={15} /> Ngày vào làm
              </span>
              <span style={{ fontWeight: 500, color: 'var(--slate-800)', fontSize: '0.875rem' }}>
                {profile.joinDate ? new Date(profile.joinDate).toLocaleDateString('vi-VN') : 'N/A'}
              </span>
            </div>

            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <span style={{ color: 'var(--slate-500)', fontSize: '0.875rem', display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
                <Clock size={15} /> Ngày tạo hồ sơ
              </span>
              <span style={{ fontWeight: 500, color: 'var(--slate-800)', fontSize: '0.875rem' }}>
                {profile.createdAt ? new Date(profile.createdAt).toLocaleDateString('vi-VN') : 'N/A'}
              </span>
            </div>
          </div>
        </Card>

        {/* Job & Work Details */}
        <Card>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '1.25rem' }}>
            <Building2 size={18} color="var(--primary-600)" />
            <h3 style={{ fontSize: '1.05rem', fontWeight: 600, color: 'var(--slate-900)' }}>
              Vị Trí & Tổ Chức
            </h3>
          </div>

          <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', paddingBottom: '0.75rem', borderBottom: '1px solid var(--border-color)' }}>
              <span style={{ color: 'var(--slate-500)', fontSize: '0.875rem', display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
                <Building2 size={15} /> Phòng ban
              </span>
              <span style={{ fontWeight: 600, color: 'var(--primary-700)', fontSize: '0.875rem' }}>
                {profile.departmentName}
              </span>
            </div>

            <div style={{ display: 'flex', justifyContent: 'space-between', paddingBottom: '0.75rem', borderBottom: '1px solid var(--border-color)' }}>
              <span style={{ color: 'var(--slate-500)', fontSize: '0.875rem', display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
                <Briefcase size={15} /> Chức danh / Vị trí
              </span>
              <span style={{ fontWeight: 500, color: 'var(--slate-800)', fontSize: '0.875rem' }}>
                {profile.position}
              </span>
            </div>

            <div style={{ display: 'flex', justifyContent: 'space-between', paddingBottom: '0.75rem', borderBottom: '1px solid var(--border-color)' }}>
              <span style={{ color: 'var(--slate-500)', fontSize: '0.875rem', display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
                <ShieldCheck size={15} /> Trạng thái công việc
              </span>
              <Badge variant={getStatusVariant(profile.status)}>
                {getStatusText(profile.status)}
              </Badge>
            </div>

            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <span style={{ color: 'var(--slate-500)', fontSize: '0.875rem', display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
                <DollarSign size={15} /> Chế độ đãi ngộ
              </span>
              <span style={{ fontWeight: 600, color: 'var(--success-main)', fontSize: '0.875rem' }}>
                Hợp đồng chính thức
              </span>
            </div>
          </div>
        </Card>
      </div>
    </div>
  );
}
