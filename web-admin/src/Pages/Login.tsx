import { useState } from "react";
import { api } from "../api";
import { setToken } from "../auth";

export default function Login({ onDone }: { onDone: () => void }) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [err, setErr] = useState<string | null>(null);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    try {
      // adjust path/body to match your login endpoint
      const resp = await api<{ token: string }>("/api/auth/login", {
        method: "POST",
        body: JSON.stringify({ email, password }),
      });
      setToken(resp.token);
      onDone();
    } catch (ex: any) {
      setErr(ex.message || "Login failed");
    }
  }

  return (
    <div style={{ maxWidth: 420, margin: "40px auto", fontFamily: "Arial" }}>
      <h2>Staff Login</h2>
      <form onSubmit={submit}>
        <div>
          <label>Email</label>
          <input value={email} onChange={(e) => setEmail(e.target.value)} style={{ width: "100%" }} />
        </div>
        <div style={{ marginTop: 12 }}>
          <label>Password</label>
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} style={{ width: "100%" }} />
        </div>
        {err && <div style={{ color: "crimson", marginTop: 12 }}>{err}</div>}
        <button style={{ marginTop: 16, width: "100%" }}>Login</button>
      </form>
    </div>
  );
}