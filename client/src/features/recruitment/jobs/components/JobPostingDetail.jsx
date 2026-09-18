import { useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useJobPosting } from '../hooks/useJobPostings';
import { usePermission } from '@/hooks/usePermission';
import { useAuthStore } from '@/features/auth';
import { Card, Button, Badge, Skeleton } from '@/components';
import { Building2, Calendar, Send, ArrowLeft } from 'lucide-react';
import { useCandidates } from '@/features/recruitment/candidates';
import { toast } from '@/components/Toast/useToastStore';
import ApplyModal from './ApplyModal';

export default function JobPostingDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { data: job, isLoading } = useJobPosting(id);
  const { isAuthenticated } = usePermission();
  const { role } = useAuthStore();

  const { data: candidateData } = useCandidates({ pageNumber: 1, pageSize: 1 });
  const myCandidate = candidateData?.items?.[0];

  const [isApplyModalOpen, setIsApplyModalOpen] = useState(false);

  const handleApply = () => {
    if (!isAuthenticated) {
      toast.info('Vui lòng đăng nhập tài khoản Ứng viên để nộp hồ sơ.');
      navigate('/login', { state: { from: { pathname: `/jobs/${id}` } } });
      return;
    }

    setIsApplyModalOpen(true);
  };

  if (isLoading) {
    return (
      <div style={{ maxWidth: '800px', margin: '0 auto' }}>
        <Skeleton height="36px" width="60%" />
        <div style={{ marginTop: '1rem' }}>
          <Skeleton height="200px" />
        </div>
      </div>
    );
  }

  if (!job) {
    return (
      <div style={{ textAlign: 'center', padding: '3rem' }}>
        <h2>Không tìm thấy tin tuyển dụng</h2>
        <Link to="/jobs" style={{ marginTop: '1rem', display: 'inline-block' }}>
          <Button variant="secondary" icon={ArrowLeft}>
            Quay lại danh sách
          </Button>
        </Link>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: '850px', margin: '0 auto' }}>
      <div style={{ marginBottom: '1.25rem' }}>
        <Link to="/jobs" style={{ display: 'inline-flex', alignItems: 'center', gap: '0.35rem', color: 'var(--text-muted)' }}>
          <ArrowLeft size={16} /> Quay lại danh sách việc làm
        </Link>
      </div>

      <Card>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: '1rem' }}>
          <div>
            <h1 style={{ fontSize: '1.75rem', fontWeight: 700, color: 'var(--slate-900)' }}>
              {job.title}
            </h1>
            <div style={{ display: 'flex', gap: '1rem', marginTop: '0.75rem', flexWrap: 'wrap', color: 'var(--slate-600)', fontSize: '0.9rem' }}>
              <span style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                <Building2 size={16} color="var(--primary-600)" />
                {job.departmentName}
              </span>
              <span style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                <Calendar size={16} color="var(--slate-400)" />
                Đăng ngày: {new Date(job.createdDate).toLocaleDateString('vi-VN')}
              </span>
              <Badge variant={job.status === 'Published' ? 'success' : 'neutral'}>
                {job.status}
              </Badge>
            </div>
          </div>

          {(!role || role === 'Candidate') && (
            <Button
              variant="primary"
              size="lg"
              icon={Send}
              onClick={handleApply}
            >
              Nộp Đơn Ứng Tuyển
            </Button>
          )}
        </div>

        <hr style={{ margin: '1.5rem 0', borderColor: 'var(--border-color)' }} />

        <div style={{ marginBottom: '2rem' }}>
          <h3 style={{ fontSize: '1.15rem', fontWeight: 600, color: 'var(--slate-900)', marginBottom: '0.75rem' }}>
            Mô tả công việc (Job Description)
          </h3>
          <div
            style={{
              whiteSpace: 'pre-line',
              lineHeight: 1.7,
              color: 'var(--slate-700)',
              backgroundColor: 'var(--slate-50)',
              padding: '1.25rem',
              borderRadius: 'var(--radius-md)',
            }}
          >
            {job.description || 'Chưa có thông tin mô tả chi tiết.'}
          </div>
        </div>

        <div>
          <h3 style={{ fontSize: '1.15rem', fontWeight: 600, color: 'var(--slate-900)', marginBottom: '0.75rem' }}>
            Yêu cầu chuyên môn (Requirements)
          </h3>
          <div
            style={{
              whiteSpace: 'pre-line',
              lineHeight: 1.7,
              color: 'var(--slate-700)',
              backgroundColor: 'var(--slate-50)',
              padding: '1.25rem',
              borderRadius: 'var(--radius-md)',
            }}
          >
            {job.requirements || 'Chưa có thông tin yêu cầu chi tiết.'}
          </div>
        </div>
      </Card>

      <ApplyModal
        isOpen={isApplyModalOpen}
        onClose={() => setIsApplyModalOpen(false)}
        job={job}
        candidate={myCandidate}
      />
    </div>
  );
}
