import { useState, useRef, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Bell,
  CheckCheck,
  Info,
  CheckCircle2,
  AlertTriangle,
  Clock,
  Inbox,
  Loader2,
} from 'lucide-react';
import { useNotificationStore } from './useNotificationStore';
import { useAuthStore } from '../auth/useAuthStore';
import { useSidebar } from '../../contexts/SidebarContext';
import { formatNotificationMessage } from './utils/notificationTemplates';
import './NotificationDropdown.css';

function formatRelativeTime(dateString) {
  if (!dateString) return '';
  const date = new Date(dateString);
  const now = new Date();
  const diffInSeconds = Math.floor((now - date) / 1000);

  if (diffInSeconds < 60) return 'Vừa xong';
  const diffInMinutes = Math.floor(diffInSeconds / 60);
  if (diffInMinutes < 60) return `${diffInMinutes} phút trước`;
  const diffInHours = Math.floor(diffInMinutes / 60);
  if (diffInHours < 24) return `${diffInHours} giờ trước`;
  const diffInDays = Math.floor(diffInHours / 24);
  if (diffInDays < 7) return `${diffInDays} ngày trước`;

  return date.toLocaleDateString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  });
}

export default function NotificationDropdown() {
  const [isOpen, setIsOpen] = useState(false);
  const [activeTab, setActiveTab] = useState('all'); // 'all' | 'unread'
  const dropdownRef = useRef(null);
  const navigate = useNavigate();

  const role = useAuthStore((state) => state.role);
  const { isMobileOpen, closeMobileSidebar } = useSidebar();
  const {
    notifications,
    unreadCount,
    isLoading,
    fetchNotifications,
    markAsRead,
    markAllAsRead,
  } = useNotificationStore();

  // Mutual exclusion: Close notifications dropdown when mobile sidebar opens
  useEffect(() => {
    if (isMobileOpen) {
      setIsOpen(false);
    }
  }, [isMobileOpen]);

  // Close when clicking outside
  useEffect(() => {
    function handleClickOutside(event) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
        setIsOpen(false);
      }
    }

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isOpen]);

  const handleToggle = () => {
    if (!isOpen) {
      // Mutual exclusion: Close mobile sidebar before opening notifications dropdown
      if (isMobileOpen) {
        closeMobileSidebar();
      }
      fetchNotifications();
    }
    setIsOpen((prev) => !prev);
  };

  const handleItemClick = async (notif) => {
    if (!notif.isRead && notif.id) {
      await markAsRead(notif.id);
    }
    setIsOpen(false);

    // Route navigation based on entity and user role
    if (notif.relatedEntity === 'Application') {
      if (role === 'Candidate') {
        navigate('/my-applications');
      } else {
        navigate('/applications');
      }
    } else if (notif.relatedEntity === 'Interview') {
      if (role === 'Candidate') {
        navigate('/my-applications');
      } else {
        navigate('/interviews');
      }
    } else if (notif.relatedEntity === 'Employee') {
      if (role === 'Employee') {
        navigate('/profile');
      } else {
        navigate('/employees');
      }
    }
  };

  const filteredNotifications =
    activeTab === 'unread'
      ? notifications.filter((n) => !n.isRead)
      : notifications;

  const getIcon = (type) => {
    const t = type?.toLowerCase();
    switch (t) {
      case 'success':
        return <CheckCircle2 size={16} />;
      case 'warning':
        return <AlertTriangle size={16} />;
      case 'actionrequired':
        return <Clock size={16} />;
      default:
        return <Info size={16} />;
    }
  };

  return (
    <div className="notification-dropdown-wrapper" ref={dropdownRef}>
      <button
        type="button"
        className="notification-bell-btn"
        onClick={handleToggle}
        aria-label="Thông báo"
      >
        <Bell size={18} />
        {unreadCount > 0 && (
          <span className="notification-badge">
            {unreadCount > 9 ? '9+' : unreadCount}
          </span>
        )}
      </button>

      {isOpen && (
        <div className="notification-menu">
          <div className="notification-header">
            <div className="notification-header-title">
              <span>Thông báo</span>
              {unreadCount > 0 && (
                <span className="notification-count-pill">
                  {unreadCount} mới
                </span>
              )}
            </div>
            {unreadCount > 0 && (
              <button
                type="button"
                className="notification-mark-all-btn"
                onClick={markAllAsRead}
              >
                <CheckCheck size={14} />
                Đọc tất cả
              </button>
            )}
          </div>

          <div className="notification-tabs">
            <button
              type="button"
              className={`notification-tab-btn ${activeTab === 'all' ? 'active' : ''}`}
              onClick={() => setActiveTab('all')}
            >
              Tất cả
            </button>
            <button
              type="button"
              className={`notification-tab-btn ${activeTab === 'unread' ? 'active' : ''}`}
              onClick={() => setActiveTab('unread')}
            >
              Chưa đọc {unreadCount > 0 ? `(${unreadCount})` : ''}
            </button>
          </div>

          <ul className="notification-list">
            {isLoading && notifications.length === 0 ? (
              <li className="notification-empty">
                <Loader2 size={24} className="animate-spin text-primary-500" />
                <span>Đang tải thông báo...</span>
              </li>
            ) : filteredNotifications.length === 0 ? (
              <li className="notification-empty">
                <div className="notification-empty-icon">
                  <Inbox size={24} />
                </div>
                <span style={{ fontWeight: 600, fontSize: '0.875rem' }}>
                  {activeTab === 'unread'
                    ? 'Không có thông báo chưa đọc'
                    : 'Không có thông báo nào'}
                </span>
                <span style={{ fontSize: '0.775rem' }}>
                  Hệ thống sẽ cập nhật khi có hoạt động mới liên quan đến bạn.
                </span>
              </li>
            ) : (
              filteredNotifications.map((notif) => {
                const typeClass = (notif.type || 'info').toLowerCase();
                return (
                  <li
                    key={notif.id || `${notif.title}-${notif.createdAt}`}
                    className={`notification-item ${!notif.isRead ? 'unread' : ''}`}
                    onClick={() => handleItemClick(notif)}
                  >
                    <div className={`notification-icon-box ${typeClass}`}>
                      {getIcon(notif.type)}
                    </div>
                    <div className="notification-content">
                      <div className="notification-title">{notif.title}</div>
                      <div className="notification-message">{formatNotificationMessage(notif)}</div>
                      <div className="notification-time">
                        {formatRelativeTime(notif.createdAt)}
                      </div>
                    </div>
                    {!notif.isRead && <span className="notification-unread-dot" />}
                  </li>
                );
              })
            )}
          </ul>
        </div>
      )}
    </div>
  );
}
