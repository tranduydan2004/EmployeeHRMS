import { useAuthStore } from '../features/auth/useAuthStore';

export function usePermission() {
  const role = useAuthStore((state) => state.role);
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated());

  const isAdmin = role === 'Admin';
  const isHR = role === 'HR';
  const isInterviewer = role === 'Interviewer';
  const isCandidate = role === 'Candidate';
  const isEmployee = role === 'Employee';

  const canManageEmployees = isAdmin || isHR;
  const canManageDepartments = isAdmin || isHR;
  const canManageJobPostings = isAdmin || isHR;
  const canViewRecruitment = isAdmin || isHR || isInterviewer;

  const hasRole = (allowedRoles = []) => {
    if (!role) return false;
    return allowedRoles.map((r) => r.toLowerCase()).includes(role.toLowerCase());
  };

  return {
    role,
    isAuthenticated,
    isAdmin,
    isHR,
    isInterviewer,
    isCandidate,
    isEmployee,
    canManageEmployees,
    canManageDepartments,
    canManageJobPostings,
    canViewRecruitment,
    hasRole,
  };
}
