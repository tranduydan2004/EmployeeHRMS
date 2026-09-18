import { useSidebar } from '../contexts/SidebarContext';
import { useAuthStore } from '../features/auth';
import { Menu, LogOut, UserCheck } from 'lucide-react';
import { Badge } from '../components';
import { NotificationDropdown } from '../features/notifications';

export default function Header() {
  const { toggleSidebar, toggleMobileSidebar } = useSidebar();
  const { userEmail, role, logout } = useAuthStore();

  const getRoleVariant = () => {
    switch (role) {
      case 'Admin':
        return 'danger';
      case 'HR':
        return 'info';
      case 'Interviewer':
        return 'warning';
      case 'Candidate':
        return 'success';
      default:
        return 'neutral';
    }
  };

  const getInitial = () => {
    if (!userEmail) return 'U';
    return userEmail.charAt(0).toUpperCase();
  };

  return (
    <header className="app-header">
      <div className="header-left">
        {/* Desktop: collapse/expand sidebar */}
        <button
          type="button"
          className="btn-icon btn-desktop-toggle"
          onClick={toggleSidebar}
          aria-label="Toggle Sidebar"
        >
          <Menu size={20} />
        </button>
        {/* Mobile: open/close drawer sidebar */}
        <button
          type="button"
          className="btn-icon btn-mobile-toggle"
          onClick={toggleMobileSidebar}
          aria-label="Open Menu"
        >
          <Menu size={20} />
        </button>
        <span style={{ fontSize: '1rem', fontWeight: 600, color: 'var(--slate-800)' }}>
          Hệ Thống Quản Trị Nhân Sự (HRMS)
        </span>
      </div>

      <div className="header-right">
        {userEmail && <NotificationDropdown />}
        {userEmail ? (
          <div className="user-profile-badge">
            <div className="avatar-circle">{getInitial()}</div>
            <div className="user-info">
              <span className="user-name">{userEmail}</span>
              <div style={{ marginTop: '2px' }}>
                <Badge variant={getRoleVariant()} icon={UserCheck}>
                  {role}
                </Badge>
              </div>
            </div>
            <button
              type="button"
              className="btn-icon"
              onClick={logout}
              title="Đăng xuất"
              style={{ marginLeft: '0.5rem', color: 'var(--danger-main)' }}
            >
              <LogOut size={18} />
            </button>
          </div>
        ) : (
          <a href="/login" className="btn btn-primary btn-sm">
            Đăng nhập
          </a>
        )}
      </div>
    </header>
  );
}
