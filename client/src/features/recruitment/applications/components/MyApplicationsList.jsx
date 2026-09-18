import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useApplications } from '../hooks/useApplications';
import { useDeleteApplication } from '../hooks/useApplicationMutations';
import ApplicationStatusBadge from './ApplicationStatusBadge';
import {
  useCandidates,
  useCandidateMutations,
  CandidateFormModal,
  ResumeUploadButton,
} from '@/features/recruitment/candidates';
import { Card, Button, Badge, Skeleton } from '@/components';
import {
  Briefcase,
  Calendar,
  Sparkles,
  Send,
  FileText,
  User,
  Mail,
  Phone,
  Edit,
  Plus,
  Trash2,
} from 'lucide-react';

export default function MyApplicationsList() {
  const { data: pagedResult, isLoading } = useApplications({ pageNumber: 1, pageSize: 50 });
  const { deleteApplication } = useDeleteApplication();

  const { data: candidateData, isLoading: isCandLoading } = useCandidates({ pageNumber: 1, pageSize: 1 });
  const myCandidate = candidateData?.items?.[0];

  const { createCandidate, updateCandidate, isCreating, isUpdating } = useCandidateMutations();

  const [isModalOpen, setIsModalOpen] = useState(false);

  const applications = pagedResult?.items || [];

  const handleOpenEdit = () => {
    setIsModalOpen(true);
  };

  const handleSaveProfile = async (payload) => {
    if (myCandidate) {
      await updateCandidate({ id: myCandidate.id, payload });
    } else {
      await createCandidate(payload);
    }
    setIsModalOpen(false);
  };

  const handleDeleteApplication = async (appId, jobTitle) => {
    if (window.confirm(`Bạn có chắc chắn muốn rút đơn ứng tuyển cho vị trí "${jobTitle}"?`)) {
      await deleteApplication(appId);
    }
  };

  return (
    <div>
      <div className="page-header">
        <div className="page-title-group">
          <h1>Đơn Ứng Tuyển Của Tôi</h1>
          <p>Quản lý hồ sơ ứng viên cá nhân và theo dõi tiến trình xét duyệt các vị trí ứng tuyển</p>
        </div>
        <Link to="/jobs">
          <Button variant="primary" icon={Briefcase}>
            Tìm Việc Làm Mới
          </Button>
        </Link>
      </div>

      {/* Candidate Profile Summary Card */}
      <Card style={{ marginBottom: '1.5rem' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '1rem' }}>
          <div>
            <h3 style={{ fontSize: '1.1rem', fontWeight: 600, color: 'var(--slate-900)', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <User size={18} color="var(--primary-600)" />
              Hồ Sơ Ứng Viên Của Bạn
            </h3>
            <p style={{ fontSize: '0.85rem', color: 'var(--text-muted)', marginTop: '0.25rem' }}>
              Thông tin này sẽ được đính kèm khi bạn nộp đơn ứng tuyển vào các vị trí việc làm
            </p>
          </div>

          {myCandidate ? (
            <Button variant="outline" size="sm" icon={Edit} onClick={handleOpenEdit}>
              Cập Nhật Thông Tin
            </Button>
          ) : (
            <Button variant="primary" size="sm" icon={Plus} onClick={handleOpenEdit}>
              Tạo Hồ Sơ Ngay
            </Button>
          )}
        </div>

        <hr style={{ margin: '1rem 0', borderColor: 'var(--border-color)' }} />

        {isCandLoading ? (
          <Skeleton height="36px" width="100%" />
        ) : myCandidate ? (
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '1.25rem' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', flexWrap: 'wrap' }}>
              <div
                className="avatar-circle"
                style={{ width: '42px', height: '42px', fontSize: '1rem', backgroundColor: 'var(--primary-100)', color: 'var(--primary-700)' }}
              >
                {myCandidate.fullName ? myCandidate.fullName.charAt(0).toUpperCase() : 'U'}
              </div>
              <div>
                <div style={{ fontWeight: 600, color: 'var(--slate-900)', fontSize: '1rem' }}>
                  {myCandidate.fullName}
                </div>
                <div style={{ display: 'flex', gap: '1.25rem', marginTop: '0.25rem', flexWrap: 'wrap', fontSize: '0.85rem', color: 'var(--slate-600)' }}>
                  <span style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                    <Mail size={14} color="var(--slate-400)" />
                    {myCandidate.email}
                  </span>
                  <span style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                    <Phone size={14} color="var(--slate-400)" />
                    {myCandidate.phone || 'Chưa cập nhật SĐT'}
                  </span>
                </div>
              </div>
            </div>

            {/* CV / Resume Actions */}
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
              <span style={{ fontSize: '0.85rem', fontWeight: 500, color: 'var(--slate-700)' }}>
                CV hiện tại:
              </span>
              <ResumeUploadButton candidate={myCandidate} size="sm" />
            </div>
          </div>
        ) : (
          <div style={{ padding: '0.5rem 0', display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '1rem' }}>
            <span style={{ fontSize: '0.9rem', color: 'var(--slate-600)' }}>
              Bạn chưa có hồ sơ ứng viên. Hãy tạo hồ sơ để sẵn sàng nộp CV ứng tuyển chỉ với 1 click!
            </span>
          </div>
        )}
      </Card>

      {/* Applications List Section */}
      <h2 style={{ fontSize: '1.25rem', fontWeight: 600, color: 'var(--slate-900)', marginBottom: '1rem' }}>
        Danh Sách Đơn Đã Nộp ({applications.length})
      </h2>

      {isLoading ? (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          {Array.from({ length: 3 }).map((_, i) => (
            <Card key={i}>
              <Skeleton height="24px" width="50%" />
              <div style={{ marginTop: '0.75rem' }}>
                <Skeleton height="40px" />
              </div>
            </Card>
          ))}
        </div>
      ) : applications.length === 0 ? (
        <Card style={{ textAlign: 'center', padding: '3.5rem 1rem' }}>
          <div
            style={{
              width: '64px',
              height: '64px',
              borderRadius: 'var(--radius-full)',
              backgroundColor: 'var(--primary-50)',
              color: 'var(--primary-600)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              margin: '0 auto 1rem auto',
            }}
          >
            <FileText size={32} />
          </div>
          <h3>Bạn chưa nộp đơn ứng tuyển nào</h3>
          <p style={{ color: 'var(--text-muted)', maxWidth: '400px', margin: '0.5rem auto 1.5rem auto' }}>
            Hãy xem danh sách các vị trí đang tuyển dụng tại công ty và nộp hồ sơ ngay hôm nay!
          </p>
          <Link to="/jobs">
            <Button variant="primary" icon={Send}>
              Khám Phá Cơ Hội Việc Làm
            </Button>
          </Link>
        </Card>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
          {applications.map((app) => (
            <Card key={app.id}>
              <div
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'flex-start',
                  flexWrap: 'wrap',
                  gap: '1rem',
                }}
              >
                <div>
                  <h3 style={{ fontSize: '1.2rem', fontWeight: 600, color: 'var(--slate-900)' }}>
                    {app.jobTitle}
                  </h3>
                  <div
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: '1rem',
                      marginTop: '0.5rem',
                      color: 'var(--slate-600)',
                      fontSize: '0.85rem',
                    }}
                  >
                    <span style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                      <Calendar size={14} color="var(--slate-400)" />
                      Ngày nộp: {new Date(app.appliedDate).toLocaleDateString('vi-VN')}
                    </span>
                    <span>Mã đơn: #{app.id}</span>
                  </div>
                </div>

                <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                  {app.aiMatchScore && (
                    <Badge variant="success" icon={Sparkles}>
                      AI Match: {app.aiMatchScore}%
                    </Badge>
                  )}
                  <ApplicationStatusBadge status={app.status} />

                  {/* Cho phép rút đơn nếu trạng thái vẫn là Applied */}
                  {app.status === 'Applied' && (
                    <Button
                      variant="outline"
                      size="sm"
                      icon={Trash2}
                      onClick={() => handleDeleteApplication(app.id, app.jobTitle)}
                      title="Rút đơn ứng tuyển"
                      style={{ color: 'var(--danger-main)', borderColor: 'var(--danger-border)' }}
                    >
                      Rút đơn
                    </Button>
                  )}
                </div>
              </div>

              {app.aiSummary && (
                <div
                  style={{
                    marginTop: '1rem',
                    padding: '0.875rem',
                    backgroundColor: 'var(--slate-50)',
                    borderRadius: 'var(--radius-md)',
                    border: '1px solid var(--border-color)',
                    fontSize: '0.875rem',
                    color: 'var(--slate-700)',
                  }}
                >
                  <strong style={{ color: 'var(--primary-700)', display: 'flex', alignItems: 'center', gap: '0.35rem', marginBottom: '0.25rem' }}>
                    <Sparkles size={14} /> Đánh giá tự động từ AI:
                  </strong>
                  {app.aiSummary}
                </div>
              )}
            </Card>
          ))}
        </div>
      )}

      {/* Modal chỉnh sửa hoặc tạo hồ sơ */}
      <CandidateFormModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSave={handleSaveProfile}
        selectedCandidate={myCandidate}
        isLoading={isCreating || isUpdating}
      />
    </div>
  );
}
