import { useI18n } from "../i18n";

type Props = {
  title: string;
  body: string;
  visible: boolean;
  onPress?: () => void;
  onClose: () => void;
};

export default function InAppNotificationToast({ title, body, visible, onPress, onClose }: Props) {
  const { t } = useI18n();

  if (!visible) {
    return null;
  }

  return (
    <div
      className={`notification-toast${onPress ? " notification-toast-action" : ""}`}
      onClick={onPress}
    >
      <div className="cluster cluster-between">
        <div className="stack-sm">
          <strong>{title}</strong>
          <span className="notification-toast-body">{body}</span>
        </div>
        <button
          onClick={(e) => {
            e.stopPropagation();
            onClose();
          }}
          className="icon-button-plain"
          aria-label={t("notifications.close", "Close notification")}
        >
          ×
        </button>
      </div>
    </div>
  );
}
