import { useEmployeeStatistics } from '../hooks/useEmployees';
import { Users, DollarSign, Award } from 'lucide-react';
import { MetricCard } from '@/features/dashboard';

export default function EmployeeStatCards() {
  const { data: stats, isLoading } = useEmployeeStatistics();

  if (isLoading || !stats || stats.length === 0) return null;

  const totalEmployees = stats.reduce((acc, curr) => acc + curr.employeeCount, 0);
  const highestAvgDept = [...stats].sort((a, b) => b.averageSalary - a.averageSalary)[0];
  const maxOverallSalary = Math.max(...stats.map((s) => s.maxSalary));

  return (
    <div className="stat-grid" style={{ marginBottom: '1.5rem' }}>
      <MetricCard
        title="Tổng Số Nhân Sự"
        value={totalEmployees}
        icon={Users}
        color="var(--primary-600)"
        bg="var(--primary-50)"
      />
      <MetricCard
        title="Phòng Ban Lương TB Cao Nhất"
        value={highestAvgDept ? highestAvgDept.departmentName : 'N/A'}
        subtitle={`${highestAvgDept?.averageSalary?.toLocaleString('vi-VN')} ₫/tháng`}
        icon={Award}
        color="var(--warning-main)"
        bg="var(--warning-bg)"
      />
      <MetricCard
        title="Mức Lương Kỷ Lục"
        value={`${maxOverallSalary?.toLocaleString('vi-VN')} ₫`}
        subtitle="Mức lương trần hiện tại"
        icon={DollarSign}
        color="var(--success-main)"
        bg="var(--success-bg)"
      />
    </div>
  );
}
