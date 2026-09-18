import { useQuery } from '@tanstack/react-query';
import axiosClient from '@/api/axiosClient';
import { useDepartments } from '@/features/departments';
import MetricCard from './MetricCard';
import { Users, Building2, Briefcase, FileSpreadsheet, Sparkles } from 'lucide-react';
import { Card, Table, Badge, Skeleton } from '@/components';

export default function DashboardOverview() {
  const { data: statsData, isLoading: isStatsLoading } = useQuery({
    queryKey: ['employees', 'statistics'],
    queryFn: async () => {
      const res = await axiosClient.get('/Employees/statistics');
      return res.data;
    },
  });

  const { data: departmentsData, isLoading: isDeptsLoading } = useDepartments();

  const { data: activeJobs, isLoading: isJobsLoading } = useQuery({
    queryKey: ['jobPostings', 'active'],
    queryFn: async () => {
      const res = await axiosClient.get('/JobPostings/active');
      return res.data;
    },
  });

  const { data: applicationsData, isLoading: isAppsLoading } = useQuery({
    queryKey: ['applications', { pageNumber: 1, pageSize: 5 }],
    queryFn: async () => {
      const res = await axiosClient.get('/Applications?pageNumber=1&pageSize=5');
      return res.data;
    },
  });

  const totalEmployees = statsData
    ? statsData.reduce((acc, curr) => acc + curr.employeeCount, 0)
    : 0;

  const totalDepartments =
    departmentsData?.totalCount ??
    (departmentsData?.items?.length || (Array.isArray(departmentsData) ? departmentsData.length : 0));

  const statColumns = [
    { key: 'departmentName', title: 'Phòng Ban' },
    {
      key: 'employeeCount',
      title: 'Số Nhân Viên',
      align: 'center',
      render: (r) => <Badge variant="info">{r.employeeCount} người</Badge>,
    },
    {
      key: 'averageSalary',
      title: 'Lương Trung Bình',
      align: 'right',
      render: (r) => `${r.averageSalary?.toLocaleString('vi-VN')} ₫`,
    },
    {
      key: 'maxSalary',
      title: 'Lương Cao Nhất',
      align: 'right',
      render: (r) => `${r.maxSalary?.toLocaleString('vi-VN')} ₫`,
    },
  ];

  return (
    <div>
      <div className="page-header">
        <div className="page-title-group">
          <h1>Bảng Điều Khiển Tổng Quan</h1>
          <p>Thống kê nhân sự, cơ cấu phòng ban và tình hình tuyển dụng doanh nghiệp</p>
        </div>
      </div>

      <div className="stat-grid">
        <MetricCard
          title="Tổng Số Nhân Viên"
          value={isStatsLoading ? '...' : totalEmployees}
          icon={Users}
          color="var(--primary-600)"
          bg="var(--primary-50)"
          subtitle="Toàn bộ công ty"
        />
        <MetricCard
          title="Phòng Ban Hoạt Động"
          value={isDeptsLoading ? '...' : totalDepartments}
          icon={Building2}
          color="var(--info-main)"
          bg="var(--info-bg)"
          subtitle="Đang quản lý"
        />
        <MetricCard
          title="Vị Trí Đang Tuyển"
          value={isJobsLoading ? '...' : activeJobs?.length || 0}
          icon={Briefcase}
          color="var(--success-main)"
          bg="var(--success-bg)"
          subtitle="Tin tuyển dụng active"
        />
        <MetricCard
          title="Đơn Ứng Tuyển"
          value={isAppsLoading ? '...' : applicationsData?.totalCount || 0}
          icon={FileSpreadsheet}
          color="var(--warning-main)"
          bg="var(--warning-bg)"
          subtitle="Tổng số hồ sơ tiếp nhận"
        />
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(450px, 1fr))', gap: '1.5rem' }}>
        <Card title="Thống Kê Lương & Nhân Sự Theo Phòng Ban">
          <Table
            columns={statColumns}
            data={statsData || []}
            isLoading={isStatsLoading}
            emptyMessage="Chưa có dữ liệu thống kê nhân viên."
          />
        </Card>

        <Card title="Đơn Ứng Tuyển Gần Đây">
          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
            {isAppsLoading ? (
              Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} height="48px" />)
            ) : applicationsData?.items?.length === 0 ? (
              <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem' }}>Chưa có đơn ứng tuyển nào.</p>
            ) : (
              applicationsData?.items?.map((app) => (
                <div
                  key={app.id}
                  style={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                    padding: '0.75rem 1rem',
                    border: '1px solid var(--border-color)',
                    borderRadius: 'var(--radius-md)',
                    backgroundColor: 'var(--slate-50)',
                  }}
                >
                  <div>
                    <div style={{ fontWeight: 600, fontSize: '0.9rem' }}>{app.candidateName}</div>
                    <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                      Ứng tuyển: {app.jobTitle}
                    </div>
                  </div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                    {app.aiMatchScore && (
                      <Badge variant="success" icon={Sparkles}>
                        AI: {app.aiMatchScore}%
                      </Badge>
                    )}
                    <Badge variant={app.status === 'Offered' ? 'success' : app.status === 'Rejected' ? 'danger' : 'info'}>
                      {app.status}
                    </Badge>
                  </div>
                </div>
              ))
            )}
          </div>
        </Card>
      </div>
    </div>
  );
}
