import { Outlet } from 'react-router-dom';
import { SidebarProvider, useSidebar } from '../contexts/SidebarContext';
import { useSignalR } from '../features/notifications';
import Header from './Header';
import Sidebar from './Sidebar';

function MainLayoutContent() {
  const { isCollapsed, isMobileOpen, closeMobileSidebar } = useSidebar();
  useSignalR();

  return (
    <div className="app-layout">
      <Sidebar />
      {/* Backdrop mờ phủ màn hình khi sidebar mở trên mobile */}
      <div
        className={`sidebar-backdrop ${isMobileOpen ? 'visible' : ''}`}
        onClick={closeMobileSidebar}
      />
      <div className={`main-wrapper ${isCollapsed ? 'sidebar-collapsed' : ''}`}>
        <Header />
        <main className="page-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

export default function MainLayout() {
  return (
    <SidebarProvider>
      <MainLayoutContent />
    </SidebarProvider>
  );
}
