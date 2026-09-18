import { NavLink } from 'react-router-dom';
import { useSidebar } from '../contexts/SidebarContext';
import { useAuthStore } from '../features/auth';
import {
  LayoutDashboard,
  Users,
  Building2,
  Briefcase,
  UserSquare2,
  FileSpreadsheet,
  CalendarCheck2,
  FileCheck2,
  UserCircle,
  Sparkles,
} from 'lucide-react';

export default function Sidebar() {
  const { isCollapsed, isMobileOpen } = useSidebar();
  const role = useAuthStore((state) => state.role);

  const getNavLinks = () => {
    switch (role) {
      case 'Admin':
      case 'HR':
        return [
          { to: '/dashboard', label: 'Bảng Điều Khiển', icon: LayoutDashboard },
          { to: '/employees', label: 'Quản Lý Nhân Viên', icon: Users },
          { to: '/departments', label: 'Phòng Ban', icon: Building2 },
          { to: '/admin/jobs', label: 'Tin Tuyển Dụng', icon: Briefcase },
          { to: '/candidates', label: 'Hồ Sơ Ứng Viên', icon: UserSquare2 },
          { to: '/applications', label: 'Đơn Ứng Tuyển', icon: FileSpreadsheet },
          { to: '/interviews', label: 'Lịch Phỏng Vấn', icon: CalendarCheck2 },
        ];

      case 'Interviewer':
        return [
          { to: '/jobs', label: 'Tin Tuyển Dụng', icon: Briefcase },
          { to: '/candidates', label: 'Ứng Viên Liên Quan', icon: UserSquare2 },
          { to: '/applications', label: 'Đơn Ứng Tuyển', icon: FileSpreadsheet },
          { to: '/interviews', label: 'Phỏng Vấn Được Gán', icon: CalendarCheck2 },
        ];

      case 'Candidate':
        return [
          { to: '/jobs', label: 'Tin Tuyển Dụng', icon: Briefcase },
          { to: '/my-applications', label: 'Đơn Của Tôi', icon: FileCheck2 },
        ];

      case 'Employee':
        return [
          { to: '/profile', label: 'Hồ Sơ Của Tôi', icon: UserCircle },
          { to: '/my-department', label: 'Phòng Ban Của Tôi', icon: Building2 },
          { to: '/jobs', label: 'Tin Tuyển Dụng', icon: Briefcase },
          { to: '/my-applications', label: 'Đơn Của Tôi', icon: FileCheck2 },
        ];

      default:
        return [{ to: '/jobs', label: 'Cơ Hội Việc Làm', icon: Briefcase }];
    }
  };

  const navLinks = getNavLinks();

  return (
    <aside
      className={`app-sidebar ${isCollapsed ? 'collapsed' : ''} ${isMobileOpen ? 'mobile-open' : ''
        }`}
    >
      <div className="sidebar-header">
        <div className="sidebar-brand">
          <div className="brand-icon">
            <Sparkles size={18} />
          </div>
          {!isCollapsed && <span>HRMS Pro</span>}
        </div>
      </div>

      <nav className="sidebar-nav">
        {navLinks.map((link) => {
          const Icon = link.icon;
          return (
            <NavLink
              key={link.to}
              to={link.to}
              className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}
              title={isCollapsed ? link.label : ''}
            >
              <Icon className="nav-item-icon" />
              {!isCollapsed && <span>{link.label}</span>}
            </NavLink>
          );
        })}
      </nav>

      {!isCollapsed && (
        <div className="sidebar-footer">
          <div>HRMS Enterprise v1.0</div>
          <div style={{ color: 'var(--slate-500)', fontSize: '0.75rem', marginTop: '2px' }}>
            .NET 8 & React 19
          </div>
        </div>
      )}
    </aside>
  );
}
