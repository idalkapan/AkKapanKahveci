import type { useCoffeeShop } from "../hooks/useCoffeeShop";
import { money } from "../lib/money";
type Props = Pick<
  ReturnType<typeof useCoffeeShop>,
  | "selected"
  | "setSelected"
  | "custom"
  | "setCustom"
  | "color"
  | "setColor"
  | "text"
  | "setText"
  | "add"
>;
export function ProductDialog({
  selected,
  setSelected,
  custom,
  setCustom,
  color,
  setColor,
  text,
  setText,
  add,
}: Props) {
  return (
    selected && (
      <div className="overlay" onClick={() => setSelected(null)}>
        <section
          className="modal"
          role="dialog"
          aria-modal="true"
          aria-label="İçeceğini kişiselleştir"
          onClick={(e) => e.stopPropagation()}
        >
          <button
            className="close"
            aria-label="Kapat"
            onClick={() => setSelected(null)}
          >
            ×
          </button>
          <span className="eyebrow">TAM SENİN ZEVKİNE GÖRE</span>
          <h2>{selected.name}</h2>
          <p>{selected.description}</p>
          {selected.category === "coffee" && (
            <>
              <label className="check">
                <input
                  type="checkbox"
                  checked={custom}
                  onChange={(e) => setCustom(e.target.checked)}
                />{" "}
                Bana özel tasarım bardak (+45 TL)
              </label>
              {custom && (
                <>
                  <div className={`cup-preview ${color}`}>
                    {text || "Senin hikâyen"}
                    <span>✿</span>
                  </div>
                  <label>
                    Bardağında ne yazsın?
                    <input
                      autoFocus
                      maxLength={24}
                      value={text}
                      onChange={(e) => setText(e.target.value)}
                      placeholder="İsmin veya kısa bir söz"
                    />
                  </label>
                  <div className="color-picker">
                    {[
                      ["pink", "Pembe"],
                      ["green", "Yeşil"],
                      ["purple", "Mor"],
                    ].map(([c, n]) => (
                      <button
                        key={c}
                        className={color === c ? "active" : ""}
                        onClick={() => setColor(c)}
                      >
                        {n}
                      </button>
                    ))}
                  </div>
                </>
              )}
            </>
          )}
          <button className="primary wide" onClick={add}>
            Sepete ekle · {money(selected.price + (custom ? 45 : 0))}
          </button>
        </section>
      </div>
    )
  );
}
