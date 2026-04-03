import { Navigate } from "react-router-dom";
import { getToken } from "./auth";
import type { ReactNode } from "react";

export default function RequireAuth({ children }: { children: ReactNode }) {
  const token = getToken();
  if (!token) return <Navigate to="/login" replace />;
  return <>{children}</>;
}