"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import type { Product, Line, ApiResponses } from "../lib/types";
import { money } from "../lib/money";
import { request, ApiError } from "../lib/api";
import { addCartLine, calculateCartTotals } from "../lib/cart";
export function useCoffeeShop() {
  const router = useRouter();
  const [user, setUser] = useState<{
    name: string;
    email: string;
  } | null>(null);
  const [products, setProducts] = useState<Product[]>([]);
  const [category, setCategory] = useState("coffee");
  const [concept, setConcept] = useState("candy");
  const [cart, setCart] = useState<Line[]>([]);
  const [open, setOpen] = useState(false);
  const [selected, setSelected] = useState<Product | null>(null);
  const [custom, setCustom] = useState(false);
  const [text, setText] = useState("Bir yudum mutluluk");
  const [color, setColor] = useState("pink");
  const [student, setStudent] = useState("none");
  const [notice, setNotice] = useState("");
  const [busy, setBusy] = useState(false);
  const [customer, setCustomer] = useState("");
  const [loaded, setLoaded] = useState(false);
  async function api<Path extends keyof ApiResponses>(
    path: Path,
    options?: RequestInit
  ): Promise<ApiResponses[Path]> {
    try {
      return await request<ApiResponses[Path]>(path, options);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401)
        router.replace("/login");
      throw error;
    }
  }
  useEffect(() => {
    api("auth/me")
      .then((d) => {
        setUser(d);
        setCustomer(d.name);
      })
      .catch(() => {});
    try {
      const saved = JSON.parse(localStorage.getItem("coffee-cart") || "[]");
      if (Array.isArray(saved)) setCart(saved);
      const theme = localStorage.getItem("coffee-concept");
      if (theme && ["candy", "forest", "sunset"].includes(theme))
        setConcept(theme);
    } catch {}
    setLoaded(true);
    api("menu")
      .then(setProducts)
      .catch((e) => setNotice(e.message));
    api("student")
      .then((d) => setStudent(d.status))
      .catch(() => {});
  }, []);
  useEffect(() => {
    if (loaded) {
      localStorage.setItem("coffee-cart", JSON.stringify(cart));
      localStorage.setItem("coffee-concept", concept);
    }
  }, [cart, concept, loaded]);
  useEffect(() => {
    if (!open && !selected) return;
    const handler = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        setOpen(false);
        setSelected(null);
      }
    };
    document.addEventListener("keydown", handler);
    return () => document.removeEventListener("keydown", handler);
  }, [open, selected]);
  const { subtotal, cupFee, discount, count } = calculateCartTotals(
    cart,
    student === "approved"
  );
  function add() {
    if (!selected) return;
    const line = {
      product: selected,
      quantity: 1,
      customCup: custom,
      cupText: custom ? text : "",
      cupColor: color,
    };
    setCart((lines) => addCartLine(lines, line));
    setSelected(null);
    setNotice("Biraz mutluluk sepetine eklendi.");
  }
  async function upload(file?: File) {
    if (!file) return;
    if (file.size > 5 * 1024 * 1024) {
      setNotice("Belge en fazla 5 MB olabilir.");
      return;
    }
    setBusy(true);
    try {
      const body = new FormData();
      body.append("document", file);
      const d = await api("student", {
        method: "POST",
        body,
      });
      setStudent(d.status);
      setNotice("Belgen alındı. Onaylandıktan sonra %10 indirim uygulanacak.");
    } catch (e) {
      setNotice((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  async function checkout() {
    setBusy(true);
    try {
      const d = await api("checkout", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          customer,
          concept,
          items: cart.map((l) => ({
            productId: l.product.id,
            quantity: l.quantity,
            customCup: l.customCup,
            cupText: l.cupText,
            cupColor: l.cupColor,
          })),
        }),
      });
      setCart([]);
      setOpen(false);
      setNotice(
        `${d.message} Sipariş #${d.id.slice(0, 8)} · ${money(d.total)}`
      );
    } catch (e) {
      setNotice((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  async function logout() {
    try {
      await api("auth/logout", {
        method: "POST",
      });
      localStorage.removeItem("coffee-cart");
      router.replace("/login");
    } catch (e) {
      setNotice((e as Error).message);
    }
  }
  async function refreshStudent() {
    try {
      const data = await api("student");
      setStudent(data.status);
    } catch (error) {
      setNotice((error as Error).message);
    }
  }
  return {
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
  };
}
