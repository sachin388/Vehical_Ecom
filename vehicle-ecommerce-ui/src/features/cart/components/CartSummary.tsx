import type { CartItem } from "../types/Cart";
import { formatCurrency } from "../../../utils/currency";

interface Props {
  items: CartItem[];
}

export default function CartSummary({ items }: Props) {
  const total = items.reduce(
    (sum, item) => sum + item.unitPrice * item.quantity,
    0
  );

  return (
    <aside>
      <h2>Summary</h2>
      <strong>{formatCurrency(total, items[0]?.currency ?? "INR")}</strong>
    </aside>
  );
}
