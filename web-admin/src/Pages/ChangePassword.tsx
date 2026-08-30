import { useState } from "react";
import { api } from "../api";
import { useI18n } from "../i18n";
import { formatAuthApiError } from "../authErrorText";

export default function ChangePassword() {
  const { t } = useI18n();
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  const [err, setErr] = useState<string | null>(null);
  const [msg, setMsg] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    setMsg(null);

    if (!currentPassword) {
      setErr(t("validation.currentPasswordRequired", "Current password is required."));
      return;
    }

    if (!newPassword) {
      setErr(t("validation.newPasswordRequired", "New password is required."));
      return;
    }

    if (newPassword !== confirmPassword) {
      setErr(t("validation.passwordsMismatch", "Passwords do not match."));
      return;
    }

    try {
      setLoading(true);

      await api("/api/auth/change-password", {
        method: "POST",
        body: JSON.stringify({
          currentPassword,
          newPassword,
        }),
      });

      setMsg(t("account.passwordChanged", "Password changed."));
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
    } catch (ex: any) {
      setErr(formatAuthApiError(ex, t, t("account.passwordChangeFailed", "Change password failed")));
    } finally {
      setLoading(false);
    }
  }

  return (
    <div style={{ maxWidth: 420, margin: "40px auto", fontFamily: "Arial" }}>
      <h2 style={{ textAlign: "center" }}>{t("page.changePassword", "Change Password")}</h2>

      <form onSubmit={submit}>
        <div>
          <label>{t("account.currentPassword", "Current password")}</label>
          <input
            type="password"
            value={currentPassword}
            onChange={(e) => setCurrentPassword(e.target.value)}
            style={{ width: "100%" }}
          />
        </div>

        <div style={{ marginTop: 12 }}>
          <label>{t("account.newPassword", "New password")}</label>
          <input
            type="password"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            style={{ width: "100%" }}
          />
        </div>

        <div style={{ marginTop: 12 }}>
          <label>{t("account.confirmNewPassword", "Confirm new password")}</label>
          <input
            type="password"
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            style={{ width: "100%" }}
          />
        </div>

        {err && <div className="alert-error spaced-top-md">{err}</div>}
        {msg && <div className="alert-success spaced-top-md">{msg}</div>}

        <button style={{ marginTop: 16, width: "100%" }} disabled={loading}>
          {loading ? t("common.saving", "Saving...") : t("account.changePasswordAction", "Change password")}
        </button>
      </form>
    </div>
  );
}
