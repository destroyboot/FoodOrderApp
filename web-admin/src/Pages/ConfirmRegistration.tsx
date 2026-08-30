import { useMemo, useState } from "react";
import { api } from "../api";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { useI18n } from "../i18n";
import { formatAuthApiError } from "../authErrorText";

export default function ConfirmRegistration() {
  const { t } = useI18n();
  const nav = useNavigate();
  const [params] = useSearchParams();

  const emailFromQuery = useMemo(() => params.get("email") ?? "", [params]);

  const [email, setEmail] = useState(emailFromQuery);
  const [code, setCode] = useState("");
  const [err, setErr] = useState<string | null>(null);
  const [msg, setMsg] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [resending, setResending] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    setMsg(null);

    if (!email.trim()) {
      setErr(t("validation.emailRequired", "Email is required."));
      return;
    }

    if (!code.trim()) {
      setErr(t("validation.confirmationCodeRequired", "Confirmation code is required."));
      return;
    }

    try {
      setLoading(true);

      await api("/api/auth/confirm-registration", {
        method: "POST",
        body: JSON.stringify({
          email,
          code,
        }),
      });

      setMsg(t("auth.accountConfirmed", "Account confirmed. You can now log in."));
      setTimeout(() => nav("/login"), 1000);
    } catch (ex: any) {
      setErr(formatAuthApiError(ex, t, t("auth.confirmationFailed", "Confirmation failed")));
    } finally {
      setLoading(false);
    }
  }

  async function resendCode() {
    setErr(null);
    setMsg(null);

    if (!email.trim()) {
      setErr(t("validation.emailRequired", "Email is required."));
      return;
    }

    try {
      setResending(true);

      await api("/api/auth/resend-registration-code", {
        method: "POST",
        body: JSON.stringify({
          email,
        }),
      });

      setMsg(t("auth.registrationCodeResent", "If the email exists, a new code was sent."));
    } catch (ex: any) {
      setErr(formatAuthApiError(ex, t, t("auth.resendFailed", "Resend failed")));
    } finally {
      setResending(false);
    }
  }

  return (
    <div style={{ maxWidth: 420, margin: "40px auto", fontFamily: "Arial" }}>
      <h2 style={{ textAlign: "center" }}>{t("auth.confirmRegistration", "Confirm Registration")}</h2>

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
          <label>{t("common.confirmationCode", "Confirmation code")}</label>
          <input
            value={code}
            onChange={(e) => setCode(e.target.value)}
            style={{ width: "100%" }}
          />
        </div>

        {err && <div className="alert-error spaced-top-md">{err}</div>}
        {msg && <div className="alert-success spaced-top-md">{msg}</div>}

        <button style={{ marginTop: 16, width: "100%" }} disabled={loading}>
          {loading ? t("auth.confirming", "Confirming...") : t("auth.confirmRegistration", "Confirm registration")}
        </button>
      </form>

      <button
        type="button"
        onClick={resendCode}
        disabled={resending}
        style={{ marginTop: 12, width: "100%" }}
      >
        {resending ? t("auth.resending", "Resending...") : t("auth.resendCode", "Resend code")}
      </button>

      <div style={{ marginTop: 16, textAlign: "center" }}>
        <Link to="/login">{t("auth.backToLogin", "Back to login")}</Link>
      </div>
    </div>
  );
}
