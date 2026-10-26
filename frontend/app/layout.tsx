import "./globals.css";
export const metadata = {
  title: "Ak Kapan Coffee — Rengini bul, kahveni seç",
  description: "Renkli kahveler, taze tatlılar ve sana özel bardaklar.",
};
export default function Layout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="tr">
      <body>{children}</body>
    </html>
  );
}
