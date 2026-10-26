import type { Line } from "./types";
export function addCartLine(lines: Line[], incoming: Line): Line[] {
  const index = lines.findIndex(
    (line) =>
      line.product.id === incoming.product.id &&
      line.customCup === incoming.customCup &&
      line.cupText === incoming.cupText &&
      line.cupColor === incoming.cupColor
  );
  return index < 0
    ? [...lines, incoming]
    : lines.map((line, current) =>
        current === index
          ? {
              ...line,
              quantity: Math.min(20, line.quantity + 1),
            }
          : line
      );
}
export function calculateCartTotals(lines: Line[], studentApproved: boolean) {
  const subtotal = lines.reduce(
    (sum, line) => sum + line.product.price * line.quantity,
    0
  );
  const cupFee = lines.reduce(
    (sum, line) => sum + (line.customCup ? 45 : 0) * line.quantity,
    0
  );
  const count = lines.reduce((sum, line) => sum + line.quantity, 0);
  return {
    subtotal,
    cupFee,
    count,
    discount: studentApproved ? subtotal * 0.1 : 0,
  };
}
