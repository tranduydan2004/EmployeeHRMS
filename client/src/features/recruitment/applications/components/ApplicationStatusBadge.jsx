import { Badge } from '@/components';
import { Clock, Eye, MessageSquare, CheckCircle2, XCircle } from 'lucide-react';

export default function ApplicationStatusBadge({ status }) {
  switch (status) {
    case 'Applied':
      return (
        <Badge variant="info" icon={Clock}>
          Đã Nộp (Applied)
        </Badge>
      );
    case 'Screening':
      return (
        <Badge variant="warning" icon={Eye}>
          Sàng Lọc (Screening)
        </Badge>
      );
    case 'Interviewing':
      return (
        <Badge variant="primary" icon={MessageSquare} style={{ backgroundColor: 'var(--primary-100)', color: 'var(--primary-800)', border: '1px solid var(--primary-300)' }}>
          Phỏng Vấn (Interviewing)
        </Badge>
      );
    case 'Offered':
      return (
        <Badge variant="success" icon={CheckCircle2}>
          Được Mời Nhận Việc (Offered)
        </Badge>
      );
    case 'Rejected':
      return (
        <Badge variant="danger" icon={XCircle}>
          Từ Chối (Rejected)
        </Badge>
      );
    default:
      return <Badge variant="neutral">{status}</Badge>;
  }
}
