import type { ReactNode } from "react";
import { useI18n } from "../i18n";

type ModalShellProps = {
  title: string;
  children: ReactNode;
  onClose: () => void;
  className?: string;
  width?: number;
  minWidth?: string;
  maxWidth?: number | string;
};

export function ModalShell({
  title,
  children,
  onClose,
  className,
  width,
  minWidth,
  maxWidth,
}: ModalShellProps) {
  const { t } = useI18n();
  const style = {
    ...(width ? { width: `min(${width}px, calc(100vw - 32px))` } : null),
    ...(minWidth ? { minWidth } : null),
    ...(maxWidth ? { maxWidth } : null),
  };

  return (
    <div className="modal-backdrop">
      <div className={`modal-card${className ? ` ${className}` : ""}`} style={style}>
        <div className="modal-header">
          <h3>{title}</h3>
          <button onClick={onClose}>{t("common.close", "Close")}</button>
        </div>
        {children}
      </div>
    </div>
  );
}
