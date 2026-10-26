import type { useCoffeeShop } from "../hooks/useCoffeeShop";
import { money } from "../lib/money";
type Props = Pick<
  ReturnType<typeof useCoffeeShop>,
  "products" | "category" | "setCategory" | "setSelected" | "setCustom"
>;
export function ProductMenu({
  products,
  category,
  setCategory,
  setSelected,
  setCustom,
}: Props) {
  return (
    <section className="menu-section" id="menu">
      <div className="section-heading">
        <div className="eyebrow">İYİ BİR MOLA BURADA BAŞLAR</div>
        <h2>
          Bir yudum <em>mutluluk.</em>
        </h2>
        <p>Özenle hazırlanmış kahveler, rengiyle gülümseten tatlar.</p>
      </div>
      <div className="menu-toolbar">
        <div className="tabs">
          <button
            className={category === "coffee" ? "active" : ""}
            onClick={() => setCategory("coffee")}
          >
            Kahveler <span>10</span>
          </button>
          <button
            className={category === "dessert" ? "active" : ""}
            onClick={() => setCategory("dessert")}
          >
            Tatlı molası <span>4</span>
          </button>
        </div>
        <span>Sevgiyle hazırlanır. Taze servis edilir. ♡</span>
      </div>
      <div className="products">
        {products
          .filter((p) => p.category === category)
          .map((p) => (
            <article className="product" key={p.id}>
              <div
                className="product-art"
                style={{
                  background: p.color + "45",
                }}
              >
                <span className="tag">{p.tag}</span>
                {p.category === "coffee" ? (
                  <div
                    className="mini-cup"
                    style={{
                      background: p.color,
                    }}
                  >
                    <div className="mini-lid" />
                    <span>
                      ak
                      <br />
                      kapan
                    </span>
                    <small>✿</small>
                  </div>
                ) : (
                  <div className="dessert-art">{p.emoji}</div>
                )}
                <span className="art-doodle">{p.emoji}</span>
              </div>
              <div className="product-info">
                <h3>{p.name}</h3>
                <p>{p.description}</p>
                <div>
                  <strong>{money(p.price)}</strong>
                  <button
                    aria-label={`${p.name} seç`}
                    onClick={() => {
                      setSelected(p);
                      setCustom(false);
                    }}
                  >
                    +
                  </button>
                </div>
              </div>
            </article>
          ))}
      </div>
      {!products.length && (
        <p className="empty">
          Menü yükleniyor. Backend’in çalıştığından emin olun.
        </p>
      )}
    </section>
  );
}
