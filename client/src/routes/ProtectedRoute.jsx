import { Navigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '../features/auth';

export default function ProtectedRoute({ children, allowedRoles = [] }) {
  const { role, isAuthenticated } = useAuthStore();
  const location = useLocation();

  if (!isAuthenticated()) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  if (allowedRoles.length > 0) {
    const hasRequiredRole = allowedRoles
      .map((r) => r.toLowerCase())
      .includes(role?.toLowerCase());

    if (!hasRequiredRole) {
      return <Navigate to="/unauthorized" replace />;
    }
  }

  return children;
}
