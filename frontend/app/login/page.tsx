"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { request } from "../../lib/api";
export default function LoginPage() {
  const router = useRouter();
  const [register, setRegister] = useState(false),
    [busy, setBusy] = useState(false),
    [error, setError] = useState("");
  async function submit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setBusy(true);
    setError("");
    const data = new FormData(e.currentTarget);
    try {
      await request("auth/" + (register ? "register" : "login"), {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(Object.fromEntries(data)),
      });
      router.replace("/");
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <main className="auth-shell">
      <section className="auth-card">
        <a className="brand" href="/">
          ak kapan<span>COFFEE & GOOD MOOD</span>
        </a>
        <div className="auth-flower" aria-hidden="true">
          ✿
        </div>
        <span className="eyebrow">KAHVE MOLANA HOŞ GELDİN</span>
        <h1>{register ? "Aramıza katıl." : "Yeniden merhaba."}</h1>
        <p>Kahven, sana özel bardağın ve öğrenci avantajların tek hesabında.</p>
        <form onSubmit={submit}>
          {register && (
            <label>
              Adın
              <input
                name="name"
                autoComplete="name"
                required
                minLength={2}
                maxLength={80}
              />
            </label>
          )}
          <label>
            E-posta
            <input
              name="email"
              type="email"
              autoComplete="email"
              required
              maxLength={254}
            />
          </label>
          <label>
            Şifre
            <input
              name="password"
              type="password"
              autoComplete={register ? "new-password" : "current-password"}
              required
              minLength={register ? 10 : 1}
              maxLength={128}
            />
          </label>
          {register && <small>En az 10 karakter kullan.</small>}
          {error && (
            <p className="auth-error" role="alert">
              {error}
            </p>
          )}
          <button className="primary wide" disabled={busy}>
            {busy ? "İşleniyor…" : register ? "Hesap oluştur ↗" : "Giriş yap ↗"}
          </button>
        </form>
        <button
          className="text-link"
          disabled={busy}
          onClick={() => {
            setRegister(!register);
            setError("");
          }}
        >
          {register
            ? "Zaten hesabın var mı? Giriş yap"
            : "Hesabın yok mu? Kayıt ol"}
        </button>
      </section>
    </main>
  );
}
