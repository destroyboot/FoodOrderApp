import type { ReactNode } from "react";

type PageShellProps = {
  title: ReactNode;
  children: ReactNode;
  error?: string | null;
  maxWidth?: number | string;
  className?: string;
  compact?: boolean;
};

export function PageShell({
  title,
  children,
  error,
  maxWidth = 1200,
  className,
  compact = false,
}: PageShellProps) {
  return (
    <div
      className={className}
      style={{
        maxWidth,
        margin: compact ? "0 auto" : "20px auto",
        fontFamily: "Arial",
      }}
    >
      <h2>{title}</h2>
      {error ? <div className={compact ? "alert-error spaced-top-md" : "alert-error"}>{error}</div> : null}
      {children}
    </div>
  );
}
