import { useState } from 'react';
import { useMyProfile, useMyDepartmentColleagues } from '../hooks/useEmployees';
import { Card, Input, Skeleton, Badge } from '@/components';
import {
  Building2,
  Users,
  Mail,
  Briefcase,
  Search,
  ExternalLink,
  IdCard,
} from 'lucide-react';

export default function MyDepartmentPage() {
  const { data: profile, isLoading: isProfileLoading } = useMyProfile();
  const { data: colleagues, isLoading: isColleaguesLoading } = useMyDepartmentColleagues();
  const [searchTerm, setSearchTerm] = useState('');

  const isLoading = isProfileLoading || isColleaguesLoading;

  if (isLoading) {
    return (
      <div style={{ maxWidth: '1080px', margin: '0 auto', display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
        <div className="page-header">
          <Skeleton height="36px" width="300px" />
          <Skeleton height="20px" width="220px" style={{ marginTop: '0.5rem' }} />
        </div>
        <Card><Skeleton height="100px" /></Card>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', gap: '1.25rem' }}>
          {Array.from({ length: 4 }).map((_, i) => (
            <Card key={i}><Skeleton height="120px" /></Card>
          ))}
        </div>
      </div>
    );
  }

  const departmentName = profile?.departmentName || 'Phòng Ban';
  const filteredColleagues = (colleagues || []).filter(
    (c) =>
      c.fullName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      c.position.toLowerCase().includes(searchTerm.toLowerCase()) ||
      c.email.toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <div style={{ maxWidth: '1080px', margin: '0 auto', display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
      {/* Header */}
      <div className="page-header" style={{ marginBottom: 0 }}>
        <div className="page-title-group">
          <h1>Phòng Ban Của Tôi: {departmentName}</h1>
          <p>Danh sách các thành viên và đồng nghiệp cùng phòng ban làm việc</p>
        </div>
      </div>

      {/* Department Summary Card */}
      <Card
        style={{
          background: 'linear-gradient(135deg, rgba(255, 255, 255, 0.95), rgba(240, 248, 255, 0.85))',
          borderColor: 'var(--primary-200)',
          boxShadow: '0 8px 24px rgba(99, 102, 241, 0.06)',
        }}
      >
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            flexWrap: 'wrap',
            gap: '1.25rem',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '1.25rem' }}>
            <div
              style={{
                width: '64px',
                height: '64px',
                borderRadius: 'var(--radius-md)',
                backgroundColor: 'var(--primary-50)',
                color: 'var(--primary-700)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                border: '1px solid var(--primary-200)',
              }}
            >
              <Building2 size={32} />
            </div>

            <div>
              <h2 style={{ fontSize: '1.35rem', fontWeight: 700, color: 'var(--slate-900)' }}>
                {departmentName}
              </h2>
              <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', marginTop: '0.35rem', fontSize: '0.875rem', color: 'var(--slate-600)' }}>
                <span style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                  <Users size={15} color="var(--primary-600)" />
                  {colleagues?.length || 0} đồng nghiệp
                </span>
                <span>•</span>
                <span>Vị trí của bạn: <strong>{profile?.position || 'Thành viên'}</strong></span>
              </div>
            </div>
          </div>

          <div style={{ width: '100%', maxWidth: '320px' }}>
            <Input
              placeholder="Tìm đồng nghiệp theo tên, vị trí..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              prefix={<Search size={16} color="var(--slate-400)" />}
            />
          </div>
        </div>
      </Card>

      {/* Colleagues Grid */}
      {filteredColleagues.length === 0 ? (
        <Card style={{ textAlign: 'center', padding: '3.5rem 1rem' }}>
          <Users size={44} color="var(--slate-400)" style={{ margin: '0 auto 1rem auto' }} />
          <h3 style={{ fontSize: '1.15rem', fontWeight: 600, color: 'var(--slate-800)' }}>
            {searchTerm ? 'Không tìm thấy đồng nghiệp phù hợp' : 'Chưa có đồng nghiệp nào khác trong phòng ban'}
          </h3>
          <p style={{ color: 'var(--text-muted)', marginTop: '0.5rem', fontSize: '0.875rem' }}>
            {searchTerm ? 'Thử tìm kiếm với từ khóa khác.' : 'Bạn hiện là thành viên duy nhất trong phòng ban này.'}
          </p>
        </Card>
      ) : (
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))',
            gap: '1.25rem',
          }}
        >
          {filteredColleagues.map((colleague) => {
            const initials = colleague.fullName
              ? colleague.fullName
                  .split(' ')
                  .filter(Boolean)
                  .slice(-2)
                  .map((n) => n[0])
                  .join('')
                  .toUpperCase()
              : 'NV';

            return (
              <Card
                key={colleague.id}
                style={{
                  display: 'flex',
                  flexDirection: 'column',
                  gap: '0.85rem',
                  transition: 'transform 0.15s ease, box-shadow 0.15s ease',
                  border: '1px solid var(--border-color)',
                }}
              >
                <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
                  <div
                    style={{
                      width: '48px',
                      height: '48px',
                      borderRadius: '50%',
                      backgroundColor: 'var(--primary-100)',
                      color: 'var(--primary-800)',
                      fontSize: '1rem',
                      fontWeight: 700,
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      border: '1px solid var(--primary-200)',
                    }}
                  >
                    {initials}
                  </div>

                  <div style={{ flex: 1, minWidth: 0 }}>
                    <h4
                      style={{
                        fontSize: '1rem',
                        fontWeight: 600,
                        color: 'var(--slate-900)',
                        overflow: 'hidden',
                        textOverflow: 'ellipsis',
                        whiteSpace: 'nowrap',
                      }}
                      title={colleague.fullName}
                    >
                      {colleague.fullName}
                    </h4>
                    <div
                      style={{
                        fontSize: '0.825rem',
                        color: 'var(--slate-600)',
                        display: 'flex',
                        alignItems: 'center',
                        gap: '0.3rem',
                        marginTop: '0.2rem',
                      }}
                    >
                      <Briefcase size={13} color="var(--primary-600)" />
                      <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                        {colleague.position}
                      </span>
                    </div>
                  </div>
                </div>

                <div
                  style={{
                    padding: '0.5rem 0.75rem',
                    backgroundColor: 'var(--slate-50)',
                    borderRadius: 'var(--radius-sm)',
                    border: '1px solid var(--border-color)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',
                    fontSize: '0.825rem',
                  }}
                >
                  <span
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: '0.4rem',
                      color: 'var(--slate-700)',
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                      whiteSpace: 'nowrap',
                    }}
                    title={colleague.email}
                  >
                    <Mail size={13} color="var(--slate-500)" />
                    {colleague.email}
                  </span>

                  <a
                    href={`mailto:${colleague.email}`}
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: '0.25rem',
                      color: 'var(--primary-600)',
                      fontWeight: 500,
                      textDecoration: 'none',
                      fontSize: '0.8rem',
                      flexShrink: 0,
                      marginLeft: '0.5rem',
                    }}
                    title={`Gửi email cho ${colleague.fullName}`}
                  >
                    Gửi mail
                    <ExternalLink size={12} />
                  </a>
                </div>
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}
