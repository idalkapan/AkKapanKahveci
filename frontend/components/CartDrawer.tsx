import type { useCoffeeShop } from "../hooks/useCoffeeShop";
import { money } from "../lib/money";
type Props = Pick<
  ReturnType<typeof useCoffeeShop>,
  | "open"
  | "setOpen"
  | "count"
  | "cart"
  | "setCart"
  | "subtotal"
  | "cupFee"
  | "discount"
  | "customer"
  | "setCustomer"
  | "checkout"
  | "busy"
>;
export function CartDrawer({
  open,
  setOpen,
  count,
  cart,
  setCart,
  subtotal,
  cupFee,
  discount,
  customer,
  setCustomer,
  checkout,
  busy,
}: Props) {
  return (
    open && (
      <div className="overlay" onClick={() => setOpen(false)}>
        <section
          className="drawer"
          role="dialog"
          aria-modal="true"
          aria-label="Sepetim"
          onClick={(e) => e.stopPropagation()}
        >
          <button
            className="close"
            aria-label="Kapat"
            onClick={() => setOpen(false)}
          >
            ×
          </button>
          <span className="eyebrow">GÜZEL BİR MOLA YOLDA</span>
          <h2>
            Sepetim <em>({count})</em>
          </h2>
          {!cart.length ? (
            <div className="empty">
              Sepetin biraz mutluluk bekliyor. ☕
              <button className="primary wide" onClick={() => setOpen(false)}>
                Kahveni seç
              </button>
            </div>
          ) : (
            <>
              <div className="cart-lines">
                {cart.map((l, i) => (
                  <div className="cart-line" key={i}>
                    <span
                      className="line-icon"
                      style={{
                        background: l.product.color + "55",
                      }}
                    >
                      {l.product.emoji}
                    </span>
                    <div>
                      <b>{l.product.name}</b>
                      {l.customCup && (
                        <small>Özel bardak: {l.cupText} · +45 TL</small>
                      )}
                      <div className="quantity">
                        <button
                          aria-label="Azalt"
                          onClick={() =>
                            setCart((c) =>
                              c.flatMap((x, j) =>
                                j === i
                                  ? x.quantity === 1
                                    ? []
                                    : [
                                        {
                                          ...x,
                                          quantity: x.quantity - 1,
                                        },
                                      ]
                                  : [x]
                              )
                            )
                          }
                        >
                          −
                        </button>
                        {l.quantity}
                        <button
                          aria-label="Artır"
                          disabled={l.quantity >= 20}
                          onClick={() =>
                            setCart((c) =>
                              c.map((x, j) =>
                                j === i
                                  ? {
                                      ...x,
                                      quantity: x.quantity + 1,
                                    }
                                  : x
                              )
                            )
                          }
                        >
                          +
                        </button>
                        <button
                          onClick={() =>
                            setCart((c) => c.filter((_, j) => i !== j))
                          }
                        >
                          Sil
                        </button>
                      </div>
                    </div>
                    <strong>
                      {money(
                        (l.product.price + (l.customCup ? 45 : 0)) * l.quantity
                      )}
                    </strong>
                  </div>
                ))}
              </div>
              <div className="totals">
                <p>
                  Ürünler <span>{money(subtotal)}</span>
                </p>
                <p>
                  Özel bardak <span>{money(cupFee)}</span>
                </p>
                {discount > 0 && (
                  <p>
                    Öğrenci indirimi (%10) <span>−{money(discount)}</span>
                  </p>
                )}
                <p className="total">
                  Toplam <span>{money(subtotal + cupFee - discount)}</span>
                </p>
              </div>
              <form
                onSubmit={(e) => {
                  e.preventDefault();
                  checkout();
                }}
              >
                <label>
                  Siparişin için adın
                  <input
                    required
                    maxLength={80}
                    value={customer}
                    onChange={(e) => setCustomer(e.target.value)}
                    placeholder="Adın"
                  />
                </label>
                <button
                  className="primary wide"
                  disabled={busy || !customer.trim()}
                >
                  {busy ? "İşleniyor…" : "Demo ödemeyi tamamla ↗"}
                </button>
              </form>
              <p className="payment-note">
                Demo ödeme: kart bilgisi istenmez, gerçek ücret alınmaz. Sipariş
                kafeden teslim alınır.
              </p>
            </>
          )}
        </section>
      </div>
    )
  );
}
