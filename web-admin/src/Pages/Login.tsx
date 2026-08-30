import { useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api";
import { clearToken, getDefaultAuthorizedRoute, setToken } from "../auth";
import { useI18n } from "../i18n";
import { formatAuthApiError } from "../authErrorText";

export default function Login({ onDone }: { onDone: (path: string) => void }) {
  const { languages, culture, setCulture, t } = useI18n();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [err, setErr] = useState<string | null>(null);
  const [languagePickerOpen, setLanguagePickerOpen] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    clearToken();

    try {
      const resp = await api<{ token: string }>("/api/auth/login", {
        method: "POST",
        body: JSON.stringify({ email, password }),
      });

      setToken(resp.token);
      onDone(getDefaultAuthorizedRoute());
    } catch (ex: any) {
      setErr(formatAuthApiError(ex, t, t("auth.loginFailed", "Login failed")));
    }
  }

  return (
    <div style={{ maxWidth: 420, margin: "40px auto", fontFamily: "Arial" }}>
      <div style={{ display: "flex", justifyContent: "flex-end", marginBottom: 10 }}>
        {languages.length > 0 ? (
          <button
            type="button"
            onClick={() => setLanguagePickerOpen(true)}
            aria-label={t("common.language", "Language")}
            style={{ justifySelf: "end", minWidth: 68, whiteSpace: "nowrap" }}
          >
            {"\u{1F310}"} {culture.slice(0, 2).toUpperCase()}
          </button>
        ) : null}
      </div>
      <h2 style={{ margin: "0 0 20px", textAlign: "center" }}>{t("auth.staffLogin", "Staff Login")}</h2>

      {languagePickerOpen ? (
        <div
          role="presentation"
          onClick={() => setLanguagePickerOpen(false)}
          style={{ position: "fixed", inset: 0, zIndex: 1000, display: "grid", placeItems: "center", padding: 20, background: "rgba(15, 23, 42, 0.45)" }}
        >
          <div
            role="dialog"
            aria-modal="true"
            aria-label={t("common.selectLanguage", "Select language")}
            onClick={(event) => event.stopPropagation()}
            style={{ width: "min(100%, 360px)", display: "grid", gap: 10, padding: 20, borderRadius: 8, background: "#fff", boxShadow: "0 22px 60px rgba(15, 23, 42, 0.3)" }}
          >
            <strong>{t("common.selectLanguage", "Select language")}</strong>
            {languages.map((language) => (
              <button
                key={language.culture}
                type="button"
                onClick={() => {
                  void setCulture(language.culture);
                  setLanguagePickerOpen(false);
                }}
                style={{ textAlign: "left", background: language.culture === culture ? "#e8f0ff" : "#fff" }}
              >
                {language.nativeName}
              </button>
            ))}
            <button type="button" onClick={() => setLanguagePickerOpen(false)}>{t("common.close", "Close")}</button>
          </div>
        </div>
      ) : null}

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
          <label>{t("common.password", "Password")}</label>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            style={{ width: "100%" }}
          />
        </div>

        {err && <div className="alert-error spaced-top-md">{err}</div>}

        <button style={{ marginTop: 16, width: "100%" }}>
          {t("auth.login", "Log in")}
        </button>
      </form>

      <div style={{ marginTop: 16, display: "flex", justifyContent: "space-between" }}>
        <Link to="/register">{t("auth.createAccount", "Create account")}</Link>
        <Link to="/forgot-password">{t("auth.forgotPassword", "Forgot password?")}</Link>
      </div>
    </div>
  );
}
