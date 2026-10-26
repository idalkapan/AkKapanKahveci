"use client";

import { useCoffeeShop } from "../hooks/useCoffeeShop";
import { ProductMenu } from "../components/ProductMenu";
import { ProductDialog } from "../components/ProductDialog";
import { CartDrawer } from "../components/CartDrawer";
export default function Home() {
  const {
    user,
    products,
    category,
    setCategory,
    concept,
    setConcept,
    cart,
    setCart,
    open,
    setOpen,
    selected,
    setSelected,
    custom,
    setCustom,
    text,
    setText,
    color,
    setColor,
    student,
    notice,
    setNotice,
    busy,
    customer,
    setCustomer,
    subtotal,
    cupFee,
    discount,
    count,
    add,
    upload,
    checkout,
    logout,
    refreshStudent,
  } = useCoffeeShop();
  if (!user)
    return (
      <main className="auth-shell">
        <p role="status">Oturum kontrol ediliyor…</p>
      </main>
    );
  return (
    <main data-theme={concept}>
      <div className="announcement">
        HER YUDUMDA BİRAZ RENK, BİRAZ MUTLULUK <span>✦</span> Öğrencilere %10
        indirim
      </div>
      <header>
        <a className="brand" href="#">
          ak kapan<span>COFFEE & GOOD MOOD</span>
        </a>
        <nav>
          <a href="#menu">Menümüz</a>
          <a href="#cup">Sana özel</a>
          <a href="#student">Öğrenci kulübü</a>
        </nav>
        <button className="text-link account-logout" onClick={logout}>
          {user.name} · Çıkış
        </button>
        <button className="cart-button" onClick={() => setOpen(true)}>
          Sepetim <span>{count}</span>
        </button>
      </header>
      <section className="hero">
        <div className="hero-copy">
          <div className="eyebrow">SIRADAN BİR KAHVE MOLASINDAN FAZLASI</div>
          <h1>
            Hayat renkli.
            <br />
            Kahven de
            <br />
            <em>öyle olsun.</em>
            <span className="spark">✳</span>
          </h1>
          <p>
            Bir fincan iyi kahve, biraz hayal gücü ve bolca güzel his. Kendi
            rengini bul, kahveni seç, anın tadını çıkar.
          </p>
          <a className="primary" href="#menu">
            Kahveni keşfet <span>↗</span>
          </a>
          <div className="hero-note">
            <span>✿</span> Küçük molalar. Büyük mutluluklar.
          </div>
        </div>
        <div className="hero-art">
          <div className="orbit">FRESH COFFEE · FRESH MOOD ·</div>
          <span className="art-star">✦</span>
          <div className="hero-cup">
            <div className="lid" />
            <div className="cup-body">
              <span>
                ak
                <br />
                kapan
              </span>
              <small>sip, smile, repeat.</small>
              <div className="cup-flower">✿</div>
            </div>
          </div>
          <div className="sticker">
            %100
            <br />
            <b>iyi his</b>
          </div>
          <div className="art-caption">YOUR DAILY DOSE OF HAPPY.</div>
          <div className="bean bean1" />
          <div className="bean bean2" />
        </div>
      </section>
      <section className="theme-bar">
        <div>
          <span className="eyebrow">BUGÜN HANGİ MODDASIN?</span>
          <p>Senin kahven, senin konseptin.</p>
        </div>
        <div className="theme-options">
          {[
            ["candy", "🍬", "Candy Club"],
            ["forest", "🌿", "Green Escape"],
            ["sunset", "☀", "Sunset Mood"],
          ].map(([id, icon, label]) => (
            <button
              key={id}
              aria-pressed={concept === id}
              className={concept === id ? "active" : ""}
              onClick={() => setConcept(id)}
            >
              {icon} {label}
            </button>
          ))}
        </div>
      </section>

      <ProductMenu
        products={products}
        category={category}
        setCategory={setCategory}
        setSelected={setSelected}
        setCustom={setCustom}
      />
      <section className="extras">
        <article className="custom-banner" id="cup">
          <div className="eyebrow">BİR BARDAKTAN DAHA FAZLASI</div>
          <h2>
            Üzerinde <em>sen varsın.</em>
          </h2>
          <p>
            İsmin, favori sözün, seçtiğin renk. Kahveni sana özel tasarım
            bardakta yudumla.
          </p>
          <a href="#menu" className="text-link">
            Kahveni seç & bardağını tasarla ↗
          </a>
          <div className="tiny-cup">
            Senin
            <br />
            hikâyen ✿
          </div>
          <span className="fee">+45 TL</span>
        </article>
        <article className="student-banner" id="student">
          <span className="eyebrow">KAMPÜSTEN KAHVEYE</span>
          <h2>
            Ders arası?
            <br />
            <em>%10 bizden.</em>
          </h2>
          <p>
            Öğrenci belgeni yükle, onaylandıktan sonra kahve ve tatlılarında
            indirim kazan.
          </p>
          <label className="upload">
            {busy
              ? "Yükleniyor…"
              : student === "approved"
              ? "✓ Öğrenci indirimin aktif"
              : student === "pending"
              ? "◷ Belgen incelemede"
              : "Öğrenci belgeni yükle ↗"}
            <input
              type="file"
              accept=".pdf,.png,.jpg,.jpeg"
              disabled={busy || student === "pending" || student === "approved"}
              onChange={(e) => upload(e.target.files?.[0])}
            />
          </label>
          <small>
            PDF, JPG veya PNG · En fazla 5 MB ·{" "}
            {student === "rejected"
              ? "Belge onaylanmadı; yeni belge yükleyebilirsin."
              : "Bardak tasarım ücreti indirime dahil değildir."}
          </small>
          {student === "pending" && (
            <button className="text-link" onClick={refreshStudent}>
              Onay durumunu yenile
            </button>
          )}
        </article>
      </section>
      <section className="closing">
        <span>GOOD COFFEE.</span>
        <span>GOOD COMPANY.</span>
        <span>GOOD MOOD. ✳</span>
      </section>
      <footer>
        <a className="brand" href="#">
          ak kapan<span>COFFEE & GOOD MOOD</span>
        </a>
        <p>Kahvenin en renkli hali. Seninle daha güzel.</p>
        <span>© {new Date().getFullYear()} Ak Kapan Coffee</span>
      </footer>
      {notice && (
        <div className="toast" role="status">
          {notice}
          <button aria-label="Bildirimi kapat" onClick={() => setNotice("")}>
            ×
          </button>
        </div>
      )}

      <ProductDialog
        selected={selected}
        setSelected={setSelected}
        custom={custom}
        setCustom={setCustom}
        color={color}
        setColor={setColor}
        text={text}
        setText={setText}
        add={add}
      />

      <CartDrawer
        open={open}
        setOpen={setOpen}
        count={count}
        cart={cart}
        setCart={setCart}
        subtotal={subtotal}
        cupFee={cupFee}
        discount={discount}
        customer={customer}
        setCustomer={setCustomer}
        checkout={checkout}
        busy={busy}
      />
    </main>
  );
}
