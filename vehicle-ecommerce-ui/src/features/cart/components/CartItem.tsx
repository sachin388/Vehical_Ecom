import type { CartItem as CartItemModel } from "../types/Cart";
import { formatCurrency } from "../../../utils/currency";

interface Props {
  item: CartItemModel;
  onRemove: (itemId: number) => void;
}

export default function CartItem({ item, onRemove }: Props) {
  return (
    <article>
      <h2>{item.name}</h2>
      <p>Quantity: {item.quantity}</p>
      <p>
        Unit Price: {formatCurrency(item.unitPrice, item.currency)}
      </p>
      <button type="button" onClick={() => onRemove(item.itemId)}>
        Remove
      </button>
    </article>
  );
}
