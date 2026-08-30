import { useMemo, useState } from "react";
import { api } from "../api";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { useI18n } from "../i18n";
import { formatAuthApiError } from "../authErrorText";

export default function ResetPassword() {
  const { t } = useI18n();
  const nav = useNavigate();
  const [params] = useSearchParams();

  const emailFromQuery = useMemo(() => params.get("email") ?? "", [params]);
  const tokenFromQuery = useMemo(() => params.get("token") ?? "", [params]);

  const [email, setEmail] = useState(emailFromQuery);
  const [token, setToken] = useState(tokenFromQuery);
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  const [err, setErr] = useState<string | null>(null);
  const [msg, setMsg] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    setMsg(null);

    if (!email.trim()) {
      setErr(t("validation.emailRequired", "Email is required."));
      return;
    }

    if (!token.trim()) {
      setErr(t("validation.resetTokenRequired", "Reset token is required."));
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

      await api("/api/auth/reset-password", {
        method: "POST",
        body: JSON.stringify({
          email,
          token,
          newPassword,
        }),
      });

      setMsg(t("auth.passwordResetSuccess", "Password has been reset successfully."));
      setTimeout(() => nav("/login"), 1000);
    } catch (ex: any) {
      setErr(formatAuthApiError(ex, t, t("auth.resetFailed", "Reset failed")));
    } finally {
      setLoading(false);
    }
  }

  return (
    <div style={{ maxWidth: 420, margin: "40px auto", fontFamily: "Arial" }}>
      <h2 style={{ textAlign: "center" }}>{t("page.resetPassword", "Reset Password")}</h2>

      <form onSubmit={submit}>
        <div>
          <label>{t("common.email", "Email")}</label>
          <input
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            style={{ width: "100%" }}
          />
        </div>

        <div style={{ marginTop: 12 }}>
          <label>{t("auth.resetToken", "Reset token")}</label>
          <input
            value={token}
            onChange={(e) => setToken(e.target.value)}
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
          <label>{t("common.confirmPassword", "Confirm password")}</label>
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
          {loading ? t("auth.resettingPassword", "Resetting...") : t("auth.resetPassword", "Reset password")}
        </button>
      </form>

      <div style={{ marginTop: 16, textAlign: "center" }}>
        <Link to="/login">{t("auth.backToLogin", "Back to login")}</Link>
      </div>
    </div>
  );
}
