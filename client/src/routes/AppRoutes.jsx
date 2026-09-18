import { Routes, Route, Navigate } from 'react-router-dom';
import { MainLayout, AuthLayout } from '../layouts';
import ProtectedRoute from './ProtectedRoute';
import UnauthorizedPage from './UnauthorizedPage';
import NotFoundPage from './NotFoundPage';

// Features
import { LoginForm, RegisterForm, ForgotPasswordForm, ResetPasswordForm, VerifyEmail, useAuthStore } from '../features/auth';

// Wrapper: redirect authenticated users away from auth pages
function GuestOnly({ children }) {
  const { isAuthenticated } = useAuthStore();
  if (isAuthenticated()) return <Navigate to="/jobs" replace />;
  return children;
}
import { DashboardOverview } from '../features/dashboard';
import { EmployeeTable, MyProfilePage, MyDepartmentPage } from '../features/employees';
import { DepartmentList } from '../features/departments';
import { JobPostingList, JobPostingDetail } from '../features/recruitment/jobs';
import { CandidateTable } from '../features/recruitment/candidates';
import { ApplicationTable, MyApplicationsList } from '../features/recruitment/applications';
import { InterviewList } from '../features/interviews';

function HomeRedirect() {
  const { role, isAuthenticated } = useAuthStore();

  if (!isAuthenticated()) {
    return <Navigate to="/jobs" replace />;
  }

  switch (role) {
    case 'Admin':
    case 'HR':
      return <Navigate to="/dashboard" replace />;
    case 'Interviewer':
      return <Navigate to="/interviews" replace />;
    case 'Candidate':
      return <Navigate to="/my-applications" replace />;
    case 'Employee':
      return <Navigate to="/profile" replace />;
    default:
      return <Navigate to="/jobs" replace />;
  }
}

export default function AppRoutes() {
  return (
    <Routes>
      {/* Auth Layout Routes */}
      <Route element={<AuthLayout />}>
        <Route path="/login" element={<LoginForm />} />
        <Route path="/register" element={<RegisterForm />} />
        <Route path="/verify-email" element={<GuestOnly><VerifyEmail /></GuestOnly>} />
        <Route path="/forgot-password" element={<GuestOnly><ForgotPasswordForm /></GuestOnly>} />
        <Route path="/reset-password" element={<GuestOnly><ResetPasswordForm /></GuestOnly>} />
      </Route>

      {/* Main Layout Routes */}
      <Route element={<MainLayout />}>
        <Route path="/" element={<HomeRedirect />} />

        {/* Public Recruitment Job Postings */}
        <Route path="/jobs" element={<JobPostingList />} />
        <Route path="/jobs/:id" element={<JobPostingDetail />} />

        {/* Candidate Routes */}
        <Route
          path="/my-applications"
          element={
            <ProtectedRoute allowedRoles={['Candidate', 'Employee']}>
              <MyApplicationsList />
            </ProtectedRoute>
          }
        />

        {/* Employee Personal Routes */}
        <Route
          path="/profile"
          element={
            <ProtectedRoute allowedRoles={['Employee', 'Admin', 'HR']}>
              <MyProfilePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/my-department"
          element={
            <ProtectedRoute allowedRoles={['Employee', 'Admin', 'HR']}>
              <MyDepartmentPage />
            </ProtectedRoute>
          }
        />

        {/* Recruitment Management Routes (Admin, HR, Interviewer) */}
        <Route
          path="/candidates"
          element={
            <ProtectedRoute allowedRoles={['Admin', 'HR', 'Interviewer']}>
              <CandidateTable />
            </ProtectedRoute>
          }
        />
        <Route
          path="/applications"
          element={
            <ProtectedRoute allowedRoles={['Admin', 'HR', 'Interviewer']}>
              <ApplicationTable />
            </ProtectedRoute>
          }
        />
        <Route
          path="/interviews"
          element={
            <ProtectedRoute allowedRoles={['Admin', 'HR', 'Interviewer']}>
              <InterviewList />
            </ProtectedRoute>
          }
        />

        {/* Administrative Routes (Admin, HR only) */}
        <Route
          path="/dashboard"
          element={
            <ProtectedRoute allowedRoles={['Admin', 'HR']}>
              <DashboardOverview />
            </ProtectedRoute>
          }
        />
        <Route
          path="/employees"
          element={
            <ProtectedRoute allowedRoles={['Admin', 'HR']}>
              <EmployeeTable />
            </ProtectedRoute>
          }
        />
        <Route
          path="/departments"
          element={
            <ProtectedRoute allowedRoles={['Admin', 'HR']}>
              <DepartmentList />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/jobs"
          element={
            <ProtectedRoute allowedRoles={['Admin', 'HR']}>
              <JobPostingList />
            </ProtectedRoute>
          }
        />

        {/* Status & Feedback Routes */}
        <Route path="/unauthorized" element={<UnauthorizedPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  );
}
