import { Navigate } from "react-router-dom";
import type { ReactNode } from "react";
import { getDefaultAuthorizedRoute, getToken, hasAnyRole } from "./auth";

export default function RequireAuth({
  children,
  allowedRoles = [],
  blockedRoles = [],
}: {
  children: ReactNode;
  allowedRoles?: string[];
  blockedRoles?: string[];
}) {
  const token = getToken();
  if (!token) return <Navigate to="/login" replace />;

  if (!hasAnyRole(allowedRoles)) {
    return <Navigate to={getDefaultAuthorizedRoute()} replace />;
  }

  if (blockedRoles.length > 0 && hasAnyRole(blockedRoles)) {
    return <Navigate to={getDefaultAuthorizedRoute()} replace />;
  }

  return <>{children}</>;
}
