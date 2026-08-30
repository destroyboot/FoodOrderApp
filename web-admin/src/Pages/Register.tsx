import { useState } from "react";
import { api } from "../api";
import { Link, useNavigate } from "react-router-dom";
import { useI18n } from "../i18n";
import { formatAuthApiError } from "../authErrorText";

export default function Register() {
  const { t } = useI18n();
  const nav = useNavigate();

  const [email, setEmail] = useState("");
  const [userName, setUserName] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [err, setErr] = useState<string | null>(null);
  const [msg, setMsg] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    setMsg(null);

    if (!email.trim()) {
      setErr(t("validation.emailRequired", "Email is required."));
      return;
    }

    if (!userName.trim()) {
      setErr(t("validation.userNameRequired", "User name is required."));
      return;
    }

    if (!password) {
      setErr(t("validation.passwordRequired", "Password is required."));
      return;
    }

    if (password !== confirmPassword) {
      setErr(t("validation.passwordsMismatch", "Passwords do not match."));
      return;
    }

    try {
      setSaving(true);

      await api("/api/auth/register", {
        method: "POST",
        body: JSON.stringify({
          email,
          password,
        }),
      });

        setMsg(t("auth.registrationCodeSent", "Confirmation code sent to your email. Please confirm registration."));
        setTimeout(() => nav(`/confirm-registration?email=${encodeURIComponent(email)}`), 1000);
    } catch (ex: any) {
      setErr(formatAuthApiError(ex, t, t("auth.registrationFailed", "Registration failed")));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div style={{ maxWidth: 420, margin: "40px auto", fontFamily: "Arial" }}>
      <h2 style={{ textAlign: "center" }}>{t("auth.register", "Register")}</h2>

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
          <label>{t("common.userName", "User name")}</label>
          <input
            value={userName}
            onChange={(e) => setUserName(e.target.value)}
            style={{ width: "100%" }}
          />
        </div>

        <div style={{ marginTop: 12 }}>
          <label>{t("common.password", "Password")}</label>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
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

        <button disabled={saving} style={{ marginTop: 16, width: "100%" }}>
          {saving ? t("auth.creatingAccount", "Creating account...") : t("auth.register", "Register")}
        </button>
      </form>

      <div style={{ marginTop: 16, textAlign: "center" }}>
        <Link to="/login">{t("auth.backToLogin", "Back to login")}</Link>
      </div>
    </div>
  );
}
