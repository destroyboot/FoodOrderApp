import { Navigate } from "react-router-dom";
import type { ReactNode } from "react";
import { getDefaultAuthorizedRoute, getToken, hasAnyRole } from "./auth";

export default function RequireAuth({
  children,
  allowedRoles = [],
  allowedFeatures = [],
  userFeatures = [],
  featuresLoaded = true,
  blockedRoles = [],
}: {
  children: ReactNode;
  allowedRoles?: string[];
  allowedFeatures?: string[];
  userFeatures?: string[];
  featuresLoaded?: boolean;
  blockedRoles?: string[];
}) {
  const token = getToken();
  if (!token) return <Navigate to="/login" replace />;

  if (allowedFeatures.length > 0) {
    if (!featuresLoaded) {
      return null;
    }

    const hasAllowedFeature = userFeatures.some((feature) => allowedFeatures.includes(feature));
    if (!hasAllowedFeature) {
      return <Navigate to={getDefaultAuthorizedRoute()} replace />;
    }
  } else if (!hasAnyRole(allowedRoles)) {
    return <Navigate to={getDefaultAuthorizedRoute()} replace />;
  }

  if (blockedRoles.length > 0 && hasAnyRole(blockedRoles)) {
    return <Navigate to={getDefaultAuthorizedRoute()} replace />;
  }

  return <>{children}</>;
}
