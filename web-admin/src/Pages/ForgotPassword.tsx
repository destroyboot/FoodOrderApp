import { useState } from "react";
import { api } from "../api";
import { Link } from "react-router-dom";
import { useI18n } from "../i18n";
import { formatAuthApiError } from "../authErrorText";

export default function ForgotPassword() {
  const { t } = useI18n();
  const [email, setEmail] = useState("");
  const [msg, setMsg] = useState<string | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    setMsg(null);

    if (!email.trim()) {
      setErr(t("validation.emailRequired", "Email is required."));
      return;
    }

    try {
      setLoading(true);

      await api("/api/auth/forgot-password", {
        method: "POST",
        body: JSON.stringify({ email }),
      });

      setMsg(t("auth.resetLinkSent", "If this email exists, a reset link has been sent."));
    } catch (ex: any) {
      setErr(formatAuthApiError(ex, t, t("auth.requestFailed", "Request failed")));
    } finally {
      setLoading(false);
    }
  }

  return (
    <div style={{ maxWidth: 420, margin: "40px auto", fontFamily: "Arial" }}>
      <h2 style={{ textAlign: "center" }}>{t("page.forgotPassword", "Forgot Password")}</h2>

      <form onSubmit={submit}>
        <label>{t("common.email", "Email")}</label>
        <input
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          style={{ width: "100%" }}
        />

        {err && <div className="alert-error spaced-top-md">{err}</div>}
        {msg && <div className="alert-success spaced-top-md">{msg}</div>}

        <button style={{ marginTop: 16, width: "100%" }} disabled={loading}>
          {loading ? t("common.sending", "Sending...") : t("auth.sendResetLink", "Send reset link")}
        </button>
      </form>

      <div style={{ marginTop: 16, textAlign: "center" }}>
        <Link to="/login">{t("auth.backToLogin", "Back to login")}</Link>
      </div>
    </div>
  );
}
