import { Component } from 'react';
import { AlertTriangle, RefreshCw } from 'lucide-react';

/**
 * Error Boundary bảo vệ ứng viên khỏi hiện tượng màn hình trắng (White Screen of Death).
 * Dữ liệu âm thanh đã ghi được bảo vệ trong IndexedDB, ứng viên có thể tải lại trang an toàn.
 */
export class CandidateErrorBoundary extends Component {
  constructor(props) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error) {
    return { hasError: true, error };
  }

  componentDidCatch(error, errorInfo) {
    console.error('[CandidateErrorBoundary] Bắt lỗi giao diện:', error, errorInfo);
  }

  handleReload = () => {
    window.location.reload();
  };

  render() {
    if (this.state.hasError) {
      return (
        <div style={{
          minHeight: '100vh',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          backgroundColor: '#0f172a',
          color: '#f8fafc',
          padding: '24px',
          fontFamily: 'Inter, system-ui, sans-serif'
        }}>
          <div style={{
            maxWidth: '520px',
            width: '100%',
            backgroundColor: '#1e293b',
            borderRadius: '16px',
            padding: '36px',
            border: '1px solid #334155',
            boxShadow: '0 20px 25px -5px rgba(0, 0, 0, 0.5)',
            textAlign: 'center'
          }}>
            <div style={{
              width: '64px',
              height: '64px',
              borderRadius: '50%',
              backgroundColor: 'rgba(239, 68, 68, 0.15)',
              color: '#ef4444',
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              marginBottom: '20px'
            }}>
              <AlertTriangle size={32} />
            </div>

            <h2 style={{ fontSize: '22px', fontWeight: '700', marginBottom: '12px' }}>
              Đã xảy ra sự cố không mong muốn
            </h2>

            <p style={{ color: '#94a3b8', fontSize: '15px', lineHeight: '1.6', marginBottom: '24px' }}>
              Ứng dụng vừa gặp phải một sự cố hiển thị nhỏ. Các câu trả lời đã ghi âm của bạn đã được sao lưu an toàn vào bộ nhớ tạm thời của máy. Vui lòng tải lại trang để tiếp tục.
            </p>

            {this.state.error && (
              <div style={{
                backgroundColor: '#0f172a',
                padding: '12px 16px',
                borderRadius: '8px',
                fontSize: '13px',
                color: '#f87171',
                fontFamily: 'monospace',
                textAlign: 'left',
                overflowX: 'auto',
                marginBottom: '24px',
                border: '1px solid #1e293b'
              }}>
                {this.state.error.message || String(this.state.error)}
              </div>
            )}

            <button
              onClick={this.handleReload}
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: '8px',
                backgroundColor: '#4f46e5',
                color: '#ffffff',
                fontWeight: '600',
                padding: '12px 24px',
                borderRadius: '10px',
                border: 'none',
                cursor: 'pointer',
                fontSize: '15px',
                transition: 'all 0.2s',
                boxShadow: '0 4px 12px rgba(79, 70, 229, 0.4)'
              }}
              onMouseEnter={(e) => e.currentTarget.style.backgroundColor = '#4338ca'}
              onMouseLeave={(e) => e.currentTarget.style.backgroundColor = '#4f46e5'}
            >
              <RefreshCw size={18} />
              Tải lại trang và tiếp tục
            </button>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}
